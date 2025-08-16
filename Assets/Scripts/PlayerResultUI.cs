using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// プレイヤー結果表示UI管理クラス
/// 指定された場所に配置されるプレイヤー結果表示用
/// </summary>
public class PlayerResultUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("プレイヤー名表示")]
    public TextMeshProUGUI playerNameText;
    
    [Tooltip("スコア表示")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("詳細情報表示（ラウンドスコア、統計など）")]
    public TextMeshProUGUI detailsText;
    
    [Tooltip("順位表示")]
    public TextMeshProUGUI rankText;
    
    [Tooltip("背景画像（順位に応じて色変更可能）")]
    public Image backgroundImage;
    
    [Header("Rank Colors")]
    [Tooltip("1位の背景色")]
    public Color firstPlaceColor = Color.yellow;
    
    [Tooltip("2位の背景色")]
    public Color secondPlaceColor = new Color(0.8f, 0.8f, 0.8f); // シルバー
    
    [Tooltip("3位の背景色")]
    public Color thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f); // ブロンズ
    
    [Tooltip("その他の背景色")]
    public Color defaultColor = Color.white;

    /// <summary>
    /// プレイヤー結果を設定（基本版）
    /// </summary>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="score">スコア</param>
    public void SetPlayerResult(string playerName, int score)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;
        
        if (scoreText != null)
            scoreText.text = $"{score}点";
        
        Debug.Log($"[PlayerResultUI] Set basic result for {playerName}: {score} points");
    }

    /// <summary>
    /// プレイヤー結果を設定（詳細版）
    /// </summary>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="score">総スコア</param>
    /// <param name="rank">順位</param>
    /// <param name="details">詳細情報</param>
    public void SetPlayerResult(string playerName, int score, int rank, string details = "")
    {
        SetPlayerResult(playerName, score);
        
        if (rankText != null)
            rankText.text = $"{rank}位";
        
        if (detailsText != null && !string.IsNullOrEmpty(details))
            detailsText.text = details;
        
        // 順位に応じて背景色を変更
        SetRankColor(rank);
        
        Debug.Log($"[PlayerResultUI] Set detailed result for {playerName}: Rank {rank}, Score {score}");
    }

    /// <summary>
    /// Name Crafter専用の詳細結果設定
    /// </summary>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="totalScore">総スコア</param>
    /// <param name="averageScore">平均スコア</param>
    /// <param name="maxRoundScore">最高ラウンドスコア</param>
    /// <param name="finalRoundScore">最終ラウンドスコア</param>
    /// <param name="rank">順位</param>
    public void SetNameCrafterResult(string playerName, int totalScore, float averageScore, 
        int maxRoundScore, int finalRoundScore, int rank)
    {
        SetPlayerResult(playerName, totalScore, rank);
        
        // 詳細情報を構築
        var details = new System.Text.StringBuilder();
        details.AppendLine($"平均: {averageScore:F1}点");
        details.AppendLine($"最高: {maxRoundScore}点");
        details.AppendLine($"最終R: {finalRoundScore}点");
        
        if (detailsText != null)
            detailsText.text = details.ToString();
        
        Debug.Log($"[PlayerResultUI] Set Name Crafter result for {playerName}: Total {totalScore}, Avg {averageScore:F1}");
    }

    /// <summary>
    /// 選択モード専用の結果設定
    /// </summary>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="averageMatchRate">平均一致率</param>
    /// <param name="bestMatchRate">最高一致率</param>
    /// <param name="totalMatches">総一致数</param>
    public void SetSelectionModeResult(string playerName, float averageMatchRate, 
        float bestMatchRate, int totalMatches)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;
        
        if (scoreText != null)
            scoreText.text = $"{averageMatchRate:F1}%";
        
        // 詳細情報を構築
        var details = new System.Text.StringBuilder();
        details.AppendLine($"最高一致率: {bestMatchRate:F1}%");
        details.AppendLine($"総一致数: {totalMatches}");
        
        if (detailsText != null)
            detailsText.text = details.ToString();
        
        Debug.Log($"[PlayerResultUI] Set Selection Mode result for {playerName}: Avg {averageMatchRate:F1}%");
    }

    /// <summary>
    /// 順位に応じて背景色を設定
    /// </summary>
    /// <param name="rank">順位</param>
    private void SetRankColor(int rank)
    {
        if (backgroundImage == null) return;
        
        Color targetColor;
        switch (rank)
        {
            case 1:
                targetColor = firstPlaceColor;
                break;
            case 2:
                targetColor = secondPlaceColor;
                break;
            case 3:
                targetColor = thirdPlaceColor;
                break;
            default:
                targetColor = defaultColor;
                break;
        }
        
        backgroundImage.color = targetColor;
    }

    /// <summary>
    /// UI要素の表示/非表示を切り替え
    /// </summary>
    /// <param name="showDetails">詳細情報を表示するか</param>
    /// <param name="showRank">順位を表示するか</param>
    public void SetUIVisibility(bool showDetails = true, bool showRank = true)
    {
        if (detailsText != null)
            detailsText.gameObject.SetActive(showDetails);
        
        if (rankText != null)
            rankText.gameObject.SetActive(showRank);
    }

    /// <summary>
    /// アニメーション付きで結果を表示
    /// </summary>
    /// <param name="delay">表示開始の遅延時間</param>
    public void AnimateIn(float delay = 0f)
    {
        // 初期状態を設定
        transform.localScale = Vector3.zero;
        
        // アニメーション実行
        StartCoroutine(AnimateInCoroutine(delay));
    }

    private System.Collections.IEnumerator AnimateInCoroutine(float delay)
    {
        // 遅延
        yield return new UnityEngine.WaitForSeconds(delay);
        
        // スケールアニメーション
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // イージング（バウンス効果）
            float scale = Mathf.Sin(progress * Mathf.PI * 0.5f);
            if (progress > 0.7f)
            {
                scale = 1f + (1f - progress) * 0.2f; // 少しオーバーシュート
            }
            
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        transform.localScale = Vector3.one;
    }
}
