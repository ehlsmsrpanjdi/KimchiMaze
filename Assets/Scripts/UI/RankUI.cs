using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class RankUI : MonoBehaviour
{

    [SerializeField]
    private Transform content;

    [SerializeField]
    private RankSlot slotPrefab;


    [Header("내정보")]
    [SerializeField]
    private TMP_Text rankText;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text timeText;


    [SerializeField]
    private int maxCount = 100;

    private readonly List<RankEntry> entries = new();
    private readonly List<RankSlot> slots = new();
    //미리 만들어두기
    private readonly Queue<RankSlot> poolingQ = new();


    private void Reset()
    {
        rankText = this.TryFindChild("RankText").GetComponent<TMP_Text>();
        nameText = this.TryFindChild("NameText").GetComponent<TMP_Text>();
        timeText = this.TryFindChild("ScoreText").GetComponent<TMP_Text>();

    }

    private void Awake()
    {
        Setting();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Setting()
    {
        //Init()에서 한번? 어디든 한번 하고
        for (int i = 0; i < maxCount; i++)
        {
            var slot = Instantiate(slotPrefab, content);
            slot.gameObject.SetActive(false);
            poolingQ.Enqueue(slot);
        }
        
    }

    private void ReturnAllPoolingSlot()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            slot.Clear();
            slot.gameObject.SetActive(false);
            poolingQ.Enqueue(slot);
        }
        slots.Clear();
    }



    private void Refresh()
    {
        //열때마다 초기화 하면 좋을듯
        ReturnAllPoolingSlot();

        if (entries.Count == 0)
        {
            return;
        }

        entries.Sort((a, b) =>
        {
            int compare = b.time.CompareTo(a.time);

            return compare;
        });


        int count = Mathf.Min(entries.Count, maxCount); //100개 이전엔 엔트리 크기만 하면될둣?
        for (int i = 0; i < count; i++)
        {
            var slot = RentSlot();
            slot.Setting(entries[i].name, entries[i].time, i + 1);
        }
    }


    private RankSlot RentSlot() //이것만 AI돌림 
    {
        RankSlot slot = poolingQ.Count > 0 ? poolingQ.Dequeue() : Instantiate(slotPrefab, content);
        slot.gameObject.SetActive(true);
        slots.Add(slot);
        return slot;
    }

    public void SettingEntries(List<RankEntry> _entries)
    {
        entries.Clear();
        if (_entries == null)
        {
            return;
        }

        for (int i = 0; i < _entries.Count; i++)
        { 
            entries.Add(_entries[i]);
        }
    }

    public void AddEntry(RankEntry _entry) //게임 끝날때? 호출하면될듯?
    {
        if (_entry == null)
        {
            return;
        }

        entries.Add(_entry);
    }

    public void SetinfoMine(string _name, float _time, int _rank)
    {
        nameText.text = _name;
        timeText.text = _time.ToString();
        rankText.text = _rank.ToString();

    }  
}        