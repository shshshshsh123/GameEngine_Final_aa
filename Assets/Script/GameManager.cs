using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool isStartScene = true;
    // SettingData (.json) 관리
    public SettingData data;    
    private string filePath;
    //CSVreader
    public List<Dictionary<string, object>> monsterData;        
    public List<Dictionary<string, object>> skillData;
    public List<Dictionary<string, object>> foodData;
    public List<Dictionary<string, object>> weaponData;
    public List<Dictionary<string, object>> ItemData;
    public List<Dictionary<string, object>> ProductData;
    public List<Dictionary<string, object>> expData;
    public List<Dictionary<string, object>> ExplainData;
    public List<Dictionary<string, object>> getExp;

    private static GameManager _instance;
    private static readonly object _lock = new object();

    public static GameManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("GameManager");
                        _instance = singleton.AddComponent<GameManager>();
                        DontDestroyOnLoad(singleton);
                    }
                }

                return _instance;
            }
        }
    }

    private void Init()
    {
        if (!gameObject.name.Contains("GameManager"))
        {
            gameObject.name = "GameManager";
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
            
            // data 초기화를 Awake에서 수행
            filePath = Path.Combine(Application.persistentDataPath, "Data/SettingData.json");    
            LoadGameData();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        monsterData = CSVreader.Read("Monster");
        skillData = CSVreader.Read("Skill");
        foodData = CSVreader.Read("Food");
        weaponData = CSVreader.Read("Weapon");
        ItemData = CSVreader.Read("Item");
        ProductData = CSVreader.Read("Production");
        expData = CSVreader.Read("PlayerExp");
        ExplainData = CSVreader.Read("Explain");
        getExp = CSVreader.Read("GetExp");
    }
    
    // 씬이동
    public void SceneMove(string sceneName) //Map UI버튼에서 호출함
    {        
        if(isStartScene)
        {
            isStartScene = false;
        }
        SceneLoader.LoadScene(sceneName);
        Time.timeScale = 1f; //활성화 상태(timescale = 0)에서 시작하므로 1로 초기화
    }

    public void StartGame()
    {
        SceneLoader.LoadScene("Base");
    }
    
    public void LoadGameData()
    {  
        // data 필드 명시적 초기화
        data = new SettingData();
        
        // 저장된 파일이 있다면
        if (System.IO.File.Exists(filePath))
        {   
            try
            {
                // 저장된 파일을 Json -> GameData클래스로 읽어오기
                string JsonRead = System.IO.File.ReadAllText(filePath);
                JsonUtility.FromJsonOverwrite(JsonRead, data);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("게임 데이터 로드 중 오류 발생: " + ex.Message);
            }
        }
    }

    public void SaveGameData()
    {   
        // 게임 이터 저장하기
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        try
        {
            string jsonData = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(filePath, jsonData);
            Debug.Log("세팅 값이 저장되??습니다: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("게임 데이터 저장 중 오류 발생: " + ex.Message);
        }
    }
}

