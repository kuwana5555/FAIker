using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 推理ゲーム用のプレイヤーデータ管理
/// </summary>
public class DeductionPlayer : NetworkBehaviour
{
    [Header("Player UI")]
    [Tooltip("プレイヤー名表示用テキスト")]
    public TextMeshProUGUI playerNameText;
    
    [Tooltip("スコア表示用テキスト")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("プレイヤーの背景画像")]
    public Image backgroundImage;
    
    [Tooltip("親プレイヤー表示用アイコン")]
    public GameObject parentPlayerIcon;
    
    [Tooltip("回答済み表示用アイコン")]
    public GameObject answeredIcon;
    
    [Tooltip("投票済み表示用アイコン")]
    public GameObject votedIcon;

    #region Network Properties
    
    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }
    
    [Networked, OnChangedRender(nameof(OnScoreChanged))]
    public int Score { get; set; }
    
    [Networked, OnChangedRender(nameof(OnAnswerStatusChanged))]
    public NetworkBool HasAnswered { get; set; }
    
    [Networked, OnChangedRender(nameof(OnVoteStatusChanged))]
    public NetworkBool HasVoted { get; set; }
    
    [Networked]
    public NetworkString<_64> CurrentAnswer { get; set; }
    
    [Networked]
    public int CurrentVote { get; set; } = -1;

    #endregion

    /// <summary>
    /// ローカルプレイヤーの参照
    /// </summary>
    public static DeductionPlayer LocalPlayer { get; private set; }
    
    /// <summary>
    /// 全プレイヤーのリスト
    /// </summary>
    public static List<DeductionPlayer> DeductionPlayerRefs { get; private set; } = new List<DeductionPlayer>();

    public override void Spawned()
    {
        base.Spawned();
        
        // プレイヤーリストに追加
        DeductionPlayerRefs.Add(this);
        
        // ローカルプレイヤーの設定
        if (Object.HasStateAuthority)
        {
            LocalPlayer = this;
            
            // FusionConnectorからプレイヤー名を取得
            if (FusionConnector.Instance != null)
            {
                string playerName = FusionConnector.Instance.LocalPlayerName;
                if (string.IsNullOrEmpty(playerName))
                {
                    PlayerName = $"Player {Object.StateAuthority.PlayerId}";
                }
                else
                {
                    PlayerName = playerName;
                }
            }
        }
        
        // UI初期化
        InitializeUI();
        
        // 状態更新
        OnPlayerNameChanged();
        OnScoreChanged();
        OnAnswerStatusChanged();
        OnVoteStatusChanged();
        
        Debug.Log($"DeductionPlayer spawned: {PlayerName.Value}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // プレイヤーリストから削除
        DeductionPlayerRefs.Remove(this);
        
        // ローカルプレイヤーの参照をクリア
        if (LocalPlayer == this)
        {
            LocalPlayer = null;
        }
        
        base.Despawned(runner, hasState);
    }

    private void InitializeUI()
    {
        // 初期状態の設定
        if (parentPlayerIcon != null)
            parentPlayerIcon.SetActive(false);
            
        if (answeredIcon != null)
            answeredIcon.SetActive(false);
            
        if (votedIcon != null)
            votedIcon.SetActive(false);
        
        // ローカルプレイヤーの背景を変更
        if (Object.HasStateAuthority && backgroundImage != null)
        {
            backgroundImage.color = new Color(0.8f, 1f, 0.8f, 1f); // 薄緑
        }
    }

    /// <summary>
    /// 回答を送信
    /// </summary>
    /// <param name="answer">回答内容</param>
    public void SubmitAnswer(string answer)
    {
        if (!Object.HasStateAuthority) return;
        
        CurrentAnswer = answer;
        HasAnswered = true;
        
        // ゲームマネージャーに回答を送信
        var gameManager = FindObjectOfType<DeductionGameManager>();
        if (gameManager != null)
        {
            int playerIndex = DeductionPlayerRefs.IndexOf(this);
            if (playerIndex >= 0)
            {
                gameManager.PlayerAnswers.Set(playerIndex, answer);
            }
        }
        
        Debug.Log($"Answer submitted: {answer}");
    }

    /// <summary>
    /// 投票を送信
    /// </summary>
    /// <param name="targetPlayerIndex">投票対象のプレイヤーインデックス</param>
    public void SubmitVote(int targetPlayerIndex)
    {
        if (!Object.HasStateAuthority) return;
        
        CurrentVote = targetPlayerIndex;
        HasVoted = true;
        
        // ゲームマネージャーに投票を送信
        var gameManager = FindObjectOfType<DeductionGameManager>();
        if (gameManager != null)
        {
            int playerIndex = DeductionPlayerRefs.IndexOf(this);
            if (playerIndex >= 0)
            {
                gameManager.PlayerVotes.Set(playerIndex, targetPlayerIndex);
            }
        }
        
        Debug.Log($"Vote submitted for player {targetPlayerIndex}");
    }

    /// <summary>
    /// スコアを追加
    /// </summary>
    /// <param name="points">追加するポイント</param>
    public void AddScore(int points)
    {
        if (!Object.HasStateAuthority) return;
        
        Score += points;
        Debug.Log($"Score added: {points}. Total: {Score}");
    }

    /// <summary>
    /// ラウンド開始時にリセット
    /// </summary>
    public void ResetForNewRound()
    {
        if (!Object.HasStateAuthority) return;
        
        HasAnswered = false;
        HasVoted = false;
        CurrentAnswer = "";
        CurrentVote = -1;
    }

    /// <summary>
    /// 親プレイヤーかどうかを判定
    /// </summary>
    /// <returns>親プレイヤーの場合true</returns>
    public bool IsParentPlayer()
    {
        var gameManager = FindObjectOfType<DeductionGameManager>();
        if (gameManager == null) return false;
        
        int playerIndex = DeductionPlayerRefs.IndexOf(this);
        return playerIndex == gameManager.ParentPlayerIndex;
    }

    #region UI Update Methods

    private void OnPlayerNameChanged()
    {
        if (playerNameText != null)
        {
            playerNameText.text = PlayerName.Value;
        }
    }

    private void OnScoreChanged()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{Score}点";
        }
    }

    private void OnAnswerStatusChanged()
    {
        if (answeredIcon != null)
        {
            answeredIcon.SetActive(HasAnswered);
        }
    }

    private void OnVoteStatusChanged()
    {
        if (votedIcon != null)
        {
            votedIcon.SetActive(HasVoted);
        }
    }

    #endregion

    /// <summary>
    /// 親プレイヤーアイコンの表示を更新
    /// </summary>
    /// <param name="isParent">親プレイヤーかどうか</param>
    public void UpdateParentPlayerDisplay(bool isParent)
    {
        if (parentPlayerIcon != null)
        {
            parentPlayerIcon.SetActive(isParent);
        }
        
        // 背景色も変更
        if (backgroundImage != null)
        {
            if (isParent)
            {
                backgroundImage.color = new Color(1f, 0.8f, 0.8f, 1f); // 薄赤（親プレイヤー）
            }
            else if (Object.HasStateAuthority)
            {
                backgroundImage.color = new Color(0.8f, 1f, 0.8f, 1f); // 薄緑（ローカルプレイヤー）
            }
            else
            {
                backgroundImage.color = Color.white; // 通常の色
            }
        }
    }

    private void Update()
    {
        // 親プレイヤー表示の更新（毎フレーム確認）
        bool isParent = IsParentPlayer();
        if (parentPlayerIcon != null && parentPlayerIcon.activeSelf != isParent)
        {
            UpdateParentPlayerDisplay(isParent);
        }
    }

    /// <summary>
    /// プレイヤーのインデックスを取得
    /// </summary>
    /// <returns>プレイヤーのインデックス</returns>
    public int GetPlayerIndex()
    {
        return DeductionPlayerRefs.IndexOf(this);
    }

    /// <summary>
    /// 指定されたインデックスのプレイヤーを取得
    /// </summary>
    /// <param name="index">プレイヤーインデックス</param>
    /// <returns>プレイヤーオブジェクト、存在しない場合はnull</returns>
    public static DeductionPlayer GetPlayerByIndex(int index)
    {
        if (index >= 0 && index < DeductionPlayerRefs.Count)
        {
            return DeductionPlayerRefs[index];
        }
        return null;
    }

    /// <summary>
    /// 全プレイヤーの回答状況をチェック
    /// </summary>
    /// <returns>全員が回答済みの場合true</returns>
    public static bool AllPlayersAnswered()
    {
        foreach (var player in DeductionPlayerRefs)
        {
            if (!player.HasAnswered)
                return false;
        }
        return DeductionPlayerRefs.Count > 0;
    }

    /// <summary>
    /// 全プレイヤーの投票状況をチェック
    /// </summary>
    /// <returns>全員が投票済みの場合true</returns>
    public static bool AllPlayersVoted()
    {
        foreach (var player in DeductionPlayerRefs)
        {
            if (!player.HasVoted)
                return false;
        }
        return DeductionPlayerRefs.Count > 0;
    }
} 