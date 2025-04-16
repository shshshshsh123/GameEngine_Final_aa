using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Monster : MonoBehaviour
{
    public enum MonsterTag
    {
        Guardian = 1,
        Scout = 2,
        Ghost = 3,
        Bulkaness = 4,
        Spider = 5,
        Player = 6,
        Dummy = 7
    };

    [System.Serializable]
    public struct MonsterStat //hitCount1,2는 guardian 전용
    {
        public int hp;
        public int maxHp;
        public int HitCount;
        public int HitCount1;   
        public int HitCount2;
        public bool GetHit;
        public bool GetHit1;
        public bool GetHit2;
        [Space(5f)]
        public float BaseMoveSpeed;
        public float moveSpeed;
        [Space(5f)]
        public int atk;
        public float atk_speed;
        public float atk_cool;
        public int atk_count;
        [Space(5f)]
        public float detect_dis;    // 인식거리
        public float return_dis;    // 스폰지점에서 일정이상 멀어지면 되돌아감
    };
    [System.Serializable]
    public struct MonsterSp
    {
        [Space(5f)]
        public string SpTag;
        public string Kind;
        [Space(5f)]
        public int atk;
        public int atk_count;
        public float atk_range;
        [Space(5f)]
        public float reuse; //쿨타임
        public float duration;//지속시간
    };
    //드래곤 hp bar
    protected GameObject hpBar = null;
    protected Image HpBarFront;
    protected Image HpBarBG;
    protected GameObject HPBarObj;
    protected GameObject HPBarBgObj;
    protected RectTransform hpBarTransform;
    public Transform HpBarPos;  

    protected bool isAttackApply = false; //공격 타점 지정하는 변수


    protected bool IsAttacked = false; //몬스터 피격상태 체크 변수
    protected bool IsFlame = false; //FlameAttack 중복 타격 방지용 변수
    
    public MonsterStat monsterStat;
    public MonsterTag monsterTag;
    public List<MonsterSp> MonsterSkill = new List<MonsterSp>(); //드래곤 스킬
    
    // 몬스터 정보 불러오기
    private Dictionary<string, object> MonsterData;
    private List<Dictionary<string, object>> SkillData = new List<Dictionary<string, object>>(); //드래곤 스킬 데이터
    private Dictionary<string, object> MonsterExp;

    //플레이어 카메라 스크립트
    GameObject CamObj;
    protected Cam CamScript;
    protected virtual void OnEnable()
    {
        CamObj = Camera.main.gameObject;
        CamScript = CamObj.GetComponent<Cam>();
    }
    protected virtual void OnDisable()
    {
        
    }
    
    protected virtual void Start() //몬스터 개별 스크립트에 onSceneLoaded 추가 필수!!
    {   
        foreach (var monster in GameManager.Instance.monsterData)
        {
            if (monster["tag"].ToString() == monsterTag.ToString())
            {
                MonsterData = monster;
                break;
            }
        }


        foreach (var skill in GameManager.Instance.skillData)
        {
            if (String.Equals(skill["mon_tag"].ToString(), monsterTag.ToString()))
            {
                SkillData.Add(skill);
            }
        }

        foreach (var expAmount in GameManager.Instance.getExp)
        {
            if (expAmount["tag"].ToString() == monsterTag.ToString())
            {
                MonsterExp = expAmount;
                break;
            }
        }

        MonsterStatInit();
        MonsterSkillInit();
    }
    protected virtual void Update()
    {
        if(monsterTag != MonsterTag.Bulkaness) 
        {
            if (hpBarTransform != null && HpBarPos != null)
            {
                hpBarTransform.position = HpBarPos.position; // HpBarPos에 따라 위치 업데이트
                Vector3 direction = (HpBarPos.position - Camera.main.transform.position).normalized;
                hpBar.transform.rotation = Quaternion.LookRotation(direction); // 카메라 방향으로 회전
            }
            else
            {
                Debug.Log("hpBarTransform 또는 HpBarPos가 null입니다.");
            }
        }
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        //if (other.gameObject.CompareTag("Player"))
        //{
        //    //플레이어가 대쉬상태일때 몬스터가 공격하면 motionTrail실행
        //    if (Player.Instance.playerStats.dashInvincibility)
        //    {
        //        CamScript.StartCoroutine(CamScript.HitStop(0.05f, 55, false));
        //    }
        //}
    }
    public void DropItem()
    {
        int foodType = 0;
        int quantity = 0;

        if(monsterTag == MonsterTag.Spider)
        {
            foodType = 1;
            quantity = UnityEngine.Random.Range(1, 4); //Spider, Scout은 1~3개 과일 드롭
        }
        else if(monsterTag == MonsterTag.Scout)
        {
            foodType = 2;
            quantity = UnityEngine.Random.Range(1, 4); //Spider, Scout은 1~3개 계란 드롭
        }
        else if(monsterTag == MonsterTag.Guardian)
        {   
            foodType = 3;
            quantity = UnityEngine.Random.Range(3, 6); //Guardian은 3~5개의 고기 드롭
        }   
        else if(monsterTag == MonsterTag.Bulkaness)
        {
            foodType = 4;
            quantity = 1; //Bulkaness는 용의 눈물, 용의 비늘, 검붉은 보석중 하나 드롭
        }

        if(foodType == 1) //거미
        {
            InventoryManager.Instance.AddItem(ItemManager.Instance.food.FoodData[0], quantity); //계란
        }
        else if(foodType == 2) //스카우트
        {
            InventoryManager.Instance.AddItem(ItemManager.Instance.food.FoodData[1], quantity); //과일
        }
        else if(foodType == 3) //가디언
        {
            InventoryManager.Instance.AddItem(ItemManager.Instance.food.FoodData[2], quantity); //고기
        }
        else if(foodType == 4) //용
        {
            int index = UnityEngine.Random.Range(9, 12); //용의 눈물, 용의 비늘, 검붉은 보석중 하나 드롭(index = 9~11)
            InventoryManager.Instance.AddItem(ItemManager.Instance.mat.MatData[index], quantity); //용의 눈물
        }
    }
    public void TakeDamage(int damage)
    {
        if (monsterStat.hp > 0) //피격을 받지 않았고 hp가 0이상이면 실행
        {
            monsterStat.HitCount++; 
            if(monsterTag == MonsterTag.Guardian) 
            {
                monsterStat.HitCount1++;
                monsterStat.HitCount2++;
            }
                
            if(monsterTag == MonsterTag.Scout || monsterTag == MonsterTag.Spider)
            {
                //spider, Scout 피격 로직 추가
            }
            else if(monsterTag == MonsterTag.Guardian)
            {
                if (monsterStat.HitCount >= 4) //기본 hit조건
                {
                    monsterStat.HitCount = 0;
                    monsterStat.GetHit = true;
                }   
                if(monsterStat.HitCount1 >= 6) //sp1 조건
                {
                    monsterStat.HitCount1 = 0;
                    monsterStat.GetHit1 = true;
                }
                if(monsterStat.HitCount2 >= 9)//sp2 조건
                {
                    monsterStat.HitCount2 = 0;
                    monsterStat.GetHit2 = true;
                }
            }
            else if (monsterTag == MonsterTag.Bulkaness)
            {
                if(monsterStat.HitCount >= 5 )
                {
                    monsterStat.HitCount = 0;
                    monsterStat.GetHit = true;  
                }
            }
            StartCoroutine(CanAttacked()); //hpbar 및 피격상태 bool값 변경
            monsterStat.hp -= damage;
        }
    }

    protected virtual void GetExp()
    {
        ExpManager.Instance.ManageLevel(int.Parse(MonsterExp["exp"].ToString()));
    }

    public void AttackDamage(int damage) //몬스터 공격 성공
    {
        if (Player.Instance.playerStats.hp > 0)
        {
            Player.Instance.GetHit(); //피격 애니메이션 실행
            Player.Instance.PlayerDamage(damage);
        }
    }
    public void EnableHpBar() //hp bar 활성화 및 fillamount 초기화
    {
        if (hpBar != null && !hpBar.activeSelf) //한번만 실행
        {   
            hpBar.SetActive(true);
            HpBarFront.fillAmount = 1;
            HpBarBG.fillAmount = 1;
        }
    }

    public void DisableHpBar()//hp bar 비활성화
    {
        if (hpBar != null && hpBar.activeSelf)
        {
            hpBar.SetActive(false);
        }
    } 
    IEnumerator CanAttacked() //Dragon HP Bar, hp 감소 함수
    {
        if (!IsAttacked) //피격을 받았는데 isattacked가 false 일때 실행
        {
            IsAttacked = true; //피격을 받은 상태로 변경
            int LastHp = monsterStat.hp;
            float t = 0;
            while (t < 1f)
            {
                t += Time.smoothDeltaTime * 10;
                HpBarFront.fillAmount = Mathf.Lerp(LastHp, monsterStat.hp, t) / monsterStat.maxHp;
                yield return null;
            }
            StartCoroutine(SubtractHpBG());
            IsAttacked = false; //피격 상태 해제
        }
    }
    IEnumerator SubtractHpBG() //hp Bar BG는 hp bar보다 천천히 감소하도록 하는 함수
    {
        float t = 0;
        while (HpBarBG.fillAmount >= HpBarFront.fillAmount)
        {
            t += Time.smoothDeltaTime * 0.05f;
            t = Mathf.Clamp(t, 0.01f, 1);
            HpBarBG.fillAmount = Mathf.Lerp(HpBarBG.fillAmount, HpBarFront.fillAmount - 0.01f, t);
            yield return null;      
        }
    }
    
    protected void MonsterStatInit() //씬 재입장시 몬스터 스텟 초기화를 위해 호출
    {
        monsterStat.maxHp = int.Parse(MonsterData["hp"].ToString());
        monsterStat.hp = monsterStat.maxHp;
        monsterStat.HitCount = 0;
        monsterStat.HitCount1 = 0;      
        monsterStat.HitCount2 = 0;  
        monsterStat.GetHit = false; //시작시 초기화
        monsterStat.GetHit1 = false;
        monsterStat.GetHit2 = false;
        monsterStat.atk = int.Parse(MonsterData["atk"].ToString());
        monsterStat.atk_speed = float.Parse(MonsterData["atk_speed"].ToString());
        monsterStat.atk_cool = monsterStat.atk_speed;
        monsterStat.atk_count = 0;
        monsterStat.BaseMoveSpeed = float.Parse(MonsterData["walk_speed"].ToString());
        monsterStat.moveSpeed = monsterStat.BaseMoveSpeed;
        monsterStat.detect_dis = float.Parse(MonsterData["detect_dis"].ToString());
        monsterStat.return_dis = float.Parse(MonsterData["return_dis"].ToString());
    }

    void MonsterSkillInit()
    {
        for(int i = 0; i < SkillData.Count; i++) //SkillData에 저장된 스킬수 만큼 반복
        {
            MonsterSp monSkill = new MonsterSp();
            monSkill.SpTag = SkillData[i]["sp_tag"].ToString();
            monSkill.Kind = SkillData[i]["kind"].ToString();
            monSkill.atk = int.Parse(SkillData[i]["atk"].ToString());
            monSkill.atk_count = int.Parse(SkillData[i]["atk_count"].ToString());
            monSkill.reuse = float.Parse(SkillData[i]["reuse"].ToString());
            monSkill.duration = float.Parse(SkillData[i]["duration"].ToString());
            MonsterSkill.Add(monSkill);
        }
    }
}
