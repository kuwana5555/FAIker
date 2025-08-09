using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// TriviaManagerをベースにした推理ゲーム管理システム
/// 既存のUI構造とネットワーク同期を活用
/// </summary>
public class DeductionGameManager : NetworkBehaviour, IStateAuthorityChanged
{
    [Header("Game Data")]
    [Tooltip("推理ゲーム用のお題データ")]
    public DeductionTopicSet deductionTopics;

    [Header("UI Elements - 既存のTriviaManagerと同じ構造")]
    [Tooltip("Container for the game elements")]
    public GameObject questionElements = null;

    #region Networked Properties - TriviaManagerと同じ構造を活用
    [Networked, Tooltip("Timer used for game phases and transitions.")]
    public TickTimer timer { get; set; }

    [Networked, Tooltip("The length of the timer, used to help get a percentage when rendering timers.")]
    public float timerLength { get; set; }

    [Tooltip("The current round number.")]
    [Networked, OnChangedRender(nameof(UpdateCurrentRound))]
    public int CurrentRound { get; set; } = 0;

    [Tooltip("The current state of the deduction game.")]
    [Networked, OnChangedRender(nameof(OnDeductionGameStateChanged))]
    public DeductionGameState GameState { get; set; } = DeductionGameState.Intro;

    [Tooltip("Index of the parent player for this round.")]
    [Networked, OnChangedRender(nameof(UpdateParentPlayer))]
    public int ParentPlayerIndex { get; set; } = -1;

    [Tooltip("Current topic for this round.")]
    [Networked, OnChangedRender(nameof(UpdateCurrentTopic))]
    public NetworkString<_64> CurrentTopic { get; set; }

    [Tooltip("First character for this round.")]
    [Networked, OnChangedRender(nameof(UpdateFirstCharacter))]
    public NetworkString<_16> CurrentFirstCharacter { get; set; }

    [Tooltip("AI's answer (only visible to parent player).")]
    [Networked]
    public NetworkString<_64> AIAnswer { get; set; }

    // プレイヤーの回答を格納（TriviaManagerのrandomizedQuestionListの代わり）
    [Networked, Capacity(20)]
    public NetworkArray<NetworkString<_64>> PlayerAnswers => default;

    // プレイヤーの投票を格納
    [Networked, Capacity(20)]
    public NetworkArray<int> PlayerVotes => default;

    #endregion

    #region UI Elements - TriviaManagerの構造を再利用
    
    /// <summary>
    /// お題、回答入力、投票関連のUI
    /// </summary>
    public TextMeshProUGUI question; // お題表示用に再利用
    public TextMeshProUGUI[] answers; // 投票時の回答表示用に再利用
    public Image[] answerHighlights; // 投票結果表示用に再利用

    /// <summary>
    /// タイマー表示（TriviaManagerと同じ）
    /// </summary>
    public Image timerVisual;
    
    [Tooltip("Gradient used to color the timer based on percentage.")]
    public Gradient timerVisualGradient;

    /// <summary>
    /// ゲーム進行表示
    /// </summary>
    public TextMeshProUGUI questionIndicatorText; // "ラウンド X / 5" 表示用に再利用
    public TextMeshProUGUI triviaMessage; // ゲーム状態メッセージ用に再利用

    [Tooltip("Button displayed to leave the game after a round ends.")]
    public GameObject leaveGameBtn;

    [Tooltip("Button displayed, only to the master client, to start a new game.")]
    public GameObject startNewGameBtn;

    [Tooltip("MonoBehaviour that displays winner at the end of a game.")]
    public TriviaEndGame endGameObject; // 結果表示用に再利用

    #endregion

    #region 推理ゲーム専用UI
    
    [Header("Deduction Game Specific UI")]
    [Tooltip("回答入力フィールド")]
    public TMP_InputField answerInputField;
    
    [Tooltip("回答送信ボタン")]
    public Button submitAnswerButton;
    
    [Tooltip("最初の文字表示用テキスト")]
    public TextMeshProUGUI firstCharacterText;
    
    [Tooltip("親プレイヤー用のAI回答表示エリア")]
    public GameObject aiAnswerDisplayArea;
    
    [Tooltip("AI回答表示用テキスト")]
    public TextMeshProUGUI aiAnswerText;

    [Tooltip("投票フェーズのUI")]
    public GameObject votingUI;
    
    [Tooltip("投票ボタンのプレハブ")]
    public Button voteButtonPrefab;
    
    [Tooltip("投票ボタンの親オブジェクト")]
    public Transform voteButtonContainer;

