using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// 推理ゲーム用のお題データを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "DeductionTopicSet", menuName = "Game Data/Deduction Topic Set")]
public class DeductionTopicSet : ScriptableObject
{
    [SerializeField, Tooltip("推理ゲームで使用するお題のリスト")]
    private List<DeductionTopic> _topics;

    /// <summary>
    /// お題のリストを取得
    /// </summary>
    public ReadOnlyCollection<DeductionTopic> Topics => _topics.AsReadOnly();

    [System.Serializable]
    public class DeductionTopic
    {
        [Tooltip("お題のテキスト（例：「好きな食べ物」「行きたい場所」など）")]
        public string topicText;
        
        [Tooltip("このお題で使用可能な最初の文字のリスト")]
        public string[] availableFirstCharacters;
        
        [Tooltip("AIが生成しそうな回答の例（開発用参考）")]
        public string[] exampleAIAnswers;
    }

    /// <summary>
    /// ランダムにお題を選択
    /// </summary>
    /// <returns>選択されたお題</returns>
    public DeductionTopic GetRandomTopic()
    {
        if (_topics == null || _topics.Count == 0)
        {
            Debug.LogWarning("Topics list is empty!");
            return null;
        }
        
        int randomIndex = Random.Range(0, _topics.Count);
        return _topics[randomIndex];
    }

    /// <summary>
    /// 指定されたお題からランダムに最初の文字を選択
    /// </summary>
    /// <param name="topic">お題</param>
    /// <returns>選択された最初の文字</returns>
    public string GetRandomFirstCharacter(DeductionTopic topic)
    {
        if (topic == null || topic.availableFirstCharacters == null || topic.availableFirstCharacters.Length == 0)
        {
            Debug.LogWarning("No available first characters for this topic!");
            return "あ"; // デフォルト文字
        }
        
        int randomIndex = Random.Range(0, topic.availableFirstCharacters.Length);
        return topic.availableFirstCharacters[randomIndex];
    }

    /// <summary>
    /// エディタ用：デフォルトのお題を作成
    /// </summary>
    [ContextMenu("Create Default Topics")]
    public void CreateDefaultTopics()
    {
        _topics = new List<DeductionTopic>
        {
            new DeductionTopic
            {
                topicText = "好きな食べ物",
                availableFirstCharacters = new string[] { "あ", "か", "さ", "た", "な", "は", "ま", "や", "ら", "わ" },
                exampleAIAnswers = new string[] { "あんぱん", "かれーらいす", "さしみ", "たまごやき", "なっとう" }
            },
            new DeductionTopic
            {
                topicText = "行きたい場所",
                availableFirstCharacters = new string[] { "あ", "か", "さ", "た", "な", "は", "ま", "や", "ら", "わ" },
                exampleAIAnswers = new string[] { "あめりか", "かんこく", "さっぽろ", "とうきょう", "なら" }
            },
            new DeductionTopic
            {
                topicText = "好きな動物",
                availableFirstCharacters = new string[] { "あ", "か", "さ", "た", "な", "は", "ま", "や", "ら", "わ" },
                exampleAIAnswers = new string[] { "あひる", "かんがるー", "さる", "たぬき", "ねこ" }
            },
            new DeductionTopic
            {
                topicText = "趣味",
                availableFirstCharacters = new string[] { "あ", "か", "さ", "た", "な", "は", "ま", "や", "ら", "わ" },
                exampleAIAnswers = new string[] { "あにめ", "かいが", "さんぽ", "たびこう", "なんぷ" }
            },
            new DeductionTopic
            {
                topicText = "欲しいもの",
                availableFirstCharacters = new string[] { "あ", "か", "さ", "た", "な", "は", "ま", "や", "ら", "わ" },
                exampleAIAnswers = new string[] { "あくせさりー", "くるま", "すまーとふぉん", "てれび", "ねっくれす" }
            }
        };
        
        Debug.Log($"Created {_topics.Count} default topics");
    }
} 