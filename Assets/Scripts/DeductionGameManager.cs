using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 推理ゲームのメイン管理システム
/// </summary>
public class DeductionGameManager : NetworkBehaviour, IStateAuthorityChanged
{
    [Header("Game Data")]
    [Tooltip("推理ゲーム用のお題データ")]
    public DeductionTopicSet topicSet;

    [Header("UI Elements")]
    [Tooltip("ゲーム画面のコンテナ")]
    public GameObject gameContainer;
    
    [Tooltip("お題表示用テキスト")]
    public TextMeshProUGUI topicText;
    
    [Tooltip("最初の文字表示用テキスト")]
    public TextMeshProUGUI firstCharacterText;
    
    [Tooltip("現在のラウンド表示用テキスト")]
    public TextMeshProUGUI roundText;
    
    [Tooltip("ゲーム状態メッセージ用テキスト")]
    public TextMeshProUGUI gameStateText;
    
    [Tooltip("回答入力フィールド")]
    public TMP_InputField answerInputField;
    
    [Tooltip("回答送信ボタン")]
    public Button submitAnswerButton;
    
    [Tooltip("投票フェーズのUI")]
    public GameObject votingUI;
    
    [Tooltip("投票ボタンのプレハブ")]
    public Button voteButtonPrefab;
    
    [Tooltip("投票ボタンの親オブジェクト")]
    public Transform voteButtonContainer;
    
    [Tooltip("結果表示UI")]
    public GameObject resultsUI;
    
    [Tooltip("結果表示用テキスト")]
    public TextMeshProUGUI resultsText;
    
    [Tooltip("スコア表示用テキスト")]
    public TextMeshProUGUI scoresText;
    
    [Tooltip("次のラウンドボタン")]
    public Button nextRoundButton;
    
    [Tooltip("ゲーム終了ボタン")]
    public Button endGameButton;

    [Header("Game Settings")]
    [Tooltip("最大ラウンド数")]
    public int maxRounds = 5;
    
    [Tooltip("回答時間（秒）")]
    public float answerTime = 60f;
    
    [Tooltip("投票時間（秒）")]
    public float votingTime = 30f;

    #region Networked Properties
    
    [Networked, OnChangedRender(nameof(OnGameStateChanged))]
    public DeductionGameState GameState { get; set; } = DeductionGameState.WaitingForPlayers;
    
    [Networked, OnChangedRender(nameof(OnCurrentRoundChanged))]
    public int CurrentRound { get; set; } = 0;
    
    [Networked]
    public int ParentPlayerIndex { get; set; } = -1;
    
    [Networked, OnChangedRender(nameof(OnTopicChanged))]
    public NetworkString<_64> CurrentTopic { get; set; }
    
    [Networked, OnChangedRender(nameof(OnFirstCharacterChanged))]
    public NetworkString<_16> CurrentFirstCharacter { get; set; }
    
    [Networked]
    public NetworkString<_64> AIAnswer { get; set; }
    
    [Networked]
    public TickTimer gameTimer { get; set; }
    
    [Networked]
    public float timerLength { get; set; }
    
    // プレイヤーの回答を格納する配列
    [Networked, Capacity(20)]
    public NetworkArray<NetworkString<_64>> PlayerAnswers => default;
    
    // プレイヤーの投票を格納する配列
    [Networked, Capacity(20)]
    public NetworkArray<int> PlayerVotes => default;

    #endregion

    public enum DeductionGameState : byte
    {
        WaitingForPlayers = 0,
        RoundStart = 1,
        AnswerPhase = 2,
        VotingPhase = 3,
        Results = 4,
        GameEnd = 5
    }

    /// <summary>
    /// ゲームマネージャーが存在するかどうか
    /// </summary>
    public static bool DeductionManagerPresent { get; private set; } = false;

    private List<Button> voteButtons = new List<Button>();
    private Dictionary<int, int> roundScores = new Dictionary<int, int>();

    public override void Spawned()
    {
        DeductionManagerPresent = true;
        
        if (HasStateAuthority)
        {
            GameState = DeductionGameState.WaitingForPlayers;
            CurrentRound = 0;
        }
        
        // UI初期化
        InitializeUI();
        
        // 状態更新
        OnGameStateChanged();
        OnCurrentRoundChanged();
        OnTopicChanged();
        OnFirstCharacterChanged();
        
        Debug.Log("DeductionGameManager spawned");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        DeductionManagerPresent = false;
    }

    private void InitializeUI()
    {
        // ボタンイベントの設定
        submitAnswerButton.onClick.AddListener(SubmitAnswer);
        nextRoundButton.onClick.AddListener(StartNextRound);
        endGameButton.onClick.AddListener(EndGame);
        
        // 初期状態の設定
        votingUI.SetActive(false);
        resultsUI.SetActive(false);
        answerInputField.gameObject.SetActive(false);
        submitAnswerButton.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        if (!HasStateAuthority) return;
        
        // 親プレイヤーをランダム選択
        SelectParentPlayer();
        
        // 最初のラウンド開始
        StartNewRound();
    }

