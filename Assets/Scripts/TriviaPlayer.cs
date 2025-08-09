using Fusion;
#if !UNITY_WEBGL
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
#endif
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// トリビアゲームと推理ゲーム両方に対応するプレイヤーシステム
/// </summary>
public class TriviaPlayer : NetworkBehaviour
{
    [Tooltip("The sprite used for every player but the local one.")]
    [SerializeField]
    Sprite _mainBackdrop;

    [Tooltip("The sprite used for the local player.")]
    [SerializeField]
    Sprite _localPlayerBackdrop;

    #region Network Properties
    [Tooltip("The name of the player")]
    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_16> PlayerName { get; set; }

    [Tooltip("Which character has the player chosen.")]
    [Networked, OnChangedRender(nameof(OnAvatarChanged))]
    public int ChosenAvatar { get; set; } = -1;

    [Tooltip("Which expression should the avatar be displaying now.")]
    [Networked, OnChangedRender(nameof(OnAvatarChanged))]
    public AvatarExpressions Expression { get; set; } = AvatarExpressions.Neutral;

    [Tooltip("What is the player's score.")]
    [Networked, OnChangedRender(nameof(OnScoreChanged))]
    public int Score { get; set; }

    [Tooltip("Score popup for visual feedback.")]
    [Networked, OnChangedRender(nameof(OnScorePopupChanged))]
    public TriviaScorePopUp ScorePopUp { get; set; }

    [Tooltip("The amount of points earned by answering the question quickly.")]
    [Networked]
    public int TimerBonusScore { get; set; }

    [Tooltip("If true, this player will be registered as the master client and displayed visually.")]
    [Networked, OnChangedRender(nameof(OnMasterClientChanged))]
    public NetworkBool IsMasterClient { get; set; }

    [Tooltip("Which answer did the player choose. For trivia: 0 is correct answer. For deduction: -1 = no answer, 1 = answered")]
    [Networked, OnChangedRender(nameof(OnAnswerChosen))]
    public int ChosenAnswer { get; set; } = -1;

#if !UNITY_WEBGL
    [Tooltip("If true, the local player is muted and will not transmit voice over the network.")]
    [Networked, OnChangedRender(nameof(OnMuteChanged))]
    public NetworkBool Muted { get; set; }
#endif

    // 推理ゲーム用の追加プロパティ
    [Tooltip("推理ゲームで回答済みかどうか")]
    [Networked, OnChangedRender(nameof(OnDeductionAnswerStatusChanged))]
    public NetworkBool HasDeductionAnswer { get; set; }

    [Tooltip("推理ゲームで投票済みかどうか")]
    [Networked, OnChangedRender(nameof(OnDeductionVoteStatusChanged))]
    public NetworkBool HasDeductionVote { get; set; }

    #endregion

    [Header("Player UI")]
    [Tooltip("Reference to the avatars a player can use.")]
    public Image avatarRenderer;

    [Tooltip("Reference to the avatar expressions.")]
    public GameObject[] facialExpressions;

    [Tooltip("The sprites used to render the character")]
    public Sprite[] avatarSprites;

    [Tooltip("The image used for the backdrop.")]
    public Image backdrop;

    [Tooltip("Reference to the name display object.")]
    public TextMeshProUGUI nameText;

    [Tooltip("Reference to the score display object.")]
    public TextMeshProUGUI scoreText;

    [Tooltip("Image that will turn on if the local player is the master client.")]
    public Image masterClientIcon;

#if !UNITY_WEBGL
    [Tooltip("Image toggled when the local player wants to mute their mic.")]
    public Image muteSpeakerIcon;

    [Tooltip("Image toggled when the a player is speaking or when the local player is recording.")]
    public Image speakingIcon;
#endif

    [Header("Deduction Game UI")]
    [Tooltip("親プレイヤー表示用アイコン")]
    public GameObject parentPlayerIcon;
    
    [Tooltip("回答済み表示用アイコン")]
    public GameObject answeredIcon;
    
