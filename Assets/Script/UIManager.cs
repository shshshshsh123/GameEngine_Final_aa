using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    public Canvas MainUI;
    public GameObject SettingUI;
    public GameObject InvenUI;
    public GameObject MapUI;
    public Button treeButton;
    public Button RockButton;
    public Button BaseButton;
    public Button BossButton;
    public GameObject ProductUI;
    public GameObject DeleteUI;
    public Slider D_Slider;
    public DeleteSlider SliderScript; //Delete slider 의 스크립트  
    public Image weaponButton;
    public Image FoodButton;
    public Image MatButton;
    public GameObject weapon;
    public GameObject Food;
    public GameObject Mat;
    public GameObject Notify_Silence;
    public Animator Silence_ani; //안내UI에 있는 animator   
    public Canvas DragonHpBar;
    public GameObject DragonHpBarObj;
    public Canvas HUD;
    public GameObject HUD_Silence;
    public GameObject HUD_Slow;
    public GameObject FoodObj;
    public Material FoodVFX; //플레이어 음식 vfx

    //씬 로드시 eventsystem 초기화
    public EventSystem eventSystem;
    //드래곤 스킬 사용시 인벤ui 못열도록 하는 변수
    public bool IsSilence = false;
    public bool IsSlow = false; 

    Color off = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    private string CurrentTex = null;
    public AnimationCurve FoodCurve;

    [Space(.5f)]
    [Header("Cursor")]
    public Texture2D MainDefaultCursor;
    public Texture2D InvDefaultCursor;
    public Texture2D InvBanCursor;

    [Space(.5f)]
    [Header("Inventory")]
    public Sprite LockTex;
    public LayerMask ApplyUI;
    [SerializeField] Image[] row1;
    [SerializeField] Image[] row2;
    [SerializeField] Image[] row3;
    [SerializeField] GameObject[] InvenText; //inven 수량
    [SerializeField] GameObject[] EquipText; //equip 수량
    public Image[] Equip;
    GameObject DragObj = null;
    GameObject DropObj = null;
    RectTransform DragButton = null;
    RectTransform DropButton = null;
    public Transform tmpParent;
    Rect rect;
    Transform PreParent = null;     
    int DeleteIndex = -1; //삭제할 아이템 인덱스
    private Player player;

    Sprite EmptyTex; //투명 텍스처
    
    [Space(.5f)]
    [Header("Player Stats")]
    public Image hpAmount;
    public Image dashCool;
    public Image dashIcon;
    public Text level;

    [Space(.5f)]
    [Header("Setting UI")]
    public GameObject[] settingUI;
    public Scrollbar bgmVolume;
    public Scrollbar sfxVolume;
    public Scrollbar monsterSfxVolume;
    public Text[] etcText;

    [Space(.5f)]
    [Header("Respawn UI")]
    public GameObject respawnUI;
    
    [Space(.5f)]
    [Header("Chest UI")]
    public GameObject chestUI;
    public Image chestIcon;
    public Sprite[] itemIconList;
    public Text chestText;
    public string[] itemTextList;

    public Image[][] inventory
    {
        get { return new Image[][] { row1, row2, row3 }; }
    }

    private static UIManager _instance;
    private static readonly object _lock = new object();

    public static UIManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("UIManager");
                        _instance = singleton.AddComponent<UIManager>();
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
        if (!gameObject.name.Contains("UIManager"))
        {
            gameObject.name = "UIManager";
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
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    private void Start()
    {      
        player = Player.Instance;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Initialize();
        
        treeButton.onClick.AddListener(() => GameManager.Instance.SceneMove("Tree"));
        RockButton.onClick.AddListener(() => GameManager.Instance.SceneMove("Rock"));
        BaseButton.onClick.AddListener(() => GameManager.Instance.SceneMove("Base"));
        BossButton.onClick.AddListener(() => GameManager.Instance.SceneMove("Boss"));

        rect = tmpParent.GetComponent<RectTransform>().rect;
        Cursor.visible = true;
        SetCursorTex(MainDefaultCursor);
        
        EmptyTex = ItemManager.Instance.empty.EmptyTex[0];

        bgmVolume.value = GameManager.Instance.data.bgmVolume;
        sfxVolume.value = GameManager.Instance.data.sfxVolume;

        InventoryManager.Instance.UpdateInvLevel();
    }

    private void Update()
    {
        if(SceneManager.GetActiveScene().name != "LoadingScene")
        {
            if (!DeleteUI.activeSelf) //deleteUI가 활성화 됐을때 뒤에 있는 아이템이 선택되지 않도�� 조건문 추가
            {
                DragAndDropUI();
                FastEquip();
            }
            if (player != null) PlayerStatUI();    // null오류 방지 (스크립트 실행순서차이)
        }
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "LoadingScene")
        {
            // LoadingScene에서 모든 UI 비활성화
            if (DeleteUI != null) DeleteUI.SetActive(false);
            if (MapUI != null) MapUI.SetActive(false);
            if (InvenUI != null) InvenUI.SetActive(false);
            if (SettingUI != null) SettingUI.SetActive(false);
            if (ProductUI != null) ProductUI.SetActive(false);
            if (respawnUI != null) respawnUI.SetActive(false);
            if (chestUI != null) chestUI.SetActive(false);
            if (MainUI != null) MainUI.gameObject.SetActive(false);
            return;
        }

        MainUI.gameObject.SetActive(true);
        Initialize();
        
        // 씬 전환 시 시간 스케일 초기화
        Time.timeScale = 1f;
    }
    private void Initialize()
    {
        eventSystem = GameObject.FindWithTag("Event").GetComponent<EventSystem>();
        
        //로딩씬을 제외한 모든 씬 시작시 모든 ��버스에 카메라를 추가함
        MainUI.worldCamera = Camera.main;
        DragonHpBar.worldCamera = Camera.main;
        HUD.worldCamera = Camera.main;
    }
    void SetCursorTex(Texture2D cursorTex)
    {
        if (CurrentTex != cursorTex.name)
        {
            Cursor.SetCursor(cursorTex, Vector2.zero, CursorMode.Auto);
            CurrentTex = cursorTex.name;
        }
    }
    public void TransSettingUI(bool cancelAxis)
    {
        if (!InvenUI.activeSelf && !MapUI.activeSelf && !ProductUI.activeSelf)
        {
            if (cancelAxis)
            {
                if (SettingUI.activeSelf)
                {
                    if (settingUI[0].activeSelf) //기본 세팅 ui만 으면 전체 비활성화
                    {
                        SettingUI.SetActive(false);
                        Time.timeScale = 1f;
                    }
                    else //다른 ui가 열려있으면 기본 세팅 ui 오픈
                    {
                        // UI 다끄고
                        for (int i = 0; i < settingUI.Length; i++)
                        {
                            settingUI[i].SetActive(false);
                        }
                        // 선택 UI 키기
                        settingUI[0].SetActive(true);

                    }
                }
                else
                {
                    SettingUI.SetActive(true);
                    Time.timeScale = 0f;
                }

            }
        }
    }
    
    public void TransInvenUI(bool invenAxis, bool cancelAxis)
    {
        if (!SettingUI.activeSelf && !MapUI.activeSelf && !ProductUI.activeSelf)
        {
            if (InvenUI.activeSelf)
            {
                if (invenAxis || cancelAxis)
                {
                    Time.timeScale = 1;
                    InventoryManager.Instance.SaveInventory();
                    InitInvenButton();
                    DeleteUI.SetActive(false);
                    InvenUI.SetActive(false);
                    VisualEffect hitVFX = Player.Instance.currentWeapon.GetComponent<WeaponScript>().HitVFX;
                    if(hitVFX != null)
                    {
                        hitVFX.enabled = true;
                        hitVFX.Stop();
                    }
                }
            }
            else
            {
                if (invenAxis) 
                {
                    if (!IsSilence)//침묵상태(드래곤1스킬)가 아닐때 인벤UI 실행가능
                    {
                        if (Silence_ani.GetBool("UTT"))
                        {
                            Silence_ani.SetBool("UTT", false);
                        }
                        Time.timeScale = 0;
                        CheckInventory();
                        TransActiveInven();
                        TransActiveEquip();
                        InvenUI.SetActive(true);
                        VisualEffect hitVFX = Player.Instance.currentWeapon.GetComponent<WeaponScript>().HitVFX;
                        if(hitVFX != null)
                        {
                            hitVFX.enabled = false;
                            hitVFX.Stop();
                        }
                    }
                    else //침묵상태면 인벤UI 실행 불가 및 안내 문구 띄우기
                    {
                        if (!Silence_ani.GetBool("UTT"))
                        {
                            Silence_ani.SetBool("UTT", true);
                        }
                    }
                }
            }
        }
        else //다른 ui가 열렸으면 안내문구 즉시 비활성화
        {
            if (Silence_ani.GetBool("UTT"))
            {
                Silence_ani.SetBool("UTT", false);
            }
        }
    }

    public void TransProductUI(bool productAxis, bool cancelAxis)
    {
        if (!SettingUI.activeSelf && !MapUI.activeSelf &&  !InvenUI.activeSelf)
        {
            if (ProductUI.activeSelf)
            {
                if(productAxis || cancelAxis)
                {
                    Time.timeScale = 1;
                    TextInspectUIManager.Instance.EnableBG();
                    ProductUI.SetActive(false);
                }
            }
            else
            {
                if (productAxis)
                {
                    Time.timeScale = 0;
                    ProductUI.SetActive(true);
                    WeaponOpen();
                }
            }
        }
    }
    public void TransMapUI(bool mapAxis, bool cancelAxis)
    {
        if (!SettingUI.activeSelf && !InvenUI.activeSelf && !ProductUI.activeSelf)
        {
            if (MapUI.activeSelf)
            {
                if (mapAxis || cancelAxis)
                {
                    Time.timeScale = 1;
                    MapUI.SetActive(false);
                }
            }
            else
            {
                if (mapAxis)
                {
                    Time.timeScale = 0;
                    MapUI.SetActive(true);
                }
            }
        }
    }
    
    public void CheckInventory() //sprite로딩
    {
        int TotalInvenIndex = InventoryManager.Instance.InvenLevel * 5;
        int RawIndex = 0;
        int invenIndex = 0;

        //인벤 채우기전에 빈칸이랑 좌물쇠 스프라이트�� 채움

        int cellSize = 15;
        for (int i = 0; i < cellSize; i++)
        {
            if (i < TotalInvenIndex)
            {
                if (i < 5)
                {
                    inventory[0][i].sprite = EmptyTex;
                }
                else if (i < 10)
                {
                    inventory[1][i - 5].sprite = EmptyTex;
                }
                else
                {
                    inventory[2][i - 10].sprite = EmptyTex;
                }
            }
            else
            {
                if (i < 10)
                {
                    inventory[1][i - 5].sprite = LockTex;
                }
                else
                {
                    inventory[2][i - 10].sprite = LockTex;
                }
            }
        }
        //인밴 sprite 채우기
        foreach (Item tmp in InventoryManager.Instance.playerInventory.items)
        {
            if (invenIndex > 4)
            {
                invenIndex = 0;
                RawIndex++;
            }
            
            if(tmp.quantity <= 0) //수량이 0 이하면 빈칸으로 채움
            {
                inventory[RawIndex][invenIndex].sprite = EmptyTex;
                invenIndex++;
            }
            else if (tmp.type == 0) //무기
            {
                int Index = ItemManager.Instance.weapon.WeaponData.FindIndex(x => x.id == tmp.id && x.tag == tmp.tag);
                inventory[RawIndex][invenIndex].sprite = ItemManager.Instance.weapon.WeaponTex[Index];
                invenIndex++;
            }
            else if (tmp.type == 1) //재료
            {
                int Index = ItemManager.Instance.mat.MatData.FindIndex(x => x.id == tmp.id && x.tag == tmp.tag);
                inventory[RawIndex][invenIndex].sprite = ItemManager.Instance.mat.MatTex[Index];
                invenIndex++;
            }
            else if (tmp.type == 2) //음식
            {
                int Index = ItemManager.Instance.food.FoodData.FindIndex(x => x.id == tmp.id && x.tag == tmp.tag);
                inventory[RawIndex][invenIndex].sprite = ItemManager.Instance.food.FoodTex[Index];
                invenIndex++;
            }
            else
            {
                inventory[RawIndex][invenIndex].sprite = EmptyTex;
                invenIndex++;
            }
        }

        int EquipIndex = 0;
        foreach (Item tmp in InventoryManager.Instance.playerEquip.items)
        {
            if(tmp.quantity <= 0) //수량이 0 이하면 빈칸으로 채움
            {
                Equip[EquipIndex++].sprite = EmptyTex;
            }
            else if(tmp.type == 0)
            {
                int Index = ItemManager.Instance.weapon.WeaponData.FindIndex(x => x.id == tmp.id && x.tag == tmp.tag);
                Equip[0].sprite = ItemManager.Instance.weapon.WeaponTex[Index];
                EquipIndex++;
            }
            else if(tmp.type == 1)
            {
                int Index = ItemManager.Instance.mat.MatData.FindIndex(x => x.id == tmp.id && x.tag == tmp.tag);
                Equip[1].sprite = ItemManager.Instance.mat.MatTex[Index];
                EquipIndex++;
            }
            else if(tmp.type == 2)
            {
                int Index = ItemManager.Instance.food.FoodData.FindIndex(x => x.id == tmp.id && x.tag == tmp.tag);
                Equip[2].sprite = ItemManager.Instance.food.FoodTex[Index];
                EquipIndex++;
            }
            else
            {
                Equip[EquipIndex++].sprite = EmptyTex;
            }
        }
    }
    
    public void SwapInven(Item a, Item b, int AIndex, int BIndex) //a : drag / b : drop
    {        
        int RowA, RowB;
        if (AIndex < 5) RowA = 0;
        else if (AIndex < 10) RowA = 1;
        else RowA = 2;
       
        if (BIndex < 5) RowB = 0;
        else if (BIndex < 10) RowB = 1;
        else RowB = 2;
        
        //UI 이미지 변경
        Sprite tmpSprite = inventory[RowA][AIndex - RowA * 5].sprite;
        inventory[RowA][AIndex - RowA * 5].sprite = inventory[RowB][BIndex - RowB * 5].sprite;
        inventory[RowB][BIndex - RowB * 5].sprite = tmpSprite;
        
        //UI 이미지에 해당하는 inventory 데이터 변경
        Item tmpItem = a;
        a = b;
        b = tmpItem;
        InventoryManager.Instance.playerInventory.items[AIndex] = a;
        InventoryManager.Instance.playerInventory.items[BIndex] = b;

        InvenToInven(AIndex, BIndex);
    }
    
    public void SwapEquip(Item a, int b, int AIndex, int BIndex) //a -> playerInventory / b -> playerEquip
    {
        int tmpType = -1;
        if (b == 16) tmpType = 0; //무기
        else if (b == 17) tmpType = 1; //가방
        else if (b == 18) tmpType = 2; //음식
        
        Item objB = InventoryManager.Instance.playerEquip.items[tmpType];

        //같은 아이템일때 수량을 더하는 코드
        if (tmpType != 0 && (a.type == objB.type && a.id == objB.id && a.tag == objB.tag))//무기가 아니면서 같은 아이템이면
        {
            InvenText[AIndex].SetActive(false);
            objB.quantity += a.quantity;
            EquipText[tmpType].GetComponentInChildren<Text>().text = objB.quantity.ToString();
            Item tmpItem = new Item();
            tmpItem = ItemManager.Instance.empty.EmptyData[AIndex];
            InventoryManager.Instance.playerInventory.items[AIndex] = tmpItem; //얕은복사때문에 오류 생겨서 깊은 복사로 변경함
            CheckInventory();  
            return;
        }

        int RowA;
        if (AIndex < 5) RowA = 0;
        else if (AIndex < 10) RowA = 1;
        else RowA = 2;
        
        //같은 type의 아이템이면 앞존건에서 걸러지고 empty랑 다른아이템이면 뒷조건에서 걸러짐
        if (tmpType == a.type || (a.type == 3 && BIndex != -1))
        {
            //바꾸���는 아이템의 type이 mat일때 가방종류가 아니면 교체 불가능하도록 예외처리
            int itemId = InventoryManager.Instance.playerInventory.items[AIndex].id;
            if (tmpType == 1 && !(itemId == 16 || itemId == 17))
            {
                return;
            }

            //UI 이미지 변경
            Sprite tmpSprite = inventory[RowA][AIndex - RowA * 5].sprite;
            inventory[RowA][AIndex - RowA * 5].sprite = Equip[tmpType].sprite;
            Equip[tmpType].sprite = tmpSprite;
            

            //UI 이미지에 해당하는 inventory 데이터 변경
            if (BIndex != -1) //장비가 있는 칸에 적용할때
            {
                Item tmpB = InventoryManager.Instance.playerEquip.items[BIndex];
                Item tmpItem = new Item();
                tmpItem = a;
                a = tmpB;
                tmpB = tmpItem;

                InvenToEquip(AIndex, BIndex);
                InventoryManager.Instance.playerInventory.items[AIndex] = a;
                InventoryManager.Instance.playerEquip.items[BIndex] = tmpB;
            }
            else //빈칸에 장비를 받을때 -> drag함수에서 조건문으로 처리해서
            {
                Item tmpB = InventoryManager.Instance.playerEquip.items[tmpType];
                Item tmpItem = a;
                a = tmpB;
                tmpB = tmpItem;

                InvenToEquip(AIndex, tmpType);
                InventoryManager.Instance.playerInventory.items[AIndex] = a;
                InventoryManager.Instance.playerEquip.items[tmpType] = tmpB;
            }
        }
    }
    //fast equip용 swapEquip()함수
    public void SwapEquip(Item a, Item b, int AIndex, int BIndex) //a -> playerInventory / b -> playerEquip
    {
        Item objB = null;
        int tmpType = b.type;
        if(tmpType != 3) { objB = InventoryManager.Instance.playerEquip.items[tmpType]; }

        if (BIndex != 0 && objB != null && (a.type == objB.type && a.id == objB.id && a.tag == objB.tag)) //무기를 제외한 같은 아이템이면 수량을 더함
        {
            InvenText[AIndex].SetActive(false);
            objB.quantity += a.quantity;
            EquipText[tmpType].GetComponentInChildren<Text>().text = objB.quantity.ToString();
            Item tmpItem = new Item();
            tmpItem = ItemManager.Instance.empty.EmptyData[AIndex];
            InventoryManager.Instance.playerInventory.items[AIndex] = tmpItem; //얕은복사때문에 오류 생겨서 깊은 복사로 변경함
            CheckInventory();  
            return;
        }
        
        int RowA;
        if (AIndex < 5) RowA = 0;
        else if (AIndex < 10) RowA = 1;
        else RowA = 2;
        
        //바꾸려는 아이템의 type이 mat일때 가방종류가 아니면 교체 불가능하도록 예외처리
        int itemId = InventoryManager.Instance.playerInventory.items[AIndex].id;
        if (a.type == 1 && !(itemId == 16 || itemId == 17))
        {
            return;
        }
        
        //같은 type의 아이템이면 앞존건에서 걸러지고 empty랑 다른아이템이면 뒷조건에서 걸러짐
        if (a.type == b.type || (b.type == 3))
        { 
            //UI 이미지 변경
            Sprite tmpSprite = inventory[RowA][AIndex - RowA * 5].sprite;
            inventory[RowA][AIndex - RowA * 5].sprite = Equip[BIndex].sprite;
            Equip[BIndex].sprite = tmpSprite;

            //UI 이미지에 해당하는 inventory 데이터 변경
            if (BIndex != -1) //equip랑 inven에 같은type의 아이템이 있는 경우
            {
                Item tmpItem = a;
                a = b;
                b = tmpItem;

                InvenToEquip(AIndex, BIndex);
                InventoryManager.Instance.playerEquip.items[BIndex] = b;
                InventoryManager.Instance.playerInventory.items[AIndex] = a;
            }
        }
        else if(b.type != 3 && a.type == 3) //equip -> inven(빈칸)으로 ���는경우 
        {
            //UI 이미지 변경
            Sprite tmpSprite = inventory[RowA][AIndex - RowA * 5].sprite;
            inventory[RowA][AIndex - RowA * 5].sprite = Equip[BIndex].sprite;
            Equip[BIndex].sprite = tmpSprite;

            //UI 이미지에 해당하는 inventory 데이터 변경
            if (BIndex != -1) //equip랑 inven에 같은type의 아이템이 있는 경우
            {
                Item tmpItem = a;
                a = b;
                b = tmpItem;
                
                EquipToInven(BIndex, AIndex);
                InventoryManager.Instance.playerEquip.items[BIndex] = b;
                InventoryManager.Instance.playerInventory.items[AIndex] = a;
            }
        }
    }
    
    void FastEquip()
    {
        if (InvenUI.activeSelf)
        {
            int InvenSize = InventoryManager.Instance.playerInventory.items.Count;
            if (Input.GetMouseButtonDown(1))
            {
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
                // PointerEventData 생성
                PointerEventData pointerData = new PointerEventData(eventSystem)
                {
                    position = Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerData, results);
                
                // 특정 레이어만 감지
                foreach (var result in results)
                {
                    if (((1 << result.gameObject.layer) & ApplyUI) != 0)  // 레이어 체크
                    {
                        int a = -1;
                        int.TryParse(result.gameObject.name, out a);

                        if (a != -1 && (InvenSize > a - 1))
                        {
                            if (InventoryManager.Instance.playerInventory.items[a - 1].type != 3)
                            {
                                DragObj = result.gameObject;
                                DragButton = DragObj.GetComponent<RectTransform>();
                                break; // 첫 번째 감지된 UI 요소로 드래그 시작
                            }
                        }
                    }
                }
            }
            if (Input.GetMouseButtonUp(1))
            {
                if (DragObj != null)
                {
                    int a = -1;
                    int.TryParse(DragObj.name, out a);
                    int b = -1;
                    if (a > -1)
                    {
                        Item ObjA = InventoryManager.Instance.playerInventory.items[a - 1];
                        if (ObjA.type == 0) b = 0;
                        else if (ObjA.type == 1) b = 1;
                        else if (ObjA.type == 2) b = 2;
                        else b = -1;
                        if (b > -1)
                        {
                            Item ObjB = InventoryManager.Instance.playerEquip.items[b];
                            if(ObjA.type == 1) //가방을 빠른장착할 경우
                            {
                                if (ObjA.id > ObjB.id) SwapEquip(ObjA, ObjB, a - 1, b); //더 좋은 방만 장착 가능하도록
                                InventoryManager.Instance.UpdateInvLevel();
                            }
                            else
                            {
                                SwapEquip(ObjA, ObjB, a - 1, b);
                            }
                        }
                        //빈칸 우클릭은 dragobj 초기화하는 위에 코드에서 처리함
                    }
                    InitInvenButton();
                }
            }
            player.EquipWeapon(); //바뀐 아이템 즉시장착되도록 함수 호출
        }
    }
    
    void DragAndDropUI()
    {
        //마우스 클릭시 
        if (InvenUI.activeSelf)
        {
            // 드래그 중이면 마우스 따라다니도록
            if (DragButton != null)
            {
                Vector2 anchoredPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    DragButton.parent as RectTransform,
                    Input.mousePosition,
                    Camera.main,
                    out anchoredPos
                );
                float width = rect.width / 2 - 100;
                float height = rect.height / 2 - 100;
                float x = Mathf.Clamp(anchoredPos.x, -width, width);
                float y = Mathf.Clamp(anchoredPos.y, -height, height);

                DragButton.anchoredPosition3D = new Vector3(x, y, 0);
            }

            int InvenSize = InventoryManager.Instance.InvenLevel * 5;
            if (Input.GetMouseButtonDown(0))
            {
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
                // PointerEventData 생성
                PointerEventData pointerData = new PointerEventData(eventSystem)
                {
                    position = Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerData, results);

                // 특정 레이어만 감지
                foreach (var result in results)
                {
                    if (((1 << result.gameObject.layer) & ApplyUI) != 0)  // 레이어 체크
                    {
                        int a = -1;
                        int.TryParse(result.gameObject.name, out a);
                        if (a > 0 && (InvenSize > a - 1 || a == 16 || a == 18))
                        {
                            DragObj = result.gameObject;
                            DragButton = DragObj.GetComponent<RectTransform>();
                            
                            PreParent = DragObj.transform.parent;
                            DragObj.transform.SetParent(tmpParent);
                            DragObj.transform.SetAsLastSibling();
                            
                            Image tmpImg = DragObj.GetComponent<Image>();
                            tmpImg.color = new Color(1, 1, 1, 0.6f);
                            tmpImg.raycastTarget = false;
                            break; // 첫 번째 감지된 UI 요소로 드래그 시작
                        }
                    }
                }
                if(DragObj == null)
                {
                    Debug.Log("DragObj is null");
                    return;
                }
            }

            //마우스 클릭 해제시
            if (Input.GetMouseButtonUp(0))
            {
                if (DragObj != null)
                {
                    PointerEventData pointerData = new PointerEventData(eventSystem)
                    {
                        position = Input.mousePosition
                    };

                    List<RaycastResult> results = new List<RaycastResult>();
                    eventSystem.RaycastAll(pointerData, results);

                    // 특정 레이어만 감지
                    foreach (var result in results)
                    {
                        if (((1 << result.gameObject.layer) & ApplyUI) != 0)  // 레이어 체크
                        {
                            DropObj = result.gameObject;
                            DropButton = DropObj.GetComponent<RectTransform>();
                            break; // 첫 번째 감지된 UI 요소로 드롭 처리
                        }
                    }

                    if (DropObj != null && DragObj != DropObj)
                    {
                        int a = -1;
                        int b = -1;
                        int.TryParse(DragObj.name, out a);
                        int.TryParse(DropObj.name, out b);
                        
                        if(b < 16 && b > InventoryManager.Instance.InvenLevel * 5) //잠겨있는 칸에 옮기려고 시도하면
                        {
                            InitInvenButton();
                            return;
                        }

                        if (a > 0 && b > 0) //inven or equip칸일 경우
                        {
                            if (a < 16) //dragObj가 인벤토리 아이템이면
                            {
                                //dragObj가 empty오브젝트면 swap실행 안되도록 예외처리
                                if (InventoryManager.Instance.playerInventory.items[a - 1].type == 3)
                                {
                                    InitInvenButton();
                                    return;
                                }
                                Item ItemA = InventoryManager.Instance.playerInventory.items[a - 1];
                                Item ItemB;
                                int Bindex;
                                //Bindex는 처음에 -1로 짜놓아서 추후에 시간남으면 수정해야함
                                if (b >= 16) { Bindex = b - 15; ItemB = InventoryManager.Instance.playerEquip.items[b - 16]; }//b가 equip칸이면
                                else { Bindex = b; ItemB = InventoryManager.Instance.playerInventory.items[b - 1]; }//b가 인벤칸이면
                                
                                if (ItemA.type == 1) //drag == mat case
                                {
                                    if (InvenSize > a - 1 && InvenSize > b - 1) //inven -> inven
                                    {
                                        SwapInven(ItemA,  ItemB, a - 1, Bindex - 1);
                                    }
                                    else if (ItemA.id > ItemB.id) //더 높은 가방이면 (가방만 교체 가능하므로)
                                    {
                                        if (b == 17) //inven -> equip (bag)
                                        {
                                            SwapEquip(ItemA, b, a - 1, Bindex - 1);
                                            InventoryManager.Instance.UpdateInvLevel();
                                        }
                                    }
                                }
                                else //drag == 무기, 음식 case
                                {
                                    if (InvenSize > b - 1) //inven -> inven
                                    {
                                        SwapInven(ItemA, ItemB, a - 1, Bindex - 1);
                                    }
                                    else SwapEquip(ItemA, b, a - 1, Bindex - 1); //inven -> equip
                                }
                            }
                            else //dragObj가 equip 아이템이면
                            {
                                //dragObj가 empty오브젝트면 swap실행 안되도록 예외처리
                                if (InvenSize < a && InventoryManager.Instance.playerEquip.items[a - 16].type == 3)
                                {
                                    InitInvenButton();
                                    return;
                                }

                                //가방 제 오류 방를 위한 함수 2개
                                Item ItemA = new Item();
                                ItemA = InventoryManager.Instance.playerEquip.items[a - 16];
                                Item ItemB = new Item();
                                int Bindex;
                                if(b >= 16) { Bindex = b - 15; ItemB = InventoryManager.Instance.playerEquip.items[b - 16]; }//b가 equip칸이면
                                else { Bindex = b; ItemB = InventoryManager.Instance.playerInventory.items[b - 1]; }//b가 인벤칸이면
                                
                                if(ItemA.type != 1) //mat을 제외한 아이템을 교체할때
                                {
                                    if (a >= 16 && a <= 18 && InvenSize > b - 1) //equip -> inven
                                        SwapEquip(ItemB, ItemA, Bindex - 1, a - 16);
                                }
                            }
                        }
                        else //func칸일 경우
                        {
                            if(DropObj.name == "Delete")
                            {
                                if (!DeleteUI.activeSelf && a > 0)
                                {
                                    DeleteUI.SetActive(true);
                                    DeleteIndex = a - 1;
                                    DeleteSize();
                                }
                            }
                        }
                    }
                    else //빈칸에서 마우스를 때면 함수종료
                    {
                        InitInvenButton();
                        Debug.Log("DropObj is null");
                        return;
                    }
                }
                InitInvenButton();
                player.EquipWeapon(); //바뀐 아이템 즉시장착되도록 함수 호출
                //클릭 땔때도 소리 재생
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
            }
        }
    }
    public void ApplyDelete()
    {
        if (DeleteUI.activeSelf && DeleteIndex > -1)//delete index가 초기화 됐으면
        {
            if (DeleteIndex < 15) //inven이면
            {
                InventoryManager.Instance.playerInventory.items[DeleteIndex].quantity -= (int)(D_Slider.value);
                if(InventoryManager.Instance.playerInventory.items[DeleteIndex].quantity <= 0) //감소시킨 후 아이템보유량이 0이하면
                {
                    int Row;
                    if (DeleteIndex < 5) Row = 0;
                    else if (DeleteIndex < 10) Row = 1;
                    else Row = 2;
                    inventory[Row][DeleteIndex - Row * 5].sprite = EmptyTex;
                    Item tmpItem = new Item();
                    tmpItem = ItemManager.Instance.empty.EmptyData[DeleteIndex];
                    InventoryManager.Instance.playerInventory.items[DeleteIndex] = tmpItem; //얕은복사때문에 오류 생겨서 깊은 복사로 변경함
                    InvenText[DeleteIndex].SetActive(false);
                }
                //삭제후 갯수가 남았으면
                InvenText[DeleteIndex].GetComponentInChildren<Text>().text = InventoryManager.Instance.playerInventory.items[DeleteIndex].quantity.ToString();
            }
            else //15, 16, 17 -> equip이면
            {
                int tmpIndex = DeleteIndex - 15;
                InventoryManager.Instance.playerEquip.items[tmpIndex].quantity -= (int)(D_Slider.value);
                if(InventoryManager.Instance.playerEquip.items[tmpIndex].quantity <= 0)//감소시킨 후 아이템보유량이 0이하면
                {
                    Item tmpItem = new Item();
                    tmpItem = ItemManager.Instance.empty.EmptyData[DeleteIndex - 15];
                    InventoryManager.Instance.playerEquip.items[tmpIndex] = tmpItem; //얕은복사때문에 오류 ��겨서 깊은 복사로 변경함
                    Equip[tmpIndex].sprite = EmptyTex;
                    EquipText[tmpIndex].SetActive(false);
                }
                //삭제후 갯수가 남았으면
                EquipText[tmpIndex].GetComponentInChildren<Text>().text = InventoryManager.Instance.playerEquip.items[tmpIndex].quantity.ToString();
            }
            DeleteIndex = -1;
            DeleteUI.SetActive(false);
        }
    }
    public void CalcelDelete()
    {
        DeleteUI.SetActive(false);
        DeleteIndex = -1;
    }
    
    void DeleteSize()
    {
        if (DeleteIndex != -1)
        {
            if(DeleteIndex < 15) //inven 이면
            {
                SliderScript.slider.maxValue = InventoryManager.Instance.playerInventory.items[DeleteIndex].quantity;
            }
            else //equip이면
            {
                SliderScript.slider.maxValue = InventoryManager.Instance.playerEquip.items[DeleteIndex - 15].quantity;
            }
        }
    }
    void InitInvenButton()
    {
        // 드래그 오브젝트 위치 초기화
        if (DragButton != null)
        {
            if (PreParent != null)
            {
                Image tmpImg = DragObj.GetComponent<Image>();
                tmpImg.color = Color.white;
                tmpImg.raycastTarget = true;
                DragObj.transform.SetParent(PreParent);
                DragObj.transform.SetSiblingIndex(0);
                PreParent = null;
            }
            DragButton.anchoredPosition3D = Vector3.zero;
        }
        if(DropButton != null) DropButton.anchoredPosition3D = Vector3.zero;

        //함수 호출 후 마지막에 초기화
        DragButton = null;
        DragObj = null;
        DropButton = null;
        DropObj = null;
    }
    
    void PlayerStatUI()
    {  
        hpAmount.fillAmount = Mathf.Lerp(hpAmount.fillAmount, (float)player.playerStats.hp / (float)player.playerStats.maxHp, Time.unscaledDeltaTime * 10);
        dashCool.fillAmount = 1 - player.playerStats.dashCool / player.playerStats.BaseDashCool;
        if (dashCool.fillAmount > 0.99f) dashIcon.color = new Color(1, 1, 1, 1);
        else dashIcon.color = new Color(1, 1, 1, 0.4f);
        level.text =player.playerStats.level.ToString();
    }

    public void PlayerHpUI(float t)
    {  
        hpAmount.fillAmount = Mathf.Lerp(hpAmount.fillAmount, (float)player.playerStats.hp / (float)player.playerStats.maxHp, t);
    }
    void InvenToInven(int AIndex, int BIndex) //a : inven(drop) / b : inven(drag)
    {
        int invenSize = InventoryManager.Instance.InvenLevel * 5;
        if (AIndex < invenSize && BIndex < invenSize) //textUI의 인덱스가 inven level보다 고 활성화 상태이면
        {
            if (InventoryManager.Instance.playerInventory.items[AIndex].type != 3)//dropObj의 data가 empty data가 아니면
            {
                InvenText[AIndex].SetActive(true);
                InvenText[BIndex].SetActive(true);
                InvenText[AIndex].GetComponentInChildren<Text>().text = InventoryManager.Instance.playerInventory.items[AIndex].quantity.ToString();
                InvenText[BIndex].GetComponentInChildren<Text>().text = InventoryManager.Instance.playerInventory.items[BIndex].quantity.ToString();
            }
            else//dropObj의 data가 empty data면
            {
                InvenText[AIndex].SetActive(false);
                InvenText[BIndex].SetActive(true);
                InvenText[BIndex].GetComponentInChildren<Text>().text = InventoryManager.Instance.playerInventory.items[BIndex].quantity.ToString();
            }
        }
    }

    void EquipToInven(int Drag, int Drop)//swap하기 전에 호출!!
    {
        Item dragItem = InventoryManager.Instance.playerEquip.items[Drag];
        Item dropItem = InventoryManager.Instance.playerInventory.items[Drop];
        if (dragItem.type != 3 && dropItem.type == 3) //drop이 빈칸이면
        {
            EquipText[Drag].SetActive(false);
            InvenText[Drop].SetActive(true);
        }
        else //둘다 아이템이 있으면
        {
            EquipText[Drag].SetActive(true);
            InvenText[Drop].SetActive(true);
        }
        InvenText[Drop].GetComponentInChildren<Text>().text = dragItem.quantity.ToString();
        EquipText[Drag].GetComponentInChildren<Text>().text = dropItem.quantity.ToString();
    }

    void InvenToEquip(int Drag, int Drop) //swap하기 전에 호출!!
    {
        Item dropItem = InventoryManager.Instance.playerEquip.items[Drop];
        Item dragItem = InventoryManager.Instance.playerInventory.items[Drag];
        if (dragItem.type != 3 && dropItem.type == 3) //drop이 빈칸이면
        {
            EquipText[Drop].SetActive(true);
            InvenText[Drag].SetActive(false);
        }
        else //drop에 아이템이 있으면
        {
            EquipText[Drop].SetActive(true);
            InvenText[Drag].SetActive(true);
        }
        InvenText[Drag].GetComponentInChildren<Text>().text = dropItem.quantity.ToString();
        EquipText[Drop].GetComponentInChildren<Text>().text = dragItem.quantity.ToString();
    }
    public void TransActiveInven()
    {
        for (int i = 0; i < InventoryManager.Instance.InvenLevel * 5; i++)
        {
            if (InventoryManager.Instance.playerInventory.items[i].type != 3) //weapon, mat, food 중에 하나면
            {
                InvenText[i].SetActive(true);
                InvenText[i].GetComponentInChildren<Text>().text = InventoryManager.Instance.playerInventory.items[i].quantity.ToString();
            }
            else //empty칸이면 
            {
                InvenText[i].SetActive(false);
            }
        }
        for(int i = InventoryManager.Instance.InvenLevel * 5; i < 15; i++) //잠겨있는칸의 수량은 비활성화 해야해서 
        {
            InvenText[i].SetActive(false);
        }
    }
    public void TransActiveEquip()
    {
        for(int i = 0; i < 3; i++)
        {
            if (InventoryManager.Instance.playerEquip.items[i].type != 3) //weapon, mat, food 중에 하나면
            {
                EquipText[i].SetActive(true);
                EquipText[i].GetComponentInChildren<Text>().text = InventoryManager.Instance.playerEquip.items[i].quantity.ToString();
            }
            else//empty칸이면 
            {
                EquipText[i].SetActive(false);
            }
        }
    }
    public void WeaponOpen()
    {
        if (!TextInspectUIManager.Instance.isEnable)//설명창이 떠있을때 ui변경 불가능하도록
        {
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
            weaponButton.color = Color.white;
            FoodButton.color = off;
            MatButton.color = off;
            weapon.SetActive(true);
            Food.SetActive(false);
            Mat.SetActive(false);
        }
    }
    public void FoodOpen()
    {
        if (!TextInspectUIManager.Instance.isEnable) //설명창이 떠있을때 ui변경 불가능하도록
        {
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
            weaponButton.color = off;
            FoodButton.color = Color.white;
            MatButton.color = off;
            weapon.SetActive(false);
            Food.SetActive(true);
            Mat.SetActive(false);
        }
    }
    public void MatOpen()
    {
        if (!TextInspectUIManager.Instance.isEnable)//설명창이 떠있을때 ui변경 불가능하도록
        {
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.Click);
            weaponButton.color = off;
            FoodButton.color = off;
            MatButton.color = Color.white;
            weapon.SetActive(false);
            Food.SetActive(false);
            Mat.SetActive(true);
        }
    }

    // 게임종료
    public void ExitGame()  // 빌드시에는 주석처리한 버전으로 해야함
    {
        //UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }

    // Setting UI
    public void OpenSettingUI(int num)  // 0 = base, 1 = sound, 2 = key, 3 = etc
    {
        // 기본창에서 닫기버튼 -> 종료
        if (num == 0 && settingUI[0].activeSelf) TransSettingUI(true);
        // UI 다끄고
        for (int i = 0; i < settingUI.Length; i++)
        {
            settingUI[i].SetActive(false);
        }
        // 선택 UI 키기
        settingUI[num].SetActive(true);
    }
    
    public void ChangeBGM()
    {
        GameManager.Instance.data.bgmVolume = bgmVolume.value;
    }
    public void ChangeSFX()
    {
        GameManager.Instance.data.sfxVolume = sfxVolume.value;
    }
    public void ChangeMonsterSFX()
    {
        GameManager.Instance.data.monsterSfxVolume = monsterSfxVolume.value;
    }
    public void TransShake()
    {
        Camera.main.GetComponent<Cam>().shakeOn = !Camera.main.GetComponent<Cam>().shakeOn;
        GameManager.Instance.data.shakeOn = Camera.main.GetComponent<Cam>().shakeOn;
        if (Camera.main.GetComponent<Cam>().shakeOn) etcText[0].text = "켜짐";
        else etcText[0].text = "꺼짐";
    }

    public void TransMotionTrail()
    {
        Camera.main.GetComponent<Cam>().motionTrailOn = !Camera.main.GetComponent<Cam>().motionTrailOn;
        GameManager.Instance.data.motionTrailOn = Camera.main.GetComponent<Cam>().motionTrailOn;
        if (Camera.main.GetComponent<Cam>().motionTrailOn) etcText[1].text = "켜짐";
        else etcText[1].text = "꺼짐";
    }

    public void CamNorthFix()
    {
        Camera.main.GetComponent<Cam>().camNorthFix = !Camera.main.GetComponent<Cam>().camNorthFix;
        GameManager.Instance.data.camNorthFix = Camera.main.GetComponent<Cam>().camNorthFix;
        if (Camera.main.GetComponent<Cam>().camNorthFix) etcText[2].text = "켜짐";
        else etcText[2].text = "꺼짐";
    }

    public void Respawn()
    {
        MainUI.gameObject.SetActive(false);
        respawnUI.SetActive(false);
        SceneLoader.LoadScene("Base");
        Time.timeScale = 1f;
        player.playerStats.hp = player.playerStats.maxHp;
        player.playerStats.GetHit = false;
    }
    public void ChestUiOpen(int rand)
    {
        Time.timeScale = 0f;
        chestUI.SetActive(true);
        chestIcon.sprite = itemIconList[rand];
        chestText.text = itemTextList[rand] + "을(를) 획득하였습니다!";
    }

    public void ChestUiExit()
    {
        chestUI.SetActive(false);
        Time.timeScale = 1f;
    }

    public IEnumerator EnableFood(float speed)
    {
        FoodObj.SetActive(true);
        float t = 0;
        while(t < 1f)
        {
            t += Time.smoothDeltaTime * speed;
            FoodVFX.SetFloat("_Alpha", Mathf.Lerp(0, 1, FoodCurve.Evaluate(t)));
            yield return null;
        }
        FoodObj.SetActive(false);
    }
}