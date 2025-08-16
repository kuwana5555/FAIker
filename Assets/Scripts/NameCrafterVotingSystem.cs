using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Name Crafter用点数配分制投票システム
/// 持ち点 (n-1) × 100 を他プレイヤーに配分する仕組み
/// </summary>
public class NameCrafterVotingSystem : MonoBehaviour
{
    [Header("Voting UI References")]
    [Tooltip("投票対象UIのプレハブ")]
    public GameObject votingTargetPrefab;
    
    [Header("Player-Specific Voting Target Positions")]
    [Tooltip("2プレイヤー時の投票対象配置場所（1つ：相手プレイヤー用）")]
    public Transform[] votingPositions2Players = new Transform[1];
    
    [Tooltip("3プレイヤー時の投票対象配置場所（2つ：他プレイヤー用）")]
    public Transform[] votingPositions3Players = new Transform[2];
    
    [Tooltip("4プレイヤー時の投票対象配置場所（3つ：他プレイヤー用）")]
    public Transform[] votingPositions4Players = new Transform[3];
    
    [Tooltip("5プレイヤー時の投票対象配置場所（4つ：他プレイヤー用）")]
    public Transform[] votingPositions5Players = new Transform[4];
    
    [Tooltip("6プレイヤー時の投票対象配置場所（5つ：他プレイヤー用）")]
    public Transform[] votingPositions6Players = new Transform[5];
    
    [Tooltip("7プレイヤー時の投票対象配置場所（6つ：他プレイヤー用）")]
    public Transform[] votingPositions7Players = new Transform[6];
    
    [Tooltip("8プレイヤー時の投票対象配置場所（7つ：他プレイヤー用）")]
    public Transform[] votingPositions8Players = new Transform[7];
    
    [Tooltip("残り持ち点表示")]
    public TextMeshProUGUI remainingPointsText;
    
    [Tooltip("総持ち点表示")]
    public TextMeshProUGUI totalPointsText;
    
    [Tooltip("投票完了ボタン")]
    public Button completeVotingButton;
    
    [Tooltip("投票状況表示")]
    public TextMeshProUGUI votingStatusText;
    
    [Header("Point Allocation Settings")]
    [Tooltip("一度に増減できる最小単位")]
    public int pointIncrement = 10;
    
    [Tooltip("一度に大きく増減する単位")]
    public int largePointIncrement = 50;
    
    [Tooltip("投票完了に必要な条件（全配分完了）")]
    public bool requireFullAllocation = true;

    // 内部状態
    private NameCrafterGameManager gameManager;
    private List<VotingTargetUI> votingTargets = new List<VotingTargetUI>();
    private Dictionary<int, int> playerAllocations = new Dictionary<int, int>();
    private int totalPoints = 0;
    private int remainingPoints = 0;
    private bool votingCompleted = false;

    /// <summary>
    /// 投票システムの初期化
    /// </summary>
    /// <param name="manager">ゲームマネージャーの参照</param>
    /// <param name="playerAnswers">プレイヤーの回答リスト</param>
    public void InitializeVoting(NameCrafterGameManager manager, Dictionary<int, string> playerAnswers)
    {
        gameManager = manager;
        votingCompleted = false;
        
        // 持ち点計算
        var players = TriviaPlayer.TriviaPlayerRefs;
        var localPlayer = TriviaPlayer.LocalPlayer;
        int localPlayerIndex = localPlayer != null ? players.IndexOf(localPlayer) : -1;
        
        totalPoints = (players.Count - 1) * 100; // (n-1) × 100
        remainingPoints = totalPoints;
        
        Debug.Log($"[VotingSystem] Initialized with {totalPoints} total points");
        
        // UI更新
        UpdatePointsDisplay();
        
        // 投票対象を作成
        CreateVotingTargets(playerAnswers, localPlayerIndex);
        
        // 投票完了ボタンの初期状態
        UpdateCompleteButton();
    }

