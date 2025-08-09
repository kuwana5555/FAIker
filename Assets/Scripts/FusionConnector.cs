using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    public Transform playerContainer;

    [Tooltip("The message shown before starting the game.")]
    public TextMeshProUGUI preGameMessage;
    
    [Header("Game Mode Selection")]
    [Tooltip("ゲームモード選択UI")]
    public GameObject gameModeSelectionUI;
    
    [Tooltip("ゲームモードセレクター")]
    public GameModeSelector gameModeSelector;

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
        if (TriviaManager.TriviaManagerPresent || DeductionGameManager.DeductionManagerPresent)
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
            (TriviaManager.TriviaManagerPresent || DeductionGameManager.DeductionManagerPresent))
        {
            Debug.Log("Game is already running.");
            return;
        }

        if (runner.IsSharedModeMasterClient)
        {
            // 選択されたゲームモードに応じて適切なプレハブをスポーン
            if (gameModeSelector != null)
            {
                switch (GameModeSelector.SelectedGameMode)
                {
                    case GameModeSelector.GameMode.Trivia:
                        if (triviaGamePrefab != null)
                        {
                            runner.Spawn(triviaGamePrefab);
                            Debug.Log("Trivia game started");
                        }
                        break;
                        
                    case GameModeSelector.GameMode.Deduction:
                        if (deductionGamePrefab != null)
                        {
                            runner.Spawn(deductionGamePrefab);
                            Debug.Log("Deduction game started");
                        }
                        break;
                }
            }
            else
            {
                // デフォルトはトリビアゲーム
                runner.Spawn(triviaGamePrefab);
            }
            
            showGameButton.SetActive(false);
        }
    }

    // 後方互換性のため古いメソッドも残しておく
    public void StartTriviaGame()
    {
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
}
