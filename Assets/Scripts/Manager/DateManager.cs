using System;
using UnityEngine;

public class DateManager
{

    private static DateManager instance;

    public static DateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new DateManager();
            }
            return instance;
        }
    }


    private DateManager()
    {
        InitializeTimeZone();
    }

    private int resetTime = 12;

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


    private string GetDayKeyNow()
    {
        DateTime kstNow = GetKstNow();
        DateTime effective = (kstNow.Hour < resetTime) ? kstNow.Date.AddDays(-1) : kstNow.Date;
        return effective.ToString("yyyyMMdd");
    }


    private void InitializeTimeZone()
    {
        try
        {
            kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
        }
        catch
        {
            try
            {
                kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
            }
            catch
            {
                // 플랫폼 호환성 문제 발생 시 로컬 시스템 시간으로 폴백
                kst = TimeZoneInfo.Local;
            }
        }
    }


    private DateTime GetKstNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kst); //시간 받아옴
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

    public EnterResult TryConsumeGameEntry()
    {
        DayReset();

        int used = PlayerPrefs.GetInt(Key_Count, 0);
        int max = GetMaxTodayInternal();

        if (used < max)
        {
            PlayerPrefs.SetInt(Key_Count, used + 1);
            PlayerPrefs.Save();
            return EnterResult.Success;
        }

        // 한도 초과일 때: 무엇을 해야 더 들어갈 수 있는지 안내
        bool pass = IsPassPurchased();
        bool adUsed = IsAdBonusUsed();


        if (!adUsed)
        {
            return EnterResult.NeedAd;
        }

        if (!pass)
        {
            return EnterResult.NeedPurchase;
        }


        return EnterResult.NoMore;
    }


    public void ApplyAdBonus()
    {
        PlayerPrefs.SetInt(Key_AD, 1);
        PlayerPrefs.Save();
    }



    private int GetMaxTodayInternal()
    {
        bool pass = PlayerPrefs.GetInt(Key_Pass, 0) == 1;
        bool ad = PlayerPrefs.GetInt(Key_AD, 0) == 1;

        int baseMax = pass ? 2 : 1;
        int max = baseMax + (ad ? 1 : 0);
        return Mathf.Min(3, max);
    }


    private bool IsPassPurchased() => PlayerPrefs.GetInt(Key_Pass, 0) == 1;
    public bool IsAdBonusUsed() => PlayerPrefs.GetInt(Key_AD, 0) == 1;
}
