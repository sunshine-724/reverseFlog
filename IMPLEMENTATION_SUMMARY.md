# 🐸 ひっくりカエル 実装・修正まとめ

Notionプロジェクト管理DB・作業DBのタスク粒度に基づき、実装および動作確認時に行った修正内容のまとめです。

---

<details>
<summary><b>① 環境準備：Unityプロジェクト初期化＋CLAUDE.md整備</b></summary>

### 📌 実装内容
- プロジェクトフォルダ構成の整備
  - `Assets/Scripts/`
  - `Assets/Prefabs/`
  - `Assets/Materials/`
  - `Assets/Sprites/`
- `CLAUDE.md` の作成
  - 今日のデモ範囲（1ステージのみ、60〜90秒クリア、Rキーリトライ）
  - 受入条件・決定事項（反転いつでも・舌なし・スコア化方針）
  - 操作キー割り当て（WASD/矢印、Space、F、R）
  - Gitブランチ運用ルール（`sunshine/D` での作業）

### 🔧 修正・調整内容
- 新規フォルダの `.meta` ファイル生成対応
</details>

---

<details>
<summary><b>② 反転コア実装：重力反転＋カメラ180度回転</b></summary>

### 📌 実装内容
- `GravityManager.cs` の新規作成
  - シングルトンパターンによる重力・状態管理
  - `Physics2D.gravity` の符号反転（通常時 `-9.81f` ⇔ 反転時 `+9.81f`）
  - `Camera.main` をコルーチン（Lerp / SmoothStep）で 180 度回転（Tweenパッケージ非依存）
  - 反転演出中の入力ロックフラグ（`IsFlipping`）
  - 反転回数カウンタ（`FlipCount`）と状態リセット機能（`ResetState()`）

### 🔧 修正・調整内容
- 反転中の挙動安定化（反転完了イベント `OnFlipCompleted` の提供）
</details>

---

<details>
<summary><b>③ カエル操作：左右移動・ジャンプ・張りつき</b></summary>

### 📌 実装内容
- `PlayerController.cs` の新規作成
  - Unity 新 Input System（PlayerInput / SendMessages）連携
  - 左右移動（`Rigidbody2D.linearVelocity`）
  - ジャンプ処理（重力方向に応じた逆方向へのインパルス加算）
  - 壁張りつき処理（壁検出＋移動入力方向判定による壁張りつき、重力スケール一時無効化）
  - 反転キー（Fキー / 右クリック）の入力受付と GravityManager 呼び出し

### 🔧 修正・調整内容（動作確認後の修正）
1. **ジャンプ直後の張り付き抑制**:
   - ジャンプ上昇中に誤って張り付かないよう `CLING_COOLDOWN`（0.25秒）を導入
2. **天井張り付き機能の削除**:
   - 天井に張り付いて離れなくなる挙動を防止するため、天井張り付き機能（`ceilingCheck` 等）を完全に削除し、壁張り付きのみに制限
3. **重力反転後のジャンプ不発バグ修正**:
   - 重力反転時に足元の判定位置が天井側へ切り替わっていなかったため、`groundCheck` のローカル Y 座標を `GravitySign` に応じて動的に反転させ、上下どちらの重力でも正常に接地判定・ジャンプができるよう修正
4. **重力反転時の左右操作反転対応**:
   - カメラが180度回転した状態でも画面見た目の直感的な操作を維持するため、反転中は左右移動入力およびスプライトの反転判定に反転補正を適用
</details>

---

<details>
<summary><b>④ ギミック最小セット：落下する箱＋スイッチ＋ゴール</b></summary>

### 📌 実装内容
- `FallingBox.cs`
  - 重力反転に従って床・天井間を落下・移動する箱（`Rigidbody2D` 物理追従、リセット対応）
- `PressureSwitch.cs`
  - 箱やプレイヤーの接触でオン/オフが切り替わる感圧スイッチ（`UnityEvent` で扉等と疎結合に連携、色変化フィードバック）
- `Door.cs`
  - スイッチからのイベントを受け取り開閉する扉（開閉時のコライダー有効/無効化、半透明・縮小演出）
- `Goal.cs`
  - プレイヤー接触でステージクリアを検知し `GameManager.StageClear()` を呼び出すゴール判定
</details>

---

<details>
<summary><b>⑤ デモ用ステージ1本組み立て＋リトライ導線</b></summary>

### 📌 実装内容
- `GameManager.cs`
  - ゲーム状態管理（Playing / Clear / Dead）
  - 経過時間タイマー計測、反転回数ログ出力・リザルト表示準備
  - Rキー押下による即時リトライ処理
  - 死亡時（画面外落下）の自動リトライ処理
- `KillZone.cs`
  - ステージの上下画面外に配置する落下死亡判定コライダー
- `StageBuilder.cs`（Editor拡張）
  - メニュー `Tools > ひっくりカエル > デモステージ生成` からワンクリックでデモパズルステージ一式（地形、プレイヤー、足場、登り壁、箱、スイッチ、扉、ゴール、KillZone、カメラ）を自動構築するユーティリティ
</details>
