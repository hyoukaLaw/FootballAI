using UnityEngine;

namespace FootballAI.FootballCore
{
public class FpsLoggerRunner : MonoBehaviour
{
    private float _elapsedTime;
    private int _frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        FpsLoggerRunner existing = Object.FindObjectOfType<FpsLoggerRunner>();
        if (existing != null)
            return;
        GameObject runner = new GameObject("FpsLoggerRunner");
        runner.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(runner);
        runner.AddComponent<FpsLoggerRunner>();
    }

    private void Update()
    {
        if (!RuntimeDebugSettings.EnableFpsLogging)
            return;
        float interval = Mathf.Max(0.1f, RuntimeDebugSettings.FpsLogInterval);
        _elapsedTime += Time.unscaledDeltaTime;
        _frameCount++;
        if (_elapsedTime < interval)
            return;
        float fps = _frameCount / _elapsedTime;
        MyLog.LogInfoNoSampling($"[Perf] FPS={fps:F1}");
        _elapsedTime = 0f;
        _frameCount = 0;
    }
}
}
