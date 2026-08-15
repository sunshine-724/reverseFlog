#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

/// <summary>
/// エディタ上からデモステージを自動生成するユーティリティ。
/// メニュー: Tools > ひっくりカエル > デモステージ生成
/// </summary>
public static class StageBuilder
{
    private const string GROUND_LAYER_NAME = "Ground";

    [MenuItem("Tools/ひっくりカエル/デモステージ生成")]
    public static void BuildDemoStage()
    {
        // ── Ground レイヤーを確保 ──
        int groundLayerIndex = EnsureLayer(GROUND_LAYER_NAME);
        LayerMask groundMask = 1 << groundLayerIndex;

        // ── 既存オブジェクトをクリア ──
        ClearTag("EditorOnly"); // 自動生成物には使わないので安全

        // ── GameManager ──
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();
        Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");

        // ── GravityManager ──
        var gravObj = new GameObject("GravityManager");
        gravObj.AddComponent<GravityManager>();
        Undo.RegisterCreatedObjectUndo(gravObj, "Create GravityManager");

        // ── プレイヤー ──
        var player = CreateSprite("Player", new Vector2(-5f, -2f), new Vector2(0.8f, 0.8f), Color.green);
        player.layer = 0; // Default
        player.tag = "Player";

        var rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        player.AddComponent<BoxCollider2D>();
        var pc = player.AddComponent<PlayerController>();

        // 接地・壁・天井チェック用の子オブジェクト
        var groundCheckObj = CreateEmptyChild(player, "GroundCheck", new Vector3(0, -0.45f, 0));
        var wallCheckObj = CreateEmptyChild(player, "WallCheck", new Vector3(0.45f, 0, 0));
        var ceilingCheckObj = CreateEmptyChild(player, "CeilingCheck", new Vector3(0, 0.45f, 0));

        // SerializedObject でフィールドを設定
        var pcSO = new SerializedObject(pc);
        pcSO.FindProperty("groundCheck").objectReferenceValue = groundCheckObj.transform;
        pcSO.FindProperty("wallCheckFront").objectReferenceValue = wallCheckObj.transform;
        pcSO.FindProperty("ceilingCheck").objectReferenceValue = ceilingCheckObj.transform;
        pcSO.FindProperty("groundLayer").intValue = groundMask;
        pcSO.ApplyModifiedProperties();

        // PlayerInput コンポーネント
        var playerInput = player.AddComponent<PlayerInput>();
        // InputActions アセットを検索して設定
        string[] guids = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            playerInput.actions = inputAsset;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;
        }

        Undo.RegisterCreatedObjectUndo(player, "Create Player");

        // ── ステージ地形 ──
        // 床（下部）
        var floor = CreateSprite("Floor", new Vector2(0, -4f), new Vector2(20f, 1f), new Color(0.4f, 0.4f, 0.4f));
        floor.layer = groundLayerIndex;
        floor.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(floor, "Create Floor");

        // 天井（上部）
        var ceiling = CreateSprite("Ceiling", new Vector2(0, 4f), new Vector2(20f, 1f), new Color(0.4f, 0.4f, 0.4f));
        ceiling.layer = groundLayerIndex;
        ceiling.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(ceiling, "Create Ceiling");

        // 左壁
        var wallL = CreateSprite("WallLeft", new Vector2(-10f, 0f), new Vector2(1f, 9f), new Color(0.4f, 0.4f, 0.4f));
        wallL.layer = groundLayerIndex;
        wallL.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(wallL, "Create WallLeft");

        // 右壁
        var wallR = CreateSprite("WallRight", new Vector2(10f, 0f), new Vector2(1f, 9f), new Color(0.4f, 0.4f, 0.4f));
        wallR.layer = groundLayerIndex;
        wallR.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(wallR, "Create WallRight");

        // ── 中間プラットフォーム（反転パズル用） ──

        // 下側の通路を塞ぐ壁（ゴール前）— 扉で開く
        // 上側にスイッチがあり、反転して箱を落としてスイッチを押す構成

        // 中段の足場（左側）
        var platformMidL = CreateSprite("PlatformMidLeft", new Vector2(-3f, -1f), new Vector2(4f, 0.5f), new Color(0.5f, 0.5f, 0.5f));
        platformMidL.layer = groundLayerIndex;
        platformMidL.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(platformMidL, "Create PlatformMidLeft");

        // 中段の足場（右側、隙間をあけて）
        var platformMidR = CreateSprite("PlatformMidRight", new Vector2(4f, -1f), new Vector2(4f, 0.5f), new Color(0.5f, 0.5f, 0.5f));
        platformMidR.layer = groundLayerIndex;
        platformMidR.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(platformMidR, "Create PlatformMidRight");

        // 上段の足場（箱が置いてある場所）
        var platformTop = CreateSprite("PlatformTop", new Vector2(3f, 1.5f), new Vector2(3f, 0.5f), new Color(0.5f, 0.5f, 0.5f));
        platformTop.layer = groundLayerIndex;
        platformTop.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(platformTop, "Create PlatformTop");

