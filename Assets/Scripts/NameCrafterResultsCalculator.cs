using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Name Crafter用結果計算システム
/// 仕様に基づいた詳細なスコア計算とランキング処理
/// </summary>
public static class NameCrafterResultsCalculator
{
    /// <summary>
    /// 通常モードの投票結果を計算
    /// </summary>
    /// <param name="players">プレイヤーリスト</param>
    /// <param name="voteAllocations">投票配分データ</param>
    /// <param name="playerAnswers">プレイヤーの回答</param>
    /// <returns>ラウンド結果</returns>
    public static NameCrafterRoundResult CalculateNormalModeResults(
        List<TriviaPlayer> players,
        int[] voteAllocations,
        string[] playerAnswers)
    {
        var result = new NameCrafterRoundResult
        {
            gameMode = NameCrafterGameManager.NameCrafterGameMode.Normal,
            playerResults = new List<PlayerRoundResult>()
        };
        
        int totalVotes = 0;
        var playerScores = new Dictionary<int, int>();
        
        // 各プレイヤーの得票数を集計
        for (int targetPlayer = 0; targetPlayer < players.Count; targetPlayer++)
        {
            int votesReceived = 0;
            
            for (int voter = 0; voter < players.Count; voter++)
            {
                if (voter == targetPlayer) continue; // 自分には投票できない
                
                int allocationIndex = voter * players.Count + targetPlayer;
                if (allocationIndex < voteAllocations.Length)
                {
                    votesReceived += voteAllocations[allocationIndex];
                }
            }
            
            playerScores[targetPlayer] = votesReceived;
            totalVotes += votesReceived;
            
            // プレイヤー結果を作成
            var playerResult = new PlayerRoundResult
            {
                playerName = players[targetPlayer].PlayerName.Value,
                answer = targetPlayer < playerAnswers.Length ? playerAnswers[targetPlayer] : "",
                votesReceived = votesReceived,
                roundScore = votesReceived,
                wasPresent = true
            };
            
            result.playerResults.Add(playerResult);
        }
        
        // 統計情報を計算
        result.totalVotes = totalVotes;
        result.averageScore = players.Count > 0 ? (float)totalVotes / players.Count : 0f;
        
        // 最も人気のあった回答を特定
        if (playerScores.Count > 0)
        {
            int mostVotedPlayerIndex = playerScores.OrderByDescending(kvp => kvp.Value).First().Key;
            result.mostPopularAnswer = mostVotedPlayerIndex < playerAnswers.Length ? 
                playerAnswers[mostVotedPlayerIndex] : "";
        }
        
        Debug.Log($"[ResultsCalculator] Normal mode results calculated - Total votes: {totalVotes}");
        
        return result;
    }
    
    /// <summary>
    /// 選択モードの一致率結果を計算
    /// </summary>
    /// <param name="players">プレイヤーリスト</param>
    /// <param name="playerSelections">プレイヤーの選択</param>
    /// <param name="selectionOptions">選択肢</param>
    /// <returns>ラウンド結果</returns>
    public static NameCrafterRoundResult CalculateSelectionModeResults(
        List<TriviaPlayer> players,
        int[] playerSelections,
        string[] selectionOptions)
    {
        var result = new NameCrafterRoundResult
        {
            gameMode = NameCrafterGameManager.NameCrafterGameMode.Selection,
            playerResults = new List<PlayerRoundResult>()
        };
        
        float totalMatchRate = 0f;
        float highestMatchRate = 0f;
        
        // 各プレイヤーの一致率を計算
        for (int player1 = 0; player1 < players.Count; player1++)
        {
            int matchCount = 0;
            int totalComparisons = 0;
            
            for (int player2 = 0; player2 < players.Count; player2++)
            {
                if (player1 == player2) continue;
                
                // 両プレイヤーが選択済みの場合のみ比較
                if (player1 < playerSelections.Length && player2 < playerSelections.Length &&
                    playerSelections[player1] >= 0 && playerSelections[player2] >= 0)
                {
                    if (playerSelections[player1] == playerSelections[player2])
                    {
                        matchCount++;
                    }
                    totalComparisons++;
                }
            }
            
            // 一致率を計算（0-100%）
            float matchRate = totalComparisons > 0 ? (float)matchCount / totalComparisons * 100f : 0f;
            
            totalMatchRate += matchRate;
            if (matchRate > highestMatchRate)
            {
                highestMatchRate = matchRate;
            }
            
            // 選択した選択肢を特定
            string selectedOption = "";
            int selectedIndex = -1;
            if (player1 < playerSelections.Length && playerSelections[player1] >= 0)
            {
                selectedIndex = playerSelections[player1];
                if (selectedIndex < selectionOptions.Length)
                {
                    selectedOption = selectionOptions[selectedIndex];
                }
            }
            
            // プレイヤー結果を作成
            var playerResult = new PlayerRoundResult
            {
                playerName = players[player1].PlayerName.Value,
                answer = selectedOption,
                selectedOption = selectedIndex,
                matchRate = matchRate,
                roundScore = Mathf.RoundToInt(matchRate), // 一致率をスコアとして使用
                wasPresent = true
            };
            
            result.playerResults.Add(playerResult);
        }
        
        // 統計情報を計算
        result.averageScore = players.Count > 0 ? totalMatchRate / players.Count : 0f;
        result.highestMatchRate = highestMatchRate;
        
        Debug.Log($"[ResultsCalculator] Selection mode results calculated - Highest match rate: {highestMatchRate:F1}%");
        
        return result;
    }
    
