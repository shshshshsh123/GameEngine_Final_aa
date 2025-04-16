using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class GuardianCtrl : Monster
{
    // 몬스터의 상태를 정의하는 열거형
    public enum State
    {
        Default,
        Idle,
        Trace,
        Attack,
        Die,
        GetHit,
        AtkSp1,
        AtkSp2
    }

    // 몬스터의 현재 상태
    public State state = State.Default;
    public float MaxDistance = 50f; // 몬스터가 추적할 최대 거리
    public float AttackDistance = 10f; // 공격 가능한 거리
    public bool isDie = false; // 몬스터의 생사 상태
    public Material mat; //몬스터 mat
    private Material MainMat;
    public Transform VFX3Pos;

    private Transform guardianTr; // 가디언의 트랜스폼
    private Transform playerTr; // 플레이어의 트랜스폼
    private NavMeshAgent _GuardianAgent; // 가디언의 네비게이션 에이전트
    private Animator _GuardianAnim; // 가디언의 애니메이터

    // 스킬 사용 상태 변수들
    bool _isGuardianSpA1 = false;
    bool _isGuardianSpA2 = false;
    bool isRot = true; // 회전 상태

    // 스킬 인덱스
    int spIndex1;
    int spIndex2;
    int spIndex3;

    bool isFirst = false; //게임 첫 시작시 초기화

    private bool GuardianGetHit = false;

    // 애니메이션 해시값
    private readonly int hashTrace = Animator.StringToHash("Guardian_M");
    private readonly int hashAttack = Animator.StringToHash("Guardian_A");
    private readonly int hashHit = Animator.StringToHash("Guardian_H");
    protected override void OnEnable()
    {
        base.OnEnable();
        if(isFirst)
        {
            InitAnimatorState(); // 애니메이터 상태 초기화 추가
            MonsterStatInit();  
            isDie = false;  
            state = State.Default;
            InitComponent(); //HPBar 초기화
        }   
    }
    protected override void OnDisable() //비활성화시 사망상태이므로 isDie 초기화
    {
        if(isDie) //게임 시작시 비활성화 상태이면 아이템을 지급해서 게임을 시작하고 몬스터가 사망한 후에 아이템 지급하도록 if문 추가
        {
            base.OnDisable(); //몬스터 사망시 플레이어에게 아이템 지급
            isDie = false;  
        }
        ObjectPooler.ReturnToPool(gameObject);
    }
    protected override void Start()
    {
        base.Start();

        spIndex1 = MonsterSkill.FindIndex(x => x.SpTag == "G_Sp1");
        spIndex2 = MonsterSkill.FindIndex(x => x.SpTag == "G_Sp2");
        spIndex3 = MonsterSkill.FindIndex(x => x.SpTag == "G_Sp3");

        isFirst = true;
        
        guardianTr = gameObject.transform; // 가디언 트랜스폼 초기화
        _GuardianAgent = GetComponent<NavMeshAgent>(); // 네비게이션 에이전트 초기화
        _GuardianAnim = GetComponent<Animator>(); // 애니메이터 초기화
        playerTr = Player.Instance.transform;   
        _GuardianAgent.speed = monsterStat.BaseMoveSpeed; // 몬스터 기본 이동 속도 설정
        
        MainMat = new Material(mat);
        GetComponentInChildren<SkinnedMeshRenderer>().material = MainMat;

        InitComponent();
    }
    private void OnDestroy()
    {
        if (MainMat != null)
        {
            Destroy(MainMat);
        }
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
                        isAttackApply = false;
                    }
                }
            }
        }
    }
    void OnApplicationQuit() //사망 효과 중간에 게임 종료시 mat 초기화
    {   
        MainMat.SetFloat("_NoiseAmount", 0.0f);
    }
    // 가디언의 상태를 체크하는 코루틴
    IEnumerator CheckGuardianState()
    {
        float SpA1Time = 0; // 스킬 1 쿨타임
        float SpA2Time = 0; // 스킬 2 쿨타임

        while (!isDie) // 몬스터가 죽지 않은 동안
        {
            monsterStat.atk_cool += Time.smoothDeltaTime; // 공격 쿨타임 증가

            if (monsterStat.hp <= 0) // HP가 0 이하일 경우
            {
                state = State.Die; // 상태를 죽음으로 변경
                GetExp(); // 경험치 획득
                isDie = true;
                yield break; // 코루틴 종료
            }

            else if (monsterStat.GetHit && !GuardianGetHit)
            {
                monsterStat.GetHit = false; 
                GuardianGetHit = true;
                
                state = State.GetHit;
                _GuardianAnim.SetBool("EndGetHit", false); //GetHit bool 값 초기화
                
                yield return new WaitUntil(() => !GuardianGetHit);
            }

            else
            {
                // 히트 카운트에 따라 상태 변경
                if (monsterStat.GetHit1 && !_isGuardianSpA1)
                {
                    monsterStat.GetHit1 = false;
                    _GuardianAnim.SetBool("EndSpA1", false);
                    _isGuardianSpA1 = true;
                    state = State.AtkSp1;
                    yield return new WaitUntil(() => !_isGuardianSpA1); // 스킬 1 종료 대기 
                }
                else if (monsterStat.GetHit2 && !_isGuardianSpA2) //monsterStat.HitCount > 10 //monsterStat.hp < 480
                {
                    monsterStat.GetHit2 = false;
                    _GuardianAnim.SetBool("EndSpA2", false);    
                    _isGuardianSpA2 = true;
                    state = State.AtkSp2; // 스킬 2 사용
                    
                    yield return new WaitUntil(() => !_isGuardianSpA2); // 스킬 2 종료 대기
                }
                else
                {
                    // 플레이어와의 거리 계산
                    float distance = Vector3.Distance(transform.position, playerTr.position);
                    if (distance <= AttackDistance)
                    {
                        state = State.Attack; // 공격 상태
                    }
                    else if (distance <= MaxDistance)
                    {
                        if(!_GuardianAnim.GetBool(hashAttack))
                        {
                            state = State.Trace; // 추적 상태
                            EnableHpBar();
                        }
                    }
                    else
                    {
                        state = State.Idle; // 대기 상태
                        DisableHpBar();
                    }
                }
            }

            SpA1Time += Time.smoothDeltaTime; // 스킬 1 쿨타임 증가
            SpA2Time += Time.smoothDeltaTime; // 스킬 2 쿨타임 증가

            
            yield return null; // 다음 프레임 대기
        }
    }

    // 가디언의 행동을 처리하는 코루틴
    IEnumerator GuardianAction()
    {
        while (!isDie) // 몬스터가 죽지 않은 동안
        {
            switch (state) // 현재 상태에 따른 행동
            {
                case State.Idle:
                    StartCoroutine(LookPlayer()); // 플레이어를 바라보는 코루틴 시작
                    if (_GuardianAnim.GetBool(hashTrace) && _GuardianAnim.GetBool(hashAttack))
                    {
                        DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                        StartOfAni();    //idle 첫시작시 agent 멈추기
                        _GuardianAgent.SetDestination(transform.position); // 현재 위치로 정지
                        _GuardianAnim.SetBool(hashTrace, false);
                        _GuardianAnim.SetBool(hashAttack, false);
                    }
                    break;

                case State.Trace:
                    // 플레이어 위치에서 공격 거리만큼 떨어진 위치로 이동
                    Vector3 Distance = (transform.position - playerTr.position).normalized;
                    _GuardianAgent.SetDestination(playerTr.position + Distance * (AttackDistance - 0.01f));
                    
                    if (!_GuardianAnim.GetBool(hashTrace) && !_GuardianAnim.GetBool(hashAttack)) //공격 진행중 이동하는 오류 해결
                    { 
                        StartCoroutine(LookPlayer(2.0f));   
                        DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제 
                        _GuardianAnim.SetBool(hashTrace, true); // 추적 애니메이션 시작
                    }
                    break;

                case State.Attack:
                    if (!_GuardianAnim.GetBool(hashAttack))
                    {
                        StartCoroutine(LookPlayer(2f));
                        _GuardianAnim.SetBool(hashTrace, false); // 추적 애니메이션 종료
                        _GuardianAnim.SetBool(hashAttack, true); // 공격 애니메이션 시작
                    }
                    break;

                case State.AtkSp1:
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                    _GuardianAgent.SetDestination(transform.position); // 현재 위치로 정지
                    _GuardianAnim.SetTrigger("Guardian_SpA1"); // 스킬 1 시작
                    
                    yield return new WaitUntil(() => !_isGuardianSpA1); // 스킬 1 종료 대기
                    break;

                case State.AtkSp2:
                    StartOfAni();  
                    // 현재 위치로 정지하고 이동 목표를 설정
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                    _GuardianAnim.SetTrigger("Guardian_SpA2"); // 스킬 2 시작 애니메이션 트리거 설정
                    
                    yield return new WaitUntil(() => !_isGuardianSpA2); // 스킬 2 종료 대기
                    break;

                case State.GetHit:
                    StartOfAni();
                    _GuardianAnim.SetTrigger(hashHit);
                    _GuardianAnim.SetBool(hashAttack, false);
                    _GuardianAnim.SetBool(hashTrace, false);
                    
                    _GuardianAgent.SetDestination(transform.position);
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                    
                    yield return new WaitUntil(() => !GuardianGetHit);
                    break;

                case State.Die:
                    yield break;
            }
            yield return null; // 다음 프레임 대기
        }
        DropItem();
        DisableHpBar();
        DieAni(); // 죽음 애니메이션 실행
    }
    IEnumerator LookPlayer(float speed = 1.2f)  // 플레이어를 바라보도록 회전하는 코루틴
    {
        // 플레이어 방향 계산
        Vector3 direction = (playerTr.position - guardianTr.position).normalized;
        // 회전할 각도 계산
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        
        if (isRot)
        {
            isRot = false; // 회전 중복 방지
            float t = 0;
            while (t < 1f)
            {
                t += Time.smoothDeltaTime * speed; // 회전 속도 조절
                t = Mathf.Clamp01(t); // t 값 클램프
                guardianTr.rotation = Quaternion.Slerp(guardianTr.rotation, lookRotation, t); // 부드러운 회전
                yield return null; // 다음 프레임까지 대기
            }
            isRot = true; // 회전 가능 상태로 복원
        }
        yield break; // 코루틴 종료
    }


    // 스킬 1을 사용하는 코루틴
    IEnumerator UseSp1()
    {
        UIManager.Instance.IsSlow = true;
        UIManager.Instance.HUD_Slow.SetActive(true);
        float playerSpeed = Player.Instance.playerStats.BaseMoveSpeed; // 플레이어 이동속도 감소
        Player.Instance.playerStats.moveSpeed = playerSpeed * 0.8f; // 플레이어 이동속도 감소

        yield return new WaitForSeconds(MonsterSkill[spIndex1].duration); // 스킬 지속시간 동안 이동속도 유지

        UIManager.Instance.IsSlow = false;
        Player.Instance.playerStats.moveSpeed = playerSpeed; // 원래 이동속도로 복원
        state = State.Idle;
        yield break; // 코루틴 종료
    }
    IEnumerator UseSp2() //돌진 스킬
    {        
        EndOfAni();
        float distance = Vector3.Distance(transform.position, playerTr.position);   
        _GuardianAgent.speed = monsterStat.BaseMoveSpeed * 2;
        while (distance > AttackDistance) //거리가 임계값보다 작으면 종료
        {
            distance = Vector3.Distance(transform.position, playerTr.position);     
            _GuardianAgent.SetDestination(playerTr.position);
            yield return null; // 다음 프레임까지 대기
        }   
        StartOfAni();
        _GuardianAgent.speed = monsterStat.BaseMoveSpeed;
        _GuardianAnim.SetTrigger("Guardian_SpA3"); // 스킬 2 시작
        yield break;
    }
    public void StartSp1()
    {
        StartCoroutine(UseSp1());
    }
    public void StartSp2()
    {
        StartCoroutine(UseSp2());
    }
    public void StartSp3()
    {
        transform.LookAt(playerTr.position);        
    }
    public void EndSp2Scream()
    {
        _GuardianAnim.SetTrigger("ScreamToSp2");
    }
    public void EndOfSp1()
    {
        _isGuardianSpA1 = false;
        
        _GuardianAnim.SetBool("EndSpA1", true);
        _GuardianAnim.SetBool(hashAttack, false);
        state = State.Trace;
    }
    public void EndOfSp2()
    {   
        _GuardianAgent.SetDestination(transform.position);  
        _isGuardianSpA2 = false;
        
        _GuardianAnim.SetBool("EndSpA2", true);
        _GuardianAnim.SetBool(hashAttack, false);
        state = State.Trace;
    }
    public void StartOfAni()
    {
        _GuardianAgent.isStopped = true;
        _GuardianAgent.updateRotation = false;
    }

    public void EndOfAni()
    {
        _GuardianAgent.isStopped = false;
        _GuardianAgent.updateRotation = true;
    }
    public void StartBasicAttack()
    {
        StartOfAni();
        StartCoroutine(LookPlayer(2f));
    }
    public void EndOfBasicAttack()
    {
        EndOfAni();
        _GuardianAnim.SetBool(hashAttack, false);
    }
    public void EndGetHit() //기본 hit anf
    {
        GuardianGetHit = false;
        monsterStat.GetHit = false; //가디언 hit ani 종료시 초기화
        _GuardianAnim.SetBool("EndGetHit", true);
    }
    public void EnableBasicAttack()
    {
        isAttackApply = true;
    }
    public void DisableBasicAttack()
    {
        isAttackApply = false;
    }
    void DieAni()
    {
        _GuardianAgent.isStopped = true;
        _GuardianAgent.updateRotation = false;
        _GuardianAnim.SetBool(hashTrace, false);
        _GuardianAnim.SetBool(hashAttack, false);

        _GuardianAnim.SetTrigger("Guardian_Die");
    }
    IEnumerator DieMat()
    {
        float t = 0f;
        while (MainMat.GetFloat("_NoiseAmount") < 1f)
        {
            t += Time.smoothDeltaTime * 1.2f;
            MainMat.SetFloat("_NoiseAmount", Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
        _GuardianAnim.SetTrigger("EndDie");  
        yield return new WaitUntil(() =>  _GuardianAnim.GetCurrentAnimatorStateInfo(0).IsName("Default"));
        gameObject.SetActive(false);    
        MainMat.SetFloat("_NoiseAmount", 0.0f);
    }
    public void EnableDieMat()
    {
        StartCoroutine(DieMat());
    }
    
    //VFX 및 SFX 관련 함수
    public void EnableSp3VFX()
    {
        ObjectPooler.SpawnFromPool("GuardianHit", VFX3Pos.position, Quaternion.identity);
    }
    public void PlayAttack()
    {
        AudioManager.Instance.PlayGuardianSFX(GuardianSfx.Attack);
    }
    public void PlayHit()
    {
        AudioManager.Instance.StopGuardianSFX(GuardianSfx.Attack);
        AudioManager.Instance.PlayGuardianSFX(GuardianSfx.Hit);
    }
    public void PlayDeath()
    {
        AudioManager.Instance.StopAllGuardianSFX();
        AudioManager.Instance.PlayGuardianSFX(GuardianSfx.Death);
    }
    public void PlaySp1()
    {
        AudioManager.Instance.StopGuardianSFX(GuardianSfx.Attack);
        AudioManager.Instance.PlayGuardianSFX(GuardianSfx.Sp1);
    }
    public void PlaySp2()   
    {
        AudioManager.Instance.StopGuardianSFX(GuardianSfx.Sp1);
        AudioManager.Instance.PlayGuardianSFX(GuardianSfx.Sp2);
    }
    public void PlaySp3()
    {
        AudioManager.Instance.StopGuardianSFX(GuardianSfx.Sp2);
        AudioManager.Instance.PlayGuardianSFX(GuardianSfx.Sp3);
    }
    void InitHpBar()
    {
        //가디언 HPBar 위치 설정
        Vector3 direction = (HpBarPos.position - Camera.main.transform.position).normalized;
        hpBar = ObjectPooler.SpawnFromPool("GuardianHUD", HpBarPos.position, Quaternion.LookRotation(direction));
        
        hpBar.transform.SetParent(UIManager.Instance.HUD.transform, false); // 부모를 HUD의 RectTransform으로 설정      
        hpBarTransform = hpBar.GetComponent<RectTransform>();
        
        HPBarObj = hpBar.transform.Find("BG").gameObject;
        HPBarBgObj = hpBar.transform.Find("HP").gameObject;
    
        HpBarFront = HPBarObj.GetComponent<Image>();
        HpBarBG = HPBarBgObj.GetComponent<Image>();

        //HpBar FillAmount 초기화
        HpBarFront.fillAmount = 1;
        HpBarBG.fillAmount = 1;
    }
    void InitComponent()
    {
        InitHpBar();  //HPBar 초기화

        StartCoroutine(LookPlayer(1.5f)); // 플레이어를 바라보는 코루틴 시작
        StartCoroutine(CheckGuardianState()); // 가디언 상태 체크 코루틴 시작
        StartCoroutine(GuardianAction()); // 가디언 행동 코루틴 시작
    }    
    void InitAnimatorState()
    {
        _GuardianAnim.SetBool(hashTrace, false);
        _GuardianAnim.SetBool(hashAttack, false);
        _GuardianAnim.SetBool("EndGetHit", false);
        _GuardianAnim.SetBool("EndSpA1", false);    
        _GuardianAnim.SetBool("EndSpA2", false);

        _GuardianAnim.ResetTrigger("Guardian_Die");
        _GuardianAnim.ResetTrigger(hashHit);
        _GuardianAnim.ResetTrigger("Guardian_SpA1");
        _GuardianAnim.ResetTrigger("Guardian_SpA2");
        _GuardianAnim.ResetTrigger("Guardian_SpA3");
        _GuardianAnim.ResetTrigger("ScreamToSp2");

        _GuardianAnim.ResetTrigger("EndDie");   

        _GuardianAnim.Play("Default", -1, 0f);
    }   
}
