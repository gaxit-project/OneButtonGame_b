using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

// JSONで保存する1回の打球データの構造を定義
[System.Serializable]
public class HitData
{
    public float time;
    public string difficulty;
    public float initialSpeed;
    public float launchAngle;
    public Vector3 impactPoint;
}

// HitDataのリストをJSONとして保存するためのラッパークラス
[System.Serializable]
public class HitDataList
{
    public List<HitData> hits = new List<HitData>();
}

// 軌跡データの構造を定義
[System.Serializable]
public class TrajectoryData
{
    public string difficulty;
    public float initialSpeed;
    public float launchAngle;
    public List<Vector3> points;
}

[System.Serializable]
public class TimeAttackRecord
{
    public string playerName = "Player";
    public float clearTime;
    public string date;
}

[System.Serializable]
public class TimeAttackRanking
{
    public List<TimeAttackRecord> records = new List<TimeAttackRecord>();
}

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance {  get; private set; }　// シングルトン

    [Header("ログ保存設定")]
    public string parentDirectoryName = "BattingLogs";

    private HitDataList hitDataList= new HitDataList();
    private TimeAttackRanking timeAttackRanking = new TimeAttackRanking();
    private string savePath;
    private string trajectoryLogPath;
    private string timeAttackLogPath;

    void Awake()
    {
        // シングルトンの設定
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも破壊されないように

        string parentFolderName = string.IsNullOrEmpty(parentDirectoryName) ? "BattingLogs" : parentDirectoryName;

        string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string parentPath = Path.Combine(documentPath, parentFolderName);
        string timeAttackFolderPath = Path.Combine(parentPath, "timeattack");

        string battingFolderPath = Path.Combine(parentPath, "batting");
        trajectoryLogPath = Path.Combine(parentPath, "trajectory");

        savePath = Path.Combine(battingFolderPath, "battingLog.json");
        timeAttackLogPath = Path.Combine(timeAttackFolderPath, "timeAttackLog.json");

        try
        {
            Directory.CreateDirectory(battingFolderPath);
            Directory.CreateDirectory(trajectoryLogPath);
            Directory.CreateDirectory(timeAttackFolderPath);
        }
        catch (Exception e)
        {
            Debug.Log($"ログディレクトリの作成に失敗しました。パスが正しいか、書き込み権限があるか確認してください。\nパス：{parentPath}\nエラー：{e.Message}");
        }


        Debug.Log("ログの保存場所：" + savePath);
        Debug.Log("軌跡ログの保存場所：" + trajectoryLogPath);

        // 既存のログファイルがあれば読み込む
        LoadData();
        LoadTimeAttackData();

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 新しい打球データをリストに追加し、ファイルに保存する
    /// </summary>
    /// <param name="difficulty"></param>
    /// <param name="speed"></param>
    /// <param name="angle"></param>
    /// <param name="impactPoint"></param>
    public void LogHit(string difficulty, float speed, float angle, Vector3 impactPoint)
    {
        // 新しいHitDataを作成
        HitData newHit = new HitData
        {
            time = Time.time,
            difficulty = difficulty,
            initialSpeed = speed,
            launchAngle = angle,
            impactPoint = impactPoint
        };

        // リストに追加
        hitDataList.hits.Add(newHit);

        SaveData();
    }

    public void SaveTrajectory(string difficulty, float speed, float angle, List<Vector3> points)
    {
        TrajectoryData data = new TrajectoryData
        {
            difficulty = difficulty,
            initialSpeed = speed,
            launchAngle = angle,
            points = points
        };

        string json = JsonUtility.ToJson(data, true);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string fileName = $"Trajectory_{timestamp}.json";
        string filePath = Path.Combine(trajectoryLogPath, fileName);
        File.WriteAllText(filePath, json);

        Debug.Log($"軌跡ファイル{fileName}を保存しました");
    }

    private void SaveData()
    {
        string json = JsonUtility.ToJson(hitDataList, true);

        File.WriteAllText(savePath, json);
    }

    private void LoadData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);

            hitDataList = JsonUtility.FromJson<HitDataList>(json);
        }
    }

    // ゲーム終了時に念のため保存
    private void OnApplicationQuit()
    {
        SaveData();
    }

    public void LogClearTime(float time)
    {
        TimeAttackRecord newRecord = new TimeAttackRecord
        {
            clearTime = time,
            date = DateTime.Now.ToString("yyyy/MM/dd HH:mm")
        };

        timeAttackRanking.records.Add(newRecord);

        timeAttackRanking.records.Sort((a, b) => a.clearTime.CompareTo(b.clearTime));

        SaveTimeAttackData();
    }

    public List<TimeAttackRecord> GetRanking()
    {
        LoadTimeAttackData();
        return timeAttackRanking.records;
    }

    private void SaveTimeAttackData()
    {
        string json = JsonUtility.ToJson(timeAttackRanking, true);
        File.WriteAllText(timeAttackLogPath, json);
    }

    private void LoadTimeAttackData()
    {
        if (File.Exists(timeAttackLogPath))
        {
            string json = File.ReadAllText(timeAttackLogPath);
            timeAttackRanking = JsonUtility.FromJson<TimeAttackRanking>(json);
        }
    }
}