    private void SelectParentPlayer()
    {
        if (!HasStateAuthority) return;
        
        var players = DeductionPlayer.DeductionPlayerRefs;
        if (players.Count > 0)
        {
            ParentPlayerIndex = Random.Range(0, players.Count);
            Debug.Log($"Parent player selected: {ParentPlayerIndex}");
        }
    }

    private void StartNewRound()
    {
        if (!HasStateAuthority) return;
        
        CurrentRound++;
        
        // 新しいお題と文字を選択
        var topic = topicSet.GetRandomTopic();
        if (topic != null)
        {
            CurrentTopic = topic.topicText;
            CurrentFirstCharacter = topicSet.GetRandomFirstCharacter(topic);
            
            // AI回答を生成
            AIAnswer = AIPlayerSystem.GenerateAIAnswer(topic.topicText, CurrentFirstCharacter.Value);
        }
        
        // プレイヤーの回答と投票をリセット
        ClearPlayerData();
        
        // 回答フェーズ開始
        GameState = DeductionGameState.RoundStart;
        SetTimer(3f); // 3秒後に回答フェーズへ
    }

    private void ClearPlayerData()
    {
        if (!HasStateAuthority) return;
        
        for (int i = 0; i < PlayerAnswers.Length; i++)
        {
            PlayerAnswers.Set(i, "");
            PlayerVotes.Set(i, -1);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        
        if (gameTimer.Expired(Runner))
        {
            switch (GameState)
            {
                case DeductionGameState.RoundStart:
                    GameState = DeductionGameState.AnswerPhase;
                    SetTimer(answerTime);
                    break;
                    
                case DeductionGameState.AnswerPhase:
                    GameState = DeductionGameState.VotingPhase;
                    SetTimer(votingTime);
                    break;
                    
                case DeductionGameState.VotingPhase:
                    CalculateResults();
                    GameState = DeductionGameState.Results;
                    SetTimer(10f); // 結果表示時間
                    break;
                    
                case DeductionGameState.Results:
                    if (CurrentRound >= maxRounds)
                    {
                        GameState = DeductionGameState.GameEnd;
                    }
                    else
                    {
                        StartNewRound();
                    }
                    break;
            }
        }
    }

    private void SetTimer(float seconds)
    {
        timerLength = seconds;
        gameTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    public void SubmitAnswer()
    {
        if (GameState != DeductionGameState.AnswerPhase) return;
        
        string answer = answerInputField.text.Trim();
        if (string.IsNullOrEmpty(answer)) return;
        
        // 最初の文字チェック
        if (!answer.StartsWith(CurrentFirstCharacter.Value))
        {
            Debug.LogWarning($"Answer must start with: {CurrentFirstCharacter.Value}");
            return;
        }
        
        var localPlayer = DeductionPlayer.LocalPlayer;
        if (localPlayer != null)
        {
            localPlayer.SubmitAnswer(answer);
            answerInputField.text = "";
            answerInputField.gameObject.SetActive(false);
            submitAnswerButton.gameObject.SetActive(false);
        }
    }

    public void SubmitVote(int targetPlayerIndex)
    {
        if (GameState != DeductionGameState.VotingPhase) return;
        
        var localPlayer = DeductionPlayer.LocalPlayer;
        if (localPlayer != null)
        {
            localPlayer.SubmitVote(targetPlayerIndex);
        }
        
        // 投票ボタンを無効化
        DisableVoteButtons();
    }

    private void DisableVoteButtons()
    {
        foreach (var button in voteButtons)
        {
            button.interactable = false;
        }
    }

    private void CalculateResults()
    {
        if (!HasStateAuthority) return;
        
        var players = DeductionPlayer.DeductionPlayerRefs;
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
        
        // スコア計算
        CalculateScores(voteCounts);
    }

    private void CalculateScores(Dictionary<int, int> voteCounts)
    {
        var players = DeductionPlayer.DeductionPlayerRefs;
        int parentIndex = ParentPlayerIndex;
        int maxVotes = voteCounts.Values.Count > 0 ? voteCounts.Values.Max() : 0;
        
        // 最も票を集めたプレイヤーを特定
        int mostVotedPlayer = -1;
        if (voteCounts.Count > 0)
        {
            mostVotedPlayer = voteCounts.FirstOrDefault(x => x.Value == maxVotes).Key;
        }
        
        // スコア付与ロジック
        if (mostVotedPlayer == parentIndex)
        {
            // 親プレイヤーが最多票 → 親プレイヤーに3点
            if (parentIndex < players.Count)
            {
                players[parentIndex].AddScore(3);
            }
        }
        else if (mostVotedPlayer >= 0)
        {
            // 子プレイヤーが最多票 → そのプレイヤーに2点
            if (mostVotedPlayer < players.Count)
            {
                players[mostVotedPlayer].AddScore(2);
            }
        }
        
        // AIの回答に投票したプレイヤーに2点
        // (この実装では簡略化のため、実際のAI回答との照合は省略)
        
        // 親プレイヤーボーナス: 誰もAIの回答に投票しなかった場合1点
        // (実装簡略化のため省略)
    }

    public void StartNextRound()
    {
        if (!HasStateAuthority) return;
        if (GameState != DeductionGameState.Results) return;
        
        // 次のラウンドの親プレイヤーを選択（ローテーション）
        var players = DeductionPlayer.DeductionPlayerRefs;
        if (players.Count > 0)
        {
            ParentPlayerIndex = (ParentPlayerIndex + 1) % players.Count;
        }
        
        StartNewRound();
    }

    public void EndGame()
    {
        if (!HasStateAuthority) return;
        
        GameState = DeductionGameState.GameEnd;
    }

    #region UI Update Methods
    
    private void OnGameStateChanged()
    {
        switch (GameState)
        {
            case DeductionGameState.WaitingForPlayers:
                gameStateText.text = "プレイヤー待機中...";
                break;
                
            case DeductionGameState.RoundStart:
                gameStateText.text = "ラウンド開始！";
                votingUI.SetActive(false);
                resultsUI.SetActive(false);
                break;
                
            case DeductionGameState.AnswerPhase:
                gameStateText.text = "回答フェーズ";
                ShowAnswerUI();
                break;
                
            case DeductionGameState.VotingPhase:
                gameStateText.text = "投票フェーズ";
                ShowVotingUI();
                break;
                
            case DeductionGameState.Results:
                gameStateText.text = "結果発表";
                ShowResultsUI();
                break;
                
            case DeductionGameState.GameEnd:
                gameStateText.text = "ゲーム終了";
                ShowFinalResults();
                break;
        }
    }
    
    private void OnCurrentRoundChanged()
    {
        roundText.text = $"ラウンド {CurrentRound} / {maxRounds}";
    }
    
    private void OnTopicChanged()
    {
        topicText.text = $"お題: {CurrentTopic.Value}";
    }
    
    private void OnFirstCharacterChanged()
    {
        firstCharacterText.text = $"最初の文字: 「{CurrentFirstCharacter.Value}」";
    }
    
    private void ShowAnswerUI()
    {
        var localPlayer = DeductionPlayer.LocalPlayer;
        bool isParent = localPlayer != null && DeductionPlayer.DeductionPlayerRefs.IndexOf(localPlayer) == ParentPlayerIndex;
        
        if (isParent)
        {
            // 親プレイヤーにはAI回答を表示
            gameStateText.text = $"AI回答: {AIAnswer.Value}\n（この回答に似せて答えてください）";
        }
        
        answerInputField.gameObject.SetActive(true);
        submitAnswerButton.gameObject.SetActive(true);
        answerInputField.placeholder.GetComponent<TextMeshProUGUI>().text = $"{CurrentFirstCharacter.Value}で始まる回答を入力...";
    }
    
    private void ShowVotingUI()
    {
        answerInputField.gameObject.SetActive(false);
        submitAnswerButton.gameObject.SetActive(false);
        votingUI.SetActive(true);
        
        CreateVoteButtons();
    }
    
    private void CreateVoteButtons()
    {
        // 既存のボタンをクリア
        foreach (var button in voteButtons)
        {
            if (button != null)
                DestroyImmediate(button.gameObject);
        }
        voteButtons.Clear();
        
        var players = DeductionPlayer.DeductionPlayerRefs;
        var localPlayer = DeductionPlayer.LocalPlayer;
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
    
    private void ShowResultsUI()
    {
        votingUI.SetActive(false);
        resultsUI.SetActive(true);
        
        // 結果テキストを更新
        UpdateResultsText();
    }
    
    private void UpdateResultsText()
    {
        var players = DeductionPlayer.DeductionPlayerRefs;
        string results = "ラウンド結果:\n\n";
        
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
        
        resultsText.text = results;
        
        // スコア表示を更新
        UpdateScoresText();
    }
    
    private void UpdateScoresText()
    {
        var players = DeductionPlayer.DeductionPlayerRefs;
        string scores = "現在のスコア:\n";
        
        // スコア順でソート
        var sortedPlayers = players.OrderByDescending(p => p.Score).ToList();
        
        foreach (var player in sortedPlayers)
        {
            scores += $"{player.PlayerName.Value}: {player.Score}点\n";
        }
        
        scoresText.text = scores;
    }
    
    private void ShowFinalResults()
    {
        resultsUI.SetActive(true);
        nextRoundButton.gameObject.SetActive(false);
        endGameButton.gameObject.SetActive(true);
        
        var players = DeductionPlayer.DeductionPlayerRefs;
        var winner = players.OrderByDescending(p => p.Score).FirstOrDefault();
        
        string finalResults = "🎉 ゲーム終了 🎉\n\n";
        if (winner != null)
        {
            finalResults += $"優勝者: {winner.PlayerName.Value} ({winner.Score}点)\n\n";
        }
        
        finalResults += "最終スコア:\n";
        var sortedPlayers = players.OrderByDescending(p => p.Score).ToList();
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            finalResults += $"{i + 1}位: {sortedPlayers[i].PlayerName.Value} - {sortedPlayers[i].Score}点\n";
        }
        
        resultsText.text = finalResults;
    }
    
    #endregion

    public void StateAuthorityChanged()
    {
        // 権限が変更された場合の処理
        Debug.Log("DeductionGameManager authority changed");
    }
} 