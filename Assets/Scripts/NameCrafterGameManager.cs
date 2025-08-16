using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Name Crafterゲーム管理システム
/// DeductionGameManagerをベースとした創作型ネーミングゲーム
/// 通常モード（自由回答）と選択モード（選択肢から選択）に対応
/// </summary>
public class NameCrafterGameManager : NetworkBehaviour, IStateAuthorityChanged
{
    [Header("Game Data")]
    [Tooltip("Name Crafterゲーム用のお題データ")]
    public NameCrafterTopicSet nameCrafterTopics;

    [Header("UI Elements - DeductionGameManagerと同じ構造")]
    [Tooltip("Container for the game elements")]
    public GameObject questionElements = null;

    #region Networked Properties
    [Networked, Tooltip("Timer used for game phases and transitions.")]
    public TickTimer timer { get; set; }

    [Networked, Tooltip("The length of the timer, used to help get a percentage when rendering timers.")]
    public float timerLength { get; set; }

    [Tooltip("The current round number.")]
    [Networked, OnChangedRender(nameof(UpdateCurrentRound))]
    public int CurrentRound { get; set; } = 0;

    [Tooltip("The current state of the name crafter game.")]
    [Networked, OnChangedRender(nameof(OnNameCrafterGameStateChanged))]
    public NameCrafterGameState GameState { get; set; } = NameCrafterGameState.Intro;

    [Tooltip("Current game mode (Normal or Selection).")]
    [Networked, OnChangedRender(nameof(UpdateGameMode))]
    public NameCrafterGameMode GameMode { get; set; } = NameCrafterGameMode.Normal;

    [Tooltip("Selected word cards for this round (3 words).")]
    [Networked, Capacity(3)]
    public NetworkArray<NetworkString<_32>> SelectedWords => default;

    [Tooltip("Players selected for word choosing this round (3 players).")]
    [Networked, Capacity(3)]
    public NetworkArray<int> WordChooserPlayers => default;

    [Tooltip("Current word chooser index (0-2).")]
    [Networked, OnChangedRender(nameof(UpdateCurrentWordChooser))]
    public int CurrentWordChooserIndex { get; set; } = 0;

    [Tooltip("Available word options for current chooser (4 options).")]
    [Networked, Capacity(4)]
    public NetworkArray<NetworkString<_32>> WordOptions => default;

    // プレイヤーの回答を格納（通常モード：自由入力、選択モード：選択肢のインデックス）
    [Networked, Capacity(20)]
    public NetworkArray<NetworkString<_64>> PlayerAnswers => default;

    // 選択モード用：プレイヤーの選択インデックス
    [Networked, Capacity(20)]
    public NetworkArray<int> PlayerSelections => default;

    // 投票システム：各プレイヤーの持ち点配分
    [Networked, Capacity(20 * 20)] // 最大20プレイヤー × 20の投票先
    public NetworkArray<int> PlayerVoteAllocations => default;

    // プレイヤーのスコア統計
    [Networked, Capacity(20)]
    public NetworkArray<int> PlayerTotalScores => default;

    [Networked, Capacity(20)]
    public NetworkArray<int> PlayerRoundScores => default;

    [Networked, Capacity(20)]
    public NetworkArray<float> PlayerVoteRates => default;

    #endregion

    #region UI Elements - 基本構造はDeductionGameManagerと同じ
    
    /// <summary>
    /// お題、回答入力、投票関連のUI
    /// </summary>
    public TextMeshProUGUI question; // お題表示用
    public TextMeshProUGUI[] answers; // 投票時の回答表示用
    public Image[] answerHighlights; // 投票結果表示用

    /// <summary>
    /// タイマー表示
    /// </summary>
    public Image timerVisual;
    
    [Tooltip("Gradient used to color the timer based on percentage.")]
    public Gradient timerVisualGradient;

    /// <summary>
    /// ゲーム進行表示
    /// </summary>
    public TextMeshProUGUI questionIndicatorText; // "ラウンド X / Y" 表示用
    public TextMeshProUGUI triviaMessage; // ゲーム状態メッセージ用

    [Tooltip("Button displayed to leave the game after a round ends.")]
    public GameObject leaveGameBtn;

    [Tooltip("Button displayed, only to the master client, to start a new game.")]
    public GameObject startNewGameBtn;

    [Tooltip("MonoBehaviour that displays winner at the end of a game.")]
    public TriviaEndGame endGameObject; // 結果表示用

    #endregion

    #region Name Crafter専用UI
    
    [Header("Name Crafter Specific UI")]
    
    [Tooltip("ゲームモード選択UI")]
    public GameObject gameModeSelectionUI;
    
    [Tooltip("通常モード選択ボタン")]
    public Button normalModeButton;
    
    [Tooltip("選択モード選択ボタン")]
    public Button selectionModeButton;

    [Tooltip("単語選択フェーズUI")]
    public GameObject wordSelectionUI;
    
    [Tooltip("現在の単語選択者表示")]
    public TextMeshProUGUI currentChooserText;
    
    [Tooltip("単語選択ボタン（4つ）")]
    public Button[] wordOptionButtons;
    
    [Tooltip("単語選択ボタンのテキスト（4つ）")]
    public TextMeshProUGUI[] wordOptionTexts;

    [Tooltip("選択された単語表示エリア")]
    public GameObject selectedWordsArea;
    
    [Tooltip("選択された単語表示テキスト（3つ）")]
    public TextMeshProUGUI[] selectedWordTexts;

    [Tooltip("回答入力フェーズUI（通常モード）")]
    public GameObject answerInputUI;
    