    /// <summary>
    /// ゲーム全体の統計を計算
    /// </summary>
    /// <param name="players">プレイヤーリスト</param>
    /// <param name="roundResults">全ラウンドの結果</param>
    /// <param name="gameMode">主要ゲームモード</param>
    /// <returns>ゲーム統計</returns>
    public static NameCrafterGameStats CalculateGameStats(
        List<TriviaPlayer> players,
        List<NameCrafterRoundResult> roundResults,
        NameCrafterGameManager.NameCrafterGameMode gameMode)
    {
        var gameStats = new NameCrafterGameStats
        {
            totalRounds = roundResults.Count,
            primaryMode = gameMode,
            gameStartTime = System.DateTime.Now, // 実際の実装では開始時間を記録
            playerStats = new List<PlayerGameStats>(),
            roundResults = new List<NameCrafterRoundResult>(roundResults)
        };
        
        // 各プレイヤーの統計を計算
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            var playerStats = CalculatePlayerGameStats(players[playerIndex], roundResults, playerIndex);
            gameStats.playerStats.Add(playerStats);
        }
        
        Debug.Log($"[ResultsCalculator] Game stats calculated for {players.Count} players, {roundResults.Count} rounds");
        
        return gameStats;
    }
    
    /// <summary>
    /// 個別プレイヤーのゲーム統計を計算
    /// </summary>
    private static PlayerGameStats CalculatePlayerGameStats(
        TriviaPlayer player,
        List<NameCrafterRoundResult> roundResults,
        int playerIndex)
    {
        var stats = new PlayerGameStats
        {
            playerName = player.PlayerName.Value
        };
        
        var playerRoundResults = new List<PlayerRoundResult>();
        
        // 各ラウンドからプレイヤーの結果を収集
        foreach (var roundResult in roundResults)
        {
            var playerResult = roundResult.playerResults.FirstOrDefault(pr => 
                pr.playerName == player.PlayerName.Value);
            
            if (playerResult != null)
            {
                playerRoundResults.Add(playerResult);
            }
        }
        
        if (playerRoundResults.Count == 0)
        {
            return stats; // 参加記録なし
        }
        
        // 基本統計
        stats.roundsParticipated = playerRoundResults.Count;
        stats.roundsWithAnswer = playerRoundResults.Count(pr => !string.IsNullOrEmpty(pr.answer));
        stats.participationRate = roundResults.Count > 0 ? 
            (float)stats.roundsParticipated / roundResults.Count * 100f : 0f;
        
        // スコア統計
        var scores = playerRoundResults.Select(pr => pr.roundScore).ToList();
        stats.totalScore = scores.Sum();
        stats.averageScore = scores.Count > 0 ? (float)scores.Sum() / scores.Count : 0f;
        stats.maxRoundScore = scores.Count > 0 ? scores.Max() : 0;
        stats.finalRoundScore = scores.Count > 0 ? scores.Last() : 0;
        
        // 通常モード統計
        var normalModeResults = playerRoundResults.Where(pr => 
            roundResults.Any(rr => rr.gameMode == NameCrafterGameManager.NameCrafterGameMode.Normal && 
                             rr.playerResults.Contains(pr))).ToList();
        
        if (normalModeResults.Count > 0)
        {
            var votes = normalModeResults.Select(pr => pr.votesReceived).ToList();
            stats.totalVotesReceived = votes.Sum();
            stats.averageVoteRate = (float)votes.Sum() / votes.Count;
            stats.bestRoundVotes = votes.Max();
        }
        
        // 選択モード統計
        var selectionModeResults = playerRoundResults.Where(pr => 
            roundResults.Any(rr => rr.gameMode == NameCrafterGameManager.NameCrafterGameMode.Selection && 
                             rr.playerResults.Contains(pr))).ToList();
        
        if (selectionModeResults.Count > 0)
        {
            var matchRates = selectionModeResults.Select(pr => pr.matchRate).ToList();
            stats.averageMatchRate = matchRates.Average();
            stats.bestMatchRate = matchRates.Max();
            
            // 一致数の計算（簡略化）
            stats.totalMatches = Mathf.RoundToInt(matchRates.Sum() / 100f * (playerRoundResults.Count - 1));
        }
        
        return stats;
    }
    
    /// <summary>
    /// 最終ランキングを計算（仕様に基づく優先度）
    /// 1. 合計点 2. 平均点 3. 最大1R得点 4. 最終R得点
    /// </summary>
    /// <param name="playerStats">プレイヤー統計リスト</param>
    /// <returns>ランキング順のプレイヤー統計</returns>
    public static List<PlayerGameStats> CalculateFinalRanking(List<PlayerGameStats> playerStats)
    {
        return playerStats.OrderBy(stats => stats, new PlayerGameStatsComparer()).ToList();
    }
    
    /// <summary>
    /// 投票完了率を計算（未回答者への自動配分用）
    /// </summary>
    /// <param name="players">プレイヤーリスト</param>
    /// <param name="voteAllocations">投票配分データ</param>
    /// <returns>完了したプレイヤー数</returns>
    public static int CalculateVotingCompletionRate(List<TriviaPlayer> players, int[] voteAllocations)
    {
        int completedVoters = 0;
        int expectedPointsPerPlayer = (players.Count - 1) * 100;
        
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
            
            if (totalAllocated == expectedPointsPerPlayer)
            {
                completedVoters++;
            }
        }
        
        return completedVoters;
    }
    
    /// <summary>
    /// 未回答者への自動配分を計算（仕様の端数処理を含む）
    /// </summary>
    /// <param name="players">プレイヤーリスト</param>
    /// <param name="incompleteVoters">未完了投票者のインデックス</param>
    /// <param name="answeredPlayers">回答済みプレイヤーのインデックス</param>
    /// <returns>自動配分結果</returns>
    public static Dictionary<int, Dictionary<int, int>> CalculateAutoDistribution(
        List<TriviaPlayer> players,
        List<int> incompleteVoters,
        List<int> answeredPlayers)
    {
        var autoDistribution = new Dictionary<int, Dictionary<int, int>>();
        int totalPoints = (players.Count - 1) * 100;
        
        foreach (int voterIndex in incompleteVoters)
        {
            var distribution = new Dictionary<int, int>();
            
            // 自分以外の回答済みプレイヤーに均等配分
            var validTargets = answeredPlayers.Where(p => p != voterIndex).ToList();
            
            if (validTargets.Count > 0)
            {
                int pointsPerTarget = totalPoints / validTargets.Count;
                int remainder = totalPoints % validTargets.Count;
                
                for (int i = 0; i < validTargets.Count; i++)
                {
                    int targetPlayer = validTargets[i];
                    int allocatedPoints = pointsPerTarget;
                    
                    // 端数は切り捨て（仕様に基づく）
                    distribution[targetPlayer] = allocatedPoints;
                }
                
                Debug.Log($"[ResultsCalculator] Auto-distributed {totalPoints} points from player {voterIndex} " +
                         $"to {validTargets.Count} targets ({pointsPerTarget} each, {remainder} remainder discarded)");
            }
            
            autoDistribution[voterIndex] = distribution;
        }
        
        return autoDistribution;
    }
}

/// <summary>
/// プレイヤー統計の比較クラス（仕様に基づく順位付け）
/// </summary>
public class PlayerGameStatsComparer : IComparer<PlayerGameStats>
{
    public int Compare(PlayerGameStats x, PlayerGameStats y)
    {
        if (x == null || y == null)
            return 0;
        
        // 1. 合計点（降順）
        if (x.totalScore != y.totalScore)
            return y.totalScore.CompareTo(x.totalScore);
        
        // 2. 平均点（降順）
        if (x.averageScore != y.averageScore)
            return y.averageScore.CompareTo(x.averageScore);
        
        // 3. 最大1R得点（降順）
        if (x.maxRoundScore != y.maxRoundScore)
            return y.maxRoundScore.CompareTo(x.maxRoundScore);
        
        // 4. 最終R得点（降順）
        if (x.finalRoundScore != y.finalRoundScore)
            return y.finalRoundScore.CompareTo(x.finalRoundScore);
        
        // すべて同じ場合は同順
        return 0;
    }
}
