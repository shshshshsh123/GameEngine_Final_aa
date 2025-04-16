using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class SpiderCtrl : Monster
{
    public enum State
    {
        Idle,
        Trace,
        Attack,
        Die,
        Default
    }
    public State state = State.Default;
    public float MaxDistance = 20f;
    public float AttackDistance = 2f;
    public bool isDie = false;
    public Material mat; 
    private Transform SpiderTr;
    private Transform PlayerTr;
    private NavMeshAgent _SpiderAgent;
    private Animator _SpiderAnim;
    bool isRot = true;

    private readonly int hashTraceP = Animator.StringToHash("Spider_M");
    private readonly int hashAttackP = Animator.StringToHash("Spider_A");

    protected override void OnEnable()
    {
        base.OnEnable();
        if (hpBar != null) //첫시작에는 실행x
        {
            InitHpBar(); //HPBar 초기화
            MonsterStatInit();
        }
    }

    protected override void OnDisable() //비활성화시 사망상태이므로 isDie 초기화
    {
        if (isDie) //게임 시작시 비활성화 상태이면 아이템을 지급해서 게임을 시작하고 몬스터가 사망한 후에 아이템 지급하도록 if문 추가
        {
            base.OnDisable(); //몬스터 사망시 플레이어에게 아이템 지급
            DisableHpBar();
            isDie = false;
        }
        ObjectPooler.ReturnToPool(gameObject);
    }

    protected override void Start()
    {
        base.Start();
        SpiderTr = GetComponent<Transform>();
        PlayerTr = Player.Instance.transform;
        _SpiderAnim = GetComponent<Animator>();
        _SpiderAgent = GetComponent<NavMeshAgent>();
        _SpiderAgent.speed = monsterStat.BaseMoveSpeed / 2;
        InitHpBar();
        DisableHpBar();   //게임 시작시 HPBar 비활성화  
        StartCoroutine(CheckSpiderState()); // 몬스터 상태 확인 코루틴 시작
        StartCoroutine(LookPlayer()); // 플레이어를 바라보는 코루틴 시작
        StartCoroutine(SpiderAction()); // 몬스터 행동 코루틴 시작
    }

    protected override void OnTriggerEnter(Collider other)
    {
        //플레이어가 대쉬상태일때 몬스터가 공격하면 motionTrail실행
        if (Player.Instance.playerStats.dashInvincibility)
        {
            if (isAttackApply && other.gameObject.CompareTag("Player")) //몬스터가 공격을 시작했을때 플레이와 접촉을 하면 경직효과 실행
            {
                CamScript.StartCoroutine(CamScript.HitStop(0.05f, 55, false));
            }
        }
        else //대쉬 무적상태가 아니면 
        {
            if (other.gameObject.CompareTag("Weapon"))
            {
                if (!monsterStat.GetHit)//피격 상태가 아니면    
                {
                    TakeDamage(Player.Instance.playerStats.atk + Player.Instance.playerStats.weapon_atk); //플레이어 공격 성공
                }
            }
            else if (other.gameObject.CompareTag("Player"))//몬스터 공격 성공
            {
                if (!Player.Instance.playerStats.AttackInvinci)//플레이어가 공격 무적상태가 아니면
                {
                    //공격이 적용가능한 시점(중복 타격 방지), 피격을 받지 않았으면 실행
                    if (isAttackApply && !IsAttacked)
                    {
                        AttackDamage(monsterStat.atk);//몬스터 공격 성공
                    }

                    isAttackApply = false;
                }
            }
        }
    }

    IEnumerator CheckSpiderState()
    {
        while (!isDie) // 몬스터가 죽지 않은 동안
        {
            if (monsterStat.hp <= 0) // HP가 0 이하일 경우
            {
                state = State.Die; // 상태를 죽음으로 변경
                GetExp(); // 경험치 획득
                isDie = true; // 몬스터 죽음 상태로 설정
                yield break; // 코루틴 종료
            }
            else
            {
                float distance = Vector3.Distance(transform.position, PlayerTr.position);// 플레이어와의 거리 계산
                if (distance <= AttackDistance)
                {
                    state = State.Attack; // 공격 상태
                }
                else if (distance <= MaxDistance)
                {
                    state = State.Trace;
                }
                else
                {
                    state = State.Idle; // 대기 상태
                }
            }
            yield return null; // 다음 프레임 대기
        }
        yield break;
    }

    IEnumerator SpiderAction()
    {
        while (!isDie) // 몬스터가 죽지 않은 동안
        {
            switch (state) // 현재 상태에 따른 행동
            {
                case State.Idle:
                    StartCoroutine(LookPlayer()); // 플레이어를 바라보는 코루틴 시작
                    if (_SpiderAnim.GetBool(hashTraceP) && _SpiderAnim.GetBool(hashAttackP))
                    {
                        DisableBasicAttack();
                        StartOfAni(); // 애니메이션 시작 시 에이전트 멈추기
                        _SpiderAgent.SetDestination(transform.position); // 현재 위치로 정지
                        _SpiderAnim.SetBool(hashTraceP, false); // 추적 애니메이션 종료
                        _SpiderAnim.SetBool(hashAttackP, false); // 공격 애니메이션 종료
                        Debug.Log("기본상태로 전환");
                    }
                    break;

                case State.Trace:
                    // 플레이어 위치에서 공격 거리만큼 떨어진 위치로 이동
                    Vector3 Distance = (transform.position - PlayerTr.position).normalized;
                    _SpiderAgent.SetDestination(PlayerTr.position + Distance * (AttackDistance - 0.01f));

                    Vector3 targetDirection = (PlayerTr.position - transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(targetDirection);
                    lookRotation *= Quaternion.Euler(0, 180, 0);

                    SpiderTr.rotation = Quaternion.Slerp(SpiderTr.rotation, lookRotation, Time.deltaTime * 5f);

                    if (!_SpiderAnim.GetBool(hashTraceP))
                    {
                        StartCoroutine(LookPlayer()); // 플레이어를 바라보는 코루틴
                        DisableBasicAttack();
                        _SpiderAnim.SetBool(hashTraceP, true); // 추적 애니메이션 시작
                        Debug.Log("추적상태로 전환");
                    }
                    break;

                case State.Attack:
                    if (!_SpiderAnim.GetBool(hashAttackP))
                    {
                        _SpiderAnim.SetBool(hashTraceP, false); // 추적 애니메이션 종료
                        _SpiderAnim.SetBool(hashAttackP, true); // 공격 애니메이션 시작
                        Debug.Log("공격상태로 전환");
                    }
                    break;

                case State.Die:
                    Debug.Log("사망상태로 전환");
                    yield break; // 죽음 상태일 때 코루틴 종료
            }
            yield return null; // 다음 프레임 대기
        }
        DieAni(); // 죽음 애니메이션 실행
    }

    IEnumerator LookPlayer(float speed = 1.2f) // 플레이어를 바라보도록 회전하는 코루틴
    {
        Vector3 direction = (PlayerTr.position - SpiderTr.position).normalized; // 플레이어 방향 계산
        Quaternion lookRotation = Quaternion.LookRotation(direction); // 회전할 각도 계산


        if (isRot)
        {
            isRot = false; // 회전 중복 방지
            float t = 0;
            while (t < 1f)
            {
                t += Time.smoothDeltaTime * speed; // 회전 속도 조절
                t = Mathf.Clamp01(t); // t 값 클램프
                SpiderTr.rotation = Quaternion.Slerp(SpiderTr.rotation, lookRotation, t); // 부드러운 회전
                yield return null; // 다음 프레임까지 대기
            }
            isRot = true; // 회전 가능 상태로 복원
        }
        yield break; // 코루틴 종료
    }

    public void StartOfAni()
    {
        _SpiderAgent.isStopped = true; // 에이전트 정지
        _SpiderAgent.updateRotation = false; // 회전 업데이트 비활성화
    }

    public void EndOfAni()
    {
        _SpiderAgent.isStopped = false; // 에이전트 재개
        _SpiderAgent.updateRotation = true; // 회전 업데이트 활성화
    }

    public void StartBasicAttack()
    {
        StartOfAni();
        StartCoroutine(LookPlayer(2f));
    }

    public void EndOfBasicAttack()
    {
        EndOfAni();
        _SpiderAnim.SetBool(hashAttackP, false);
    }

    public void EnableBasicAttack()
    {
        isAttackApply = true;
    }
    public void DisableBasicAttack()
    {
        isAttackApply = false;
    }

    IEnumerator DieMat()
    {
        float t = 0f;
        while (mat.GetFloat("_NoiseAmount") < 1f)
        {
            t += Time.smoothDeltaTime * 1.2f;
            mat.SetFloat("_NoiseAmount", Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
        mat.SetFloat("_NoiseAmount", 0.0f);
        state = State.Default;
        gameObject.SetActive(false);
    }

    public void EnableDieMat()
    {
        StartCoroutine(DieMat());
    }

    void DieAni()
    {
        _SpiderAgent.isStopped = true; // 에이전트 정지
        _SpiderAgent.updateRotation = false; // 회전 업데이트 비활성화
        _SpiderAnim.SetBool(hashTraceP, false); // 추적 애니메이션 종료
        _SpiderAnim.SetBool(hashAttackP, false); // 공격 애니메이션 종료

        _SpiderAnim.SetTrigger("Spider_Die"); // 죽음 애니메이션 트리거
    }

    void InitHpBar()
    {
        Vector3 direction = (HpBarPos.position - Camera.main.transform.position).normalized;
        hpBar = ObjectPooler.SpawnFromPool("SpiderHUD", HpBarPos.position, Quaternion.LookRotation(direction));
        
        hpBar.transform.SetParent(UIManager.Instance.HUD.transform, false); // 부모를 HUD의 RectTransform으로 설정      
        hpBarTransform = hpBar.GetComponent<RectTransform>();
        
        HPBarObj = hpBar.transform.Find("BG").gameObject;
        HPBarBgObj = hpBar.transform.Find("HP").gameObject;
    
        HpBarFront = HPBarObj.GetComponent<Image>();
        HpBarBG = HPBarBgObj.GetComponent<Image>();
    }
}