        // 張りつき用の壁（中央付近、プレイヤーが壁ジャンプ的に使う）
        var climbWall = CreateSprite("ClimbWall", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 3f), new Color(0.3f, 0.5f, 0.3f));
        climbWall.layer = groundLayerIndex;
        climbWall.AddComponent<BoxCollider2D>();
        Undo.RegisterCreatedObjectUndo(climbWall, "Create ClimbWall");

        // ── 箱 ──
        var box = CreateSprite("FallingBox", new Vector2(3f, 2.5f), new Vector2(0.8f, 0.8f), new Color(0.6f, 0.4f, 0.2f));
        box.AddComponent<BoxCollider2D>();
        box.AddComponent<FallingBox>();
        var boxRb = box.GetComponent<Rigidbody2D>();
        if (boxRb == null)
        {
            boxRb = box.AddComponent<Rigidbody2D>();
            boxRb.mass = 2f;
            boxRb.freezeRotation = true;
        }
        Undo.RegisterCreatedObjectUndo(box, "Create FallingBox");

        // ── スイッチ（天井近く — 反転して箱を落とす場所） ──
        var switchObj = CreateSprite("PressureSwitch", new Vector2(3f, 3.2f), new Vector2(1.2f, 0.3f), Color.red);
        switchObj.AddComponent<PressureSwitch>();
        Undo.RegisterCreatedObjectUndo(switchObj, "Create PressureSwitch");

        // ── 扉（ゴール手前を塞ぐ） ──
        var door = CreateSprite("Door", new Vector2(7f, -2.5f), new Vector2(0.5f, 2.5f), new Color(0.5f, 0.3f, 0.1f));
        door.layer = groundLayerIndex;
        door.AddComponent<BoxCollider2D>();
        door.AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(door, "Create Door");

        // スイッチ → 扉 を接続
        var switchComp = switchObj.GetComponent<PressureSwitch>();
        var doorComp = door.GetComponent<Door>();
        if (switchComp != null && doorComp != null)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                switchComp.OnActivated,
                doorComp.Open);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                switchComp.OnDeactivated,
                doorComp.Close);
        }

        // ── ゴール ──
        var goal = CreateSprite("Goal", new Vector2(8.5f, -2.5f), new Vector2(1f, 2f), Color.yellow);
        goal.AddComponent<Goal>();
        Undo.RegisterCreatedObjectUndo(goal, "Create Goal");

        // ── KillZone（上下） ──
        var killBottom = new GameObject("KillZone_Bottom");
        killBottom.transform.position = new Vector3(0, -8f, 0);
        var killBottomCol = killBottom.AddComponent<BoxCollider2D>();
        killBottomCol.size = new Vector2(30f, 2f);
        killBottomCol.isTrigger = true;
        killBottom.AddComponent<KillZone>();
        Undo.RegisterCreatedObjectUndo(killBottom, "Create KillZone Bottom");

        var killTop = new GameObject("KillZone_Top");
        killTop.transform.position = new Vector3(0, 8f, 0);
        var killTopCol = killTop.AddComponent<BoxCollider2D>();
        killTopCol.size = new Vector2(30f, 2f);
        killTopCol.isTrigger = true;
        killTop.AddComponent<KillZone>();
        Undo.RegisterCreatedObjectUndo(killTop, "Create KillZone Top");

        // ── カメラ設定 ──
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 0, -10);
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }

        Debug.Log("🐸 デモステージを生成しました！ Play ボタンで動作確認できます。");
        Debug.Log("操作: A/D = 移動, Space = ジャンプ, F = 反転, R = リトライ");
    }

    // ── ヘルパー ─────────────────────────────────────────

    private static GameObject CreateSprite(string name, Vector2 position, Vector2 scale, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.position = new Vector3(position.x, position.y, 0);
        obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteSprite();
        sr.color = color;

        return obj;
    }

    private static GameObject CreateEmptyChild(GameObject parent, string name, Vector3 localPos)
    {
        var child = new GameObject(name);
        child.transform.parent = parent.transform;
        child.transform.localPosition = localPos;
        return child;
    }

    private static Sprite cachedWhiteSprite;
    private static Sprite CreateWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;

        // 1x1 の白テクスチャからスプライトを生成
        var tex = new Texture2D(4, 4);
        var colors = new Color[16];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return cachedWhiteSprite;
    }

    private static int EnsureLayer(string layerName)
    {
        // 既存のレイヤーを探す
        for (int i = 0; i < 32; i++)
        {
            if (LayerMask.LayerToName(i) == layerName)
                return i;
        }

        // 空きスロットに追加
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // User Layer は 8〜31
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"レイヤー '{layerName}' をスロット {i} に追加しました");
                return i;
            }
        }

        Debug.LogError($"レイヤー '{layerName}' を追加する空きスロットがありません");
        return 0;
    }

    private static void ClearTag(string tag)
    {
        // 特に何もクリアしない（既存シーンを壊さないため）
    }
}
#endif
