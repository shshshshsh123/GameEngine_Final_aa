using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Product
{
    public int type;
    public int id;
    public string tag;
    public List<int> element;

    public Product()
    {
        element = new List<int>();
    }
}

public class ItemManager : MonoBehaviour
{
    [Serializable]  
    public class Weapon
    {
        public readonly List<Item> WeaponData = new List<Item>();
        public List<Sprite> WeaponTex = new List<Sprite>();
    }
    
    [Serializable]
    public class Mat
    {
        public readonly List<Item> MatData = new List<Item>();
        public List<Sprite> MatTex = new List<Sprite>();
    }

    [Serializable]
    public class Food
    {
        public readonly List<Item> FoodData = new List<Item>();
        public List<Sprite> FoodTex = new List<Sprite>();
    }

    [Serializable]
    public class Empty
    {
        public List<Item> EmptyData = new List<Item>();
        public List<Sprite> EmptyTex = new List<Sprite>();
    }



    public Weapon weapon = new Weapon();
    public Mat mat = new Mat();
    public Food food = new Food();
    public Empty empty = new Empty();
    public List<Product> product = new List<Product>();
    
    //제작할때 재료 비교를 위한 리스트
    public List<Item> productList = new List<Item>();

    private static ItemManager _instance;
    private static readonly object _lock = new object();

    public static ItemManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ItemManager>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("ItemManager");
                        _instance = singleton.AddComponent<ItemManager>();
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
        if (!gameObject.name.Contains("ItemManager"))
        {
            gameObject.name = "ItemManager";
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
        InitWeapon();
        InitMat();
        InitFood();
        InitProduct();

        InitProductList();
    }
    private void InitWeapon()
    {
        foreach (var tmp in GameManager.Instance.weaponData)
        {
            Item newItem = new Item();
            newItem.type = 0;
            if (int.TryParse(tmp["id"].ToString(), out int id))
                newItem.id = id;
            else
                Debug.LogError("Invalid ID for weapon data.");

            newItem.tag = tmp["tag"].ToString();
            newItem.quantity = 1;

            if (int.TryParse(tmp["atk"].ToString(), out int atk))
                newItem.atk = atk;
            else
                Debug.LogError("Invalid ATK for weapon data.");

            if (float.TryParse(tmp["atk_speed"].ToString(), out float atkSpeed))
                newItem.atk_speed = atkSpeed;
            else
                Debug.LogError("Invalid ATK Speed for weapon data.");

            if (int.TryParse(tmp["durability"].ToString(), out int durability))
                newItem.durability = durability;
            else
                Debug.LogError("Invalid Durability for weapon data.");

            if (float.TryParse(tmp["atk_range"].ToString(), out float atkRange))
                newItem.atk_range = atkRange;
            else
                Debug.LogError("Invalid ATK Range for weapon data.");

            weapon.WeaponData.Add(newItem);
        }
    }
    private void InitMat()
    {
        foreach (var tmp in GameManager.Instance.ItemData)
        {
            Item newItem = new Item();
            newItem.type = 1;
            newItem.id = int.Parse(tmp["id"].ToString());
            newItem.tag = tmp["tag"].ToString();
            newItem.quantity = 0;
            newItem.probability = float.Parse(tmp["probability"].ToString());

            mat.MatData.Add(newItem);
        }
    }

