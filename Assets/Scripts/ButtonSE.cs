using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンを押したらSEが鳴るスクリプト
/// インスペクター上でAudioClipを指定可能
/// </summary>
public class ButtonSE : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("ボタン押下時に再生するSE")]
    public AudioClip buttonSE;
    
    [Tooltip("SEの音量 (0.0 ~ 1.0)")]
    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;
    
    [Tooltip("SEのピッチ (0.5 ~ 2.0)")]
    [Range(0.5f, 2.0f)]
    public float pitch = 1.0f;
    
    [Header("Audio Source")]
    [Tooltip("AudioSourceコンポーネント（自動取得されます）")]
    public AudioSource audioSource;
    
    private Button button;
    
    private void Awake()
    {
        // Buttonコンポーネントを取得
        button = GetComponent<Button>();
        
        // AudioSourceコンポーネントを取得（なければ作成）
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // AudioSourceの設定
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
    }
    
    private void Start()
    {
        // ボタンのクリックイベントにSE再生を追加
        if (button != null)
        {
            button.onClick.AddListener(PlayButtonSE);
        }
        else
        {
            Debug.LogWarning("ButtonSE: Button component not found on " + gameObject.name);
        }
    }
    
    /// <summary>
    /// ボタンSEを再生
    /// </summary>
    public void PlayButtonSE()
    {
        if (buttonSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonSE, volume);
        }
        else if (buttonSE == null)
        {
            Debug.LogWarning("ButtonSE: No audio clip assigned on " + gameObject.name);
        }
    }
    
    /// <summary>
    /// 音量を動的に変更
    /// </summary>
    /// <param name="newVolume">新しい音量 (0.0 ~ 1.0)</param>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    /// <summary>
    /// ピッチを動的に変更
    /// </summary>
    /// <param name="newPitch">新しいピッチ (0.5 ~ 2.0)</param>
    public void SetPitch(float newPitch)
    {
        pitch = Mathf.Clamp(newPitch, 0.5f, 2.0f);
        if (audioSource != null)
        {
            audioSource.pitch = pitch;
        }
    }
    
    /// <summary>
    /// 新しいSEを設定
    /// </summary>
    /// <param name="newSE">新しいAudioClip</param>
    public void SetButtonSE(AudioClip newSE)
    {
        buttonSE = newSE;
    }
    
    private void OnDestroy()
    {
        // イベントリスナーの削除
        if (button != null)
        {
            button.onClick.RemoveListener(PlayButtonSE);
        }
    }
}
