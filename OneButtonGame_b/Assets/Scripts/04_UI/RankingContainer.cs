using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class RankingContainer : MonoBehaviour
{
    [Header("UIコンポーネント")]
    public GameObject rankingEntryPrefab;
    public Transform rankingContainer;
    public TextMeshProUGUI yourTimeText;

    // Start is called before the first frame update
    void Start()
    {
        if (GameManager.Instance != null)
        {
            float clearTime = GameManager.Instance.GetClearTime();

            //System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(clearTime);
            yourTimeText.text = "Score : " + clearTime.ToString("F0");
        }
        else
        {
            yourTimeText.text = "Score : ---";
        }

        DisplayRanking();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DisplayRanking()
    {
        if (DataLogger.Instance == null)
        {
            return;
        }

        List<TimeAttackRecord> ranking = DataLogger.Instance.GetRanking();

        Debug.Log("読み込んだランキングの件数" + ranking.Count);

        foreach (Transform child in rankingContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < ranking.Count; i++)
        {
            GameObject entryGO = Instantiate(rankingEntryPrefab, rankingContainer);
            RankingEntryUI entryUI = entryGO.GetComponent<RankingEntryUI>();
            entryUI.SetData(i + 1, ranking[i].playerName, ranking[i].clearTime);
        }
    }
}
