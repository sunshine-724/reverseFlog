using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// ゲーム状態管理。リトライ・クリア・死亡・タイマーを扱う。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Clear,
        Dead
    }

    [Header("UI（任意）")]
    [SerializeField] private GameObject clearUI;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI clearInfoText;

    public GameState CurrentState { get; private set; }

    /// <summary>ステージ開始からの経過時間</summary>
    public float ElapsedTime { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CurrentState = GameState.Playing;
        ElapsedTime = 0f;

        if (clearUI != null)
            clearUI.SetActive(false);
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            ElapsedTime += Time.deltaTime;

            // タイマー表示
            if (timerText != null)
            {
                timerText.text = FormatTime(ElapsedTime);
            }
        }

        // R キーでリトライ（どの状態でも可能）
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Retry();
        }
    }

    /// <summary>ゴール到達時に呼ばれる</summary>
    public void StageClear()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.Clear;
        Time.timeScale = 0f; // ゲームを一時停止

        int flipCount = GravityManager.Instance != null
            ? GravityManager.Instance.FlipCount
            : 0;

        Debug.Log($"Stage Clear! Time: {FormatTime(ElapsedTime)}, Flips: {flipCount}");

        if (clearUI != null)
            clearUI.SetActive(true);

        if (clearInfoText != null)
        {
            clearInfoText.text = $"クリアタイム: {FormatTime(ElapsedTime)}\n反転回数: {flipCount}";
        }
    }

    /// <summary>死亡時に呼ばれる（KillZone から）</summary>
    public void PlayerDied()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.Dead;
        // 少し待ってからリトライ（即リトライでも良い）
        Invoke(nameof(Retry), 0.3f);
    }

    /// <summary>シーンを再読み込みしてリトライ</summary>
    public void Retry()
    {
        Time.timeScale = 1f;

        // GravityManager のリセット
        if (GravityManager.Instance != null)
            GravityManager.Instance.ResetState();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60f);
        float seconds = time % 60f;
        return $"{minutes:00}:{seconds:05.2f}";
    }
}
