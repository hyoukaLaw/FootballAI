using UnityEngine;

namespace FootballAI.FootballCore
{
public static class MyLog
{
    private static float _lastInfoLogTime = float.NegativeInfinity;
    private static float _lastWarningLogTime = float.NegativeInfinity;
    private static float _lastErrorLogTime = float.NegativeInfinity;

    public static void LogInfo(string message)
    {
        if (!ShouldLog(LogLevel.Info)) return;
        if (!PassesSampling(ref _lastInfoLogTime, RuntimeDebugSettings.InfoLogMinInterval)) return;
        Debug.Log($"[INFO] {message}");
    }

    public static void LogWarning(string message)
    {
        if (!ShouldLog(LogLevel.Warning)) return;
        if (!PassesSampling(ref _lastWarningLogTime, RuntimeDebugSettings.WarningLogMinInterval)) return;
        Debug.LogWarning($"[WARNING] {message}");
    }

    public static void LogError(string message)
    {
        if (!ShouldLog(LogLevel.Error)) return;
        if (!PassesSampling(ref _lastErrorLogTime, RuntimeDebugSettings.ErrorLogMinInterval)) return;
        Debug.LogError($"[ERROR] {message}");
    }

    private static bool ShouldLog(LogLevel level)
    {
        return RuntimeDebugSettings.MinLogLevel != LogLevel.Off && level >= RuntimeDebugSettings.MinLogLevel;
    }

    private static bool PassesSampling(ref float lastLogTime, float minInterval)
    {
        if (minInterval <= 0f) return true;

        float now = Time.unscaledTime;
        if (now - lastLogTime < minInterval)
        {
            return false;
        }

        lastLogTime = now;
        return true;
    }
}
}
