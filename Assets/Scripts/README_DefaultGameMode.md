# FusionConnector デフォルトゲームモード機能

## 概要

FusionConnectorにデフォルトゲームモード機能を追加しました。これにより、GameModeSelectorが存在しないシーンでも特定のゲームモードを確定で使用できるようになります。

## 機能

### 1. デフォルトゲームモード設定
- **Default Game Mode**: GameModeSelectorがない場合に使用するゲームモード
- **Force Default Mode**: GameModeSelectorの存在に関係なく、常にデフォルトモードを使用

### 2. 利用可能なゲームモード
- **Trivia**: トリビアゲーム
- **Deduction**: 推理ゲーム  
- **NameCrafter**: Name Crafterゲーム

## Inspector設定

### FusionConnectorのInspector

```
[Header("Default Game Mode (when GameModeSelector is not available)")]
Default Game Mode: [ドロップダウン] (Trivia/Deduction/NameCrafter)
Force Default Mode: [チェックボックス]
```

## 使用パターン

### パターン1: 通常のゲームモード選択シーン
```
GameModeSelector: 存在する
Force Default Mode: false (無効)
→ プレイヤーがGameModeSelectorでモードを選択
```

### パターン2: Name Crafter確定シーン
```
GameModeSelector: 存在しない（またはあっても無視）
Default Game Mode: NameCrafter
Force Default Mode: true (有効)
→ 常にName Crafterが起動
```

### パターン3: 推理ゲーム専用シーン
```
GameModeSelector: 存在しない
Default Game Mode: Deduction  
Force Default Mode: false (無効)
→ GameModeSelectorがないため推理ゲームが起動
```

## セットアップ方法

### 1. Name Crafter確定シーンの作成

1. **新しいシーンを作成**
   - File > New Scene

2. **FusionConnectorを配置**
   - 既存のFusionConnectorをコピーまたは新規作成

3. **Inspector設定**
   ```
   Default Game Mode: NameCrafter
   Force Default Mode: ✓ (チェック)
   Name Crafter Game Prefab: [NameCrafterGameManagerPrefab]
   ```

4. **GameModeSelectorを削除**
   - GameModeSelectorコンポーネントを削除
   - ゲームモード選択UIを削除

5. **UIの調整**
   - 「ゲーム開始」ボタンのみ表示
   - ゲームモード選択関連のUIを非表示

### 2. 動的なゲームモード設定

スクリプトからも制御可能：

```csharp
// Name Crafterを強制使用
FusionConnector.Instance.SetDefaultGameMode(DefaultGameMode.NameCrafter);
FusionConnector.Instance.SetForceDefaultMode(true);

// 推理ゲームをデフォルトに（GameModeSelectorがない場合のみ）
FusionConnector.Instance.SetDefaultGameMode(DefaultGameMode.Deduction);
FusionConnector.Instance.SetForceDefaultMode(false);
```

## デバッグ機能

### ログ出力
ゲーム開始時に詳細なログが出力されます：

```
StartSelectedGame called. GameModeSelector: False
ForceDefaultMode: True
DefaultGameMode: NameCrafter
Using default game mode: NameCrafter (Default: NameCrafter)
Name Crafter game started
```

### 現在の設定確認
```csharp
string info = FusionConnector.Instance.GetCurrentGameModeInfo();
Debug.Log(info);
// 出力例: "強制デフォルトモード: NameCrafter"
```

## 実用例

### Name Crafter専用ルーム
```csharp
public class NameCrafterRoomManager : MonoBehaviour
{
    void Start()
    {
        // Name Crafterを確定で使用
        FusionConnector.Instance.SetDefaultGameMode(DefaultGameMode.NameCrafter);
        FusionConnector.Instance.SetForceDefaultMode(true);
        
        // ゲームモード選択UIを非表示
        FusionConnector.Instance.ShowGameModeSelection(false);
    }
}
```

### 条件付きゲームモード
```csharp
public class ConditionalGameMode : MonoBehaviour
{
    [SerializeField] private bool isNameCrafterEvent = true;
    
    void Start()
    {
        if (isNameCrafterEvent)
        {
            // イベント期間中はName Crafterのみ
            FusionConnector.Instance.SetDefaultGameMode(DefaultGameMode.NameCrafter);
            FusionConnector.Instance.SetForceDefaultMode(true);
        }
        else
        {
            // 通常時は選択可能
            FusionConnector.Instance.SetForceDefaultMode(false);
        }
    }
}
```

## 利点

1. **シーン特化**: 特定のゲームモード専用シーンを簡単に作成
2. **柔軟性**: Inspector設定とスクリプト制御の両方に対応
3. **後方互換性**: 既存のGameModeSelector機能はそのまま動作
4. **デバッグ支援**: 詳細なログと設定確認機能

## 注意事項

- Force Default Modeが有効な場合、GameModeSelectorの選択は無視されます
- 対応するPrefabが設定されていない場合、エラーログが出力されます
- デフォルトモードはInspectorまたはスクリプトで設定可能です

---

これで、Name Crafterを確定で使用するシーンや、その他の特化シーンを簡単に作成できるようになりました！
