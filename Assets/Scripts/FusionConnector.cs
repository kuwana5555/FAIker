using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// デフォルトゲームモード（GameModeSelectorがない場合に使用）
/// </summary>
public enum DefaultGameMode
{
    Trivia,
    Deduction,
    NameCrafter
}

public class FusionConnector : MonoBehaviour
{
    public string LocalPlayerName { get; set; }

    public string LocalRoomName { get; set; }

    [SerializeField, Tooltip("The network runner prefab that will be instantiated when looking starting the game.")]
    private NetworkRunner _networkRunnerPrefab;

    [Tooltip("The canvas group that handles interactivity for the game.")]
    public CanvasGroup canvasGroup;

    [Tooltip("The GameObject that contains the main menu.")]
    public GameObject mainMenuObject;

    [Tooltip("The Game Object that handles the game itself")]
    public GameObject mainGameObject;

    [Tooltip("GameObject that appears if there is a network error when trying to join a room.")]
    public GameObject errorMessageObject;

    [Tooltip("The GameObject that displays the button to start the game.")]
    public GameObject showGameButton;

    [Tooltip("Text object that displays the room name.")]
    public TextMeshProUGUI roomName;

    [Tooltip("Prefab for the trivia game itself.")]
    public NetworkObject triviaGamePrefab;
    
    [Tooltip("Prefab for the deduction game itself.")]
    public NetworkObject deductionGamePrefab;
    
    [Tooltip("Prefab for the name crafter game itself.")]
    public NetworkObject nameCrafterGamePrefab;

    public Transform playerContainer;

    [Tooltip("The message shown before starting the game.")]
    public TextMeshProUGUI preGameMessage;
    
    [Header("Game Mode Selection")]
    [Tooltip("ゲームモード選択UI")]
    public GameObject gameModeSelectionUI;
    
    [Tooltip("ゲームモードセレクター")]
    public GameModeSelector gameModeSelector;
    
    [Header("Default Game Mode (when GameModeSelector is not available)")]
    [Tooltip("GameModeSelectorがない場合のデフォルトゲームモード")]
    public DefaultGameMode defaultGameMode = DefaultGameMode.Trivia;
    
    [Tooltip("GameModeSelectorの存在に関係なく、常にデフォルトモードを使用する")]
    public bool forceDefaultMode = false;

    public static FusionConnector Instance { get; private set; }