    [Tooltip("回答入力フィールド")]
    public TMP_InputField answerInputField;

    [Tooltip("回答送信ボタン")]
    public Button submitAnswerButton;

    [Tooltip("選択フェーズUI（選択モード）")]
    public GameObject selectionUI;
    
    [Tooltip("選択肢ボタン（4つ）")]
    public Button[] selectionOptionButtons;
    
    [Tooltip("選択肢ボタンのテキスト（4つ）")]
    public TextMeshProUGUI[] selectionOptionTexts;

    [Tooltip("投票フェーズUI")]
    public GameObject votingUI;

    [Tooltip("結果表示UI")]
    public GameObject resultsUI;
    
    [Tooltip("結果表示テキスト")]
    public TextMeshProUGUI resultsText;
    
    [Header("Player-Specific Result Display Positions")]
    [Tooltip("2プレイヤー時の結果表示配置場所")]
    public Transform[] resultPositions2Players = new Transform[2];
    
    [Tooltip("3プレイヤー時の結果表示配置場所")]
    public Transform[] resultPositions3Players = new Transform[3];
    
    [Tooltip("4プレイヤー時の結果表示配置場所")]
    public Transform[] resultPositions4Players = new Transform[4];
    
    [Tooltip("5プレイヤー時の結果表示配置場所")]
    public Transform[] resultPositions5Players = new Transform[5];
    
    [Tooltip("6プレイヤー時の結果表示配置場所")]
    public Transform[] resultPositions6Players = new Transform[6];
    
    [Tooltip("7プレイヤー時の結果表示配置場所")]
    public Transform[] resultPositions7Players = new Transform[7];
    
    [Tooltip("8プレイヤー時の結果表示配置場所")]
    public Transform[] resultPositions8Players = new Transform[8];
    
    [Tooltip("プレイヤー結果表示プレハブ")]
    public GameObject playerResultPrefab;

    #endregion

    [Header("Game Rules")]
    [Tooltip("The maximum number of rounds to play.")]
    [Min(1)]
    public int maxRounds = 5;

    [Tooltip("Time for word selection phase.")]
    public float wordSelectionTime = 30f;

    [Tooltip("Time for answer creation phase (Normal mode).")]
    public float answerCreationTime = 90f;
    
    [Tooltip("Time for selection phase (Selection mode).")]
    public float selectionTime = 60f;

    [Tooltip("Time for voting phase.")]
    public float votingTime = 150f;

    [Tooltip("Time for results display.")]
    public float resultsTime = 10f;

    #region SFX
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
    /// Name Crafterゲーム管理システムが存在するかどうか
    /// </summary>
    public static bool NameCrafterManagerPresent { get; private set; } = false;

    /// <summary>
    /// Name Crafterゲームの状態
    /// </summary>
    public enum NameCrafterGameState : byte
    {
        Intro = 0,
        ModeSelection = 1,        // ゲームモード選択
        WordSelection = 2,        // 単語選択フェーズ（3人が順番に選択）
        AnswerCreation = 3,       // 回答作成フェーズ（通常モード）
        Selection = 4,            // 選択フェーズ（選択モード）
        Voting = 5,               // 投票フェーズ（通常モードのみ）
        Results = 6,              // 結果表示
        GameOver = 7,             // ゲーム終了
        NewRound = 8,
    }

    /// <summary>
    /// Name Crafterゲームのモード
    /// </summary>
    public enum NameCrafterGameMode : byte
    {
        Normal = 0,      // 通常モード（自由回答 + 投票）
        Selection = 1,   // 選択モード（選択肢から選択 + 一致率計算）
    }

    // 投票システム用（NameCrafterVotingSystemコンポーネントが管理）

    public override void Spawned()
    {
        // 初期化
        if (CurrentRound == 0)
            questionIndicatorText.text = "";
        else
            questionIndicatorText.text = "Round: " + CurrentRound + " / " + maxRounds;

        // セッション設定
        if (Runner.IsSharedModeMasterClient)
        {
            Runner.SessionInfo.IsOpen = false;
            Runner.SessionInfo.IsVisible = false;
        }

        // 権限を持つクライアントが初期設定を行う
        if (HasStateAuthority)
        {
            // タイマー設定のデバッグ
            Debug.Log($"[NameCrafter] Initial timer settings");
            
            // デフォルト値設定
            if (wordSelectionTime <= 0) wordSelectionTime = 30f;
            if (answerCreationTime <= 0) answerCreationTime = 90f;
            if (selectionTime <= 0) selectionTime = 60f;
            if (votingTime <= 0) votingTime = 150f;
            if (resultsTime <= 0) resultsTime = 10f;
            
            timerLength = 3f;
            timer = TickTimer.CreateFromSeconds(Runner, timerLength);
            CurrentRound = 0;
        }

        NameCrafterManagerPresent = true;

        FusionConnector.Instance?.SetPregameMessage(string.Empty);

        // UI初期化
        InitializeUI();

        // 状態更新
        OnNameCrafterGameStateChanged();
        UpdateCurrentRound();
        UpdateGameMode();
        UpdateCurrentWordChooser();

        Debug.Log("[NameCrafter] NameCrafterGameManager spawned");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        NameCrafterManagerPresent = false;
    }

    private void InitializeUI()
    {
        // 初期状態の設定
        if (gameModeSelectionUI != null) gameModeSelectionUI.SetActive(false);
        if (wordSelectionUI != null) wordSelectionUI.SetActive(false);
        if (selectedWordsArea != null) selectedWordsArea.SetActive(false);
        if (answerInputUI != null) answerInputUI.SetActive(false);
        if (selectionUI != null) selectionUI.SetActive(false);
        if (votingUI != null) votingUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);
        