    /// <summary>
    /// 投票対象UIを作成（プレイヤー数に応じて指定された場所に配置）
    /// </summary>
    private void CreateVotingTargets(Dictionary<int, string> playerAnswers, int localPlayerIndex)
    {
        // 既存の投票対象をクリア
        ClearVotingTargets();
        
        var players = TriviaPlayer.TriviaPlayerRefs;
        
        // 投票対象プレイヤーのリストを作成（自分以外の回答済みプレイヤー）
        var votingTargetPlayers = new List<(int playerIndex, string answer, string playerName)>();
        
        foreach (var kvp in playerAnswers)
        {
            int playerIndex = kvp.Key;
            string answer = kvp.Value;
            
            // 自分は除外
            if (playerIndex == localPlayerIndex) continue;
            
            // 未回答は除外
            if (string.IsNullOrEmpty(answer)) continue;
            
            votingTargetPlayers.Add((playerIndex, answer, players[playerIndex].PlayerName.Value));
        }
        
        // プレイヤー数に応じた配置場所を取得
        Transform[] positions = GetVotingPositions(players.Count);
        
        if (positions == null || positions.Length == 0)
        {
            Debug.LogError($"[VotingSystem] No voting positions configured for {players.Count} players!");
            return;
        }
        
        // 配置可能数をチェック
        int maxTargets = Mathf.Min(votingTargetPlayers.Count, positions.Length);
        
        if (votingTargetPlayers.Count > positions.Length)
        {
            Debug.LogWarning($"[VotingSystem] {votingTargetPlayers.Count} voting targets but only {positions.Length} positions available for {players.Count} players. Some targets will be skipped.");
        }
        
        // 各投票対象を指定された場所に配置
        for (int i = 0; i < maxTargets; i++)
        {
            var targetData = votingTargetPlayers[i];
            Transform position = positions[i];
            
            if (position == null)
            {
                Debug.LogWarning($"[VotingSystem] Voting position {i} is null for {players.Count} players!");
                continue;
            }
            
            CreateVotingTarget(targetData.playerIndex, targetData.answer, targetData.playerName, position);
        }
        
        Debug.Log($"[VotingSystem] Created {votingTargets.Count} voting targets for {players.Count} players at specified positions");
    }

    /// <summary>
    /// 個別の投票対象UIを作成（指定された場所に配置）
    /// </summary>
    private void CreateVotingTarget(int playerIndex, string answer, string playerName, Transform position)
    {
        if (votingTargetPrefab == null || position == null) return;
        
        GameObject targetObj = Instantiate(votingTargetPrefab, position);
        VotingTargetUI targetUI = targetObj.GetComponent<VotingTargetUI>();
        
        if (targetUI == null)
        {
            targetUI = targetObj.AddComponent<VotingTargetUI>();
        }
        
        targetUI.Initialize(playerIndex, playerName, answer, this);
        votingTargets.Add(targetUI);
        
        // 配分データ初期化
        playerAllocations[playerIndex] = 0;
        
        Debug.Log($"[VotingSystem] Created voting target for player {playerIndex} ({playerName}) at position {position.name}");
    }

    /// <summary>
    /// プレイヤー数に応じた投票対象配置場所を取得
    /// </summary>
    /// <param name="playerCount">総プレイヤー数</param>
    /// <returns>配置場所のTransform配列</returns>
    private Transform[] GetVotingPositions(int playerCount)
    {
        switch (playerCount)
        {
            case 2: return votingPositions2Players;
            case 3: return votingPositions3Players;
            case 4: return votingPositions4Players;
            case 5: return votingPositions5Players;
            case 6: return votingPositions6Players;
            case 7: return votingPositions7Players;
            case 8: return votingPositions8Players;
            default:
                Debug.LogWarning($"[VotingSystem] No voting positions configured for {playerCount} players!");
                return new Transform[0];
        }
    }

    /// <summary>
    /// 既存の投票対象をクリア
    /// </summary>
    private void ClearVotingTargets()
    {
        foreach (var target in votingTargets)
        {
            if (target != null && target.gameObject != null)
            {
                Destroy(target.gameObject);
            }
        }
        
        votingTargets.Clear();
        playerAllocations.Clear();
    }