    private void Awake()
    {
        Application.targetFrameRate = 60;

        if (Instance != null)
        {
            Destroy(gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public async void StartGame(bool joinRandomRoom)
    {
        canvasGroup.interactable = false;

        StartGameArgs startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = joinRandomRoom ? string.Empty : LocalRoomName,
            PlayerCount = 20,
        };

        NetworkRunner newRunner = Instantiate(_networkRunnerPrefab);

        StartGameResult result = await newRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            roomName.text = "Room:  " + newRunner.SessionInfo.Name;

            GoToGame();
        }
        else
        {
            roomName.text = string.Empty;

            GoToMainMenu();
            
            errorMessageObject.SetActive(true);
            TextMeshProUGUI gui = errorMessageObject.GetComponentInChildren<TextMeshProUGUI>();
            if (gui)
                gui.text = result.ErrorMessage;

            Debug.LogError(result.ErrorMessage);
        }

        canvasGroup.interactable = true;
    }

    public void GoToMainMenu()
    {
        mainMenuObject.SetActive(true);
        mainGameObject.SetActive(false);
        
        // ゲームモード選択UIを表示
        if (gameModeSelectionUI != null)
        {
            gameModeSelectionUI.SetActive(true);
        }
    }

    public void GoToGame()
    {
        mainMenuObject.SetActive(false);
        mainGameObject.SetActive(true);
        
        // ゲームモード選択UIを非表示
        if (gameModeSelectionUI != null)
        {
            gameModeSelectionUI.SetActive(false);
        }
    }

    internal void OnPlayerJoin(NetworkRunner runner)
    {
        // ゲームが既に開始されている場合は何もしない
        if (TriviaManager.TriviaManagerPresent || 
            DeductionGameManager.DeductionManagerPresent || 
            NameCrafterGameManager.NameCrafterManagerPresent)
        {
            return;
        }

        if (runner.IsSharedModeMasterClient == true)
        {
            SetPregameMessage("Game Is Ready To Start");
        }
        else
        {
            SetPregameMessage("Waiting for master client to start game.");
        }
    }
    
    public void SetPregameMessage(string message)
    {
        preGameMessage.text = message;
    }

    public void StartSelectedGame()
    {
        NetworkRunner runner = null;
        // If no runner has been assigned, we cannot start the game
        if (NetworkRunner.Instances.Count > 0)
        {
            runner = NetworkRunner.Instances[0];
        }

        if (runner == null)
        {
            Debug.Log("No runner found.");
            return;
        }

        // ゲームが既に開始されている場合は何もしない
        if (runner.IsSharedModeMasterClient && 
            (TriviaManager.TriviaManagerPresent || 
             DeductionGameManager.DeductionManagerPresent || 
             NameCrafterGameManager.NameCrafterManagerPresent))
        {
            Debug.Log("Game is already running.");
            return;
        }

        if (runner.IsSharedModeMasterClient)
        {
            // デバッグ情報を追加
            Debug.Log($"StartSelectedGame called. GameModeSelector: {gameModeSelector != null}");
            Debug.Log($"ForceDefaultMode: {forceDefaultMode}");
            Debug.Log($"DefaultGameMode: {defaultGameMode}");
            Debug.Log($"TriviaGamePrefab assigned: {triviaGamePrefab != null}");
            Debug.Log($"DeductionGamePrefab assigned: {deductionGamePrefab != null}");
            Debug.Log($"NameCrafterGamePrefab assigned: {nameCrafterGamePrefab != null}");
            
            // ゲームモードの決定
            GameModeSelector.GameMode selectedMode;
            
            if (forceDefaultMode || gameModeSelector == null)
            {
                // デフォルトモードを使用
                selectedMode = ConvertDefaultToSelectorMode(defaultGameMode);
                Debug.Log($"Using default game mode: {selectedMode} (Default: {defaultGameMode})");
            }
            else
            {
                // GameModeSelectorから取得
                selectedMode = GameModeSelector.SelectedGameMode;
                Debug.Log($"Using selected game mode: {selectedMode}");
            }
            
            // 選択されたゲームモードに応じて適切なプレハブをスポーン
            switch (selectedMode)
            {
                case GameModeSelector.GameMode.Trivia:
                    if (triviaGamePrefab != null)
                    {
                        runner.Spawn(triviaGamePrefab);
                        Debug.Log("Trivia game started");
                    }
                    else
                    {
                        Debug.LogError("TriviaGamePrefab is not assigned!");
                    }
                    break;
                    
                case GameModeSelector.GameMode.Deduction:
                    if (deductionGamePrefab != null)
                    {
                        runner.Spawn(deductionGamePrefab);
                        Debug.Log("Deduction game started");
                    }
                    else
                    {
                        Debug.LogError("DeductionGamePrefab is not assigned!");
                    }
                    break;
                    
                case GameModeSelector.GameMode.NameCrafter:
                    if (nameCrafterGamePrefab != null)
                    {
                        runner.Spawn(nameCrafterGamePrefab);
                        Debug.Log("Name Crafter game started");
                    }
                    else
                    {
                        Debug.LogError("NameCrafterGamePrefab is not assigned!");
                    }
                    break;
            }
            
            showGameButton.SetActive(false);
        }
    }

    /// <summary>
    /// DefaultGameModeをGameModeSelector.GameModeに変換
    /// </summary>
    /// <param name="defaultMode">変換するDefaultGameMode</param>
    /// <returns>対応するGameModeSelector.GameMode</returns>
    private GameModeSelector.GameMode ConvertDefaultToSelectorMode(DefaultGameMode defaultMode)
    {
        switch (defaultMode)
        {
            case DefaultGameMode.Trivia:
                return GameModeSelector.GameMode.Trivia;
            case DefaultGameMode.Deduction:
                return GameModeSelector.GameMode.Deduction;
            case DefaultGameMode.NameCrafter:
                return GameModeSelector.GameMode.NameCrafter;
            default:
                Debug.LogWarning($"Unknown DefaultGameMode: {defaultMode}, falling back to Trivia");
                return GameModeSelector.GameMode.Trivia;
        }
    }

    // 後方互換性のため古いメソッドも残しておく（デバッグ用のログ追加）
    public void StartTriviaGame()
    {
        Debug.LogWarning("StartTriviaGame() is deprecated. Use StartSelectedGame() instead.");
        // GameModeSelectorを一時的にTriviaに設定
        if (gameModeSelector != null)
        {
            gameModeSelector.SelectGameMode(GameModeSelector.GameMode.Trivia);
        }
        StartSelectedGame();
    }

    /// <summary>
    /// 推理ゲームを開始
    /// </summary>
    public void StartDeductionGame()
    {
        if (gameModeSelector != null)
        {
            gameModeSelector.SelectGameMode(GameModeSelector.GameMode.Deduction);
        }
        StartSelectedGame();
    }

    /// <summary>
    /// Name Crafterゲームを開始
    /// </summary>
    public void StartNameCrafterGame()
    {
        if (gameModeSelector != null)
        {
            gameModeSelector.SelectGameMode(GameModeSelector.GameMode.NameCrafter);
        }
        StartSelectedGame();
    }

    /// <summary>
    /// 現在のゲームを終了してメインメニューに戻る
    /// </summary>
    public async void LeaveCurrentGame()
    {
        NetworkRunner runner = null;
        if (NetworkRunner.Instances.Count > 0)
        {
            runner = NetworkRunner.Instances[0];
        }

        if (runner != null)
        {
            await runner.Shutdown(true, ShutdownReason.Ok);
        }

        GoToMainMenu();
    }

    /// <summary>
    /// ゲームモード選択UIの表示/非表示を切り替え
    /// </summary>
    /// <param name="show">表示するかどうか</param>
    public void ShowGameModeSelection(bool show)
    {
        if (gameModeSelectionUI != null)
        {
            gameModeSelectionUI.SetActive(show);
        }
    }

    /// <summary>
    /// デフォルトゲームモードを設定
    /// </summary>
    /// <param name="mode">設定するデフォルトモード</param>
    public void SetDefaultGameMode(DefaultGameMode mode)
    {
        defaultGameMode = mode;
        Debug.Log($"Default game mode set to: {mode}");
    }

    /// <summary>
    /// 強制デフォルトモードの有効/無効を切り替え
    /// </summary>
    /// <param name="force">強制するかどうか</param>
    public void SetForceDefaultMode(bool force)
    {
        forceDefaultMode = force;
        Debug.Log($"Force default mode set to: {force}");
    }

    /// <summary>
    /// 現在のゲームモード設定を取得（デバッグ用）
    /// </summary>
    /// <returns>現在のゲームモード情報</returns>
    public string GetCurrentGameModeInfo()
    {
        if (forceDefaultMode)
        {
            return $"強制デフォルトモード: {defaultGameMode}";
        }
        else if (gameModeSelector == null)
        {
            return $"デフォルトモード（GameModeSelector不在）: {defaultGameMode}";
        }
        else
        {
            return $"GameModeSelector使用: {GameModeSelector.SelectedGameMode}";
        }
    }
}
