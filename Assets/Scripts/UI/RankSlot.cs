using TMPro;
using UnityEngine;

public class RankSlot : MonoBehaviour
{
    

    [SerializeField]
    private TMP_Text r_Rank;
    [SerializeField]
    private TMP_Text r_Time;
    [SerializeField]
    private TMP_Text r_Name;


    private void Reset()
    {
        r_Rank = this.TryGetChildComponent<TMP_Text>("RankText").GetComponent<TMP_Text>();
        r_Time = this.TryGetChildComponent<TMP_Text>("TimeText").GetComponent<TMP_Text>();
        r_Name = this.TryGetChildComponent<TMP_Text>("NameText").GetComponent<TMP_Text>();

    }

    public void Setting(string _name, float _time, int _rank)
    {
        r_Time.text = _time.ToString();
        r_Name.text = _name;
        r_Rank.text = _rank.ToString();
        
    }

    public void Clear()
    {
        r_Time.text = string.Empty;
        r_Name.text = string.Empty;
        r_Rank.text = string.Empty;


    }




}
