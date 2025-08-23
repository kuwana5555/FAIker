using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 個別投票対象のUI管理クラス
/// Name Crafter用点数配分制投票システムの投票対象UI
/// </summary>
public class VotingTargetUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("プレイヤー名表示用（匿名性のため空文字を設定）")]
    public TextMeshProUGUI playerNameText;
    [Tooltip("プレイヤーの回答表示用")]
    public TextMeshProUGUI answerText;
    [Tooltip("現在の配分点数表示用")]
    public TextMeshProUGUI currentPointsText;
    public TMP_InputField pointsInputField;
    public Button increaseSmallButton;
    public Button increaseLargeButton;
    public Button decreaseSmallButton;
    public Button decreaseLargeButton;
    public Button setZeroButton;
    public Button setMaxButton;

    private int targetPlayerIndex;
    private NameCrafterVotingSystem votingSystem;

    /// <summary>
    /// 投票対象UIの初期化
    /// </summary>
    public void Initialize(int playerIndex, string playerName, string answer, NameCrafterVotingSystem system)
    {
        Debug.Log($"[VotingTargetUI] Initialize called for player {playerIndex} with answer '{answer}'");
        
        targetPlayerIndex = playerIndex;
        votingSystem = system;

        if (votingSystem == null)
        {
            Debug.LogError("[VotingTargetUI] VotingSystem is null during initialization!");
        }
        else
        {
            Debug.Log("[VotingTargetUI] VotingSystem reference set successfully");
        }

        // プレイヤー情報表示（匿名性のためプレイヤー名は表示しない）
        if (playerNameText != null)
            playerNameText.text = ""; // プレイヤー名は表示しない

        if (answerText != null)
        {
            answerText.text = answer; // 回答のみ表示
            Debug.Log($"[VotingTargetUI] Answer text set to: '{answer}'");
        }
        else
        {
            Debug.LogError("[VotingTargetUI] AnswerText is null!");
        }

        // ボタンイベント設定
        SetupButtons();

        // 入力フィールド設定
        if (pointsInputField != null)
        {
            pointsInputField.onEndEdit.RemoveAllListeners();
            pointsInputField.onEndEdit.AddListener((string value) => {
                Debug.Log($"[VotingTargetUI] 🔥 InputField CHANGED! Player {targetPlayerIndex}, Value: '{value}'");
                OnPointsInputChanged(value);
            });
            pointsInputField.onSelect.AddListener((string value) => {
                Debug.Log($"[VotingTargetUI] 🔥 InputField SELECTED! Player {targetPlayerIndex}");
            });
            Debug.Log("[VotingTargetUI] Points input field setup complete");
        }
        else
        {
            Debug.LogWarning("[VotingTargetUI] Points input field is null");
        }

        UpdateDisplay();
        
        // トラブルシューティング情報を出力
        PerformTroubleshooting();
        
        Debug.Log($"[VotingTargetUI] Initialize completed for player {playerIndex}");
    }
    
    /// <summary>
    /// トラブルシューティング情報の出力
    /// </summary>
    private void PerformTroubleshooting()
    {
        Debug.Log("=== VotingTargetUI Troubleshooting ===");
        
        // UI要素の存在確認
        Debug.Log($"UI Elements Status:");
        Debug.Log($"  AnswerText: {(answerText != null ? "✅" : "❌")}");
        Debug.Log($"  CurrentPointsText: {(currentPointsText != null ? "✅" : "❌")}");
        Debug.Log($"  PointsInputField: {(pointsInputField != null ? "✅" : "❌")}");
        Debug.Log($"  IncreaseSmallButton: {(increaseSmallButton != null ? "✅" : "❌")}");
        Debug.Log($"  IncreaseLargeButton: {(increaseLargeButton != null ? "✅" : "❌")}");
        Debug.Log($"  DecreaseSmallButton: {(decreaseSmallButton != null ? "✅" : "❌")}");
        Debug.Log($"  DecreaseLargeButton: {(decreaseLargeButton != null ? "✅" : "❌")}");
        Debug.Log($"  SetZeroButton: {(setZeroButton != null ? "✅" : "❌")}");
        Debug.Log($"  SetMaxButton: {(setMaxButton != null ? "✅" : "❌")}");
        
        // ボタンの状態確認
        if (increaseSmallButton != null)
        {
            Debug.Log($"IncreaseSmallButton Status:");
            Debug.Log($"  Interactable: {increaseSmallButton.interactable}");
            Debug.Log($"  GameObject Active: {increaseSmallButton.gameObject.activeInHierarchy}");
            Debug.Log($"  Component Enabled: {increaseSmallButton.enabled}");
        }
        
        // VotingSystem参照確認
        Debug.Log($"VotingSystem Reference: {(votingSystem != null ? "✅" : "❌")}");
        if (votingSystem != null)
        {
            Debug.Log($"  VotingSystem GameObject: {votingSystem.gameObject.name}");
        }
        
        // Canvas Group確認
        var canvasGroup = GetComponentInParent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"Canvas Group Found:");
            Debug.Log($"  Interactable: {canvasGroup.interactable}");
            Debug.Log($"  Blocks Raycasts: {canvasGroup.blocksRaycasts}");
            Debug.Log($"  Alpha: {canvasGroup.alpha}");
            
            if (!canvasGroup.interactable)
            {
                Debug.LogError("❌ Canvas Group is NOT interactable - this will block all clicks!");
            }
            if (!canvasGroup.blocksRaycasts)
            {
                Debug.LogError("❌ Canvas Group blocks raycasts is OFF - this will block all clicks!");
            }
        }
        else
        {
            Debug.Log("Canvas Group: Not Found");
        }
        
        // UI階層とRaycast問題の診断
        Debug.Log("=== UI Hierarchy Diagnosis ===");
        DiagnoseUIHierarchy();
        
        Debug.Log("=== End Troubleshooting ===");
    }
    
    /// <summary>
    /// UI階層とRaycast問題の診断
    /// </summary>
    private void DiagnoseUIHierarchy()
    {
        if (increaseSmallButton != null)
        {
            var button = increaseSmallButton;
            var rectTransform = button.GetComponent<RectTransform>();
            
            Debug.Log($"Button Hierarchy Diagnosis:");
            Debug.Log($"  Button Name: {button.gameObject.name}");
            Debug.Log($"  Parent: {(button.transform.parent != null ? button.transform.parent.name : "None")}");
            Debug.Log($"  Sibling Index: {button.transform.GetSiblingIndex()}");
            Debug.Log($"  Canvas: {FindCanvasInParents(button.transform)}");
            
            if (rectTransform != null)
            {
                Debug.Log($"  RectTransform Size: {rectTransform.rect.size}");
                Debug.Log($"  World Position: {rectTransform.position}");
            }
            
            // Raycast Target確認
            var graphic = button.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null)
            {
                Debug.Log($"  Raycast Target: {graphic.raycastTarget}");
            }
            
            // 上位階層のRaycast Targetを確認
            CheckParentRaycastTargets(button.transform);
        }
    }
    
    /// <summary>
    /// 親オブジェクトのRaycast Target設定を確認
    /// </summary>
    private void CheckParentRaycastTargets(Transform current)
    {
        Debug.Log("Parent Raycast Target Check:");
        
        Transform parent = current.parent;
        int level = 0;
        
        while (parent != null && level < 5) // 最大5階層まで確認
        {
            var graphic = parent.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null)
            {
                Debug.Log($"  Level {level} - {parent.name}: Raycast Target = {graphic.raycastTarget}");
                if (graphic.raycastTarget)
                {
                    Debug.LogWarning($"  ⚠️ Parent '{parent.name}' has Raycast Target enabled - this might block clicks!");
                }
            }
            
            parent = parent.parent;
            level++;
        }
    }
    
    /// <summary>
    /// Canvas情報を取得
    /// </summary>
    private string FindCanvasInParents(Transform current)
    {
        while (current != null)
        {
            var canvas = current.GetComponent<Canvas>();
            if (canvas != null)
            {
                return $"{current.name} (Sort Order: {canvas.sortingOrder})";
            }
            current = current.parent;
        }
        return "Not Found";
    }

    /// <summary>
    /// ボタンイベントの設定
    /// </summary>
    private void SetupButtons()
    {
        Debug.Log($"[VotingTargetUI] SetupButtons called for player {targetPlayerIndex}");
        
        if (increaseSmallButton != null)
        {
            increaseSmallButton.onClick.RemoveAllListeners();
            increaseSmallButton.onClick.AddListener(() => {
                Debug.Log($"[VotingTargetUI] 🔥 IncreaseSmallButton CLICKED! Player {targetPlayerIndex}");
                ChangePoints(10);
            });
            Debug.Log($"[VotingTargetUI] IncreaseSmallButton setup complete, interactable: {increaseSmallButton.interactable}");
        }
        else
        {
            Debug.LogError("[VotingTargetUI] IncreaseSmallButton is null!");
        }

        if (increaseLargeButton != null)
        {
            increaseLargeButton.onClick.RemoveAllListeners();
            increaseLargeButton.onClick.AddListener(() => {
                Debug.Log($"[VotingTargetUI] 🔥 IncreaseLargeButton CLICKED! Player {targetPlayerIndex}");
                ChangePoints(50);
            });
            Debug.Log($"[VotingTargetUI] IncreaseLargeButton setup complete, interactable: {increaseLargeButton.interactable}");
        }
        else
        {
            Debug.LogError("[VotingTargetUI] IncreaseLargeButton is null!");
        }

        if (decreaseSmallButton != null)
        {
            decreaseSmallButton.onClick.RemoveAllListeners();
            decreaseSmallButton.onClick.AddListener(() => ChangePoints(-10));
            Debug.Log($"[VotingTargetUI] DecreaseSmallButton setup complete, interactable: {decreaseSmallButton.interactable}");
        }
        else
        {
            Debug.LogError("[VotingTargetUI] DecreaseSmallButton is null!");
        }

        if (decreaseLargeButton != null)
        {
            decreaseLargeButton.onClick.RemoveAllListeners();
            decreaseLargeButton.onClick.AddListener(() => ChangePoints(-50));
            Debug.Log($"[VotingTargetUI] DecreaseLargeButton setup complete, interactable: {decreaseLargeButton.interactable}");
        }
        else
        {
            Debug.LogError("[VotingTargetUI] DecreaseLargeButton is null!");
        }

        if (setZeroButton != null)
        {
            setZeroButton.onClick.RemoveAllListeners();
            setZeroButton.onClick.AddListener(() => SetPoints(0));
            Debug.Log($"[VotingTargetUI] SetZeroButton setup complete, interactable: {setZeroButton.interactable}");
        }
        else
        {
            Debug.LogError("[VotingTargetUI] SetZeroButton is null!");
        }

        if (setMaxButton != null)
        {
            setMaxButton.onClick.RemoveAllListeners();
            setMaxButton.onClick.AddListener(() => SetMaxPoints());
            Debug.Log($"[VotingTargetUI] SetMaxButton setup complete, interactable: {setMaxButton.interactable}");
        }
        else
        {
            Debug.LogError("[VotingTargetUI] SetMaxButton is null!");
        }
    }

    /// <summary>
    /// 配分点数の変更
    /// </summary>
    private void ChangePoints(int change)
    {
        Debug.Log($"[VotingTargetUI] ChangePoints called: player {targetPlayerIndex}, change {change}");
        
        if (votingSystem != null)
        {
            Debug.Log($"[VotingTargetUI] VotingSystem is available, calling AllocatePoints");
            votingSystem.AllocatePoints(targetPlayerIndex, change);
        }
        else
        {
            Debug.LogError($"[VotingTargetUI] VotingSystem is null! Cannot change points for player {targetPlayerIndex}");
        }
    }

    /// <summary>
    /// 配分点数の直接設定
    /// </summary>
    private void SetPoints(int points)
    {
        if (votingSystem != null)
        {
            votingSystem.SetAllocation(targetPlayerIndex, points);
        }
    }

    /// <summary>
    /// 最大配分（残り持ち点全て）
    /// </summary>
    private void SetMaxPoints()
    {
        if (votingSystem != null)
        {
            int currentAllocation = votingSystem.GetPlayerAllocation(targetPlayerIndex);
            int remainingPoints = votingSystem.GetRemainingPoints();
            int maxPossible = currentAllocation + remainingPoints;
            
            votingSystem.SetAllocation(targetPlayerIndex, maxPossible);
        }
    }

    /// <summary>
    /// 入力フィールドからの点数変更
    /// </summary>
    private void OnPointsInputChanged(string input)
    {
        if (int.TryParse(input, out int targetPoints))
        {
            SetPoints(targetPoints);
        }
        else
        {
            // 無効な入力の場合は現在値に戻す
            UpdateDisplay();
        }
    }

    /// <summary>
    /// 表示の更新
    /// </summary>
    public void UpdateDisplay()
    {
        if (votingSystem == null) return;

        int currentPoints = votingSystem.GetPlayerAllocation(targetPlayerIndex);

        if (currentPointsText != null)
            currentPointsText.text = currentPoints.ToString();

        if (pointsInputField != null)
            pointsInputField.text = currentPoints.ToString();
    }

    /// <summary>
    /// UI要素の有効/無効切り替え
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (increaseSmallButton != null) increaseSmallButton.interactable = interactable;
        if (increaseLargeButton != null) increaseLargeButton.interactable = interactable;
        if (decreaseSmallButton != null) decreaseSmallButton.interactable = interactable;
        if (decreaseLargeButton != null) decreaseLargeButton.interactable = interactable;
        if (setZeroButton != null) setZeroButton.interactable = interactable;
        if (setMaxButton != null) setMaxButton.interactable = interactable;
        if (pointsInputField != null) pointsInputField.interactable = interactable;
    }
}
