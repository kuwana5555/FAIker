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
    
    [Tooltip("ゲームモード選択UI")]
    public GameObject gameModeSelectionUI;
    
    [Tooltip("現在選択されているゲームモードを表示するテキスト")]
    public TextMeshProUGUI selectedGameModeText;

    public enum GameMode
    {
        Trivia,
        Deduction
    }

    public static GameMode SelectedGameMode { get; private set; } = GameMode.Trivia;

    private void Start()
    {
        // ボタンのイベントを設定
        triviaGameButton.onClick.AddListener(() => SelectGameMode(GameMode.Trivia));
        deductionGameButton.onClick.AddListener(() => SelectGameMode(GameMode.Deduction));
        
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
        string modeText = SelectedGameMode == GameMode.Trivia ? "トリビアゲーム" : "推理ゲーム";
        selectedGameModeText.text = $"選択中: {modeText}";
        
        // ボタンの見た目を更新
        UpdateButtonAppearance();
    }

    private void UpdateButtonAppearance()
    {
        // 選択されているボタンをハイライト
        ColorBlock triviaColors = triviaGameButton.colors;
        ColorBlock deductionColors = deductionGameButton.colors;
        
        if (SelectedGameMode == GameMode.Trivia)
        {
            triviaColors.normalColor = Color.green;
            deductionColors.normalColor = Color.white;
        }
        else
        {
            triviaColors.normalColor = Color.white;
            deductionColors.normalColor = Color.green;
        }
        
        triviaGameButton.colors = triviaColors;
        deductionGameButton.colors = deductionColors;
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