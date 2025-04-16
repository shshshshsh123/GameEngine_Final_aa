using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

[Serializable]
public struct PlayerStats
{
    public int level;
    public float CurrentExp;
    public int hp;
    public int maxHp;
    public bool GetHit; // Get Hit
    [Space(5f)]
    public float BaseMoveSpeed;
    public float moveSpeed;
    [Space(5f)]
    public int atk;
    public float atk_speed;
    public float atk_cool;
    public int weapon_atk;  // Weapon Attack
    public bool isAttack; // Attack
    public bool AttackInvinci; // Attack Invincibility
    [Space(5f)]
    public float BaseDashCool;
    public float dashCool;
    public bool isDash;
    public bool dashInvincibility;
};
    
public class Player : MonoBehaviour
{
    // Player Movement
    float vAxis;    // Vertical Axis
    float hAxis;    // Horizontal Axis
    bool dashAxis;  // Dash
    bool invenAxis; // Inventory
    bool attackAxis;    // Attack
    bool cancelAxis; // Cancel
    bool eatAxis;   // Eat
    public bool actionAxis;    // Action
    bool productAxis;
    bool mapAxis;   // Map
    // Player Stats
    float DashT = 0f;
    public float eatCool = 0f;  // Eat Cooldown
    public PlayerStats playerStats;
    [Header("Current Weapon")]
    [SerializeField] string[] weaponName = new string[] 
    { 
        "Hand",
        "Spear",
        "Axe",
        "Pickaxe",
        "Knife",
        "WoodBow",
        "IronBow",
        "Sword"
    };
    public GameObject currentWeapon;
    public List<GameObject> WeaponList;
    public List<WeaponScript> weaponScript;
    public Transform weaponTransform;
    public Transform bowTransform;
    [SerializeField] Vector3[] SpawnPos = new Vector3[4];
    private Rigidbody rb;
    // Player Data
    private PlayerStats playerData;

    // Player MotionTrail
    Cam camScript;

    [Header("Bow VFX")]
    // Player Bow VFX
    public Transform FirePos;
    [SerializeField] Transform Arrow;

    // Player Can Move
    public bool CanMove = true;
    bool isEndFall = true; //¶¥¿¡ ¶³¾îÁö¸é true

    // Player Animator
    private Animator animator;

    // Player Volume
    Volume volume;
    Vignette vignette;

    [Header("Don't Use")]
    // Player Move Direction
    public Vector3 moveDir;

    private static Player _instance;
    private static readonly object _lock = new object();

    public static Player Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<Player>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("Player");
                        _instance = singleton.AddComponent<Player>();
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
        if (!gameObject.name.Contains("Player"))
        {
            gameObject.name = "Player";
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
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {            
        // Resources 폴더에서 Animator 로드
        RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("Animator/Player");
        if (animatorController != null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = gameObject.AddComponent<Animator>();

            // Animator Controller 설정
            animator.runtimeAnimatorController = animatorController;
            Debug.Log("Animator assigned successfully.");
        }
        else
        {
            Debug.LogError("Failed to load Animator from Resources.");
        }

        foreach (var monster in GameManager.Instance.monsterData)
        {
            if (monster["tag"].ToString() == "Player")
            {
                playerData.atk_speed = float.Parse(monster["atk_speed"].ToString());
                playerData.BaseMoveSpeed = float.Parse(monster["walk_speed"].ToString());
                break;
            }
        }
        
        foreach (var skill in GameManager.Instance.skillData)
        {
            if (skill["mon_tag"].ToString() == "Player")
            {
                playerData.BaseDashCool = float.Parse(skill["reuse"].ToString());
                break;
            }
        }

        // Resources 폴더에서 무기 프리팹들 로드
        GameObject[] weaponPrefabs = Resources.LoadAll<GameObject>("WeaponPrefabs/Weapons");
        if (weaponPrefabs != null && weaponPrefabs.Length > 0)
        {
            WeaponList = new List<GameObject>();
            weaponScript = new List<WeaponScript>();
            foreach (GameObject prefab in weaponPrefabs)
            {
                GameObject weapon;
                if(prefab.name != "WoodBow" && prefab.name != "IronBow")
                {
                    weapon = Instantiate(prefab, weaponTransform);
                }
                else
                {
                    weapon = Instantiate(prefab, bowTransform);
                }
                weapon.SetActive(false);
                WeaponList.Add(weapon);
                // 무기 이름 순서대로 WeaponList 정렬
                WeaponList.Sort((a, b) => {
                    int indexA = Array.FindIndex(weaponName, x => a.name.Contains(x));
                    int indexB = Array.FindIndex(weaponName, x => b.name.Contains(x));
                    return indexA.CompareTo(indexB);
                });
            }
            foreach(GameObject weapon in WeaponList)
            {
                weaponScript.Add(weapon.GetComponent<WeaponScript>());
            }
        }
        else
        {
            Debug.LogError("No weapon prefabs found in Resources/Prefabs/Weapons");
        }
        
        foreach(GameObject weapon in WeaponList)
        {
           if(weapon.name == "Hand")
           {
                currentWeapon = weapon;
                Application.Quit(); 
                break;
           }
        }
        rb = GetComponent<Rigidbody>();
        InitVar();
        PlayerStatInit();   
        EquipWeapon();
        SceneManager.sceneLoaded += OnSceneLoaded;  
    }    
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "LoadingScene")
        {
            return; // LoadingScene에서는 모든 업데���트 로직 스킵
        }

