using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 推理ゲーム専用のUI管理クラス
/// </summary>
public class DeductionGameUI : MonoBehaviour
{
    [Header("Main UI Panels")]
    [Tooltip("ゲーム全体のメインパネル")]
    public GameObject mainPanel;
    
    [Tooltip("回答フェーズのUI")]
    public GameObject answerPhasePanel;
    
    [Tooltip("投票フェーズのUI")]
    public GameObject votingPhasePanel;
    
    [Tooltip("結果表示のUI")]
    public GameObject resultsPanel;

    [Header("Game Info Display")]
    [Tooltip("お題表示エリア")]
    public GameObject topicDisplayArea;
    
    [Tooltip("ラウンド情報表示エリア")]
    public GameObject roundInfoArea;
    
    [Tooltip("プレイヤーリスト表示エリア")]
    public GameObject playerListArea;

    [Header("Interactive Elements")]
    [Tooltip("回答入力エリア")]
    public GameObject answerInputArea;
    
    [Tooltip("投票ボタンエリア")]
    public GameObject votingButtonArea;
    
    [Tooltip("ゲーム制御ボタンエリア")]
    public GameObject gameControlArea;

    /// <summary>
    /// UI初期化
    /// </summary>
    public void InitializeUI()
    {
        // 全パネルを非表示にする
        SetPanelActive(answerPhasePanel, false);
        SetPanelActive(votingPhasePanel, false);
        SetPanelActive(resultsPanel, false);
        
        // メインパネルのみ表示
        SetPanelActive(mainPanel, true);
        
        Debug.Log("DeductionGameUI initialized");
    }

    /// <summary>
    /// 回答フェーズのUI表示
    /// </summary>
    public void ShowAnswerPhaseUI()
    {
        SetPanelActive(answerPhasePanel, true);
        SetPanelActive(votingPhasePanel, false);
        SetPanelActive(resultsPanel, false);
    }

    /// <summary>
    /// 投票フェーズのUI表示
    /// </summary>
    public void ShowVotingPhaseUI()
    {
        SetPanelActive(answerPhasePanel, false);
        SetPanelActive(votingPhasePanel, true);
        SetPanelActive(resultsPanel, false);
    }

    /// <summary>
    /// 結果表示UI表示
    /// </summary>
    public void ShowResultsUI()
    {
        SetPanelActive(answerPhasePanel, false);
        SetPanelActive(votingPhasePanel, false);
        SetPanelActive(resultsPanel, true);
    }

    /// <summary>
    /// パネルのアクティブ状態を安全に設定
    /// </summary>
    /// <param name="panel">対象パネル</param>
    /// <param name="active">アクティブ状態</param>
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    /// <summary>
    /// すべてのUIを非表示
    /// </summary>
    public void HideAllUI()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(answerPhasePanel, false);
        SetPanelActive(votingPhasePanel, false);
        SetPanelActive(resultsPanel, false);
    }
} 