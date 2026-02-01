using UnityEngine;

public static class MyLog
{
    public static void LogInfo(string message)
    {
        Debug.Log($"[INFO] {message}");
    }

    public static void LogWarning(string message)
    {
        Debug.LogWarning($"[WARNING] {message}");
    }

    public static void LogError(string message)
    {
        Debug.LogError($"[ERROR] {message}");
    }
}
