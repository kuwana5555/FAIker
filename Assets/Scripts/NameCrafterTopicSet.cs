using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Name Crafterゲーム用のお題・単語データセット
/// DeductionTopicSetと同様の構造で単語カードを管理
/// </summary>
[CreateAssetMenu(fileName = "NameCrafterTopicSet", menuName = "Game Data/Name Crafter Topic Set")]
public class NameCrafterTopicSet : ScriptableObject
{
    [Header("Word Cards Database")]
    [Tooltip("形容詞の単語カード")]
    public List<WordCard> adjectives = new List<WordCard>();
    
    [Tooltip("名詞の単語カード")]
    public List<WordCard> nouns = new List<WordCard>();
    
    [Header("Selection Mode Options")]
    [Tooltip("選択モード用の名前候補")]
    public List<string> selectionOptions = new List<string>();
    
    [Header("Game Settings")]
    [Tooltip("1ラウンドで使用する単語数（デフォルト: 3）")]
    public int wordsPerRound = 3;
    
    [Tooltip("単語選択時の選択肢数（デフォルト: 4）")]
    public int optionsPerSelection = 4;

    /// <summary>
    /// 指定された品詞から指定数の単語選択肢を取得
    /// </summary>
    /// <param name="partOfSpeech">品詞（"形容詞" または "名詞"）</param>
    /// <param name="count">取得する単語数</param>
    /// <returns>単語のリスト</returns>
    public List<string> GetWordOptions(string partOfSpeech, int count)
    {
        List<WordCard> sourceList;
        
        switch (partOfSpeech)
        {
            case "形容詞":
                sourceList = adjectives;
                break;
            case "名詞":
                sourceList = nouns;
                break;
            default:
                Debug.LogWarning($"[NameCrafter] Unknown part of speech: {partOfSpeech}");
                return new List<string>();
        }
        
        if (sourceList.Count == 0)
        {
            Debug.LogWarning($"[NameCrafter] No words available for part of speech: {partOfSpeech}");
            return new List<string>();
        }
        
        // ランダムに選択
        var shuffled = sourceList.OrderBy(x => Random.value).Take(count).ToList();
        return shuffled.Select(card => card.word).ToList();
    }
    
    /// <summary>
    /// 選択モード用の選択肢を取得
    /// </summary>
    /// <param name="count">取得する選択肢数</param>
    /// <returns>選択肢のリスト</returns>
    public List<string> GetSelectionOptions(int count)
    {
        if (selectionOptions.Count == 0)
        {
            Debug.LogWarning("[NameCrafter] No selection options available");
            return new List<string>();
        }
        
        // ランダムに選択
        var shuffled = selectionOptions.OrderBy(x => Random.value).Take(count).ToList();
        return shuffled;
    }
    
    /// <summary>
    /// ランダムな形容詞を取得
    /// </summary>
    public WordCard GetRandomAdjective()
    {
        if (adjectives.Count == 0) return null;
        return adjectives[Random.Range(0, adjectives.Count)];
    }
    
    /// <summary>
    /// ランダムな名詞を取得
    /// </summary>
    public WordCard GetRandomNoun()
    {
        if (nouns.Count == 0) return null;
        return nouns[Random.Range(0, nouns.Count)];
    }
    
    /// <summary>
    /// デバッグ用：データセットの統計情報を表示
    /// </summary>
    [ContextMenu("Show Dataset Statistics")]
    public void ShowStatistics()
    {
        Debug.Log($"[NameCrafter] Dataset Statistics:");
        Debug.Log($"  Adjectives: {adjectives.Count}");
        Debug.Log($"  Nouns: {nouns.Count}");
        Debug.Log($"  Selection Options: {selectionOptions.Count}");
        Debug.Log($"  Total Word Cards: {adjectives.Count + nouns.Count}");
    }
    
    /// <summary>
    /// エディタ用：サンプルデータを生成
    /// </summary>
    [ContextMenu("Generate Sample Data")]
    public void GenerateSampleData()
    {
        #if UNITY_EDITOR
        // サンプル形容詞
        var sampleAdjectives = new string[]
        {
            "美しい", "強力な", "神秘的な", "古代の", "輝く", "暗黒の", "聖なる", "魔法の",
            "勇敢な", "優雅な", "恐ろしい", "巨大な", "小さな", "素早い", "賢い", "愚かな",
            "熱い", "冷たい", "明るい", "暗い", "新しい", "古い", "高い", "低い",
            "広い", "狭い", "深い", "浅い", "重い", "軽い", "硬い", "柔らかい"
        };
        
        // サンプル名詞
        var sampleNouns = new string[]
        {
            "剣", "盾", "王冠", "宝石", "ドラゴン", "騎士", "魔法使い", "城",
            "森", "山", "海", "星", "月", "太陽", "雲", "風",
            "花", "木", "鳥", "狼", "熊", "鷹", "蛇", "虎",
            "炎", "氷", "雷", "光", "闇", "水", "土", "空気"
        };
        
        // サンプル選択肢（完成した名前の例）
        var sampleSelections = new string[]
        {
            "美しき月の剣", "暗黒の炎龍", "聖なる光の盾", "古代の賢者",
            "輝く星の王冠", "神秘的な森の番人", "勇敢な雷の騎士", "優雅な花の妖精",
            "恐ろしい氷の魔王", "巨大な山の巨人", "素早い風の刃", "深き海の守護者",
            "明るい太陽の使者", "暗い闇の支配者", "新しい希望の光", "古い伝説の英雄"
        };
        
        // データをクリア
        adjectives.Clear();
        nouns.Clear();
        selectionOptions.Clear();
        
        // 形容詞を追加
        foreach (string adj in sampleAdjectives)
        {
            adjectives.Add(new WordCard
            {
                word = adj,
                category = "形容詞",
                description = $"{adj}な特徴を表す形容詞"
            });
        }
        
        // 名詞を追加
        foreach (string noun in sampleNouns)
        {
            nouns.Add(new WordCard
            {
                word = noun,
                category = "名詞",
                description = $"{noun}を表す名詞"
            });
        }
        
        // 選択肢を追加
        selectionOptions.AddRange(sampleSelections);
        
        Debug.Log("[NameCrafter] Sample data generated successfully!");
        
        // エディタでの変更を保存
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}

/// <summary>
/// 単語カードデータ
/// </summary>
[System.Serializable]
public class WordCard
{
    [Header("Basic Info")]
    [Tooltip("単語")]
    public string word = "";
    