    private void InitFood()
    {
        foreach (var tmp in GameManager.Instance.foodData)
        {
            Item newItem = new Item();
            newItem.type = 2;
            newItem.id = int.Parse(tmp["id"].ToString());
            newItem.tag = tmp["tag"].ToString();
            newItem.quantity = 0;

            newItem.reg = int.Parse(tmp["hp"].ToString());
            newItem.reuse = float.Parse(tmp["reuse"].ToString());
            newItem.probability = float.Parse(tmp["probability"].ToString());

            food.FoodData.Add(newItem);
        }
    }
    private void InitProduct()
    {
        foreach (var tmp in GameManager.Instance.ProductData)
        {
            Product newProduct = new Product();
            newProduct.type = int.Parse(tmp["type"].ToString());
            newProduct.id = int.Parse(tmp["id"].ToString());
            newProduct.tag = tmp["tag"].ToString();

            newProduct.element.Add(int.Parse(tmp["Wood"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Grass"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Rock"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Coal"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Teeth"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Leather"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Zipper"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Iron"].ToString()));
            newProduct.element.Add(int.Parse(tmp["OldBow"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Tear"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Scale"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Jewerly"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Egg"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Fruit"].ToString()));
            newProduct.element.Add(int.Parse(tmp["Meat"].ToString()));

            product.Add(newProduct);
        }
    }

    private void InitProductList()
    {
        for (int i = 0; i < 12; i++)
        {
            productList.Add(mat.MatData[i]);
        }
        for (int i = 0; i < 3; i++)
        {
            productList.Add(food.FoodData[i]);
        }
    }

    public void WeaponProduct()
    {
        if (!TextInspectUIManager.Instance.isEnable) //설명창이 떠있을때 제작 불가능하도록
        {
            GameObject current = UIManager.Instance.eventSystem.currentSelectedGameObject;
            Animator _ani = current.GetComponent<Animator>();
            if (_ani != null && _ani.GetBool("CanCreate"))
            {
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
                Item data = new Item();
                data = weapon.WeaponData.Find(x => x.tag == current.name);
                CreateItem(data, 1, _ani);
            }
        }
    }
    public void MatProduct()
    {
        if (!TextInspectUIManager.Instance.isEnable) //설명창이 떠있을때 제작 불가능하도록
        {
            GameObject current = UIManager.Instance.eventSystem.currentSelectedGameObject;
            Animator _ani = current.GetComponent<Animator>();
            int Size = (int)(current.GetComponentInChildren<Slider>().value);
            if (_ani != null && _ani.GetBool("CanCreate"))
            {
                if (Size > 0)
                {
                    AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
                    Item data = new Item();
                    data = mat.MatData.Find(x => x.tag == current.name);
                    CreateItem(data, Size, _ani);
                }
                else
                {
                    AudioManager.Instance.PlaySfx(AudioManager.Sfx.Error);
                    _ani.SetBool("CanCreate", false);
                    _ani.SetTrigger("Fail");
                }
            }
        }
    }
    public void FoodProduct()
    {
        if (!TextInspectUIManager.Instance.isEnable) //설명창이 떠있을때 제작 불가능하도록
        {
            GameObject current = UIManager.Instance.eventSystem.currentSelectedGameObject;
            Animator _ani = current.GetComponent<Animator>();
            int Size = (int)(current.GetComponentInChildren<Slider>().value);
            if (_ani != null && _ani.GetBool("CanCreate"))
            {
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
                if (Size > 0)
                {
                    Item data = new Item();
                    data = food.FoodData.Find(x => x.tag == current.name);
                    CreateItem(data, Size, _ani);
                }
                else
                {
                    AudioManager.Instance.PlaySfx(AudioManager.Sfx.Error);
                    _ani.SetBool("CanCreate", false);
                    _ani.SetTrigger("Fail");
                }
            }
        }
    }

    private void CreateItem(Item data, int Size, Animator _ani) //data : 제작할 아이템 data / size : 제작 개수 / _ani 제작아이콘 animator
    {
        List<Item> tmpInven = new List<Item>();

        foreach (var item in InventoryManager.Instance.playerInventory.items)
        {
            tmpInven.Add(item);
        }

        if (tmpInven.FindIndex(x => x.type == 3) != -1)//인벤에 empty data가 있으면
        {
            foreach (Product tmp in product) //product == 제작식
            {
                if (tmp.type == data.type && tmp.id == data.id && tmp.tag == data.tag) //어떤 아이템을 제작하는지 확인
                {
                    int IsCheck = 0;
                    for (int i = 0; i < tmp.element.Count; i++) //제작하는데 필요한 아이템 확인
                    {
                        if (tmp.element[i] > 0) //제작하는데 필요한 아이템이면
                        {
                            int CurrentIndex = tmpInven.FindIndex(x => x.tag == productList[i].tag);

                            if (CurrentIndex != -1) //필요한 아이템이 아이템이 있으면
                            {
                                if (tmp.element[i] <= tmpInven[CurrentIndex].quantity) //아이템이 충분히 있으면
                                {
                                    IsCheck++;
                                }
                                else //아이템이 부족하면 
                                {
                                    AudioManager.Instance.PlaySfx(AudioManager.Sfx.Error);
                                    _ani.SetBool("CanCreate", false);
                                    _ani.SetTrigger("Fail");
                                    return; //제작 종료
                                }
                            }
                            else //필요한 아이템이 없으면
                            {
                                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Error);
                                _ani.SetBool("CanCreate", false);
                                _ani.SetTrigger("Fail");
                                return; //제작 종료
                            }
                        }
                    }
                    if (IsCheck > 0) //제작하는데 아이템을 한종류 이상 사용했으면
                    { //보유량을 제작 갯수만큼 감소시킴
                        for (int i = 0; i < tmp.element.Count; i++)
                        {
                            int CurrentIndex = tmpInven.FindIndex(x => x.type == productList[i].type && x.id == productList[i].id &&
                                                              x.tag == productList[i].tag);
                            if (CurrentIndex != -1) //보유량은 이미 충분하므로 -1만 검사함
                            {
                                tmpInven[CurrentIndex].quantity -= tmp.element[i] * Size; //보유량을 요구량 * 제작 수 만큼 감소시킴
                                if (tmpInven[CurrentIndex].quantity <= 0)
                                {
                                    InventoryManager.Instance.playerInventory.items[CurrentIndex] = empty.EmptyData[CurrentIndex].Clone();
                                }
                            }
                        }
                        //여기까지 오면 제작할 조건을 모두 만족한 것임
                        Item currentItem = tmpInven.Find(x => x.type == data.type && x.id == data.id && x.tag == data.tag);
                        if (currentItem != null && data.type != 0) //기가 아닌아이템이 현재 인벤에 같은 아이템이 있으면
                        {
                            currentItem.quantity += Size;
                        }
                        else //제작할 아이템(data)를 인벤토리에 ??가함
                        {
                            Item tmpItem = data.Clone();
                            InventoryManager.Instance.AddItem(tmpItem, Size);
                        }
                        _ani.SetBool("CanCreate", false);
                        _ani.SetTrigger("Success");
                    }
                    return;
                }
            }
        }
        _ani.SetBool("CanCreate", false);
        _ani.SetTrigger("Fail");
    }
    
    public int SliderItemSize(Transform slider)
    {
        List<Item> tmpInven = new List<Item>();

        foreach (var item in InventoryManager.Instance.playerInventory.items)
        {
            tmpInven.Add(item);
        }   
        
        Product CheckList = new Product();
        for (int i = 0; i < product[0].element.Count; i++) CheckList.element.Add(0); //element를 0으로 채움 (초기화)

        foreach (Product tmp in product) //product == 제작식
        {
            if (tmp.tag == slider.parent.name) //어떤 아이템을 제작하는지 확인
            {
                int IsCheck = 0;
                for (int i = 0; i < tmp.element.Count; i++) //제작하는데 필요한 아이템 확인
                {
                    if (tmp.element[i] > 0) //제작하는데 필요한 아이템이면
                    {
                        int CurrentIndex = tmpInven.FindIndex(x => x.tag == productList[i].tag);

                        if (CurrentIndex != -1) //필요한 아이템이 아이템이 있으면
                        {
                            if (tmp.element[i] <= tmpInven[CurrentIndex].quantity) //아이템이 충분히 있으면
                            {
                                CheckList.element[i] = tmpInven[CurrentIndex].quantity / tmp.element[i]; //각 재료???로 제작가능한 갯수로 초기화함
                                IsCheck++;
                            }
                            else //아이템이 부족하면 
                            {
                                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Error);
                                return 0; //제작 종료
                            }
                        }
                        else //필요한 아이템이 없으면
                        {
                            AudioManager.Instance.PlaySfx(AudioManager.Sfx.Error);
                            return 0; //제작 종료
                        }
                    }
                }
                if (IsCheck > 0) //한번이라도 부족하면 reutnr 0;을 하므로 제작하는데 필요한 아이템이 모두 있음
                {
                    int MinSize = int.MaxValue;
                    for (int i = 0; i < CheckList.element.Count; i++)
                    {
                        if (CheckList.element[i] != 0 && MinSize > CheckList.element[i])
                        {
                            MinSize = CheckList.element[i];
                        }
                    }
                    return MinSize;
                }
            }
        }
        return 0;
    }
}
