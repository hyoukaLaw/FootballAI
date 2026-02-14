using UnityEngine;

namespace FootballAI.FootballCore
{
public class FrameRateLimiter : MonoBehaviour
{
    [Header("Frame Rate")]
    public bool UseVSync = false;
    [Range(0, 4)]
    public int VSyncCount = 0;
    public int TargetFrameRate = 60;

    private void Awake()
    {
        ApplySettings();
    }

    private void OnValidate()
    {
        VSyncCount = Mathf.Clamp(VSyncCount, 0, 4);
        if (TargetFrameRate < 1)
            TargetFrameRate = 1;
        ApplySettings();
    }

    public void ApplySettings()
    {
        QualitySettings.vSyncCount = UseVSync ? VSyncCount : 0;
        Application.targetFrameRate = TargetFrameRate;
    }
}
}