    #endregion

    [Header("Game Rules - TriviaManagerと同じ構造")]
    [Tooltip("The maximum number of rounds to play.")]
    [Min(1)]
    public int maxRounds = 5;

    [Tooltip("The amount of time for answer phase.")]
    public float answerTime = 60f;

    [Tooltip("The amount of time for voting phase.")]
    public float votingTime = 30f;

    #region SFX - TriviaManagerと同じ
    [Header("SFX Audio Sources")]
    [SerializeField, Tooltip("AudioSource played when the local player submits answer.")]
    private AudioSource _confirmSFX;

    [SerializeField, Tooltip("AudioSource played when there's an error.")]
    private AudioSource _errorSFX;

    [SerializeField, Tooltip("AudioSource played when the local player gets correct result.")]
    private AudioSource _correctSFX;

    [SerializeField, Tooltip("AudioSource played when the local player gets incorrect result.")]
    private AudioSource _incorrectSFX;
    #endregion

    /// <summary>
    /// 推理ゲーム管理システムが存在するかどうか
    /// </summary>
    public static bool DeductionManagerPresent { get; private set; } = false;

    /// <summary>
    /// 推理ゲームの状態
    /// </summary>
    public enum DeductionGameState : byte
    {
        Intro = 0,
        AnswerPhase = 1,
        VotingPhase = 2,
        Results = 3,
        GameOver = 4,
        NewRound = 5,
    }

    private List<Button> voteButtons = new List<Button>();

    public override void Spawned()
    {
        // TriviaManagerと同じ初期化パターン
        if (CurrentRound == 0)
            questionIndicatorText.text = "";
        else
            questionIndicatorText.text = "Round: " + CurrentRound + " / " + maxRounds;

        // プレイヤーの参加を制限（TriviaManagerと同じ）
        if (Runner.IsSharedModeMasterClient)
        {
            Runner.SessionInfo.IsOpen = false;
            Runner.SessionInfo.IsVisible = false;
        }

        // 権限を持つクライアントが初期設定を行う
        if (HasStateAuthority)
        {
            timerLength = 3f;
            timer = TickTimer.CreateFromSeconds(Runner, timerLength);
            CurrentRound = 0;
            SelectParentPlayer();
        }

        DeductionManagerPresent = true;

        FusionConnector.Instance?.SetPregameMessage(string.Empty);

        // UI初期化
        InitializeUI();

        // 状態更新
        OnDeductionGameStateChanged();
        UpdateCurrentRound();
        UpdateCurrentTopic();
        UpdateFirstCharacter();
        UpdateParentPlayer();

        Debug.Log("DeductionGameManager spawned");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        DeductionManagerPresent = false;
    }

    private void InitializeUI()
    {
        // 初期状態の設定
        if (votingUI != null) votingUI.SetActive(false);
        if (aiAnswerDisplayArea != null) aiAnswerDisplayArea.SetActive(false);
        if (answerInputField != null) answerInputField.gameObject.SetActive(false);
        if (submitAnswerButton != null) 
        {
            submitAnswerButton.gameObject.SetActive(false);
            submitAnswerButton.onClick.AddListener(SubmitAnswer);
        }

        // 既存のUI要素を非表示
        if (questionElements != null) questionElements.SetActive(false);
    }

    private void SelectParentPlayer()
    {
        if (!HasStateAuthority) return;
        
        var players = TriviaPlayer.TriviaPlayerRefs; // 既存のプレイヤーシステムを活用
        if (players.Count > 0)
        {
            ParentPlayerIndex = Random.Range(0, players.Count);
            Debug.Log($"Parent player selected: {ParentPlayerIndex}");
        }
    }

