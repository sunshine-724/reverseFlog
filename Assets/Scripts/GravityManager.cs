using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// ステージ全体の重力反転を管理するシングルトン。
/// Physics2D.gravity の Y 方向を反転し、カメラを 180 度回転させる。
/// 反転中は isFlipping = true となり、他スクリプトが入力ロックに使える。
/// </summary>
public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance { get; private set; }

    [Header("設定")]
    [Tooltip("反転アニメーションの所要時間（秒）")]
    [SerializeField] private float flipDuration = 0.3f;

    [Tooltip("デフォルトの重力の大きさ")]
    [SerializeField] private float gravityMagnitude = 9.81f;

    /// <summary>現在反転中か（true の間は入力ロック）</summary>
    public bool IsFlipping { get; private set; }

    /// <summary>現在重力が反転しているか</summary>
    public bool IsGravityInverted { get; private set; }

    /// <summary>累計反転回数</summary>
    public int FlipCount { get; private set; }

    /// <summary>重力方向の符号（通常: -1, 反転: +1）</summary>
    public float GravitySign => IsGravityInverted ? 1f : -1f;

    /// <summary>反転が完了したときに発火するイベント</summary>
    public event Action OnFlipCompleted;

    private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCamera = Camera.main;
        // 初期重力を設定
        Physics2D.gravity = new Vector2(0f, -gravityMagnitude);
        IsGravityInverted = false;
        FlipCount = 0;
    }

    /// <summary>
    /// 反転入力を受けたときに呼ぶ。反転中は無視する。
    /// </summary>
    public void Flip()
    {
        if (IsFlipping) return;
        StartCoroutine(FlipCoroutine());
    }

    /// <summary>
    /// 状態をリセットする（リトライ時に呼ぶ）。
    /// </summary>
    public void ResetState()
    {
        StopAllCoroutines();
        IsFlipping = false;
        IsGravityInverted = false;
        FlipCount = 0;
        Physics2D.gravity = new Vector2(0f, -gravityMagnitude);
        if (mainCamera != null)
        {
            mainCamera.transform.rotation = Quaternion.identity;
        }
    }

    private IEnumerator FlipCoroutine()
    {
        IsFlipping = true;

        // 重力を即座に反転
        IsGravityInverted = !IsGravityInverted;
        Physics2D.gravity = new Vector2(0f, GravitySign * gravityMagnitude);
        FlipCount++;

        // カメラを 180 度回転（Lerp）
        float targetZ = IsGravityInverted ? 180f : 0f;
        // 前回の角度から最短ルートで回転するため、現在の Z を取得
        Quaternion startRot = mainCamera.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, targetZ);

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flipDuration);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        mainCamera.transform.rotation = endRot;

        IsFlipping = false;
        OnFlipCompleted?.Invoke();
    }
}
