using UnityEngine;
using UnityEngine.UI;

public class GameManagerUI : MonoBehaviour
{
    [Header("UI References")]
    public Button ResumeButton;

    void Start()
    {
        if (ResumeButton != null)
        {
            ResumeButton.onClick.AddListener(OnResumeButtonClick);
        }
    }

    void OnResumeButtonClick()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.ResumeGame();
        }
    }
}
