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
        
        // 投票完了ボタンのクリックイベント設定
        SetupCompleteButton();
        
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
        
        // 🎲 投票対象をランダムにシャッフル（公平性のため）
        ShuffleVotingTargets(votingTargetPlayers);
        
        Debug.Log("[VotingSystem] Voting targets shuffled for fairness");
        
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
        if (votingTargetPrefab == null)
        {
            Debug.LogError("[VotingSystem] Voting target prefab is null! Cannot create voting targets.");
            return;
        }
        
        if (position == null)
        {
            Debug.LogError($"[VotingSystem] Position is null for player {playerIndex}! Cannot create voting target.");
            return;
        }
        
        GameObject targetObj = Instantiate(votingTargetPrefab, position);
        VotingTargetUI targetUI = targetObj.GetComponent<VotingTargetUI>();
        
        if (targetUI == null)
        {
            Debug.LogWarning($"[VotingSystem] VotingTargetUI component not found on prefab for player {playerIndex}. Adding component.");
            targetUI = targetObj.AddComponent<VotingTargetUI>();
        }
        
        targetUI.Initialize(playerIndex, playerName, answer, this);
        votingTargets.Add(targetUI);
        
        // 配分データ初期化
        playerAllocations[playerIndex] = 0;
        
        Debug.Log($"[VotingSystem] Created voting target for player {playerIndex} ({playerName}) at position {position.name}");
    }

    /// <summary>
    /// 投票対象リストをランダムにシャッフル（Fisher-Yates アルゴリズム）
    /// </summary>
    /// <param name="list">シャッフルする投票対象リスト</param>
    private void ShuffleVotingTargets(List<(int playerIndex, string answer, string playerName)> list)
    {
        Debug.Log($"[VotingSystem] Shuffling {list.Count} voting targets...");
        
        // シャッフル前の順序をログ出力
        for (int i = 0; i < list.Count; i++)
        {
            Debug.Log($"[VotingSystem] Before shuffle [{i}]: Player {list[i].playerIndex} - '{list[i].answer}'");
        }
        
        // Fisher-Yates シャッフル
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
        
        // シャッフル後の順序をログ出力
        Debug.Log("[VotingSystem] After shuffle:");
        for (int i = 0; i < list.Count; i++)
        {
            Debug.Log($"[VotingSystem] After shuffle [{i}]: Player {list[i].playerIndex} - '{list[i].answer}'");
        }
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
        Debug.Log($"[VotingSystem] AllocatePoints called: player {playerIndex}, change {change}");
        Debug.Log($"[VotingSystem] Current state - votingCompleted: {votingCompleted}, remainingPoints: {remainingPoints}");
        
        if (votingCompleted)
        {
            Debug.LogWarning("[VotingSystem] Voting already completed, cannot allocate points");
            return 0;
        }
        
        if (!playerAllocations.ContainsKey(playerIndex))
        {
            Debug.LogError($"[VotingSystem] Player {playerIndex} not found in allocations dictionary");
            return 0;
        }
        
        int currentAllocation = playerAllocations[playerIndex];
        int newAllocation = Mathf.Clamp(currentAllocation + change, 0, totalPoints);
        int actualChange = newAllocation - currentAllocation;
        
        Debug.Log($"[VotingSystem] Current allocation: {currentAllocation}, proposed: {newAllocation}, actual change: {actualChange}");
        
        // 残り持ち点をチェック
        if (remainingPoints - actualChange < 0)
        {
            // 残り持ち点を超える配分は不可
            actualChange = remainingPoints;
            newAllocation = currentAllocation + actualChange;
            Debug.Log($"[VotingSystem] Adjusted for remaining points - new allocation: {newAllocation}, actual change: {actualChange}");
        }
        
        if (actualChange != 0)
        {
            playerAllocations[playerIndex] = newAllocation;
            remainingPoints -= actualChange;
            
            Debug.Log($"[VotingSystem] ✅ Points allocated successfully! Player {playerIndex}: {currentAllocation} → {newAllocation}");
            Debug.Log($"[VotingSystem] Remaining points: {remainingPoints + actualChange} → {remainingPoints}");
            
            // UI更新
            UpdatePointsDisplay();
            UpdateVotingTargets();
            UpdateCompleteButton();
        }
        else
        {
            Debug.Log($"[VotingSystem] No change made for player {playerIndex}");
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
                gameManager.OnVotingCompleted(localPlayerIndex, playerAllocations);
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
    /// 投票完了ボタンのクリックイベントを設定
    /// </summary>
    private void SetupCompleteButton()
    {
        if (completeVotingButton != null)
        {
            // 既存のリスナーをクリア
            completeVotingButton.onClick.RemoveAllListeners();
            
            // 新しいリスナーを追加
            completeVotingButton.onClick.AddListener(CompleteVoting);
            
            Debug.Log("[VotingSystem] Complete button click event setup");
        }
        else
        {
            Debug.LogWarning("[VotingSystem] Complete voting button is null - cannot setup click event");
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


