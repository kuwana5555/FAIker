using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AIプレイヤーの回答を生成するシステム
/// </summary>
public static class AIPlayerSystem
{
    // 各お題カテゴリごとの回答パターン
    private static readonly Dictionary<string, List<string>> answerPatterns = new Dictionary<string, List<string>>
    {
        ["好きな食べ物"] = new List<string>
        {
            "あんぱん", "あいすくりーむ", "あんみつ",
            "かれーらいす", "からあげ", "かつどん", "かき",
            "さしみ", "すし", "そば", "さらだ",
            "たまごやき", "てんぷら", "とんかつ", "たこやき",
            "なっとう", "にくじゃが", "なべ",
            "はんばーがー", "ひやしちゅうか", "ほっとけーき",
            "まぐろ", "みそしる", "もち",
            "やきにく", "やきとり", "ようかん",
            "らーめん", "りんご", "れもん",
            "わかめ", "わさび"
        },
        
        ["行きたい場所"] = new List<string>
        {
            "あめりか", "あきはばら", "あたみ",
            "かんこく", "きょうと", "くまもと", "かまくら",
            "さっぽろ", "しんじゅく", "しずおか", "さいたま",
            "とうきょう", "たいわん", "とちぎ",
            "なら", "にっこう", "なごや",
            "はわい", "ひろしま", "ほっかいどう",
            "まにら", "みやざき", "もなこ",
            "やまがた", "よこはま", "ゆふいん",
            "ろーま", "りょこう", "れきしはくぶつかん",
            "わかやま"
        },
        
        ["好きな動物"] = new List<string>
        {
            "あひる", "あざらし", "ありくい",
            "かんがるー", "きりん", "くま", "かえる",
            "さる", "しまうま", "すずめ", "さめ",
            "たぬき", "とら", "つる", "たこ",
            "ねこ", "にわとり", "のうさぎ",
            "はむすたー", "ひつじ", "ふくろう", "へび",
            "まんぼう", "みつばち", "もぐら",
            "やぎ", "やまあらし", "ゆきひょう",
            "らいおん", "りす", "れっさーぱんだ",
            "わに", "わしみずく"
        },
        
        ["趣味"] = new List<string>
        {
            "あにめ", "あーと", "あくせさりー",
            "かいが", "きゃんぷ", "くっきんぐ", "からおけ",
            "さんぽ", "しゃしん", "すぽーつ", "そうじ",
            "たびこう", "つり", "てにす", "とれーにんぐ",
            "なんぷ", "にゅーす", "のみもの",
            "はいきんぐ", "ひなたぼっこ", "ふぁっしょん",
            "まんが", "みゅーじっく", "もでるがん",
            "やきゅう", "よみもの", "よが",
            "らんにんぐ", "りょうり", "れでぃんぐ",
            "わいん"
        },
        
        ["欲しいもの"] = new List<string>
        {
            "あくせさりー", "あいふぉん", "あーと",
            "くるま", "きーぼーど", "かめら", "かばん",
            "すまーとふぉん", "そふぁ", "すにーかー",
            "てれび", "つくえ", "とけい", "たぶれっと",
            "ねっくれす", "のーとぱそこん", "にんてんどー",
            "ひーたー", "ふぁっしょん", "ほん", "へっどふぉん",
            "まっく", "みらー", "もにたー",
            "やま", "ゆびわ", "よふく",
            "らじお", "りゅっく", "れいぞうこ",
            "わいん", "わーるどかっぷ"
        }
    };

    /// <summary>
    /// 指定されたお題と最初の文字でAI回答を生成
    /// </summary>
    /// <param name="topicText">お題</param>
    /// <param name="firstCharacter">最初の文字</param>
    /// <returns>AI回答</returns>
    public static string GenerateAIAnswer(string topicText, string firstCharacter)
    {
        // お題に対応する回答パターンを取得
        if (!answerPatterns.ContainsKey(topicText))
        {
            Debug.LogWarning($"No answer patterns found for topic: {topicText}");
            return firstCharacter + "んさー"; // デフォルト回答
        }

        List<string> patterns = answerPatterns[topicText];
        List<string> matchingAnswers = new List<string>();

        // 指定された最初の文字で始まる回答を検索
        foreach (string pattern in patterns)
        {
            if (pattern.StartsWith(firstCharacter))
            {
                matchingAnswers.Add(pattern);
            }
        }

        // マッチする回答がない場合は汎用的な回答を生成
        if (matchingAnswers.Count == 0)
        {
            return GenerateGenericAnswer(firstCharacter);
        }

        // ランダムに選択して返す
        int randomIndex = Random.Range(0, matchingAnswers.Count);
        return matchingAnswers[randomIndex];
    }

    /// <summary>
    /// 汎用的なAI回答を生成（パターンにマッチしない場合）
    /// </summary>
    /// <param name="firstCharacter">最初の文字</param>
    /// <returns>汎用AI回答</returns>
    private static string GenerateGenericAnswer(string firstCharacter)
    {
        // 汎用的な単語の後ろ部分
        string[] genericEndings = {
            "んど", "んぐ", "んた", "んせい", "んか", "んしょう",
            "いと", "いす", "いん", "いき", "いち", "いしょう",
            "うと", "うす", "うん", "うき", "うち", "うしょう",
            "えと", "えす", "えん", "えき", "えち", "えしょう",
            "おと", "おす", "おん", "おき", "おち", "おしょう"
        };

        string randomEnding = genericEndings[Random.Range(0, genericEndings.Length)];
        return firstCharacter + randomEnding;
    }

    /// <summary>
    /// 新しいお題カテゴリの回答パターンを追加
    /// </summary>
    /// <param name="topicText">お題テキスト</param>
    /// <param name="patterns">回答パターンのリスト</param>
    public static void AddAnswerPatterns(string topicText, List<string> patterns)
    {
        if (answerPatterns.ContainsKey(topicText))
        {
            answerPatterns[topicText].AddRange(patterns);
        }
        else
        {
            answerPatterns[topicText] = new List<string>(patterns);
        }
        
        Debug.Log($"Added {patterns.Count} answer patterns for topic: {topicText}");
    }

    /// <summary>
    /// 利用可能なお題カテゴリのリストを取得
    /// </summary>
    /// <returns>お題カテゴリのリスト</returns>
    public static List<string> GetAvailableTopics()
    {
        return new List<string>(answerPatterns.Keys);
    }
} 