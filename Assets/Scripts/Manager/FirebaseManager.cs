using Firebase;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseManager
{
    private static FirebaseManager instance;
    public static FirebaseManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new FirebaseManager();
                instance.Init();
            }
            return instance;
        }
    }

    const string URL = "https://projectmaze-ea0ef-default-rtdb.firebaseio.com/";
    const string Rank = "Rank";

    private DatabaseReference dbRef;
    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    private FirebaseManager() { }

    async void Init()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        var app = FirebaseApp.DefaultInstance;

        app.Options.DatabaseUrl = new Uri(URL);

        ReadData();

        if (app.Options.DatabaseUrl == null)
        {
            Debug.LogError("Database URL is null!");
            isInitialized = false;
            return;
        }
        else
        {
            isInitialized = true;
            return;
        }
    }

    public async void ReadData()
    {
        dbRef = FirebaseDatabase.DefaultInstance.GetReference("Rank");
        DataSnapshot snapshot = await dbRef.GetValueAsync();

        if (!snapshot.Exists)
            return;

        WriteRank(5, "test", 3.14f);
    }


    public async void WriteRank(int rank, string playerName, float clearTime)
    {
        DatabaseReference rankRef =
            FirebaseDatabase.DefaultInstance.GetReference("Rank").Child(rank.ToString());

        Dictionary<string, object> data = new Dictionary<string, object>
    {
        { "name", playerName },
        { "time", clearTime }
    };

        await rankRef.SetValueAsync(data);

        Debug.Log($"{rank}등 기록 저장 완료");
    }

}

[Serializable]
public class RankEntry
{
    public string name;
    public float time;

    public RankEntry(string name, float time)
    {
        this.name = name;
        this.time = time;
    }
}