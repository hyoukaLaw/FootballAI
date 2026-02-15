using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FootballAI.FootballCore
{
public class GameManagerUI : MonoBehaviour
{
    [Header("UI References")]
    public Button ResumeButton;
    public TMP_Text ScoreText; // 比分显示文本 (TextMeshPro)
    public Button CloseButton;

    public TMP_Text CountDownTextMain;
    public TMP_Text CountDownTextSeconds;

    void Start()
    {
        if (ResumeButton != null)
        {
            ResumeButton.onClick.AddListener(OnResumeButtonClick);
            CloseButton.onClick.AddListener(OnCloseButtonClick);
        }

        // 注册比分变化事件
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnScoreChanged.AddListener(UpdateScoreDisplay);
            
            // 初始化显示
            // UpdateScoreDisplay(MatchManager.Instance.RedScore, MatchManager.Instance.BlueScore);
        }
        SetCountDownVisible(false);
    }

    private void Update()
    {
        UpdateKickoffCountDownDisplay();
    }

    void OnResumeButtonClick()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.ResumeGame();
        }
    }

    /// <summary>
    /// 更新比分显示
    /// </summary>
    /// <param name="redScore">红方得分</param>
    /// <param name="blueScore">蓝方得分</param>
    void UpdateScoreDisplay(int redScore, int blueScore)
    {
        if (ScoreText != null)
        {
            ScoreText.text = $"红 {redScore}:{blueScore} 蓝";
        }
    }

    private void OnDestroy()
    {
        // 取消注册事件监听
        if (MatchManager.Instance != null && MatchManager.Instance.OnScoreChanged != null)
        {
            MatchManager.Instance.OnScoreChanged.RemoveListener(UpdateScoreDisplay);
        }
    }

    private void OnCloseButtonClick()
    {
        MatchManager.Instance.QuitGame();
    }

    private void UpdateKickoffCountDownDisplay()
    {
        if (MatchManager.Instance == null)
        {
            SetCountDownVisible(false);
            return;
        }
        bool isCountDownActive = MatchManager.Instance.GetIsKickoffCountDownActive();
        SetCountDownVisible(isCountDownActive);
        if (!isCountDownActive || CountDownTextSeconds == null)
            return;
        float remainingSeconds = MatchManager.Instance.GetKickoffCountDownRemainingSeconds();
        int displaySeconds = Mathf.CeilToInt(remainingSeconds);
        CountDownTextSeconds.text = displaySeconds.ToString();
    }

    private void SetCountDownVisible(bool isVisible)
    {
        if (CountDownTextMain != null)
            CountDownTextMain.gameObject.SetActive(isVisible);
        if (CountDownTextSeconds != null)
            CountDownTextSeconds.gameObject.SetActive(isVisible);
    }
}
}
