using UnityEngine;
using TMPro;
using System;

public class RankingEntryUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timeText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetData(int rank, string playerName, float clearTime)
    {
        rankText.text = rank + " ";
        nameText.text = playerName;

        TimeSpan timeSpan = TimeSpan.FromSeconds(clearTime);
        timeText.text = timeSpan.ToString(@"mm\:ss\.ff");
    }
}
