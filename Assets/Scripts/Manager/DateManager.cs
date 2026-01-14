using System;
using Unity.VisualScripting;
using UnityEngine;

public class DateManager : MonoBehaviour
{

    private int resetTime = 12;


    private GameManager gameManager;

    private const string Key_Day = "Day_Key";
    private const string Key_Count = "Count_Key";
    private const string Key_AD = "AD_Key";
    private const string Key_Pass = "Pass_Key";


    private TimeZoneInfo kst;
    public enum EnterResult
    {
        Success,
        NeedAd,
        NeedPurchase,
        NoMore
    }


    public void init(GameManager gm)
    {
        gameManager = gm;
        
       
    }
    private string GetDayKeyNow()
    {
        DateTime kstNow = GetKstNow();
        DateTime effective = (kstNow.Hour < resetTime) ? kstNow.Date.AddDays(-1) : kstNow.Date;
        return effective.ToString("yyyyMMdd");
    }
    /*
    public EnterResult TryConsumeGameEntry()
    {
        DayReset();
    }
     */


    private DateTime GetKstNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kst); //½Ã°£ ¹Þ¾Æ¿È
    }

      

    private void DayReset()
    {
        string todayKey = GetDayKeyNow();
        string saveKey = PlayerPrefs.GetString(Key_Day, "");

        if (saveKey == todayKey)
        {
            return;        
        }

        PlayerPrefs.SetString(Key_Day, todayKey);
        PlayerPrefs.SetInt(Key_Count, 0);
        PlayerPrefs.SetInt(Key_AD, 0);
        PlayerPrefs.Save();

    }

    private int GetMaxTodayInternal()
    {
        bool pass = PlayerPrefs.GetInt(Key_Pass, 0) == 1;
        bool ad = PlayerPrefs.GetInt(Key_AD, 0) == 1;

        int baseMax = pass ? 2 : 0;
        int max = baseMax + (ad ? 1 : 0);
        return Mathf.Min(3, max);
    }

}