    [Tooltip("投票済み表示用アイコン")]
    public GameObject votedIcon;

    [Tooltip("The sprites used to render the character")]
    public GameObject avatarSelectableSpriteGameObject;

    [SerializeField, Tooltip("Audio source that players when selecting an avatar.")]
    private AudioSource _clickLocalPlayerAudio;

    [SerializeField, Tooltip("Audio source that plays when trying to select another player's avatar.")]
    private AudioSource _clickRemotePlayerAudio;

    [SerializeField, Tooltip("Audio source that players when a new avatar is selected.")]
    private AudioSource _onChangeAvatarAudio;

    [SerializeField, Tooltip("Text pop up for when an answer has been answered.")]
    private TextMeshProUGUI _scorePopUpText;

    [SerializeField, Tooltip("Animator triggered when score is updated or an incorrect answer is given.")]
    private Animator _scorePopUpAnimator;

    /// <summary>
    /// Static reference to the local player
    /// </summary>
    public static TriviaPlayer LocalPlayer;

#if !UNITY_WEBGL
    [SerializeField, Tooltip("Reference to the voice network object that will show if a player is speaking or not.")]
    private VoiceNetworkObject _voiceNetworkObject;

    [SerializeField, Tooltip("Reference to the recorder for this player.")]
    private Recorder _recorder;
#endif

    /// <summary>
    /// A list of all players currently in the game.
    /// </summary>
    public static List<TriviaPlayer> TriviaPlayerRefs = new List<TriviaPlayer>();
    
    public enum AvatarExpressions
    {
        Neutral = 0,
        AnswerSelected = 1,
        Angry_WrongAnswer = 2,
        Happy_CorrectAnswer = 3,
    }

    /// <summary>
    /// When a character is spawned, we have to do the checks that a user would do in case someone spawns late.
    /// </summary>
    public override void Spawned()
    {
        base.Spawned();

        // Adds this player to a list of player refs and then sorts the order by index
        TriviaPlayerRefs.Add(this);
        TriviaPlayerRefs.Sort((x, y) => x.Object.StateAuthority.AsIndex - y.Object.StateAuthority.AsIndex);

        // The OnRenderChanged functions are called during spawn to make sure they are set properly for players who have already joined the room.
        OnAnswerChosen();
        OnScoreChanged();
        OnPlayerNameChanged();
        OnAvatarChanged();
        OnDeductionAnswerStatusChanged();
        OnDeductionVoteStatusChanged();

        // We assign the local test player a different sprite
        if (Object.HasStateAuthority == true)
        {
            backdrop.sprite = _localPlayerBackdrop;
            LocalPlayer = this;
        }
        else
        {
            backdrop.sprite = _mainBackdrop;
        }

        transform.SetParent(FusionConnector.Instance.playerContainer, false);

        // Sets the master client value on spawn
        if (HasStateAuthority)
        {
            IsMasterClient = Runner.IsSharedModeMasterClient;
        }
        masterClientIcon.enabled = IsMasterClient;

#if !UNITY_WEBGL
        OnMuteChanged();
#endif

        // Initialize deduction game UI
        InitializeDeductionUI();

        // Hides the avatar selector on spawn
        avatarSelectableSpriteGameObject.gameObject.SetActive(false);

        // We show the "Start Game Button" for the master client only, regardless of the number of players in the room.
        bool showGameButton = Runner.IsSharedModeMasterClient && 
                             !TriviaManager.TriviaManagerPresent && 
                             !DeductionGameManager.DeductionManagerPresent;
        FusionConnector.Instance.showGameButton.SetActive(showGameButton);
    }

    private void InitializeDeductionUI()
    {
        // 推理ゲーム用UI要素の初期化
        if (parentPlayerIcon != null) parentPlayerIcon.SetActive(false);
        if (answeredIcon != null) answeredIcon.SetActive(false);
        if (votedIcon != null) votedIcon.SetActive(false);
    }

