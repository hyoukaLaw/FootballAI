using UnityEngine;

public class TimeManager
{
    private static TimeManager _instance;
    public static TimeManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new TimeManager();
            return _instance;
        }
    }

    public float GetDeltaTime()
    {
        return Time.deltaTime;
    }
}
