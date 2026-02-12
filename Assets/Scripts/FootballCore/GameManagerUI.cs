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

    void Start()
    {
        if (ResumeButton != null)
        {
            ResumeButton.onClick.AddListener(OnResumeButtonClick);
        }

        // 注册比分变化事件
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnScoreChanged.AddListener(UpdateScoreDisplay);
            
            // 初始化显示
            // UpdateScoreDisplay(MatchManager.Instance.RedScore, MatchManager.Instance.BlueScore);
        }
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
}
}