    public void ShowDropdown()
    {
        if (HasStateAuthority)
        {
            avatarSelectableSpriteGameObject.SetActive(true);
            _clickLocalPlayerAudio.Play();
        }
        else
        {
            _clickRemotePlayerAudio.Play();
        }
    }

    void OnAnswerChosen()
    {
        if (HasStateAuthority)
        {
            if (ChosenAnswer >= 0)
            {
                Expression = AvatarExpressions.AnswerSelected;
            }
            else
            {
                Expression = AvatarExpressions.Neutral;
            }
        }
    }

    public void MakeAvatarSelection(Transform t)
    {
        if (HasStateAuthority)
        {
            ChosenAvatar = t.GetSiblingIndex();
            avatarSelectableSpriteGameObject.SetActive(false);

            _onChangeAvatarAudio.Play();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Removes the player from the list
        TriviaPlayerRefs.Remove(this);

        // Sets the local test play to null
        if (this == LocalPlayer)
            LocalPlayer = null;

        if (HasStateAuthority)
            IsMasterClient = runner.IsSharedModeMasterClient;

        bool showGameButton = Runner.IsSharedModeMasterClient && 
                             !TriviaManager.TriviaManagerPresent && 
                             !DeductionGameManager.DeductionManagerPresent;
        FusionConnector.Instance.showGameButton.SetActive(showGameButton);
    }

    void OnPlayerNameChanged()
    {
        nameText.text = PlayerName.Value;
    }

    void OnAvatarChanged()
    {
        // Sets which avatar face and expression to choose
        if (ChosenAvatar >= 0)
            avatarRenderer.sprite = avatarSprites[ChosenAvatar];
        else
            avatarRenderer.sprite = null;

        for (int i = 0; i < facialExpressions.Length; i++)
        {
            facialExpressions[i].SetActive(ChosenAvatar >= 0 && (int)Expression == i);
        }
    }

    void OnScoreChanged()
    {
        scoreText.text = Score.ToString();
    }

    void OnScorePopupChanged()
    {
        if (ScorePopUp.Score > 0)
        {
            _scorePopUpText.text = string.Format("+{0}", ScorePopUp.Score);
            _scorePopUpAnimator.SetTrigger("CorrectAnswer");
        }
        else
        {
            _scorePopUpText.text = "X";
            _scorePopUpAnimator.SetTrigger("WrongAnswer");
        }
    }

    void OnMasterClientChanged()
    {
        masterClientIcon.enabled = IsMasterClient;
    }

    #region 推理ゲーム用メソッド

    /// <summary>
    /// 推理ゲーム用：回答状態が変更された時の処理
    /// </summary>
    void OnDeductionAnswerStatusChanged()
    {
        if (answeredIcon != null)
        {
            answeredIcon.SetActive(HasDeductionAnswer);
        }
    }

    /// <summary>
    /// 推理ゲーム用：投票状態が変更された時の処理
    /// </summary>
    void OnDeductionVoteStatusChanged()
    {
        if (votedIcon != null)
        {
            votedIcon.SetActive(HasDeductionVote);
        }
    }

    /// <summary>
    /// 推理ゲーム用：親プレイヤーかどうかを判定
    /// </summary>
    /// <returns>親プレイヤーの場合true</returns>
    public bool IsParentPlayer()
    {
        var deductionManager = FindObjectOfType<DeductionGameManager>();
        if (deductionManager == null) return false;
        
        int playerIndex = TriviaPlayerRefs.IndexOf(this);
        return playerIndex == deductionManager.ParentPlayerIndex;
    }

    /// <summary>
    /// 推理ゲーム用：親プレイヤー表示の更新
    /// </summary>
    /// <param name="isParent">親プレイヤーかどうか</param>
    public void UpdateParentPlayerDisplay(bool isParent)
    {
        if (parentPlayerIcon != null)
        {
            parentPlayerIcon.SetActive(isParent);
        }
        
        // 背景色も変更
        if (backdrop != null)
        {
            if (isParent)
            {
                backdrop.color = new Color(1f, 0.8f, 0.8f, 1f); // 薄赤（親プレイヤー）
            }
            else if (Object.HasStateAuthority)
            {
                backdrop.color = new Color(0.8f, 1f, 0.8f, 1f); // 薄緑（ローカルプレイヤー）
            }
            else
            {
                backdrop.color = Color.white; // 通常の色
            }
        }
    }

    /// <summary>
    /// 推理ゲーム用：ラウンド開始時のリセット
    /// </summary>
    public void ResetForDeductionRound()
    {
        if (!HasStateAuthority) return;
        
        HasDeductionAnswer = false;
        HasDeductionVote = false;
        ChosenAnswer = -1; // 回答リセット
        Expression = AvatarExpressions.Neutral;
    }

    /// <summary>
    /// 推理ゲーム用：回答完了をマーク
    /// </summary>
    public void MarkDeductionAnswered()
    {
        if (!HasStateAuthority) return;
        
        HasDeductionAnswer = true;
        ChosenAnswer = 1; // 回答済みマーク
    }

    /// <summary>
    /// 推理ゲーム用：投票完了をマーク
    /// </summary>
    public void MarkDeductionVoted()
    {
        if (!HasStateAuthority) return;
        
        HasDeductionVote = true;
    }

    /// <summary>
    /// 推理ゲーム用：プレイヤーのインデックスを取得
    /// </summary>
    /// <returns>プレイヤーのインデックス</returns>
    public int GetPlayerIndex()
    {
        return TriviaPlayerRefs.IndexOf(this);
    }

    /// <summary>
    /// 推理ゲーム用：指定されたインデックスのプレイヤーを取得
    /// </summary>
    /// <param name="index">プレイヤーインデックス</param>
    /// <returns>プレイヤーオブジェクト、存在しない場合はnull</returns>
    public static TriviaPlayer GetPlayerByIndex(int index)
    {
        if (index >= 0 && index < TriviaPlayerRefs.Count)
        {
            return TriviaPlayerRefs[index];
        }
        return null;
    }

    /// <summary>
    /// 推理ゲーム用：全プレイヤーの回答状況をチェック
    /// </summary>
    /// <returns>全員が回答済みの場合true</returns>
    public static bool AllPlayersAnsweredDeduction()
    {
        foreach (var player in TriviaPlayerRefs)
        {
            if (!player.HasDeductionAnswer)
                return false;
        }
        return TriviaPlayerRefs.Count > 0;
    }

    /// <summary>
    /// 推理ゲーム用：全プレイヤーの投票状況をチェック
    /// </summary>
    /// <returns>全員が投票済みの場合true</returns>
    public static bool AllPlayersVotedDeduction()
    {
        foreach (var player in TriviaPlayerRefs)
        {
            if (!player.HasDeductionVote)
                return false;
        }
        return TriviaPlayerRefs.Count > 0;
    }

    #endregion

    private void Update()
    {
        // 推理ゲーム用：親プレイヤー表示の更新（毎フレーム確認）
        if (DeductionGameManager.DeductionManagerPresent)
        {
            bool isParent = IsParentPlayer();
            if (parentPlayerIcon != null && parentPlayerIcon.activeSelf != isParent)
            {
                UpdateParentPlayerDisplay(isParent);
            }
        }

#if !UNITY_WEBGL
        speakingIcon.enabled = (_voiceNetworkObject.SpeakerInUse && _voiceNetworkObject.IsSpeaking) || (_voiceNetworkObject.RecorderInUse && _voiceNetworkObject.IsRecording);
#endif
    }

#if !UNITY_WEBGL
    public void ToggleVoiceTransmission()
    {
        if (HasStateAuthority)
        {
            Muted = !Muted;
            _recorder.TransmitEnabled = !Muted;
        }
    }

    public void OnMuteChanged()
    {
        muteSpeakerIcon.enabled = Muted;
    }
#endif
}
