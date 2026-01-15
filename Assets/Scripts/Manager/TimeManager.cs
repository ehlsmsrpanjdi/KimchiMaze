using System;

public class TimeManager
{
    private static TimeManager instance;
    public static TimeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TimeManager();
            }
            return instance;
        }
    }

    private DateTime startTime;
    private DateTime endTime;
    private bool isRunning = false;

    private TimeManager() { }

    public void StartTimer()
    {
        startTime = DateTime.Now;
        isRunning = true;
    }

    public float EndTimer()
    {
        if (!isRunning)
        {
            UnityEngine.Debug.LogWarning("Timer was not started!");
            return 0f;
        }

        endTime = DateTime.Now;
        isRunning = false;

        TimeSpan elapsed = endTime - startTime;
        return (float)elapsed.TotalSeconds;
    }

    public float GetCurrentElapsed()
    {
        if (!isRunning)
            return 0f;

        TimeSpan elapsed = DateTime.Now - startTime;
        return (float)elapsed.TotalSeconds;
    }

    public void Reset()
    {
        isRunning = false;
    }
}