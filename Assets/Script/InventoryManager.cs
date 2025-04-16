using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class Item
{
    public int type; //0 -> 무기, 1 -> 재료, 2 -> 음식
    public int id;
    public string tag;
    public int quantity;
    
    //무기
    public int atk;
    public float atk_speed;
    public int durability; //내구성
    public float atk_range;

    //재료 및 음식
    public float probability;

    //음식
    public int reg; //피회복
    public float reuse; //재사용 대기시간

    public Item Clone()
    {
        return (Item)this.MemberwiseClone();
    }
}

[Serializable]
public class Inventory
{
    public List<Item> items;
    public Inventory()
    {
        items = new List<Item>();
    }
}

[Serializable]
public class Equip
{
    public List<Item> items;
    public Equip()
    {
        items = new List<Item>();
    }
}
public class InventoryManager : MonoBehaviour
{
    public int InvenLevel = 1;
    public Inventory playerInventory = new Inventory();
    public Equip playerEquip = new Equip();
    private string savePath;
    private string EquipPath;
    Item A_Bag; //1단계 가방
    Item B_Bag; //2단계 가방

    private static InventoryManager _instance;
    private static readonly object _lock = new object();

    public static InventoryManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<InventoryManager>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("InventoryManager");
                        _instance = singleton.AddComponent<InventoryManager>();
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
        if (!gameObject.name.Contains("InventoryManager"))
        {
            gameObject.name = "InventoryManager";
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
        SaveInventory();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Start()
    {
        A_Bag = ItemManager.Instance.mat.MatData[12].Clone();
        B_Bag = ItemManager.Instance.mat.MatData[13].Clone();

        savePath = Path.Combine(Application.persistentDataPath, "Data/inventory.json");
        EquipPath = Path.Combine(Application.persistentDataPath, "Data/Equip.json");
        LoadInventory();
        
        // AddItem(ItemManager.Instance.weapon.WeaponData[7], 1);
        // AddItem(ItemManager.Instance.weapon.WeaponData[6], 1);
        // AddItem(ItemManager.Instance.food.FoodData[8], 100);
        // AddItem(ItemManager.Instance.mat.MatData[13], 1);
        //AddItem(ItemManager.Instance.mat.MatData[12], 1);
        //AddItem(ItemManager.Instance.mat.MatData[1], 100);
    }

    public void LoadInventory()
    {
        playerInventory = new Inventory();
        playerEquip = new Equip();
        
        InitializeDefaultInventory();
        InitializeDefaultEquip();
        
        if (System.IO.File.Exists(savePath))
        {
            try
            {
                string loadedJson = System.IO.File.ReadAllText(savePath); 
                JsonUtility.FromJsonOverwrite(loadedJson, playerInventory);
            }
            catch (Exception e)
            {
                Debug.LogError("인벤토리 로드 중 오류 발생: " + e.Message);
            }
        }
        
        if (System.IO.File.Exists(EquipPath))
        {
            try
            {
                string loadedJson = System.IO.File.ReadAllText(EquipPath);
                JsonUtility.FromJsonOverwrite(loadedJson, playerEquip);
            }
            catch (Exception e)
            {
                Debug.LogError("장비 로드 중 오류 발생: " + e.Message);
            }
        }
    }

    private void InitializeDefaultInventory()
    {
        playerInventory.items.Clear();
        
        for (int i = 0; i < 5; i++)
        {
            Item tmpItem = ItemManager.Instance.empty.EmptyData[i].Clone();
            playerInventory.items.Add(tmpItem);
        }
    }

    private void InitializeDefaultEquip()
    {
        playerEquip.items.Clear();
        
        for (int i = 0; i < 3; i++)
        {
            Item tmpItem = ItemManager.Instance.empty.EmptyData[i].Clone();
            playerEquip.items.Add(tmpItem);
        }
    }

    public void SaveInventory()
    {
        // 디렉토리 존재 여부 확인 및 생성
        string directoryPath = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string jsonData = JsonUtility.ToJson(playerInventory);
        System.IO.File.WriteAllText(savePath, jsonData);
        Debug.Log("인벤토리 저장되었습니다: " + savePath);

        string EquipJson = JsonUtility.ToJson(playerEquip);
        System.IO.File.WriteAllText(EquipPath, EquipJson);
        Debug.Log("장비가 저장되었습니다: " + EquipPath);
    }

    public void UpdateInvLevel()
    {
        foreach (Item tmp in playerEquip.items) {
            if (Equals(tmp, A_Bag)) //1단계 가방 10칸
            {
                SetInventoryLevel(2);
                return;
            }
            else if (Equals(tmp, B_Bag)) //2단계 가방 15칸
            {
                SetInventoryLevel(3);
                return;
            }
        }
        SetInventoryLevel(1); //가방x 5칸
    }

    private void SetInventoryLevel(int level)
    {
        InvenLevel = level;
        AddEmptyInven();
        UIManager.Instance.TransActiveInven();
        UIManager.Instance.CheckInventory();
    }

    //가방을 한번장착하면 해제가 불가능하도록 만들어서 clear는 필요음
    void AddEmptyInven()
    {
        int size = InvenLevel * 5;
        int currentInven = playerInventory.items.Count; //��재 인벤의 아이템 수

        if (currentInven < size)
        {
            for (int i = currentInven; i < size; i++)
            {
                AddItem(ItemManager.Instance.empty.EmptyData[i], 1);
            }
        }
    }

    public void AddItem(Item item, int quantity = 1)
    {
        item.quantity = quantity;
        Debug.Log($"AddItem 호출됨: type={item.type}, id={item.id}, tag={item.tag}, quantity={quantity}");
        
        int itemIndex = playerInventory.items.FindIndex(x => x.type == item.type && x.id == item.id && x.tag == item.tag);
        int emptyIndex = playerInventory.items.FindIndex(x => x.type == 3);
        
        Debug.Log($"itemIndex: {itemIndex}, emptyIndex: {emptyIndex}");
        
        if(emptyIndex < InvenLevel * 5 && emptyIndex != -1)
        {
            if (item.type == 0) // 무기 추가
            {
                if (emptyIndex != -1) // 빈 슬롯이 있으면
                {
                    playerInventory.items[emptyIndex] = item.Clone();
                    Debug.Log($"빈 슬롯({emptyIndex})에 무기 추가");
                }
            }
            else if (item.type == 3) // 빈 슬롯 추가
            {
                playerInventory.items.Add(item.Clone());
                Debug.Log($"빈 슬롯 추가, 현재 인벤토리 크기: {playerInventory.items.Count}");
            }
            else // 기타 아이템 추가
            {
                if(itemIndex != -1) // 기존 아이템 수량 증가
                {
                    playerInventory.items[itemIndex].quantity += quantity;
                    Debug.Log($"아이템 수량 증가: index={itemIndex}, 새로운 수량={playerInventory.items[itemIndex].quantity}");
                }
                else
                {
                    if(emptyIndex != -1) // 빈 슬롯에 추가
                    {
                        playerInventory.items[emptyIndex] = item.Clone();
                        Debug.Log($"빈 슬롯({emptyIndex})에 아이템 추가");
                    }
                    else // 빈 슬롯이 없으면 인벤토리 끝에 추가
                    {
                        playerInventory.items.Add(item.Clone());
                        Debug.Log($"인벤토리 끝에 아이템 추가, 현재 인벤토리 크기: {playerInventory.items.Count}");
                    }
                }
            }
        }
        else
        {
            if(item.type == 1 || item.type == 2) //mat or food
            {
                if(itemIndex != -1) // 기존 아이템 수량 증가
                {
                    playerInventory.items[itemIndex].quantity += quantity;
                    Debug.Log($"아이템 수량 증가: index={itemIndex}, 새로운 수량={playerInventory.items[itemIndex].quantity}");
                }   
                else
                {
                    Debug.Log("인벤토리가 꽉 찼습니다.");
                }
            }
        }
    }
    public bool Equals(Item x, Item y)
    {
        if (x.type == y.type && x.id == y.id && x.tag == y.tag)
        {
            return true;
        }
        return false;
    }
}