    /// <summary>
    /// TriviaManagerのFixedUpdateNetworkと同じパターンでゲーム進行を管理
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (timer.Expired(Runner))
        {
            switch (GameState)
            {
                case DeductionGameState.Intro:
                    StartNewRound();
                    break;
                    
                case DeductionGameState.AnswerPhase:
                    // 回答フェーズ終了 → 投票フェーズ
                    GameState = DeductionGameState.VotingPhase;
                    timerLength = votingTime;
                    timer = TickTimer.CreateFromSeconds(Runner, timerLength);
                    break;
                    
                case DeductionGameState.VotingPhase:
                    // 投票フェーズ終了 → 結果表示
                    CalculateResults();
                    GameState = DeductionGameState.Results;
                    timerLength = 5f;
                    timer = TickTimer.CreateFromSeconds(Runner, timerLength);
                    break;
                    
                case DeductionGameState.Results:
                    // 結果表示終了
                    if (CurrentRound >= maxRounds)
                    {
                        GameState = DeductionGameState.GameOver;
                    }
                    else
                    {
                        // 次のラウンドの親プレイヤーを選択
                        var players = TriviaPlayer.TriviaPlayerRefs;
                        if (players.Count > 0)
                        {
                            ParentPlayerIndex = (ParentPlayerIndex + 1) % players.Count;
                        }
                        StartNewRound();
                    }
                    break;
            }
        }
    }

    private void StartNewRound()
    {
        if (!HasStateAuthority) return;

        CurrentRound++;
        
        // 新しいお題と文字を選択
        if (deductionTopics != null)
        {
            var topic = deductionTopics.GetRandomTopic();
            if (topic != null)
            {
                CurrentTopic = topic.topicText;
                CurrentFirstCharacter = deductionTopics.GetRandomFirstCharacter(topic);
                
                // AI回答を生成
                AIAnswer = AIPlayerSystem.GenerateAIAnswer(topic.topicText, CurrentFirstCharacter.Value);
            }
        }

        // プレイヤーデータをリセット
        ClearPlayerData();

        // 回答フェーズ開始
        GameState = DeductionGameState.AnswerPhase;
        timerLength = answerTime;
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
    }

    private void ClearPlayerData()
    {
        if (!HasStateAuthority) return;
        
        for (int i = 0; i < PlayerAnswers.Length; i++)
        {
            PlayerAnswers.Set(i, "");
            PlayerVotes.Set(i, -1);
        }

        // プレイヤーの状態もリセット
        var players = TriviaPlayer.TriviaPlayerRefs;
        foreach (var player in players)
        {
            player.ChosenAnswer = -1; // 回答リセット用に再利用
        }
    }

    /// <summary>
    /// TriviaManagerのUpdate()と同じパターンでタイマー表示を更新
    /// </summary>
    public void Update()
    {
        // タイマー表示の更新（TriviaManagerと同じ）
        if (timerVisual != null)
        {
            float? remainingTime = timer.RemainingTime(Runner);
            if (remainingTime.HasValue)
            {
                float percent = remainingTime.Value / timerLength;
                timerVisual.fillAmount = percent;
                if (timerVisualGradient != null)
                {
                    timerVisual.color = timerVisualGradient.Evaluate(percent);
                }
            }
            else
            {
                timerVisual.fillAmount = 0f;
            }
        }
    }

    public void SubmitAnswer()
    {
        if (GameState != DeductionGameState.AnswerPhase) return;
        if (answerInputField == null) return;
        
        string answer = answerInputField.text.Trim();
        if (string.IsNullOrEmpty(answer)) return;
        
        // 最初の文字チェック
        if (!answer.StartsWith(CurrentFirstCharacter.Value))
        {
            if (_errorSFX != null) _errorSFX.Play();
            Debug.LogWarning($"Answer must start with: {CurrentFirstCharacter.Value}");
            return;
        }
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        if (localPlayer != null)
        {
            // 既存のTriviaPlayerシステムを活用
            localPlayer.ChosenAnswer = 1; // 回答済みマークとして使用
            
            // 回答をネットワーク配列に保存
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            if (playerIndex >= 0 && HasInputAuthority)
            {
                RPC_SubmitAnswer(playerIndex, answer);
            }
            
            if (_confirmSFX != null) _confirmSFX.Play();
            
            // UI更新
            answerInputField.text = "";
            answerInputField.gameObject.SetActive(false);
            submitAnswerButton.gameObject.SetActive(false);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitAnswer(int playerIndex, string answer)
    {
        if (playerIndex >= 0 && playerIndex < PlayerAnswers.Length)
        {
            PlayerAnswers.Set(playerIndex, answer);
            Debug.Log($"Answer received from player {playerIndex}: {answer}");
        }
    }

    public void SubmitVote(int targetPlayerIndex)
    {
        if (GameState != DeductionGameState.VotingPhase) return;
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        if (localPlayer != null)
        {
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            if (playerIndex >= 0 && HasInputAuthority)
            {
                RPC_SubmitVote(playerIndex, targetPlayerIndex);
            }
        }
        
        // 投票ボタンを無効化
        DisableVoteButtons();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitVote(int playerIndex, int targetPlayerIndex)
    {
        if (playerIndex >= 0 && playerIndex < PlayerVotes.Length)
        {
            PlayerVotes.Set(playerIndex, targetPlayerIndex);
            Debug.Log($"Vote received from player {playerIndex} for player {targetPlayerIndex}");
        }
    }

    private void DisableVoteButtons()
    {
        foreach (var button in voteButtons)
        {
            if (button != null)
                button.interactable = false;
        }
    }

    private void CalculateResults()
    {
        if (!HasStateAuthority) return;
        
        var players = TriviaPlayer.TriviaPlayerRefs;
        var voteCounts = new Dictionary<int, int>();
        
        // 投票を集計
        for (int i = 0; i < players.Count; i++)
        {
            int vote = PlayerVotes[i];
            if (vote >= 0 && vote < players.Count)
            {
                if (!voteCounts.ContainsKey(vote))
                    voteCounts[vote] = 0;
                voteCounts[vote]++;
            }
        }
        
        // スコア計算（TriviaPlayerのScoreシステムを活用）
        CalculateScores(voteCounts);
    }

    private void CalculateScores(Dictionary<int, int> voteCounts)
    {
        var players = TriviaPlayer.TriviaPlayerRefs;
        int parentIndex = ParentPlayerIndex;
        
        if (voteCounts.Count == 0) return;
        
        int maxVotes = voteCounts.Values.Max();
        int mostVotedPlayer = voteCounts.FirstOrDefault(x => x.Value == maxVotes).Key;
        
        // スコア付与
        if (mostVotedPlayer == parentIndex && parentIndex < players.Count)
        {
            // 親プレイヤーが最多票 → 3点
            players[parentIndex].Score += 3;
        }
        else if (mostVotedPlayer < players.Count)
        {
            // 子プレイヤーが最多票 → 2点
            players[mostVotedPlayer].Score += 2;
        }
    }

    #region UI Update Methods - TriviaManagerのパターンを踏襲

    private void OnDeductionGameStateChanged()
    {
        switch (GameState)
        {
            case DeductionGameState.Intro:
                triviaMessage.text = "推理ゲーム開始\nまもなく最初のラウンドが始まります";
                if (questionElements != null) questionElements.SetActive(false);
                if (votingUI != null) votingUI.SetActive(false);
                if (endGameObject != null) endGameObject.Hide();
                break;
                
            case DeductionGameState.AnswerPhase:
                ShowAnswerPhase();
                break;
                
            case DeductionGameState.VotingPhase:
                ShowVotingPhase();
                break;
                
            case DeductionGameState.Results:
                ShowResults();
                break;
                
            case DeductionGameState.GameOver:
                ShowGameOver();
                break;
        }
    }

    private void ShowAnswerPhase()
    {
        triviaMessage.text = "回答フェーズ";
        
        if (questionElements != null) questionElements.SetActive(true);
        if (votingUI != null) votingUI.SetActive(false);
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        bool isParent = localPlayer != null && 
                       TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer) == ParentPlayerIndex;
        
        if (isParent)
        {
            // 親プレイヤーにAI回答を表示
            if (aiAnswerDisplayArea != null) aiAnswerDisplayArea.SetActive(true);
            if (aiAnswerText != null) aiAnswerText.text = $"AI回答: {AIAnswer.Value}\n（この回答に似せて答えてください）";
        }
        else
        {
            if (aiAnswerDisplayArea != null) aiAnswerDisplayArea.SetActive(false);
        }
        
        // 回答入力UIを表示
        if (answerInputField != null) 
        {
            answerInputField.gameObject.SetActive(true);
            answerInputField.text = "";
        }
        if (submitAnswerButton != null) submitAnswerButton.gameObject.SetActive(true);
    }

    private void ShowVotingPhase()
    {
        triviaMessage.text = "投票フェーズ - AIの回答だと思うものを選んでください";
        
        if (answerInputField != null) answerInputField.gameObject.SetActive(false);
        if (submitAnswerButton != null) submitAnswerButton.gameObject.SetActive(false);
        if (aiAnswerDisplayArea != null) aiAnswerDisplayArea.SetActive(false);
        if (votingUI != null) votingUI.SetActive(true);
        
        CreateVoteButtons();
    }

    private void CreateVoteButtons()
    {
        if (voteButtonPrefab == null || voteButtonContainer == null) return;
        
        // 既存のボタンをクリア
        foreach (var button in voteButtons)
        {
            if (button != null)
                DestroyImmediate(button.gameObject);
        }
        voteButtons.Clear();
        
        var players = TriviaPlayer.TriviaPlayerRefs;
        var localPlayer = TriviaPlayer.LocalPlayer;
        int localPlayerIndex = players.IndexOf(localPlayer);
        
        for (int i = 0; i < players.Count; i++)
        {
            if (i == localPlayerIndex) continue; // 自分には投票できない
            
            var button = Instantiate(voteButtonPrefab, voteButtonContainer);
            var playerName = players[i].PlayerName.Value;
            var playerAnswer = PlayerAnswers[i].Value;
            
            button.GetComponentInChildren<TextMeshProUGUI>().text = $"{playerName}: {playerAnswer}";
            
            int playerIndex = i; // クロージャ用
            button.onClick.AddListener(() => SubmitVote(playerIndex));
            
            voteButtons.Add(button);
        }
    }

    private void ShowResults()
    {
        triviaMessage.text = "結果発表";
        
        if (votingUI != null) votingUI.SetActive(false);
        if (questionElements != null) questionElements.SetActive(true);
        
        // 結果表示の詳細をTriviaManagerのパターンで実装
        DisplayRoundResults();
    }

    private void DisplayRoundResults()
    {
        var players = TriviaPlayer.TriviaPlayerRefs;
        string results = $"ラウンド {CurrentRound} 結果\n\n";
        results += $"お題: {CurrentTopic.Value}\n";
        results += $"最初の文字: {CurrentFirstCharacter.Value}\n";
        results += $"AI回答: {AIAnswer.Value}\n";
        results += $"親プレイヤー: {(ParentPlayerIndex < players.Count ? players[ParentPlayerIndex].PlayerName.Value : "不明")}\n\n";
        
        results += "全プレイヤーの回答:\n";
        for (int i = 0; i < players.Count; i++)
        {
            string answer = PlayerAnswers[i].Value;
            if (!string.IsNullOrEmpty(answer))
            {
                results += $"{players[i].PlayerName.Value}: {answer}\n";
            }
        }
        
        triviaMessage.text = results;
    }

    private void ShowGameOver()
    {
        triviaMessage.text = "ゲーム終了！";
        
        if (questionElements != null) questionElements.SetActive(false);
        if (votingUI != null) votingUI.SetActive(false);
        
        // TriviaManagerのendGameObjectシステムを活用
        if (endGameObject != null)
        {
            var players = TriviaPlayer.TriviaPlayerRefs;
            var sortedPlayers = players.OrderByDescending(p => p.Score).ToList();
            var winners = sortedPlayers.Take(3).ToList();
            endGameObject.Show(winners);
        }
        
        // ゲーム制御ボタンを表示
        if (leaveGameBtn != null) leaveGameBtn.SetActive(true);
        if (startNewGameBtn != null) startNewGameBtn.SetActive(Runner.IsSharedModeMasterClient);
    }

    private void UpdateCurrentRound()
    {
        if (questionIndicatorText != null)
        {
            if (CurrentRound == 0)
                questionIndicatorText.text = "";
            else
                questionIndicatorText.text = "Round: " + CurrentRound + " / " + maxRounds;
        }
    }

    private void UpdateCurrentTopic()
    {
        if (question != null)
        {
            question.text = $"お題: {CurrentTopic.Value}";
        }
    }

    private void UpdateFirstCharacter()
    {
        if (firstCharacterText != null)
        {
            firstCharacterText.text = $"最初の文字: 「{CurrentFirstCharacter.Value}」";
        }
    }

    private void UpdateParentPlayer()
    {
        // プレイヤーの表示更新は各プレイヤー側で処理
        Debug.Log($"Parent player updated: {ParentPlayerIndex}");
    }

    #endregion

    /// <summary>
    /// TriviaManagerと同じパターンでゲーム終了処理
    /// </summary>
    public async void LeaveGame()
    {
        await Runner.Shutdown(true, ShutdownReason.Ok);

        FusionConnector fc = GameObject.FindObjectOfType<FusionConnector>();
        if (fc)
        {
            fc.mainMenuObject.SetActive(true);
            fc.mainGameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 新しいゲーム開始
    /// </summary>
    public void StartNewGame()
    {
        if (HasStateAuthority == false)
            return;

        GameState = DeductionGameState.NewRound;
        CurrentRound = 0;

        // プレイヤーのスコアをリセット
        var players = TriviaPlayer.TriviaPlayerRefs;
        foreach (var player in players)
        {
            player.Score = 0;
        }

        // 新しい親プレイヤーを選択
        SelectParentPlayer();

        // 初期タイマー設定
        timerLength = 3f;
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
    }

    public void StateAuthorityChanged()
    {
        if (GameState == DeductionGameState.GameOver)
        {
            if (startNewGameBtn != null)
                startNewGameBtn.SetActive(Runner.IsSharedModeMasterClient);
        }
    }
} 