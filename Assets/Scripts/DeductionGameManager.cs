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
    
    // 投票フィールドに表示される回答（シャッフル済み、4つ固定）
    [Networked, Capacity(4)]
    public NetworkArray<NetworkString<_64>> ShuffledAnswersForVoting => default;
    
    // 各投票フィールドに対応する元のプレイヤーインデックス（-1はAI回答）
    [Networked, Capacity(4)]
    public NetworkArray<int> VoteFieldPlayerMapping => default;

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

    [Tooltip("親プレイヤー用のAI回答入力エリア")]
    public GameObject aiAnswerInputArea;

    [Tooltip("親プレイヤー用のAI回答入力フィールド")]
    public TMP_InputField aiAnswerInputField;

    [Tooltip("親プレイヤー用の自分の回答入力フィールド")]
    public TMP_InputField parentAnswerInputField;

    [Tooltip("親プレイヤーの両回答送信ボタン")]
    public Button submitBothAnswersButton;

        [Tooltip("投票フェーズのUI")]
    public GameObject votingUI;
    
    [Header("Fixed Voting Fields (4 slots)")]
    [Tooltip("投票フィールド1のテキスト")]
    public TextMeshProUGUI voteField1Text;
    
    [Tooltip("投票フィールド1のボタン")]
    public Button voteField1Button;
    
    [Tooltip("投票フィールド2のテキスト")]
    public TextMeshProUGUI voteField2Text;
    
    [Tooltip("投票フィールド2のボタン")]
    public Button voteField2Button;
    
    [Tooltip("投票フィールド3のテキスト")]
    public TextMeshProUGUI voteField3Text;
    
    [Tooltip("投票フィールド3のボタン")]
    public Button voteField3Button;
    
    [Tooltip("投票フィールド4のテキスト")]
    public TextMeshProUGUI voteField4Text;
    
    [Tooltip("投票フィールド4のボタン")]
    public Button voteField4Button;

    #endregion

    [Header("Results Phase UI")]
    [Tooltip("Results表示専用のUIパネル（Resultsフェーズでのみアクティブ）")]
    public GameObject resultsUI;
    
    [Tooltip("Results表示用のテキスト（結果内容を表示）")]
    public TextMeshProUGUI resultsText;

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
        AnswerPhase = 1,      // 全プレイヤーが同時に回答を入力（親は2つ、子は1つ）
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
            // タイマー設定のデバッグ
            Debug.Log($"[PhaseCheck] Initial timer settings - answerTime: {answerTime}, votingTime: {votingTime}");
            
            // answerTimeが0の場合はデフォルト値を設定
            if (answerTime <= 0)
            {
                answerTime = 60f;
                Debug.LogWarning("[PhaseCheck] answerTime was 0, setting to default 60 seconds");
            }
            if (votingTime <= 0)
            {
                votingTime = 30f;
                Debug.LogWarning("[PhaseCheck] votingTime was 0, setting to default 30 seconds");
            }
            
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

        Debug.Log("[PhaseCheck] DeductionGameManager spawned");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        DeductionManagerPresent = false;
    }

    private void InitializeUI()
    {
        // 初期状態の設定
        if (votingUI != null) votingUI.SetActive(false);
        if (aiAnswerInputArea != null) aiAnswerInputArea.SetActive(false);
        if (answerInputField != null) answerInputField.gameObject.SetActive(false);
        if (submitAnswerButton != null) 
        {
            submitAnswerButton.gameObject.SetActive(false);
            submitAnswerButton.onClick.AddListener(SubmitAnswer);
        }
        
        // 親プレイヤー用UI初期化
        if (aiAnswerInputField != null) aiAnswerInputField.gameObject.SetActive(false);
        if (parentAnswerInputField != null) parentAnswerInputField.gameObject.SetActive(false);
        if (submitBothAnswersButton != null)
        {
            submitBothAnswersButton.gameObject.SetActive(false);
            submitBothAnswersButton.onClick.RemoveAllListeners(); // 既存リスナーをクリア
            submitBothAnswersButton.onClick.AddListener(TestButtonClick); // テスト用メソッドを使用
            Debug.Log("[PhaseCheck] SubmitBothAnswersButton onClick event connected to TestButtonClick");
        }
        else
        {
            Debug.LogError("[PhaseCheck] SubmitBothAnswersButton is null! Check Inspector assignment.");
        }

        // Results UI初期化
        if (resultsUI != null) resultsUI.SetActive(false);
        Debug.Log("[PhaseCheck] Results UI initialized as inactive");

        // 既存のUI要素を非表示
        if (questionElements != null) questionElements.SetActive(false);
        
        // 投票フィールドのボタンイベント設定
        InitializeVoteButtons();
    }
    
    private void InitializeVoteButtons()
    {
        if (voteField1Button != null) voteField1Button.onClick.AddListener(() => VoteForField(0));
        if (voteField2Button != null) voteField2Button.onClick.AddListener(() => VoteForField(1));
        if (voteField3Button != null) voteField3Button.onClick.AddListener(() => VoteForField(2));
        if (voteField4Button != null) voteField4Button.onClick.AddListener(() => VoteForField(3));
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
                    Debug.Log("[PhaseCheck] AnswerPhase timer expired, moving to VotingPhase");
                    Debug.Log($"[PhaseCheck] Answer phase lasted: {answerTime} seconds");
                    PrepareVotingFields(); // 投票フィールドの準備
                    GameState = DeductionGameState.VotingPhase;
                    timerLength = votingTime;
                    timer = TickTimer.CreateFromSeconds(Runner, timerLength);
                    break;
                    
                case DeductionGameState.VotingPhase:
                    // 投票フェーズ終了 → 結果表示
                    CalculateResults();
                    Debug.Log($"[PhaseCheck] Transitioning to Results - CurrentRound: {CurrentRound}, maxRounds: {maxRounds}");
                    GameState = DeductionGameState.Results;
                    timerLength = 5f;
                    timer = TickTimer.CreateFromSeconds(Runner, timerLength);
                    Debug.Log($"[PhaseCheck] Results timer set to {timerLength} seconds");
                    break;
                    
                case DeductionGameState.Results:
                    // 結果表示終了（5秒間表示後に次のラウンドまたはゲーム終了）
                    Debug.Log("[PhaseCheck] Results phase timer expired");
                    if (CurrentRound >= maxRounds)
                    {
                        Debug.Log("[PhaseCheck] All rounds completed, transitioning to GameOver");
                        GameState = DeductionGameState.GameOver;
                    }
                    else
                    {
                        Debug.Log($"[PhaseCheck] Round {CurrentRound}/{maxRounds} completed, starting next round");
                        // 次のラウンドの親プレイヤーを選択
                        var players = TriviaPlayer.TriviaPlayerRefs;
                        if (players.Count > 0)
                        {
                            ParentPlayerIndex = (ParentPlayerIndex + 1) % players.Count;
                            Debug.Log($"[PhaseCheck] New parent player index: {ParentPlayerIndex}");
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
                
                // AI回答は親プレイヤーが手動で入力するため、空にする
                AIAnswer = "";
                Debug.Log($"[PhaseCheck] Topic: {CurrentTopic.Value}, First char: {CurrentFirstCharacter.Value}");
                Debug.Log("[PhaseCheck] AI answer will be manually input by parent player");
            }
        }

        // プレイヤーデータをリセット
        ClearPlayerData();

        // 新ラウンド開始時にResults UIを非表示
        if (resultsUI != null) 
        {
            resultsUI.SetActive(false);
            Debug.Log("[PhaseCheck] Results UI deactivated for new round");
        }

        // 回答フェーズ開始
        Debug.Log("[PhaseCheck] Transitioning to AnswerPhase from StartNewRound");
        Debug.Log($"[PhaseCheck] Answer time: {answerTime} seconds");
        GameState = DeductionGameState.AnswerPhase;
        timerLength = answerTime;
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
        Debug.Log($"[PhaseCheck] Timer set to: {timerLength} seconds");
    }

    private void ClearPlayerData()
    {
        if (!HasStateAuthority) return;
        
        for (int i = 0; i < PlayerAnswers.Length; i++)
        {
            PlayerAnswers.Set(i, "");
            PlayerVotes.Set(i, -1);
        }
        
        // 投票フィールドデータもリセット
        for (int i = 0; i < ShuffledAnswersForVoting.Length; i++)
        {
            ShuffledAnswersForVoting.Set(i, "");
        }
        
        for (int i = 0; i < VoteFieldPlayerMapping.Length; i++)
        {
            VoteFieldPlayerMapping.Set(i, -2); // -2 = 空のフィールド
        }

        // プレイヤーの状態もリセット
        var players = TriviaPlayer.TriviaPlayerRefs;
        foreach (var player in players)
        {
            player.ChosenAnswer = -1; // 回答リセット用に再利用
        }
    }
    
    private void PrepareVotingFields()
    {
        if (!HasStateAuthority) return;
        
        Debug.Log("[PhaseCheck] Preparing voting fields with shuffled answers");
        Debug.Log($"[PhaseCheck] Current AIAnswer: '{AIAnswer.Value}'");
        Debug.Log($"[PhaseCheck] PlayerAnswers array length: {PlayerAnswers.Length}");
        
        // 全ての回答を収集
        List<(string answer, int playerIndex)> allAnswers = new List<(string, int)>();
        
        // AI回答を追加（playerIndex = -1）
        if (!string.IsNullOrEmpty(AIAnswer.Value))
        {
            allAnswers.Add((AIAnswer.Value, -1));
            Debug.Log($"[PhaseCheck] Added AI answer: '{AIAnswer.Value}'");
        }
        else
        {
            Debug.LogWarning($"[PhaseCheck] AI Answer is empty: '{AIAnswer.Value}'");
        }
        
        // プレイヤー回答を追加
        for (int i = 0; i < PlayerAnswers.Length; i++)
        {
            Debug.Log($"[PhaseCheck] Checking PlayerAnswers[{i}]: '{PlayerAnswers[i].Value}'");
            if (!string.IsNullOrEmpty(PlayerAnswers[i].Value))
            {
                allAnswers.Add((PlayerAnswers[i].Value, i));
                Debug.Log($"[PhaseCheck] Added player {i} answer: '{PlayerAnswers[i].Value}'");
            }
        }
        
        Debug.Log($"[PhaseCheck] Total answers collected: {allAnswers.Count}");
        
        // 回答をシャッフル
        for (int i = allAnswers.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = allAnswers[i];
            allAnswers[i] = allAnswers[randomIndex];
            allAnswers[randomIndex] = temp;
        }
        
        // 4つのフィールドに配置
        for (int fieldIndex = 0; fieldIndex < 4; fieldIndex++)
        {
            if (fieldIndex < allAnswers.Count)
            {
                // 回答がある場合
                ShuffledAnswersForVoting.Set(fieldIndex, allAnswers[fieldIndex].answer);
                VoteFieldPlayerMapping.Set(fieldIndex, allAnswers[fieldIndex].playerIndex);
                Debug.Log($"[PhaseCheck] Field {fieldIndex}: '{allAnswers[fieldIndex].answer}' (Player {allAnswers[fieldIndex].playerIndex})");
            }
            else
            {
                // 空のフィールド
                ShuffledAnswersForVoting.Set(fieldIndex, "");
                VoteFieldPlayerMapping.Set(fieldIndex, -2); // -2 = 空のフィールド
                Debug.Log($"[PhaseCheck] Field {fieldIndex}: Empty");
            }
        }
        
        Debug.Log($"[PhaseCheck] Prepared {allAnswers.Count} answers in voting fields");
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

    // テスト用：ボタンが正しく動作するかの確認
    public void TestButtonClick()
    {
        Debug.Log("[PhaseCheck] *** TEST BUTTON CLICKED - EVENT SYSTEM WORKS ***");
        SubmitBothAnswers();
    }
    
    public void SubmitBothAnswers()
    {
        Debug.Log($"[PhaseCheck] *** SubmitBothAnswers METHOD CALLED ***");
        Debug.Log($"[PhaseCheck] SubmitBothAnswers called - GameState: {GameState}");
        
        if (GameState != DeductionGameState.AnswerPhase) 
        {
            Debug.LogWarning($"[PhaseCheck] Wrong game state for submission: {GameState}");
            return;
        }
        
        if (aiAnswerInputField == null || parentAnswerInputField == null) 
        {
            Debug.LogError($"[PhaseCheck] Input fields are null - AI: {aiAnswerInputField == null}, Parent: {parentAnswerInputField == null}");
            return;
        }
        
        string aiAnswer = aiAnswerInputField.text.Trim();
        string parentAnswer = parentAnswerInputField.text.Trim();
        
        Debug.Log($"[PhaseCheck] Input values - AI: '{aiAnswer}', Parent: '{parentAnswer}'");
        
        // 両方の回答が入力されているかチェック
        if (string.IsNullOrEmpty(aiAnswer))
        {
            Debug.LogWarning("[PhaseCheck] AI Answer is empty!");
            if (_errorSFX != null) _errorSFX.Play();
            return;
        }
        
        if (string.IsNullOrEmpty(parentAnswer))
        {
            Debug.LogWarning("[PhaseCheck] Parent Answer is empty!");
            if (_errorSFX != null) _errorSFX.Play();
            return;
        }
        
        // 最初の文字チェック
        if (!aiAnswer.StartsWith(CurrentFirstCharacter.Value))
        {
            if (_errorSFX != null) _errorSFX.Play();
            Debug.LogWarning($"[PhaseCheck] AI Answer must start with: {CurrentFirstCharacter.Value}, got: {aiAnswer}");
            return;
        }
        
        if (!parentAnswer.StartsWith(CurrentFirstCharacter.Value))
        {
            if (_errorSFX != null) _errorSFX.Play();
            Debug.LogWarning($"[PhaseCheck] Parent Answer must start with: {CurrentFirstCharacter.Value}, got: {parentAnswer}");
            return;
        }
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        bool isParent = localPlayer != null && 
                       TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer) == ParentPlayerIndex;
        
        Debug.Log($"[PhaseCheck] Player check - LocalPlayer: {localPlayer != null}, IsParent: {isParent}, HasInputAuthority: {HasInputAuthority}");
        
        if (isParent)
        {
            // AI回答と親プレイヤーの回答を同時に送信
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            Debug.Log($"[PhaseCheck] ATTEMPTING RPC SEND - PlayerIndex: {playerIndex}, HasInputAuthority: {HasInputAuthority}");
            
            try
            {
                RPC_SubmitBothAnswers(aiAnswer, playerIndex, parentAnswer);
                Debug.Log($"[PhaseCheck] RPC SENT SUCCESSFULLY - AI: '{aiAnswer}', Parent[{playerIndex}]: '{parentAnswer}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhaseCheck] RPC SEND FAILED: {e.Message}");
            }
            
            if (_confirmSFX != null) _confirmSFX.Play();
            
            // UI更新
            aiAnswerInputField.text = "";
            parentAnswerInputField.text = "";
            aiAnswerInputField.gameObject.SetActive(false);
            parentAnswerInputField.gameObject.SetActive(false);
            submitBothAnswersButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[PhaseCheck] Cannot submit - Not parent player. IsParent: {isParent}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitBothAnswers(string aiAnswer, int parentPlayerIndex, string parentAnswer)
    {
        Debug.Log($"[PhaseCheck] RPC_SubmitBothAnswers received - AI: '{aiAnswer}', Parent[{parentPlayerIndex}]: '{parentAnswer}'");
        
        // AI回答を設定
        AIAnswer = aiAnswer;
        Debug.Log($"[PhaseCheck] AI Answer set to: '{AIAnswer.Value}'");
        
        // 親プレイヤーの回答を設定
        if (parentPlayerIndex >= 0 && parentPlayerIndex < PlayerAnswers.Length)
        {
            PlayerAnswers.Set(parentPlayerIndex, parentAnswer);
            Debug.Log($"[PhaseCheck] Parent answer set at index {parentPlayerIndex}: '{PlayerAnswers[parentPlayerIndex].Value}'");
        }
        else
        {
            Debug.LogError($"[PhaseCheck] Invalid parent player index: {parentPlayerIndex}, array length: {PlayerAnswers.Length}");
        }
        
        Debug.Log($"[PhaseCheck] Both answers stored successfully");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitVote(int playerIndex, int fieldIndex)
    {
        if (playerIndex >= 0 && playerIndex < PlayerVotes.Length)
        {
            PlayerVotes.Set(playerIndex, fieldIndex);
            Debug.Log($"[PhaseCheck] Player {playerIndex} voted for field {fieldIndex}");
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
        Debug.Log($"[PhaseCheck] Game state changed to: {GameState}");
        
        switch (GameState)
        {
            case DeductionGameState.Intro:
                Debug.Log("[PhaseCheck] Showing Intro state");
                triviaMessage.text = "推理ゲーム開始\nまもなく最初のラウンドが始まります";
                if (questionElements != null) questionElements.SetActive(false);
                if (votingUI != null) votingUI.SetActive(false);
                if (resultsUI != null) resultsUI.SetActive(false); // Results UI非表示
                if (endGameObject != null) endGameObject.Hide();
                break;
                

                
            case DeductionGameState.AnswerPhase:
                Debug.Log("[PhaseCheck] Transitioning to Answer Phase");
                ShowAnswerPhase();
                break;
                
            case DeductionGameState.VotingPhase:
                Debug.Log("[PhaseCheck] Transitioning to Voting Phase");
                ShowVotingPhase();
                break;
                
            case DeductionGameState.Results:
                Debug.Log("[PhaseCheck] Transitioning to Results Phase");
                ShowResults();
                break;
                
            case DeductionGameState.GameOver:
                Debug.Log("[PhaseCheck] Transitioning to Game Over");
                ShowGameOver();
                break;
        }
    }



    private void ShowAnswerPhase()
    {
        Debug.Log("[PhaseCheck] ShowAnswerPhase called - Simultaneous answering for all players");
        
        if (questionElements != null) questionElements.SetActive(true);
        if (votingUI != null) votingUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false); // Results UI非表示
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        bool isParent = localPlayer != null && 
                       TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer) == ParentPlayerIndex;
        
        if (isParent)
        {
            // 親プレイヤー：AI回答と自分の回答の両方を入力
            triviaMessage.text = "回答フェーズ - AI回答と自分の回答を入力してください";
            
            // 親プレイヤー専用UI表示
            if (aiAnswerInputArea != null) aiAnswerInputArea.SetActive(true);
            
            // AI回答入力フィールド
            if (aiAnswerInputField != null) 
            {
                aiAnswerInputField.gameObject.SetActive(true);
                aiAnswerInputField.text = "";
                aiAnswerInputField.placeholder.GetComponent<TextMeshProUGUI>().text = "AI回答を入力...";
            }
            
            // 親プレイヤーの回答入力フィールド
            if (parentAnswerInputField != null)
            {
                parentAnswerInputField.gameObject.SetActive(true);
                parentAnswerInputField.text = "";
                parentAnswerInputField.placeholder.GetComponent<TextMeshProUGUI>().text = "あなたの回答を入力...";
            }
            
            // 両方送信ボタン
            if (submitBothAnswersButton != null) 
            {
                submitBothAnswersButton.gameObject.SetActive(true);
                Debug.Log($"[PhaseCheck] SubmitBothAnswersButton activated - Interactable: {submitBothAnswersButton.interactable}");
                Debug.Log($"[PhaseCheck] SubmitBothAnswersButton listeners count: {submitBothAnswersButton.onClick.GetPersistentEventCount()}");
            }
            else
            {
                Debug.LogError("[PhaseCheck] SubmitBothAnswersButton is null in ShowAnswerPhase!");
            }
            
            // 通常の回答UIは非表示
            if (answerInputField != null) answerInputField.gameObject.SetActive(false);
            if (submitAnswerButton != null) submitAnswerButton.gameObject.SetActive(false);
            
            Debug.Log("[PhaseCheck] Parent UI activated - AI answer field and parent answer field");
        }
        else
        {
            // 子プレイヤー：通常の回答入力のみ
            triviaMessage.text = "回答フェーズ - お題に回答してください";
            
            // 親プレイヤー専用UIは非表示
            if (aiAnswerInputArea != null) aiAnswerInputArea.SetActive(false);
            
            // 通常の回答UI表示
            Debug.Log($"[PhaseCheck] AnswerInputField null: {answerInputField == null}");
            Debug.Log($"[PhaseCheck] SubmitAnswerButton null: {submitAnswerButton == null}");
            
            if (answerInputField != null) 
            {
                answerInputField.gameObject.SetActive(true);
                answerInputField.text = "";
                Debug.Log("[PhaseCheck] AnswerInputField activated for child player");
            }
            else
            {
                Debug.LogError("[PhaseCheck] AnswerInputField is null! Check Inspector settings.");
            }
            
            if (submitAnswerButton != null) 
            {
                submitAnswerButton.gameObject.SetActive(true);
                Debug.Log("[PhaseCheck] SubmitAnswerButton activated for child player");
            }
            else
            {
                Debug.LogError("[PhaseCheck] SubmitAnswerButton is null! Check Inspector settings.");
            }
        }
    }

    private void ShowVotingPhase()
    {
        Debug.Log("[PhaseCheck] ShowVotingPhase called");
        triviaMessage.text = "投票フェーズ - AIの回答だと思うものを選んでください";
        
        // 全ての回答入力UIを非表示
        if (answerInputField != null) answerInputField.gameObject.SetActive(false);
        if (submitAnswerButton != null) submitAnswerButton.gameObject.SetActive(false);
        
        // 親プレイヤー専用UIを完全に非表示
        if (aiAnswerInputArea != null) aiAnswerInputArea.SetActive(false);
        if (aiAnswerInputField != null) aiAnswerInputField.gameObject.SetActive(false);
        if (parentAnswerInputField != null) parentAnswerInputField.gameObject.SetActive(false);
        if (submitBothAnswersButton != null) submitBothAnswersButton.gameObject.SetActive(false);
        
        // Results UI非表示
        if (resultsUI != null) resultsUI.SetActive(false);
        
        Debug.Log("[PhaseCheck] All answer input UI elements hidden");
        
        if (votingUI != null) votingUI.SetActive(true);
        
        UpdateVotingFields();
    }
    
    private void UpdateVotingFields()
    {
        Debug.Log("[PhaseCheck] Updating voting fields display");
        
        // フィールド1
        if (voteField1Text != null)
        {
            voteField1Text.text = ShuffledAnswersForVoting[0].Value;
        }
        if (voteField1Button != null)
        {
            voteField1Button.gameObject.SetActive(!string.IsNullOrEmpty(ShuffledAnswersForVoting[0].Value));
        }
        
        // フィールド2
        if (voteField2Text != null)
        {
            voteField2Text.text = ShuffledAnswersForVoting[1].Value;
        }
        if (voteField2Button != null)
        {
            voteField2Button.gameObject.SetActive(!string.IsNullOrEmpty(ShuffledAnswersForVoting[1].Value));
        }
        
        // フィールド3
        if (voteField3Text != null)
        {
            voteField3Text.text = ShuffledAnswersForVoting[2].Value;
        }
        if (voteField3Button != null)
        {
            voteField3Button.gameObject.SetActive(!string.IsNullOrEmpty(ShuffledAnswersForVoting[2].Value));
        }
        
        // フィールド4
        if (voteField4Text != null)
        {
            voteField4Text.text = ShuffledAnswersForVoting[3].Value;
        }
        if (voteField4Button != null)
        {
            voteField4Button.gameObject.SetActive(!string.IsNullOrEmpty(ShuffledAnswersForVoting[3].Value));
        }
        
        Debug.Log($"[PhaseCheck] Updated voting fields: '{ShuffledAnswersForVoting[0].Value}', '{ShuffledAnswersForVoting[1].Value}', '{ShuffledAnswersForVoting[2].Value}', '{ShuffledAnswersForVoting[3].Value}'");
    }
    
    public void VoteForField(int fieldIndex)
    {
        if (GameState != DeductionGameState.VotingPhase) return;
        if (fieldIndex < 0 || fieldIndex >= 4) return;
        if (string.IsNullOrEmpty(ShuffledAnswersForVoting[fieldIndex].Value)) return;
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        if (localPlayer == null) return;
        
        // 親プレイヤーは投票できない
        bool isParent = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer) == ParentPlayerIndex;
        if (isParent)
        {
            Debug.Log("[PhaseCheck] Parent player cannot vote");
            if (_errorSFX != null) _errorSFX.Play();
            return;
        }
        
        int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
        if (playerIndex >= 0 && HasInputAuthority)
        {
            // フィールドに投票（フィールドインデックスを保存）
            RPC_SubmitVote(playerIndex, fieldIndex);
            Debug.Log($"[PhaseCheck] Player {playerIndex} voted for field {fieldIndex}: '{ShuffledAnswersForVoting[fieldIndex].Value}'");
            
            if (_confirmSFX != null) _confirmSFX.Play();
        }
    }



    private void ShowResults()
    {
        Debug.Log("[PhaseCheck] ShowResults called");
        
        // 他のUIを非表示
        if (votingUI != null) votingUI.SetActive(false);
        if (questionElements != null) questionElements.SetActive(false);
        if (aiAnswerInputArea != null) aiAnswerInputArea.SetActive(false);
        
        // Inspector設定の詳細デバッグ
        Debug.Log($"[PhaseCheck] resultsUI null check: {(resultsUI == null ? "NULL" : "NOT NULL")}");
        Debug.Log($"[PhaseCheck] resultsText null check: {(resultsText == null ? "NULL" : "NOT NULL")}");
        if (resultsUI != null)
        {
            Debug.Log($"[PhaseCheck] resultsUI name: {resultsUI.name}");
            Debug.Log($"[PhaseCheck] resultsUI activeInHierarchy before: {resultsUI.activeInHierarchy}");
        }
        
        // Results専用UIを表示
        if (resultsUI != null) 
        {
            resultsUI.SetActive(true);
            Debug.Log($"[PhaseCheck] resultsUI activeInHierarchy after: {resultsUI.activeInHierarchy}");
            Debug.Log("[PhaseCheck] Results UI activated");
        }
        else
        {
            Debug.LogError("[PhaseCheck] resultsUI is NULL! Check Inspector settings.");
            // フォールバック: questionElementsを使用
            if (questionElements != null) 
            {
                questionElements.SetActive(true);
                Debug.Log("[PhaseCheck] Using questionElements as fallback for results display");
            }
        }
        
        // 結果表示の詳細を実装
        DisplayRoundResults();
    }

    private void DisplayRoundResults()
    {
        Debug.Log("[PhaseCheck] DisplayRoundResults called");
        
        // シンプルな結果表示: AI回答のみ
        string results = $"AI回答：{AIAnswer.Value}";
        
        Debug.Log($"[PhaseCheck] Setting results text: {results}");
        
        // Results専用テキストを優先使用
        if (resultsText != null)
        {
            resultsText.text = results;
            Debug.Log("[PhaseCheck] Results displayed in resultsText");
        }
        else if (triviaMessage != null)
        {
            triviaMessage.text = results;
            Debug.Log("[PhaseCheck] Results displayed in triviaMessage (fallback)");
        }
        else
        {
            Debug.LogError("[PhaseCheck] No text component available for results display!");
        }
    }

    private void ShowGameOver()
    {
        Debug.Log("[PhaseCheck] ShowGameOver called");
        
        // 全てのゲームUIを非表示
        if (questionElements != null) questionElements.SetActive(false);
        if (votingUI != null) votingUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);
        if (aiAnswerInputArea != null) aiAnswerInputArea.SetActive(false);
        
        // ゲーム終了メッセージ
        if (triviaMessage != null) triviaMessage.text = "ゲーム終了！";
        
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