    [Tooltip("品詞カテゴリ（形容詞、名詞など）")]
    public string category = "";
    
    [Tooltip("単語の説明・意味")]
    [TextArea(2, 4)]
    public string description = "";
    
    [Header("Game Properties")]
    [Tooltip("この単語の使用頻度重み（高いほど選ばれやすい）")]
    [Range(0.1f, 2.0f)]
    public float weight = 1.0f;
    
    [Tooltip("この単語の難易度レベル")]
    [Range(1, 5)]
    public int difficultyLevel = 1;
    
    [Header("Tags")]
    [Tooltip("単語に関連するタグ（ファンタジー、現代、自然など）")]
    public List<string> tags = new List<string>();
    
    /// <summary>
    /// この単語カードが有効かどうか
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(category);
    }
    
    /// <summary>
    /// 指定されたタグを含むかどうか
    /// </summary>
    public bool HasTag(string tag)
    {
        return tags.Contains(tag);
    }
    
    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    public override string ToString()
    {
        return $"{word} ({category})";
    }
}

/// <summary>
/// Name Crafterゲーム用のラウンド結果データ
/// </summary>
[System.Serializable]
public class NameCrafterRoundResult
{
    [Header("Round Info")]
    public int roundNumber;
    public NameCrafterGameManager.NameCrafterGameMode gameMode;
    
    [Header("Selected Words")]
    public List<string> selectedWords = new List<string>();
    
    [Header("Player Results")]
    public List<PlayerRoundResult> playerResults = new List<PlayerRoundResult>();
    
    [Header("Statistics")]
    public float averageScore;
    public int totalVotes;
    public string mostPopularAnswer;
    public float highestMatchRate; // 選択モード用
}

/// <summary>
/// プレイヤーのラウンド結果
/// </summary>
[System.Serializable]
public class PlayerRoundResult
{
    public string playerName;
    public string answer;           // 通常モード：作成した名前
    public int selectedOption;      // 選択モード：選択したオプション
    public int votesReceived;       // 通常モード：獲得票数
    public float matchRate;         // 選択モード：一致率
    public int roundScore;
    public bool wasPresent;         // そのラウンドに参加していたか
}

/// <summary>
/// ゲーム全体の統計データ
/// </summary>
[System.Serializable]
public class NameCrafterGameStats
{
    [Header("Game Info")]
    public int totalRounds;
    public NameCrafterGameManager.NameCrafterGameMode primaryMode;
    public System.DateTime gameStartTime;
    public float totalGameDuration;
    
    [Header("Player Stats")]
    public List<PlayerGameStats> playerStats = new List<PlayerGameStats>();
    
    [Header("Round History")]
    public List<NameCrafterRoundResult> roundResults = new List<NameCrafterRoundResult>();
}

/// <summary>
/// プレイヤーのゲーム全体統計
/// </summary>
[System.Serializable]
public class PlayerGameStats
{
    public string playerName;
    
    [Header("Scores")]
    public int totalScore;
    public float averageScore;
    public int maxRoundScore;
    public int finalRoundScore;
    
    [Header("Voting Stats (Normal Mode)")]
    public int totalVotesReceived;
    public float averageVoteRate;
    public int bestRoundVotes;
    
    [Header("Selection Stats (Selection Mode)")]
    public float averageMatchRate;
    public float bestMatchRate;
    public int totalMatches;
    
    [Header("Participation")]
    public int roundsParticipated;
    public int roundsWithAnswer;
    public float participationRate;
    
    /// <summary>
    /// 最終順位計算用の比較
    /// 仕様に基づく優先度：合計点 > 平均点 > 最大1R得点 > 最終R得点
    /// </summary>
    public int CompareTo(PlayerGameStats other)
    {
        // 1. 合計点
        if (totalScore != other.totalScore)
            return other.totalScore.CompareTo(totalScore);
        
        // 2. 平均点
        if (averageScore != other.averageScore)
            return other.averageScore.CompareTo(averageScore);
        
        // 3. 最大1R得点
        if (maxRoundScore != other.maxRoundScore)
            return other.maxRoundScore.CompareTo(maxRoundScore);
        
        // 4. 最終R得点
        if (finalRoundScore != other.finalRoundScore)
            return other.finalRoundScore.CompareTo(finalRoundScore);
        
        // すべて同じ場合は同順
        return 0;
    }
}