    /// <summary>
    /// 指定プレイヤーへの配分を変更
    /// </summary>
    /// <param name="playerIndex">対象プレイヤーのインデックス</param>
    /// <param name="change">変更量（正数で増加、負数で減少）</param>
    /// <returns>実際に変更された量</returns>
    public int AllocatePoints(int playerIndex, int change)
    {
        if (votingCompleted) return 0;
        if (!playerAllocations.ContainsKey(playerIndex)) return 0;
        
        int currentAllocation = playerAllocations[playerIndex];
        int newAllocation = Mathf.Clamp(currentAllocation + change, 0, totalPoints);
        int actualChange = newAllocation - currentAllocation;
        
        // 残り持ち点をチェック
        if (remainingPoints - actualChange < 0)
        {
            // 残り持ち点を超える配分は不可
            actualChange = remainingPoints;
            newAllocation = currentAllocation + actualChange;
        }
        
        if (actualChange != 0)
        {
            playerAllocations[playerIndex] = newAllocation;
            remainingPoints -= actualChange;
            
            // UI更新
            UpdatePointsDisplay();
            UpdateVotingTargets();
            UpdateCompleteButton();
            
            Debug.Log($"[VotingSystem] Player {playerIndex} allocation changed by {actualChange} to {newAllocation}");
        }
        
        return actualChange;
    }

    /// <summary>
    /// 指定プレイヤーへの配分を直接設定
    /// </summary>
    /// <param name="playerIndex">対象プレイヤーのインデックス</param>
    /// <param name="targetPoints">設定したい配分点数</param>
    /// <returns>実際に設定された点数</returns>
    public int SetAllocation(int playerIndex, int targetPoints)
    {
        if (votingCompleted) return 0;
        if (!playerAllocations.ContainsKey(playerIndex)) return 0;
        
        int currentAllocation = playerAllocations[playerIndex];
        int change = targetPoints - currentAllocation;
        
        return AllocatePoints(playerIndex, change);
    }

    /// <summary>
    /// 全配分をリセット
    /// </summary>
    public void ResetAllAllocations()
    {
        if (votingCompleted) return;
        
        foreach (var playerIndex in playerAllocations.Keys.ToList())
        {
            playerAllocations[playerIndex] = 0;
        }
        
        remainingPoints = totalPoints;
        
        UpdatePointsDisplay();
        UpdateVotingTargets();
        UpdateCompleteButton();
        
        Debug.Log("[VotingSystem] All allocations reset");
    }

    /// <summary>
    /// 残り持ち点を均等配分
    /// </summary>
    public void DistributeRemainingEvenly()
    {
        if (votingCompleted || remainingPoints <= 0) return;
        
        int targetCount = playerAllocations.Count;
        if (targetCount == 0) return;
        
        int pointsPerTarget = remainingPoints / targetCount;
        int remainder = remainingPoints % targetCount;
        
        var playerIndices = playerAllocations.Keys.ToList();
        
        for (int i = 0; i < playerIndices.Count; i++)
        {
            int playerIndex = playerIndices[i];
            int extraPoint = (i < remainder) ? 1 : 0;
            int addPoints = pointsPerTarget + extraPoint;
            
            if (addPoints > 0)
            {
                AllocatePoints(playerIndex, addPoints);
            }
        }
        
        Debug.Log("[VotingSystem] Remaining points distributed evenly");
    }

    /// <summary>
    /// 投票完了処理
    /// </summary>
    public void CompleteVoting()
    {
        if (votingCompleted) return;
        
        if (requireFullAllocation && remainingPoints > 0)
        {
            Debug.LogWarning("[VotingSystem] Cannot complete voting: points remaining");
            return;
        }
        
        votingCompleted = true;
        
        // ゲームマネージャーに投票結果を送信
        if (gameManager != null)
        {
            var localPlayer = TriviaPlayer.LocalPlayer;
            if (localPlayer != null)
            {
                int localPlayerIndex = TriviaPlayer.TriviaPlayerRefs.IndexOf(localPlayer);
                gameManager.SubmitVotingResults(localPlayerIndex, playerAllocations);
            }
        }
        
        // UI更新
        UpdateCompleteButton();
        DisableAllVotingTargets();
        
        Debug.Log("[VotingSystem] Voting completed");
    }