        if(rb.velocity.magnitude > 0.1f)
        {
            rb.velocity = Vector3.zero;
        }
        GetInput();     
        if(CanMove)
        {
            PlayerAttack();
            EatFood();
            PlayerMove();   
        }

        // UI
        PlayerDash();//Dash
        OnPauseUI();
        OnInvenUI();
        OnMapUI();
        OnProductUI();  
    }   

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            transform.position -= transform.forward * 2f;
        }
    }
    // OnSceneLoaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LoadingScene")
        {
            CanMove = false;
            return;
        }
            
        if (Camera.main != null)
        {
            camScript = Camera.main.GetComponent<Cam>();
        }
        if(scene.name == "Base") transform.position = SpawnPos[0];
        else if(scene.name == "Rock") transform.position = SpawnPos[1];
        else if(scene.name == "Tree") transform.position = SpawnPos[2]; 
        else if(scene.name == "Boss") transform.position = SpawnPos[3];
        InitVar();
        CanMove = true;
    }  
    private void InitComponent()
    {        
        volume = GameObject.FindWithTag("Volume").GetComponent<Volume>();
        volume.profile.TryGet<Vignette>(out vignette);
    }
    private void InitVar()
    {
        InitComponent();
        // End Attackf
        animator.SetBool("EndAttack", true);
        animator.SetBool("IsStand", true);
        animator.SetBool("IsGetHit", false);
        animator.SetBool("IsDash", false);

        // Can Move
        CanMove = true;
        isEndFall = true;
    }
    void PlayerStatInit()
    {
        playerStats.level = ExpManager.Instance.playerExp.level;
        playerStats.CurrentExp = ExpManager.Instance.playerExp.exp;
        playerStats.maxHp = ExpManager.Instance.playerExp.MaxHp;
        playerStats.hp = ExpManager.Instance.playerExp.MaxHp;
        playerStats.GetHit = false;
        playerStats.atk = ExpManager.Instance.playerExp.atk;
        playerStats.atk_speed = playerData.atk_speed;
        playerStats.atk_cool = 0.0f;
        playerStats.isAttack = false;
        playerStats.AttackInvinci = false;
        playerStats.BaseMoveSpeed = playerData.BaseMoveSpeed;
        playerStats.moveSpeed = playerData.BaseMoveSpeed;
        playerStats.BaseDashCool = playerData.BaseDashCool;
        playerStats.dashCool = 0;
        playerStats.isDash = false;
        playerStats.dashInvincibility = false; //Dash Invincibility
    }

    void GetInput()
    {
        vAxis = Input.GetAxis("Vertical");
        hAxis = Input.GetAxis("Horizontal");
        dashAxis = Input.GetKey(KeyCode.Space);
        invenAxis = Input.GetKeyDown(KeyCode.I);
        attackAxis = Input.GetMouseButton(0);
        cancelAxis = Input.GetKeyDown(KeyCode.Escape);
        eatAxis = Input.GetMouseButton(1);
        actionAxis = Input.GetKey(KeyCode.E);
        mapAxis = Input.GetKeyDown(KeyCode.M);
        productAxis = Input.GetKeyDown(KeyCode.P);
    }

    void PlayerMove()
    {
        if (isEndFall && (hAxis != 0 || vAxis != 0)) // Player Can Move
        {
            float moveX = hAxis * Mathf.Sqrt(1 - vAxis * vAxis / 2);
            float moveZ = vAxis * Mathf.Sqrt(1 - hAxis * hAxis / 2);

            animator.SetBool("isMove", true);
            // Move Direction
            moveDir = Vector3.right * moveX + Vector3.forward * moveZ;
            transform.position += moveDir * playerStats.moveSpeed * Time.smoothDeltaTime;

            // Look Rotation
            Quaternion lookRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);

            // Foot Step
            AudioManager.Instance.PlayFootStep();
        }
        else
        {
            animator.SetBool("isMove", false);
            AudioManager.Instance.StopFootStep();
        }
    }

    void PlayerDash()
    {
        // Dash Cool
        playerStats.dashCool -= Time.deltaTime;
        if (CanMove && !playerStats.isDash && dashAxis && playerStats.dashCool <= 0.0f) //Dash
        {
            AudioManager.Instance.StopFootStep();
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.Dash);
            animator.SetTrigger("Dash");
            animator.SetBool("IsDash", true);
            animator.SetBool("IsGetHit", false);
            animator.SetBool("IsStand", true); //Stand Ani
            currentWeapon.GetComponent<WeaponScript>().P_GetHit = false; //Weapon Get Hit
            
            playerStats.dashCool = playerStats.BaseDashCool;
            playerStats.moveSpeed = playerStats.BaseMoveSpeed * 3f;
            
            playerStats.isDash = true; //Dash
            playerStats.dashInvincibility = true;   // Dash Invincibility
            isEndFall = true; //Stand Ani
            playerStats.GetHit = false; //Get Hit
        }

        if (playerStats.isDash)
        {
            DashT += Time.deltaTime;
            DashT = Mathf.Clamp01(DashT);
            playerStats.moveSpeed = Mathf.Lerp(playerStats.moveSpeed, playerStats.BaseMoveSpeed, DashT);
            if (DashT >= 1)
            {
                playerStats.moveSpeed = playerStats.BaseMoveSpeed;
                playerStats.isDash = false;
            }
        }
        else
        {
            if (DashT > 0) //Dash Time
            {
                DashT = 0;
            }
        }
    }

    void OnPauseUI()
    {
        if(cancelAxis)
        {
            UIManager.Instance.TransSettingUI(cancelAxis);
        }
    }

    void OnInvenUI()
    {
        if (invenAxis || cancelAxis)
        {
            if (!attackAxis)
            {
                UIManager.Instance.TransInvenUI(invenAxis, cancelAxis);
            }
        }
    }
    void OnProductUI()
    {
        if(productAxis || cancelAxis)
        {
            UIManager.Instance.TransProductUI(productAxis, cancelAxis);
        }
    }
    void OnMapUI()
    {
        if(mapAxis || cancelAxis)
        {
            UIManager.Instance.TransMapUI(mapAxis, cancelAxis);
        }
    }
    public void PlayerDamage(int damage)
    {
        playerStats.hp -= damage;
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Hit);
        if (playerStats.hp <= 0)
        {
            StartCoroutine(PlayerDeath());
        }
    }
    IEnumerator PlayerDeath() // Player death noise effect coroutine
    {
        Time.timeScale = 0f;
        float t = 0;
        while(t < 1f)
        {
            t += Time.unscaledDeltaTime;
            UIManager.Instance.PlayerHpUI(t);
            yield return null;
        }
        UIManager.Instance.respawnUI.SetActive(true);
    }

    void PlayerAttack()
    {
        playerStats.atk_cool -= Time.deltaTime;
        // Check if the attack can be performed (not in dash, not in end attack, not getting hit) && TimeScale is 1
        if (Time.timeScale >= 0.99f && attackAxis && !animator.GetBool("IsDash") && !animator.GetBool("EndAttack")
            && !animator.GetBool("IsGetHit") && playerStats.atk_cool < 0.0f && isEndFall) // Can perform attack
        {
            GameObject tmpWeapon = currentWeapon;
            playerStats.atk_cool = playerStats.atk_speed;

            foreach(GameObject weapon in WeaponList)
            {
                if(tmpWeapon.GetInstanceID() == weapon.GetInstanceID())
                {
                    if(weapon.name.Contains("WoodBow"))
                    {
                        animator.SetTrigger("WoodBow");
                        playerStats.moveSpeed = playerStats.BaseMoveSpeed / 4f;
                        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Bow);
                    }
                    else if(weapon.name.Contains("IronBow"))
                    {
                        animator.SetTrigger("IronBow"); 
                        playerStats.moveSpeed = playerStats.BaseMoveSpeed / 4f;
                        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Bow);
                    }
                    else if(weapon.name.Contains("Axe") || weapon.name.Contains("Pickaxe"))
                    {
                        animator.SetTrigger("Axe");
                        playerStats.moveSpeed = playerStats.BaseMoveSpeed / 3f;
                    }
                    else if(weapon.name.Contains("Hand"))
                    {
                        animator.SetTrigger("Hand");
                        playerStats.moveSpeed = playerStats.BaseMoveSpeed / 1.3f;
                    }
                    else if(weapon.name.Contains("Spear"))
                    {
                        animator.SetTrigger("Spear");
                        playerStats.moveSpeed = playerStats.BaseMoveSpeed / 3f;
                    }
                    else if(weapon.name.Contains("Knife"))
                    {
                        animator.SetTrigger("Knife");
                        playerStats.moveSpeed = playerStats.BaseMoveSpeed / 1.3f;
                    }
                    else if(weapon.name.Contains("Sword"))
                    {
                        animator.SetTrigger("Attack");
                        playerStats.moveSpeed = playerStats.BaseMoveSpeed / 2f;
                    }
                    break;
                }
            }
            playerStats.isAttack = true; // Set attack state
        }
    }

    public void EquipWeapon()
    {
        // 먼저 필수 참조들이 유효한지 확인
        if (WeaponList == null || WeaponList.Count == 0)
        {
            Debug.LogError("WeaponList is null or empty");
            return;
        }

        if (ItemManager.Instance == null || ItemManager.Instance.weapon == null)
        {
            Debug.LogError("ItemManager or weapon data is not initialized");
            return;
        }

        int ItemIndex = 0; // hand
        int i = 0;
        foreach(GameObject weapon in WeaponList)
        {
            if(weapon.name == "Hand")
            {
                ItemIndex = i;
                break;
            }
            i++;
        }
        int weaponIndex = InventoryManager.Instance?.playerEquip?.items?.FindIndex(x => x.type == 0) ?? -1;
        
        if (weaponIndex != -1) // If weapon found in equipment
        {
            Item tmp = InventoryManager.Instance.playerEquip.items[weaponIndex];
            // Find the corresponding weapon data in item manager
            ItemIndex = ItemManager.Instance.weapon.WeaponData.FindIndex(x => x.id == tmp.id && x.tag == tmp.tag);
        }

        // WeaponList 범위 체크
        if (ItemIndex >= WeaponList.Count)
        {
            Debug.LogError($"Invalid weapon index: {ItemIndex}. WeaponList count: {WeaponList.Count}");
            return;
        }
        
        // 무기 교체 로직
        if (WeaponList.Contains(currentWeapon))
        {
            currentWeapon.SetActive(false);
        }
        
        currentWeapon = WeaponList[ItemIndex];
        if (WeaponList.Contains(currentWeapon))
        {
            currentWeapon.SetActive(true);
        }
        else
        {
            Debug.LogError($"Weapon at index {ItemIndex} is null");
            return;
        }

        // 스탯 설정 전 범위 체크
        if (ItemIndex < ItemManager.Instance.weapon.WeaponData.Count)
        {
            playerStats.weapon_atk = ItemManager.Instance.weapon.WeaponData[ItemIndex].atk;
            playerStats.atk_speed = 1.0f / ItemManager.Instance.weapon.WeaponData[ItemIndex].atk_speed;
        }
        else
        {
            Debug.LogError("Invalid weapon data index");
        }
    }

    public void EndOfAttack()
    {
        playerStats.moveSpeed = playerStats.BaseMoveSpeed;
    }
    public void EndOfMotionTrail()
    {
        if (camScript != null && !camScript.PlayMotionTrail) // If motion trail is not playing
        {
            playerStats.isAttack = false; // Reset attack state
        }
    }
    public void EndOfAni() // Set end attack state
    {
        animator.SetBool("EndAttack", true);
    }
    public void EndDash()
    {
        playerStats.dashInvincibility = false;
        animator.SetBool("IsDash", false);
        AudioManager.Instance.PlayFootStep();
    }
    public void InitEndAttack() // Reset to default state (idle, run)
    {
        animator.SetBool("EndAttack", false);
    }
    public void WoodArrow() // Spawn wooden arrow
    {
        float degree = transform.eulerAngles.y;
        Vector3 dir = new Vector3(90, 0, -degree);
        ObjectPooler.SpawnFromPool("WoodArrow", FirePos.position, Quaternion.Euler(dir));
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Arrow);
    }

    public void IronArrow() // Spawn iron arrow
    {
        float degree = transform.eulerAngles.y;
        Vector3 dir = new Vector3(90, 0, -degree);
        ObjectPooler.SpawnFromPool("IronArrow", FirePos.position, Quaternion.Euler(dir));
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Arrow);
    }

    public void OnKnife() // Trigger knife effect
    {
        foreach (var weapon in weaponScript)
        {
            if (weapon.gameObject.GetInstanceID() == currentWeapon.GetInstanceID())
            {
                StartCoroutine(weapon.Knife());
                break;
            }
        }
    }
    public void OnSword() // Trigger sword effect 
    {
        foreach (var weapon in weaponScript)
        {
            if (weapon.gameObject.GetInstanceID() == currentWeapon.GetInstanceID())
            {
                StartCoroutine(weapon.Sword());
                break;
            }
        }
    }

    public void KnifeSfx()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Knife);
    }
    public void SwordSfx()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Sword);
    }

    public void SpearSfx()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Spear);
    }
    public void AttackHand()
    {
        foreach (var weapon in weaponScript)
        {
            if (weapon.gameObject.GetInstanceID() == currentWeapon.GetInstanceID())
            {
                StartCoroutine(weapon.Hand());
                break;
            }
        }
    }
    public void AttackSpear()
    {
        foreach (var weapon in weaponScript)
        {
            if (weapon.gameObject.GetInstanceID() == currentWeapon.GetInstanceID())
            {
                StartCoroutine(weapon.Spear());
                break;
            }
        }
    }
    public void AttackAxe()
    {
        foreach (var weapon in weaponScript)
        {
            if (weapon.gameObject.GetInstanceID() == currentWeapon.GetInstanceID())
            {
                StartCoroutine(weapon.Axe());
                break;
            }
        }
    }
    public void GetHit() // Get hit by monster
    {
        if (Time.timeScale >= 0.99f && !animator.GetBool("IsDash"))
        {
            playerStats.GetHit = true; // Set hit state
            isEndFall = true; // Set end fall state

            currentWeapon.GetComponent<WeaponScript>().EnableHitBox(); // Enable weapon collider
            animator.SetBool("EndAttack", true); // Set end attack state

            StartCoroutine(HitVolume());  // Trigger hit volume effect

            animator.SetBool("IsGetHit", true);
            animator.SetTrigger("GetHit");
            playerStats.moveSpeed = playerStats.BaseMoveSpeed * 0.5f;

        }
    }
    public void EndGetHit()
    {
        playerStats.GetHit = false;
        currentWeapon.GetComponent<WeaponScript>().P_GetHit = false; // Reset weapon hit state
        playerStats.moveSpeed = playerStats.BaseMoveSpeed;
        animator.SetBool("IsGetHit", false);
    }

    void EatFood()
    {
        eatCool += Time.deltaTime;
        if (Time.timeScale != 1 || !isEndFall || !eatAxis) return;
        
        Item food = InventoryManager.Instance.playerEquip.items.Find(x => x.type == 2);
        if (food == null) return; // If food is empty, return
        if (eatCool < food.reuse) return;
        if (food.quantity > 0)
        {
            StartCoroutine(UIManager.Instance.EnableFood(2f));
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.Eat);
            food.quantity--;
            playerStats.hp += food.reg;
            if(playerStats.hp > playerStats.maxHp)
            {
                playerStats.hp = playerStats.maxHp;
            }
            eatCool = 0f; // Reset cooldown
        }
    }
    public void StartFall()
    {
        isEndFall = false;
        animator.SetBool("IsStand", false);
        animator.SetTrigger("Fall");
    }

    public void EndFall()
    {
        animator.SetTrigger("Stand");
    }
    public void EndStand()
    {
        isEndFall = true;
        animator.SetBool("IsStand", true);
    }
    IEnumerator HitVolume()
    {
        float t = 0;
        while (playerStats.GetHit)
        {
            t = 0;
            while (playerStats.GetHit && t < 1f) // Fade in hit effect
            {
                t += Time.smoothDeltaTime * 2f;
                if (t < 1f) vignette.intensity.value = Mathf.Lerp(0, 0.3f, t);
                yield return null;
            }
            t = 0f;
            while (playerStats.GetHit && t < 1f) // Fade out hit effect
            {
                t += Time.smoothDeltaTime * 2.5f;
                if (t < 1f) vignette.intensity.value = Mathf.Lerp(0.3f, 0f, t);
                yield return null;
            }
        }
        t = 0;
        float currentValue = vignette.intensity.value;
        while(t < 1f)
        {
            t += Time.smoothDeltaTime * 4f;
            vignette.intensity.value = Mathf.Lerp(currentValue, 0f, t);
            yield return null;   
        }
        vignette.intensity.value = 0f;
    }
}