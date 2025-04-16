using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

[Serializable]
public class Exp
{
    public int level;
    public int incHP;
    public float need_exp;
    public int atk;
}

[Serializable]
public class CurrentExp
{
    public int level;
    public float exp;
    public int MaxHp;
    public int atk;
    public CurrentExp()
    {
        level = 1;
        exp = 0f;
        MaxHp = 100;
        atk = 1;
    }
}   

public class ExpManager : MonoBehaviour
{
    public List<Exp> ExpData = new List<Exp>(); //플레이어 레벨업 조건과 관련된 데이터
    public CurrentExp playerExp;
    private string savePath;
    private Player player;

    private static ExpManager _instance;
    private static readonly object _lock = new object();

    public static ExpManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ExpManager>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("ExpManager");
                        _instance = singleton.AddComponent<ExpManager>();
                        DontDestroyOnLoad(singleton);
                    }
                    else
                    {
                        DontDestroyOnLoad(_instance.gameObject);
                    }

                    _instance.Init();
                }

                return _instance;
            }
        }
    }

    private void Init()
    {
        if (!gameObject.name.Contains("ExpManager"))
        {
            gameObject.name = "ExpManager";
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveExp(player.playerStats);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Start()
    {
        player = Player.Instance;
        
        savePath = Path.Combine(Application.persistentDataPath, "Data/Exp.json");
        InitEXP();
        LoadExp();
    }

    public void InitEXP()
    {
        foreach (var tmp in GameManager.Instance.expData)
        {
            Exp newExp = new Exp();
            newExp.level = int.Parse(tmp["level"].ToString());
            newExp.incHP = int.Parse(tmp["increase_hp"].ToString());
            newExp.need_exp = float.Parse(tmp["need_exp"].ToString());
            newExp.atk = int.Parse(tmp["atk"].ToString());
            
            ExpData.Add(newExp);
        }
    }

    public void ManageLevel(float ExpAmount) //경험치 획득(몬스터 처치 or 아이템 획득)시 호출
    {
        player.playerStats.CurrentExp += ExpAmount;
        foreach (Exp tmp in ExpData)
        {
            if (tmp.level == player.playerStats.level + 1)
            {
                if (player.playerStats.CurrentExp > tmp.need_exp)
                {
                    player.playerStats.level++;
                    player.playerStats.maxHp += tmp.incHP;
                    player.playerStats.hp = player.playerStats.maxHp; //레벨���시 체력 풀 회복
                    player.playerStats.CurrentExp -= tmp.need_exp; //오버된 경험치는 보존
                    player.playerStats.atk = tmp.atk;
                    SaveExp(player.playerStats);
                    continue;   // 한번에 2레벨이상 올라갈경우 반복을 위함
                }
                break;
            }
        }
    }
    public void LoadExp()
    {
        playerExp = new CurrentExp();
        
        if (System.IO.File.Exists(savePath))
        {
            try
            {
                string jsonData = System.IO.File.ReadAllText(savePath);
                JsonUtility.FromJsonOverwrite(jsonData, playerExp);
                Debug.Log("플레이어의 경험치와 레벨이 로드되었습니다: " + playerExp.level + " " + playerExp.exp + " " + playerExp.MaxHp + " " + playerExp.atk);
            }
            catch (Exception ex)
            {
                Debug.LogError("경험치 로드 중 오류 발생: " + ex.Message);
            }
        }
    }

    public void SaveExp(PlayerStats stat)
    {
        try
        {
            // 디렉토리 존재 여부 확인 및 생성
            string directoryPath = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }   
            playerExp.level = stat.level;
            playerExp.exp = stat.CurrentExp;
            playerExp.MaxHp = stat.maxHp;
            playerExp.atk = stat.atk;
            string jsonData = JsonUtility.ToJson(playerExp);
            System.IO.File.WriteAllText(savePath, jsonData);
            Debug.Log("플레이어의 경험치와 레벨이 저장되었습니다: " + savePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("경험치 저장 중 오류 발생: " + ex.Message);
        }
    }

    private void InitializeDefaultExp()
    {
        playerExp.level = 1;
        playerExp.exp = 0;
        playerExp.MaxHp = 100;
        playerExp.atk = 10;
        Debug.Log("기본 경험치와 레벨이 초기화되었습니다: " + playerExp.level + " " + playerExp.exp + " " + playerExp.MaxHp + " " + playerExp.atk);
    }
}
