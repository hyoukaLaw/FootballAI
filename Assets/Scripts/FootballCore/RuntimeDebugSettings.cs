public enum LogLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Off = 3
}

public static class RuntimeDebugSettings
{
    public static bool EnableExecutionPathTracing = false;
    public static bool EnableCandidateVisualization = false;
    public static bool EnableBlueOverlapDiagnostics = false;
    public static float BlueOverlapDiagnosticInterval = 0.25f;

    public static LogLevel MinLogLevel = LogLevel.Warning;
    public static float InfoLogMinInterval = 0.1f;
    public static float WarningLogMinInterval = 0f;
    public static float ErrorLogMinInterval = 0f;

    public static bool ShouldTraceExecutionPath()
    {
        return EnableExecutionPathTracing;
    }
}