    /// <summary>
    /// 持ち点表示を更新
    /// </summary>
    private void UpdatePointsDisplay()
    {
        if (remainingPointsText != null)
        {
            remainingPointsText.text = $"残り持ち点: {remainingPoints}";
        }
        
        if (totalPointsText != null)
        {
            totalPointsText.text = $"総持ち点: {totalPoints}";
        }
        
        if (votingStatusText != null)
        {
            int allocatedPoints = totalPoints - remainingPoints;
            float allocationRate = totalPoints > 0 ? (float)allocatedPoints / totalPoints * 100f : 0f;
            votingStatusText.text = $"配分済み: {allocatedPoints}/{totalPoints} ({allocationRate:F1}%)";
        }
    }

    /// <summary>
    /// 投票対象UIを更新
    /// </summary>
    private void UpdateVotingTargets()
    {
        foreach (var target in votingTargets)
        {
            if (target != null)
            {
                target.UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// 投票完了ボタンの状態を更新
    /// </summary>
    private void UpdateCompleteButton()
    {
        if (completeVotingButton != null)
        {
            bool canComplete = !votingCompleted && (!requireFullAllocation || remainingPoints == 0);
            completeVotingButton.interactable = canComplete;
            
            // ボタンテキスト更新
            var buttonText = completeVotingButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (votingCompleted)
                {
                    buttonText.text = "投票完了済み";
                }
                else if (requireFullAllocation && remainingPoints > 0)
                {
                    buttonText.text = $"投票完了 ({remainingPoints}点残り)";
                }
                else
                {
                    buttonText.text = "投票完了";
                }
            }
        }
    }

    /// <summary>
    /// 全投票対象を無効化
    /// </summary>
    private void DisableAllVotingTargets()
    {
        foreach (var target in votingTargets)
        {
            if (target != null)
            {
                target.SetInteractable(false);
            }
        }
    }

    /// <summary>
    /// 現在の配分状況を取得
    /// </summary>
    public Dictionary<int, int> GetCurrentAllocations()
    {
        return new Dictionary<int, int>(playerAllocations);
    }

    /// <summary>
    /// 投票が完了しているかどうか
    /// </summary>
    public bool IsVotingCompleted()
    {
        return votingCompleted;
    }

    /// <summary>
    /// 残り持ち点を取得
    /// </summary>
    public int GetRemainingPoints()
    {
        return remainingPoints;
    }

    /// <summary>
    /// 指定プレイヤーの現在配分を取得
    /// </summary>
    public int GetPlayerAllocation(int playerIndex)
    {
        return playerAllocations.ContainsKey(playerIndex) ? playerAllocations[playerIndex] : 0;
    }
}

/// <summary>
/// 個別投票対象のUI管理クラス
/// </summary>
public class VotingTargetUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI answerText;
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
        targetPlayerIndex = playerIndex;
        votingSystem = system;

        // プレイヤー情報表示
        if (playerNameText != null)
            playerNameText.text = playerName;

        if (answerText != null)
            answerText.text = answer;

        // ボタンイベント設定
        SetupButtons();

        // 入力フィールド設定
        if (pointsInputField != null)
        {
            pointsInputField.onEndEdit.AddListener(OnPointsInputChanged);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// ボタンイベントの設定
    /// </summary>
    private void SetupButtons()
    {
        if (increaseSmallButton != null)
            increaseSmallButton.onClick.AddListener(() => ChangePoints(10));

        if (increaseLargeButton != null)
            increaseLargeButton.onClick.AddListener(() => ChangePoints(50));

        if (decreaseSmallButton != null)
            decreaseSmallButton.onClick.AddListener(() => ChangePoints(-10));

        if (decreaseLargeButton != null)
            decreaseLargeButton.onClick.AddListener(() => ChangePoints(-50));

        if (setZeroButton != null)
            setZeroButton.onClick.AddListener(() => SetPoints(0));

        if (setMaxButton != null)
            setMaxButton.onClick.AddListener(() => SetMaxPoints());
    }

    /// <summary>
    /// 配分点数の変更
    /// </summary>
    private void ChangePoints(int change)
    {
        if (votingSystem != null)
        {
            votingSystem.AllocatePoints(targetPlayerIndex, change);
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
