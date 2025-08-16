using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ゲームモードの選択を管理するクラス
/// </summary>
public class GameModeSelector : MonoBehaviour
{
    [Header("Game Mode Selection")]
    [Tooltip("トリビアゲームモードのボタン")]
    public Button triviaGameButton;
    
    [Tooltip("推理ゲームモードのボタン")]
    public Button deductionGameButton;
    
    [Tooltip("Name Crafterゲームモードのボタン")]
    public Button nameCrafterGameButton;
    
    [Tooltip("ゲームモード選択UI")]
    public GameObject gameModeSelectionUI;
    
    [Tooltip("現在選択されているゲームモードを表示するテキスト")]
    public TextMeshProUGUI selectedGameModeText;

    public enum GameMode
    {
        Trivia,
        Deduction,
        NameCrafter
    }

    public static GameMode SelectedGameMode { get; private set; } = GameMode.Trivia;

    private void Start()
    {
        // ボタンのイベントを設定
        if (triviaGameButton != null)
            triviaGameButton.onClick.AddListener(() => SelectGameMode(GameMode.Trivia));
        
        if (deductionGameButton != null)
            deductionGameButton.onClick.AddListener(() => SelectGameMode(GameMode.Deduction));
        
        if (nameCrafterGameButton != null)
            nameCrafterGameButton.onClick.AddListener(() => SelectGameMode(GameMode.NameCrafter));
        
        // 初期選択
        SelectGameMode(GameMode.Trivia);
    }

    public void SelectGameMode(GameMode mode)
    {
        SelectedGameMode = mode;
        
        // UI更新
        UpdateGameModeDisplay();
        
        Debug.Log($"Game mode selected: {mode}");
    }

    private void UpdateGameModeDisplay()
    {
        string modeText;
        switch (SelectedGameMode)
        {
            case GameMode.Trivia:
                modeText = "トリビアゲーム";
                break;
            case GameMode.Deduction:
                modeText = "推理ゲーム";
                break;
            case GameMode.NameCrafter:
                modeText = "Name Crafterゲーム";
                break;
            default:
                modeText = "不明なゲーム";
                break;
        }
        
        if (selectedGameModeText != null)
            selectedGameModeText.text = $"選択中: {modeText}";
        
        // ボタンの見た目を更新
        UpdateButtonAppearance();
    }

    private void UpdateButtonAppearance()
    {
        // 選択されているボタンをハイライト
        if (triviaGameButton != null)
        {
            ColorBlock triviaColors = triviaGameButton.colors;
            triviaColors.normalColor = (SelectedGameMode == GameMode.Trivia) ? Color.green : Color.white;
            triviaGameButton.colors = triviaColors;
        }
        
        if (deductionGameButton != null)
        {
            ColorBlock deductionColors = deductionGameButton.colors;
            deductionColors.normalColor = (SelectedGameMode == GameMode.Deduction) ? Color.green : Color.white;
            deductionGameButton.colors = deductionColors;
        }
        
        if (nameCrafterGameButton != null)
        {
            ColorBlock nameCrafterColors = nameCrafterGameButton.colors;
            nameCrafterColors.normalColor = (SelectedGameMode == GameMode.NameCrafter) ? Color.green : Color.white;
            nameCrafterGameButton.colors = nameCrafterColors;
        }
    }

    public void ShowGameModeSelection()
    {
        gameModeSelectionUI.SetActive(true);
    }

    public void HideGameModeSelection()
    {
        gameModeSelectionUI.SetActive(false);
    }
} 