        // ボタンイベント設定
        if (normalModeButton != null)
            normalModeButton.onClick.AddListener(() => SelectGameMode(NameCrafterGameMode.Normal));
        
        if (selectionModeButton != null)
            selectionModeButton.onClick.AddListener(() => SelectGameMode(NameCrafterGameMode.Selection));
        
        if (submitAnswerButton != null)
            submitAnswerButton.onClick.AddListener(SubmitAnswer);

        // 単語選択ボタン
        for (int i = 0; i < wordOptionButtons.Length; i++)
        {
            int index = i; // クロージャ対策
            if (wordOptionButtons[i] != null)
                wordOptionButtons[i].onClick.AddListener(() => SelectWordOption(index));
        }

        // 選択肢ボタン
        for (int i = 0; i < selectionOptionButtons.Length; i++)
        {
            int index = i; // クロージャ対策
            if (selectionOptionButtons[i] != null)
                selectionOptionButtons[i].onClick.AddListener(() => SelectOption(index));
        }

        // 既存のUI要素を非表示
        if (questionElements != null) questionElements.SetActive(false);
        
        Debug.Log("[NameCrafter] UI initialized");
    }

    /// <summary>
    /// ゲーム進行管理
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (timer.Expired(Runner))
        {
            switch (GameState)
            {
                case NameCrafterGameState.Intro:
                    StartModeSelection();
                    break;
                    
                case NameCrafterGameState.ModeSelection:
                    // タイムアウト時はデフォルトで通常モードに
                    GameMode = NameCrafterGameMode.Normal;
                    StartNewRound();
                    break;
                    
                case NameCrafterGameState.WordSelection:
                    // 次の単語選択者へ、または回答フェーズへ
                    if (CurrentWordChooserIndex < 2)
                    {
                        CurrentWordChooserIndex++;
                        PrepareWordOptions();
                        timer = TickTimer.CreateFromSeconds(Runner, wordSelectionTime);
                    }
                    else
                    {
                        StartAnswerPhase();
                    }
                    break;
                    
                case NameCrafterGameState.AnswerCreation:
                    // 通常モード：投票フェーズへ
                    StartVotingPhase();
                    break;
                    
                case NameCrafterGameState.Selection:
                    // 選択モード：結果表示へ
                    CalculateSelectionResults();
                    GameState = NameCrafterGameState.Results;
                    timerLength = resultsTime;
                    timer = TickTimer.CreateFromSeconds(Runner, timerLength);
                    break;
                    
                case NameCrafterGameState.Voting:
                    // 投票フェーズ終了：結果計算
                    CalculateVotingResults();
                    GameState = NameCrafterGameState.Results;
                    timerLength = resultsTime;
                    timer = TickTimer.CreateFromSeconds(Runner, timerLength);
                    break;
                    
                case NameCrafterGameState.Results:
                    // 次のラウンドまたはゲーム終了
                    if (CurrentRound >= maxRounds)
                    {
                        GameState = NameCrafterGameState.GameOver;
                    }
                    else
                    {
                        StartNewRound();
                    }
                    break;
            }
        }
    }

    private void StartModeSelection()
    {
        if (!HasStateAuthority) return;
        
        Debug.Log("[NameCrafter] Starting mode selection");
        GameState = NameCrafterGameState.ModeSelection;
        timerLength = 30f; // モード選択時間
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
    }

    private void StartNewRound()
    {
        if (!HasStateAuthority) return;

        CurrentRound++;
        Debug.Log($"[NameCrafter] Starting round {CurrentRound}");
        
        // プレイヤーデータをリセット
        ClearPlayerData();
        
        // 単語選択者を決定（3人をローテーション）
        SelectWordChoosers();
        
        // 単語選択フェーズ開始
        CurrentWordChooserIndex = 0;
        PrepareWordOptions();
        GameState = NameCrafterGameState.WordSelection;
        timerLength = wordSelectionTime;
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
    }

    private void SelectWordChoosers()
    {
        if (!HasStateAuthority) return;
        
        var players = TriviaPlayer.TriviaPlayerRefs;
        if (players.Count < 3)
        {
            Debug.LogWarning("[NameCrafter] Not enough players for word selection");
            return;
        }
        
        // ローテーション方式で3人を選択
        int baseIndex = (CurrentRound - 1) * 3 % players.Count;
        for (int i = 0; i < 3; i++)
        {
            int playerIndex = (baseIndex + i) % players.Count;
            WordChooserPlayers.Set(i, playerIndex);
        }
        
        Debug.Log($"[NameCrafter] Word choosers: {WordChooserPlayers[0]}, {WordChooserPlayers[1]}, {WordChooserPlayers[2]}");
    }

    private void PrepareWordOptions()
    {
        if (!HasStateAuthority) return;
        if (nameCrafterTopics == null) return;
        
        // 現在の選択者に応じて品詞を決定
        string partOfSpeech = GetPartOfSpeechForChooser(CurrentWordChooserIndex);
        var options = nameCrafterTopics.GetWordOptions(partOfSpeech, 4);
        
        for (int i = 0; i < 4; i++)
        {
            if (i < options.Count)
                WordOptions.Set(i, options[i]);
            else
                WordOptions.Set(i, "");
        }
        
        Debug.Log($"[NameCrafter] Prepared word options for chooser {CurrentWordChooserIndex}: {partOfSpeech}");
    }

    private string GetPartOfSpeechForChooser(int chooserIndex)
    {
        // 形容詞2つ、名詞1つの順番
        switch (chooserIndex)
        {
            case 0: return "形容詞";
            case 1: return "形容詞";
            case 2: return "名詞";
            default: return "形容詞";
        }
    }

    private void StartAnswerPhase()
    {
        if (!HasStateAuthority) return;
        
        Debug.Log("[NameCrafter] Starting answer phase");
        
        if (GameMode == NameCrafterGameMode.Normal)
        {
            GameState = NameCrafterGameState.AnswerCreation;
            timerLength = answerCreationTime;
        }
        else
        {
            GameState = NameCrafterGameState.Selection;
            timerLength = selectionTime;
            PrepareSelectionOptions();
        }
        
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
    }

    private void PrepareSelectionOptions()
    {
        if (!HasStateAuthority) return;
        if (nameCrafterTopics == null) return;
        
        // 選択モード用の4つの選択肢を準備
        var options = nameCrafterTopics.GetSelectionOptions(4);
        for (int i = 0; i < 4; i++)
        {
            if (i < options.Count)
                WordOptions.Set(i, options[i]);
            else
                WordOptions.Set(i, "");
        }
        
        Debug.Log("[NameCrafter] Prepared selection options");
    }

    private void StartVotingPhase()
    {
        if (!HasStateAuthority) return;
        
        Debug.Log("[NameCrafter] Starting voting phase");
        GameState = NameCrafterGameState.Voting;
        timerLength = votingTime;
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
    }

    private void ClearPlayerData()
    {
        if (!HasStateAuthority) return;
        
        for (int i = 0; i < PlayerAnswers.Length; i++)
        {
            PlayerAnswers.Set(i, "");
            PlayerSelections.Set(i, -1);
            PlayerRoundScores.Set(i, 0);
        }
        
        // 投票データクリア
        for (int i = 0; i < PlayerVoteAllocations.Length; i++)
        {
            PlayerVoteAllocations.Set(i, 0);
        }
        
        // 選択された単語をクリア
        for (int i = 0; i < SelectedWords.Length; i++)
        {
            SelectedWords.Set(i, "");
        }
        
        Debug.Log("[NameCrafter] Player data cleared");
    }

    /// <summary>
    /// タイマー表示更新
    /// </summary>
    public void Update()
    {
        // タイマー表示の更新
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

    #region User Input Methods

    public void SelectGameMode(NameCrafterGameMode mode)
    {
        if (GameState != NameCrafterGameState.ModeSelection) return;
        if (!Runner.IsSharedModeMasterClient) return; // ホストのみ選択可能
        
        RPC_SelectGameMode(mode);
    }

    /// <summary>
    /// 投票システムから投票結果を受信
    /// </summary>
    /// <param name="voterIndex">投票者のインデックス</param>
    /// <param name="allocations">各プレイヤーへの配分</param>
    public void SubmitVotingResults(int voterIndex, Dictionary<int, int> allocations)
    {
        if (GameState != NameCrafterGameState.Voting) return;
        if (!HasInputAuthority) return;
        
        // 配分データをネットワーク配列に変換してRPC送信
        var players = TriviaPlayer.TriviaPlayerRefs;
        var allocationArray = new int[players.Count];
        
        foreach (var kvp in allocations)
        {
            if (kvp.Key >= 0 && kvp.Key < allocationArray.Length)
            {
                allocationArray[kvp.Key] = kvp.Value;
            }
        }
        
        RPC_SubmitVotingResults(voterIndex, allocationArray);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SelectGameMode(NameCrafterGameMode mode)
    {
        GameMode = mode;
        Debug.Log($"[NameCrafter] Game mode selected: {mode}");
        
        // すぐに新ラウンド開始
        StartNewRound();
    }

    public void SelectWordOption(int optionIndex)
    {
        if (GameState != NameCrafterGameState.WordSelection) return;
        if (optionIndex < 0 || optionIndex >= 4) return;
        if (string.IsNullOrEmpty(WordOptions[optionIndex].Value)) return;
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        if (localPlayer == null) return;
        
        int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
        int currentChooser = WordChooserPlayers[CurrentWordChooserIndex];
        
        if (playerIndex != currentChooser) return; // 選択権限チェック
        
        RPC_SelectWordOption(CurrentWordChooserIndex, WordOptions[optionIndex].Value);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SelectWordOption(int chooserIndex, string selectedWord)
    {
        if (chooserIndex >= 0 && chooserIndex < 3)
        {
            SelectedWords.Set(chooserIndex, selectedWord);
            Debug.Log($"[NameCrafter] Word {chooserIndex} selected: {selectedWord}");
            
            // 次の選択者へ
            if (chooserIndex < 2)
            {
                CurrentWordChooserIndex++;
                PrepareWordOptions();
                timer = TickTimer.CreateFromSeconds(Runner, wordSelectionTime);
            }
            else
            {
                StartAnswerPhase();
            }
        }
    }

    public void SubmitAnswer()
    {
        if (GameState != NameCrafterGameState.AnswerCreation) return;
        if (answerInputField == null) return;
        
        string answer = answerInputField.text.Trim();
        if (string.IsNullOrEmpty(answer)) return;
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        if (localPlayer != null)
        {
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            if (playerIndex >= 0 && HasInputAuthority)
            {
                RPC_SubmitAnswer(playerIndex, answer);
                
                if (_confirmSFX != null) _confirmSFX.Play();
                
                // UI更新
                answerInputField.text = "";
                answerInputField.gameObject.SetActive(false);
                submitAnswerButton.gameObject.SetActive(false);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitAnswer(int playerIndex, string answer)
    {
        if (playerIndex >= 0 && playerIndex < PlayerAnswers.Length)
        {
            PlayerAnswers.Set(playerIndex, answer);
            Debug.Log($"[NameCrafter] Answer received from player {playerIndex}: {answer}");
        }
    }

    public void SelectOption(int optionIndex)
    {
        if (GameState != NameCrafterGameState.Selection) return;
        if (optionIndex < 0 || optionIndex >= 4) return;
        if (string.IsNullOrEmpty(WordOptions[optionIndex].Value)) return;
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        if (localPlayer != null)
        {
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            if (playerIndex >= 0 && HasInputAuthority)
            {
                RPC_SelectOption(playerIndex, optionIndex);
                
                if (_confirmSFX != null) _confirmSFX.Play();
                
                // 選択ボタンを無効化
                foreach (var button in selectionOptionButtons)
                {
                    if (button != null) button.interactable = false;
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SelectOption(int playerIndex, int optionIndex)
    {
        if (playerIndex >= 0 && playerIndex < PlayerSelections.Length)
        {
            PlayerSelections.Set(playerIndex, optionIndex);
            Debug.Log($"[NameCrafter] Selection received from player {playerIndex}: option {optionIndex}");
        }
    }

    public void CompleteVoting()
    {
        if (GameState != NameCrafterGameState.Voting) return;
        
        // 投票システムから完了状態をチェック
        var votingSystem = GetComponent<NameCrafterVotingSystem>();
        if (votingSystem != null && votingSystem.GetRemainingPoints() > 0) return; // まだ配分が完了していない
        
        var localPlayer = TriviaPlayer.LocalPlayer;
        if (localPlayer != null)
        {
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            if (playerIndex >= 0 && HasInputAuthority)
            {
                RPC_CompleteVoting(playerIndex);
                
                if (_confirmSFX != null) _confirmSFX.Play();
                
                // 投票完了処理（投票システムが管理）
                Debug.Log("[NameCrafter] Voting completed by local player");
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitVotingResults(int voterIndex, int[] allocations)
    {
        Debug.Log($"[NameCrafter] Voting results received from player {voterIndex}");
        
        var players = TriviaPlayer.TriviaPlayerRefs;
        
        // 配分データをネットワーク配列に保存
        for (int targetIndex = 0; targetIndex < allocations.Length && targetIndex < players.Count; targetIndex++)
        {
            int allocationArrayIndex = voterIndex * players.Count + targetIndex;
            if (allocationArrayIndex < PlayerVoteAllocations.Length)
            {
                PlayerVoteAllocations.Set(allocationArrayIndex, allocations[targetIndex]);
                
                if (allocations[targetIndex] > 0)
                {
                    Debug.Log($"[NameCrafter] Player {voterIndex} allocated {allocations[targetIndex]} points to player {targetIndex}");
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CompleteVoting(int playerIndex)
    {
        Debug.Log($"[NameCrafter] Voting completed by player {playerIndex}");
        // 投票完了の処理（必要に応じて実装）
    }

    #endregion

    #region Results Calculation

    private void CalculateVotingResults()
    {
        if (!HasStateAuthority) return;
        
        Debug.Log("[NameCrafter] Calculating voting results using ResultsCalculator");
        
        var players = TriviaPlayer.TriviaPlayerRefs;
        
        // 投票配分データを配列に変換
        var voteAllocations = new int[PlayerVoteAllocations.Length];
        for (int i = 0; i < PlayerVoteAllocations.Length; i++)
        {
            voteAllocations[i] = PlayerVoteAllocations[i];
        }
        
        // プレイヤー回答を配列に変換
        var playerAnswers = new string[players.Count];
        for (int i = 0; i < players.Count; i++)
        {
            playerAnswers[i] = i < PlayerAnswers.Length ? PlayerAnswers[i].Value : "";
        }
        
        // 未完了投票者の自動配分処理
        HandleIncompleteVoting(players, voteAllocations, playerAnswers);
        
        // 結果を計算
        var roundResult = NameCrafterResultsCalculator.CalculateNormalModeResults(
            players, voteAllocations, playerAnswers);
        
        // ネットワーク配列を更新
        for (int i = 0; i < players.Count && i < roundResult.playerResults.Count; i++)
        {
            var playerResult = roundResult.playerResults[i];
            PlayerRoundScores.Set(i, playerResult.roundScore);
            
            // 総スコアを更新
            int currentTotal = PlayerTotalScores[i];
            PlayerTotalScores.Set(i, currentTotal + playerResult.roundScore);
            
            Debug.Log($"[NameCrafter] Player {i} ({playerResult.playerName}) scored {playerResult.roundScore} points this round");
        }
    }

    private void CalculateSelectionResults()
    {
        if (!HasStateAuthority) return;
        
        Debug.Log("[NameCrafter] Calculating selection results using ResultsCalculator");
        
        var players = TriviaPlayer.TriviaPlayerRefs;
        
        // プレイヤー選択データを配列に変換
        var playerSelections = new int[players.Count];
        for (int i = 0; i < players.Count; i++)
        {
            playerSelections[i] = i < PlayerSelections.Length ? PlayerSelections[i] : -1;
        }
        
        // 選択肢を配列に変換
        var selectionOptions = new string[WordOptions.Length];
        for (int i = 0; i < WordOptions.Length; i++)
        {
            selectionOptions[i] = WordOptions[i].Value;
        }
        
        // 結果を計算
        var roundResult = NameCrafterResultsCalculator.CalculateSelectionModeResults(
            players, playerSelections, selectionOptions);
        
        // ネットワーク配列を更新
        for (int i = 0; i < players.Count && i < roundResult.playerResults.Count; i++)
        {
            var playerResult = roundResult.playerResults[i];
            PlayerVoteRates.Set(i, playerResult.matchRate);
            PlayerRoundScores.Set(i, playerResult.roundScore);
            
            Debug.Log($"[NameCrafter] Player {i} ({playerResult.playerName}) match rate: {playerResult.matchRate:F1}%");
        }
    }

    /// <summary>
    /// 未完了投票者への自動配分処理（仕様に基づく端数処理を含む）
    /// </summary>
    private void HandleIncompleteVoting(List<TriviaPlayer> players, int[] voteAllocations, string[] playerAnswers)
    {
        // 投票完了率をチェック
        int completedVoters = NameCrafterResultsCalculator.CalculateVotingCompletionRate(players, voteAllocations);
        int incompleteCount = players.Count - completedVoters;
        
        if (incompleteCount > 0)
        {
            Debug.Log($"[NameCrafter] {incompleteCount} players have incomplete voting, applying auto-distribution");
            
            // 未完了投票者と回答済みプレイヤーを特定
            var incompleteVoters = new List<int>();
            var answeredPlayers = new List<int>();
            
            int expectedPoints = (players.Count - 1) * 100;
            
            for (int voter = 0; voter < players.Count; voter++)
            {
                int totalAllocated = 0;
                for (int target = 0; target < players.Count; target++)
                {
                    if (voter == target) continue;
                    int allocationIndex = voter * players.Count + target;
                    if (allocationIndex < voteAllocations.Length)
                    {
                        totalAllocated += voteAllocations[allocationIndex];
                    }
                }
                
                if (totalAllocated < expectedPoints)
                {
                    incompleteVoters.Add(voter);
                }
                
                if (!string.IsNullOrEmpty(playerAnswers[voter]))
                {
                    answeredPlayers.Add(voter);
                }
            }
            
            // 自動配分を計算
            var autoDistribution = NameCrafterResultsCalculator.CalculateAutoDistribution(
                players, incompleteVoters, answeredPlayers);
            
            // 自動配分を適用
            foreach (var voterDistribution in autoDistribution)
            {
                int voterIndex = voterDistribution.Key;
                foreach (var targetAllocation in voterDistribution.Value)
                {
                    int targetIndex = targetAllocation.Key;
                    int points = targetAllocation.Value;
                    
                    int allocationIndex = voterIndex * players.Count + targetIndex;
                    if (allocationIndex < PlayerVoteAllocations.Length)
                    {
                        PlayerVoteAllocations.Set(allocationIndex, points);
                        voteAllocations[allocationIndex] = points; // ローカル配列も更新
                    }
                }
            }
        }
    }

    #endregion

    #region UI Update Methods

    private void OnNameCrafterGameStateChanged()
    {
        Debug.Log($"[NameCrafter] Game state changed to: {GameState}");
        
        switch (GameState)
        {
            case NameCrafterGameState.Intro:
                ShowIntro();
                break;
                
            case NameCrafterGameState.ModeSelection:
                ShowModeSelection();
                break;
                
            case NameCrafterGameState.WordSelection:
                ShowWordSelection();
                break;
                
            case NameCrafterGameState.AnswerCreation:
                ShowAnswerCreation();
                break;
                
            case NameCrafterGameState.Selection:
                ShowSelection();
                break;
                
            case NameCrafterGameState.Voting:
                ShowVoting();
                break;
                
            case NameCrafterGameState.Results:
                ShowResults();
                break;
                
            case NameCrafterGameState.GameOver:
                ShowGameOver();
                break;
        }
    }

    private void ShowIntro()
    {
        triviaMessage.text = "Name Crafterゲーム開始\nまもなくゲームが始まります";
        
        HideAllGameUI();
        if (endGameObject != null) endGameObject.Hide();
    }

    private void ShowModeSelection()
    {
        triviaMessage.text = "ゲームモードを選択してください";
        
        HideAllGameUI();
        if (gameModeSelectionUI != null) gameModeSelectionUI.SetActive(true);
        
        // ホストのみ選択可能
        bool canSelect = Runner.IsSharedModeMasterClient;
        if (normalModeButton != null) normalModeButton.interactable = canSelect;
        if (selectionModeButton != null) selectionModeButton.interactable = canSelect;
    }

    private void ShowWordSelection()
    {
        var players = TriviaPlayer.TriviaPlayerRefs;
        int currentChooser = WordChooserPlayers[CurrentWordChooserIndex];
        
        if (currentChooser < players.Count)
        {
            string chooserName = players[currentChooser].PlayerName.Value;
            string partOfSpeech = GetPartOfSpeechForChooser(CurrentWordChooserIndex);
            triviaMessage.text = $"{chooserName}さんが{partOfSpeech}を選択中...";
        }
        
        HideAllGameUI();
        if (wordSelectionUI != null) wordSelectionUI.SetActive(true);
        if (selectedWordsArea != null) selectedWordsArea.SetActive(true);
        
        UpdateWordSelectionUI();
        UpdateSelectedWordsDisplay();
    }

    private void ShowAnswerCreation()
    {
        triviaMessage.text = "選択された単語を使って名前を作成してください";
        
        HideAllGameUI();
        if (answerInputUI != null) answerInputUI.SetActive(true);
        if (selectedWordsArea != null) selectedWordsArea.SetActive(true);
        
        // 回答入力フィールドを有効化
        if (answerInputField != null)
        {
            answerInputField.gameObject.SetActive(true);
            answerInputField.text = "";
            answerInputField.interactable = true;
        }
        
        if (submitAnswerButton != null)
        {
            submitAnswerButton.gameObject.SetActive(true);
            submitAnswerButton.interactable = true;
        }
        
        UpdateSelectedWordsDisplay();
    }

    private void ShowSelection()
    {
        triviaMessage.text = "最適な名前を選択してください";
        
        HideAllGameUI();
        if (selectionUI != null) selectionUI.SetActive(true);
        if (selectedWordsArea != null) selectedWordsArea.SetActive(true);
        
        UpdateSelectionUI();
        UpdateSelectedWordsDisplay();
    }

    private void ShowVoting()
    {
        triviaMessage.text = "他のプレイヤーの作品に投票してください";
        
        HideAllGameUI();
        if (votingUI != null) votingUI.SetActive(true);
        
        SetupVotingUI();
    }

    private void ShowResults()
    {
        HideAllGameUI();
        if (resultsUI != null) resultsUI.SetActive(true);
        
        DisplayResults();
    }

    private void ShowGameOver()
    {
        triviaMessage.text = "ゲーム終了！";
        
        HideAllGameUI();
        
        // 最終結果表示
        if (endGameObject != null)
        {
            var players = TriviaPlayer.TriviaPlayerRefs;
            var sortedPlayers = players.OrderByDescending(p => PlayerTotalScores[players.IndexOf(p)]).ToList();
            var winners = sortedPlayers.Take(3).ToList();
            endGameObject.Show(winners);
        }
        
        // ゲーム制御ボタンを表示
        if (leaveGameBtn != null) leaveGameBtn.SetActive(true);
        if (startNewGameBtn != null) startNewGameBtn.SetActive(Runner.IsSharedModeMasterClient);
    }

    private void HideAllGameUI()
    {
        if (gameModeSelectionUI != null) gameModeSelectionUI.SetActive(false);
        if (wordSelectionUI != null) wordSelectionUI.SetActive(false);
        if (selectedWordsArea != null) selectedWordsArea.SetActive(false);
        if (answerInputUI != null) answerInputUI.SetActive(false);
        if (selectionUI != null) selectionUI.SetActive(false);
        if (votingUI != null) votingUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);
        if (questionElements != null) questionElements.SetActive(false);
    }

    private void UpdateWordSelectionUI()
    {
        var localPlayer = TriviaPlayer.LocalPlayer;
        bool canSelect = false;
        
        if (localPlayer != null)
        {
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            int currentChooser = WordChooserPlayers[CurrentWordChooserIndex];
            canSelect = (playerIndex == currentChooser);
        }
        
        // 単語選択ボタンの更新
        for (int i = 0; i < wordOptionButtons.Length; i++)
        {
            if (wordOptionButtons[i] != null)
            {
                wordOptionButtons[i].interactable = canSelect && !string.IsNullOrEmpty(WordOptions[i].Value);
            }
            
            if (wordOptionTexts[i] != null)
            {
                wordOptionTexts[i].text = WordOptions[i].Value;
            }
        }
        
        // 現在の選択者表示
        if (currentChooserText != null)
        {
            var players = TriviaPlayer.TriviaPlayerRefs;
            int currentChooser = WordChooserPlayers[CurrentWordChooserIndex];
            
            if (currentChooser < players.Count)
            {
                string chooserName = players[currentChooser].PlayerName.Value;
                string partOfSpeech = GetPartOfSpeechForChooser(CurrentWordChooserIndex);
                currentChooserText.text = $"{chooserName}が{partOfSpeech}を選択中";
            }
        }
    }

    private void UpdateSelectedWordsDisplay()
    {
        for (int i = 0; i < selectedWordTexts.Length && i < 3; i++)
        {
            if (selectedWordTexts[i] != null)
            {
                string word = SelectedWords[i].Value;
                selectedWordTexts[i].text = string.IsNullOrEmpty(word) ? "?" : word;
            }
        }
    }

    private void UpdateSelectionUI()
    {
        var localPlayer = TriviaPlayer.LocalPlayer;
        bool hasSelected = false;
        
        if (localPlayer != null)
        {
            int playerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
            hasSelected = PlayerSelections[playerIndex] >= 0;
        }
        
        // 選択肢ボタンの更新
        for (int i = 0; i < selectionOptionButtons.Length; i++)
        {
            if (selectionOptionButtons[i] != null)
            {
                selectionOptionButtons[i].interactable = !hasSelected && !string.IsNullOrEmpty(WordOptions[i].Value);
            }
            
            if (selectionOptionTexts[i] != null)
            {
                selectionOptionTexts[i].text = WordOptions[i].Value;
            }
        }
    }

    private void SetupVotingUI()
    {
        // 投票システムコンポーネントを取得または作成
        var votingSystem = GetComponent<NameCrafterVotingSystem>();
        if (votingSystem == null)
        {
            Debug.LogWarning("[NameCrafter] NameCrafterVotingSystem component not found");
            return;
        }
        
        // 投票対象データを準備
        var players = TriviaPlayer.TriviaPlayerRefs;
        var localPlayer = TriviaPlayer.LocalPlayer;
        int localPlayerIndex = localPlayer != null ? players.IndexOf(localPlayer) : -1;
        
        var playerAnswers = new Dictionary<int, string>();
        
        for (int i = 0; i < players.Count; i++)
        {
            if (i == localPlayerIndex) continue; // 自分は除外
            if (string.IsNullOrEmpty(PlayerAnswers[i].Value)) continue; // 未回答は除外
            
            playerAnswers[i] = PlayerAnswers[i].Value;
        }
        
        // 投票システムを初期化
        votingSystem.InitializeVoting(this, playerAnswers);
        
        Debug.Log($"[NameCrafter] Voting UI setup completed with {playerAnswers.Count} targets");
    }

    /// <summary>
    /// 投票フェーズでの完了ボタン処理
    /// </summary>
    public void OnVotingCompleted()
    {
        // 投票システムから呼ばれる完了処理
        Debug.Log("[NameCrafter] Voting phase completed by local player");
        
        // 必要に応じて追加の処理を実装
    }

    private void DisplayResults()
    {
        if (resultsText == null) return;
        
        string results = "";
        
        if (GameMode == NameCrafterGameMode.Normal)
        {
            results = DisplayVotingResults();
        }
        else
        {
            results = DisplaySelectionResults();
        }
        
        resultsText.text = results;
    }

    private string DisplayVotingResults()
    {
        var players = TriviaPlayer.TriviaPlayerRefs;
        var results = new System.Text.StringBuilder();
        
        results.AppendLine($"ラウンド {CurrentRound} 結果:");
        results.AppendLine();
        
        // プレイヤーを得点順にソート
        var playerScores = new List<(int index, int score, string name, string answer)>();
        
        for (int i = 0; i < players.Count; i++)
        {
            playerScores.Add((i, PlayerRoundScores[i], players[i].PlayerName.Value, PlayerAnswers[i].Value));
        }
        
        playerScores.Sort((a, b) => b.score.CompareTo(a.score));
        
        foreach (var (index, score, name, answer) in playerScores)
        {
            results.AppendLine($"{name}: {score}点");
            results.AppendLine($"「{answer}」");
            results.AppendLine();
        }
        
        return results.ToString();
    }

    private string DisplaySelectionResults()
    {
        var players = TriviaPlayer.TriviaPlayerRefs;
        var results = new System.Text.StringBuilder();
        
        results.AppendLine($"ラウンド {CurrentRound} 一致率:");
        results.AppendLine();
        
        for (int i = 0; i < players.Count; i++)
        {
            string playerName = players[i].PlayerName.Value;
            float matchRate = PlayerVoteRates[i];
            int selection = PlayerSelections[i];
            string selectedOption = selection >= 0 ? WordOptions[selection].Value : "未選択";
            
            results.AppendLine($"{playerName}: {matchRate:F1}%");
            results.AppendLine($"選択: 「{selectedOption}」");
            results.AppendLine();
        }
        
        return results.ToString();
    }

    /// <summary>
    /// プレイヤー数に応じた結果表示配置場所を取得
    /// </summary>
    /// <param name="playerCount">総プレイヤー数</param>
    /// <returns>配置場所のTransform配列</returns>
    private Transform[] GetResultPositions(int playerCount)
    {
        switch (playerCount)
        {
            case 2: return resultPositions2Players;
            case 3: return resultPositions3Players;
            case 4: return resultPositions4Players;
            case 5: return resultPositions5Players;
            case 6: return resultPositions6Players;
            case 7: return resultPositions7Players;
            case 8: return resultPositions8Players;
            default:
                Debug.LogWarning($"[NameCrafter] No result positions configured for {playerCount} players!");
                return new Transform[0];
        }
    }

    /// <summary>
    /// 指定された場所にプレイヤー結果UIを作成
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス</param>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="score">スコア</param>
    /// <param name="position">配置場所</param>
    private void CreatePlayerResultUI(int playerIndex, string playerName, int score, Transform position)
    {
        if (playerResultPrefab == null || position == null) return;
        
        GameObject resultObj = Instantiate(playerResultPrefab, position);
        
        // プレイヤー結果UIの設定（PlayerResultUIコンポーネントがある場合）
        var resultUI = resultObj.GetComponent<PlayerResultUI>();
        if (resultUI != null)
        {
            resultUI.SetPlayerResult(playerName, score);
        }
        else
        {
            // フォールバック：テキストコンポーネントを直接設定
            var textComponent = resultObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"{playerName}: {score}点";
            }
        }
        
        Debug.Log($"[NameCrafter] Created result UI for player {playerIndex} ({playerName}) at position {position.name}");
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

    private void UpdateGameMode()
    {
        Debug.Log($"[NameCrafter] Game mode updated: {GameMode}");
    }

    private void UpdateCurrentWordChooser()
    {
        Debug.Log($"[NameCrafter] Current word chooser updated: {CurrentWordChooserIndex}");
    }

    #endregion

    #region Game Control Methods

    /// <summary>
    /// ゲーム終了処理
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

        GameState = NameCrafterGameState.NewRound;
        CurrentRound = 0;

        // プレイヤーのスコアをリセット
        var players = TriviaPlayer.TriviaPlayerRefs;
        for (int i = 0; i < players.Count; i++)
        {
            players[i].Score = 0;
            PlayerTotalScores.Set(i, 0);
            PlayerRoundScores.Set(i, 0);
            PlayerVoteRates.Set(i, 0f);
        }

        // 初期タイマー設定
        timerLength = 3f;
        timer = TickTimer.CreateFromSeconds(Runner, timerLength);
    }

    public void StateAuthorityChanged()
    {
        if (GameState == NameCrafterGameState.GameOver)
        {
            if (startNewGameBtn != null)
                startNewGameBtn.SetActive(Runner.IsSharedModeMasterClient);
        }
    }

    #endregion
}
