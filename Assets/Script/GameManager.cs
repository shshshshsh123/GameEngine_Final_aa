using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool isStartScene = true;
    // SettingData (.json) ����
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
            
            // data �ʱ�ȭ�� Awake���� ����
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
    
    // ���̵�
    public void SceneMove(string sceneName) //Map UI��ư���� ȣ����
    {        
        if(isStartScene)
        {
            isStartScene = false;
        }
        SceneLoader.LoadScene(sceneName);
        Time.timeScale = 1f; //Ȱ��ȭ ����(timescale = 0)���� �����ϹǷ� 1�� �ʱ�ȭ
    }

    public void StartGame()
    {
        SceneLoader.LoadScene("Base");
    }
    
    public void LoadGameData()
    {  
        // data �ʵ� ����� �ʱ�ȭ
        data = new SettingData();
        
        // ����� ������ �ִٸ�
        if (System.IO.File.Exists(filePath))
        {   
            try
            {
                // ����� ������ Json -> GameDataŬ������ �о����
                string JsonRead = System.IO.File.ReadAllText(filePath);
                JsonUtility.FromJsonOverwrite(JsonRead, data);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("���� ������ �ε� �� ���� �߻�: " + ex.Message);
            }
        }
    }

    public void SaveGameData()
    {   
        // ���� ���� �����ϱ�
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        try
        {
            string jsonData = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(filePath, jsonData);
            Debug.Log("���� ���� �����??���ϴ�: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("���� ������ ���� �� ���� �߻�: " + ex.Message);
        }
    }
}

