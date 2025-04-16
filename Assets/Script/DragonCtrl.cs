using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
public class DragonCtrl : Monster
{
    public enum State
    {
        Default, //설정값으로 바로 시작해서 default를 기본으로 설정
        GetHit,
        Idle,
        Trace,
        Attack,
        AtkSp1,
        AtkSp2,
        AtkSp3,
        AtkSp4, //강화 공격
        AtkSp5, //land Attack
        Die
    }
    //몬스터의 현재 상태
    public State state = State.Default;
    //추적 사거리
    public float MaxDistance = 50f;
    //공격 사거리
    public float AttackDistance = 14f;
    //몬스터 사망여부
    public bool isDie = false;
    //몬스터 mat
    public Material mat;
    //flame
    public ParticleSystem Flame;
    //sp4에서 점프 위치
    Vector3 OriPos;
    Vector3 jumpPos;

    //sp5에서 쓸 애니메이션 커브
    [SerializeField] private AnimationCurve mainCurve;

    private Transform playerTr;
    private NavMeshAgent _DragonAgent;
    private Animator _DragonAnim;

    //드래곤 스킬 인덱스
    int spIndex1;
    int spIndex2;
    int spIndex3;
    int spIndex4;
    int spIndex5;
    
    //중복사용 방지 (isSp는 waitUnitl체크 용 변수
    bool isSp1 = false;
    bool isSp2 = false;
    bool isSp3 = false;
    bool isSp4 = false;
    bool isSp5 = false; //land Attack
    bool isRot = true;
    bool useSp3 = false;//true면 2페이즈

    bool isFirst = false; //게임 첫 시작시 초기화

    private readonly int hashTraceD = Animator.StringToHash("Dragon_M");
    private readonly int hashAttackD = Animator.StringToHash("Dragon_A");
    private readonly int hashHitD = Animator.StringToHash("Dragon_H");

    private void Awake()
    {
        Flame.Stop();
    }
    protected override void OnEnable()
    {
        base.OnEnable();   
        if(isFirst)
        {
            InitAnimatorState(); // 애니메이터 상태 초기화 추가
            MonsterStatInit();
            state = State.Default;
            InitComponent();
        }
    }   
    protected override void OnDisable() //몬스터가 죽으면(비활성화) isDie 초기화
    {
        if(isDie) //게임 시작시 비활성화 상태이면 아이템을 지급해서 게임을 시작하고 몬스터가 사망한 후에 아이템 지급하도록 if문 추가
        {
            base.OnDisable(); //몬스터 사망시 플레이어에게 아이템 지급
            isDie = false;
        }  
    }
    protected override void Start()
    {
        base.Start();

        //monsterSkill index 초기화
        spIndex1 = MonsterSkill.FindIndex(x => x.SpTag == "D_Sp1");
        spIndex2 = MonsterSkill.FindIndex(x => x.SpTag == "D_Sp2");
        spIndex3 = MonsterSkill.FindIndex(x => x.SpTag == "D_Sp3");
        spIndex4 = MonsterSkill.FindIndex(x => x.SpTag == "D_Sp4");
        spIndex5 = MonsterSkill.FindIndex(x => x.SpTag == "D_Sp5");
        
        isFirst = true;

        playerTr = Player.Instance.transform;

        _DragonAgent = GetComponent<NavMeshAgent>();

        _DragonAnim = GetComponent<Animator>();
                
        _DragonAgent.speed = monsterStat.BaseMoveSpeed;
        
        InitComponent();
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
                TakeDamage(Player.Instance.playerStats.atk + Player.Instance.playerStats.weapon_atk); //플레이어 공격 성공
            }
            else if (other.gameObject.CompareTag("Player"))//몬스터 공격 성공
            {
                if (!Player.Instance.playerStats.AttackInvinci)//플레이어가 공격 무적상태가 아니면
                {
                    //공격이 적용가능한 시점(중복 타격 방지), 피격을 받지 않았으면 실행
                    if (isAttackApply && !IsAttacked)
                    {
                        if (state == State.AtkSp4) //강화공격 데미지 적용
                        {
                            AttackDamage(MonsterSkill[spIndex4].atk);
                            StartCoroutine(CamScript.ShakeCam(0.5f));
                        }
                        else
                        {
                            AttackDamage(monsterStat.atk);//몬스터 공격 성공
                        }
                        isAttackApply = false;
                    }
                }
            }
        }
    }
    void OnApplicationQuit() //사망 효과 중간에 게임 종료시 mat 초기화
    {
        Flame.Stop();
        mat.SetFloat("_NoiseAmount", 0.3f);
    }
    
    IEnumerator CheckDragonState()
    {
        float Sp1T = 0; //스킬1 쿨타임
        float sp2T = 0;
        float Sp5T = MonsterSkill[spIndex5].reuse;
        while (!isDie)
        {
            monsterStat.atk_cool += Time.smoothDeltaTime; //공격 쿨타임 계산

            if (monsterStat.hp <= 0)
            {
                state = State.Die;
                GetExp();
                isDie = true;   
                yield break;
            }
            Sp1T += Time.smoothDeltaTime;
            sp2T += Time.smoothDeltaTime;
            Sp5T += Time.smoothDeltaTime;
            if (!useSp3 && (float)monsterStat.hp / (float)monsterStat.maxHp <= 0.5f)//드래곤의 hp가 50% 이하면서 첫번째 스킬 시전이면
            {
                monsterStat.GetHit = false; //3스킬 초기화
                _DragonAnim.SetBool("EndFinalAttack", false);
                useSp3 = true;
                isSp3 = true;
                state = State.AtkSp3;
                yield return new WaitUntil(() => !isSp3);
            }
            else
            {
                if (Sp1T >= MonsterSkill[spIndex1].reuse && IsAttacked)
                {
                    monsterStat.GetHit = false; //1스킬 초기화
                    _DragonAnim.SetBool("EndScreamAttack", false);
                    isSp1 = true;
                    Sp1T = 0f;
                    state = State.AtkSp1;
                    yield return new WaitUntil(() => !isSp1);
                }
                else if (sp2T >= MonsterSkill[spIndex2].reuse)
                {
                    monsterStat.GetHit = false; //2스킬 초기화
                    _DragonAnim.SetBool("EndFlameAttack", false);
                    isSp2 = true;
                    sp2T = 0f;
                    state = State.AtkSp2;
                    yield return new WaitUntil(() => !isSp2);
                }
                else if (monsterStat.GetHit) //피격을 당한 상태면
                {
                    _DragonAnim.SetBool("EndGetHit", false); //GetHit bool 값 초기화
                    state = State.GetHit;

                    yield return new WaitUntil(() => !monsterStat.GetHit); //피격을 당한동안은 행동 제약
                }
                else //스킬을 사용하는 상태가 아니면
                {
                    float distance = Vector3.Distance(transform.position, playerTr.position);
                    if (Sp5T > MonsterSkill[spIndex5].reuse && distance < AttackDistance - 6f) //쿨타임이 됐고 몬스터 안에 플레이어가 들어오면 플레이어를 날려보냄
                    {
                        Sp5T = 0f;
                        isSp5 = true;
                        state = State.AtkSp5;
                        _DragonAnim.SetBool("EndLandAttack", false);

                        yield return new WaitUntil(() => !isSp5);
                    }
                    else if (distance <= AttackDistance)
                    {
                        if (useSp3 && monsterStat.atk_count > 5) //2페이즈
                        {
                            monsterStat.atk_count = 0; //공격횟수 초기화
                            isSp4 = true;
                            _DragonAnim.SetBool("EndJumpAttack", false); //JumpAttack 애니메이션 초기화
                            state = State.AtkSp4;

                            yield return new WaitUntil(() => !isSp4);
                        }
                        else { state = State.Attack; } //1페이즈
                    }
                    else if (distance < MaxDistance)
                    {
                        if(!_DragonAnim.GetBool(hashAttackD))
                        {
                            state = State.Trace;
                            EnableHpBar();
                        }
                    }
                    else
                    {
                        state = State.Idle;
                        DisableHpBar();
                    }
                }
            }
            yield return null; //빠른 처리를 위해 null로 수정
        }
        yield break;    
    }
    
    IEnumerator DragonAction()
    {
        while (!isDie)
        {
            switch (state)
            {
                case State.Default:
                    break;
                    
                case State.GetHit:
                    StartOfAni();
                    _DragonAnim.SetTrigger(hashHitD);
                    _DragonAnim.SetBool(hashAttackD, false);
                    _DragonAnim.SetBool(hashTraceD, false);
                    
                    _DragonAgent.SetDestination(transform.position);
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                    yield return new WaitUntil(() => !monsterStat.GetHit); //피격을 당한동안은 행동 제약
                    break;
                    
                case State.Idle:
                    StartCoroutine(LookPlayer());//idle 상태에서는 플레이어를 바라보도록 함

                    if (_DragonAnim.GetBool(hashTraceD) && _DragonAnim.GetBool(hashAttackD))
                    {
                        DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                        StartOfAni();    //idle 첫시작시 agent 멈추기
                        _DragonAgent.SetDestination(transform.position);
                        _DragonAnim.SetBool(hashAttackD, false);
                        _DragonAnim.SetBool(hashTraceD, false);
                    }
                    break;

                case State.Trace:
                    Vector3 Distance = (transform.position - playerTr.position).normalized;
                    _DragonAgent.SetDestination(playerTr.position + Distance * (AttackDistance - 0.01f)); //attackDistance까지만 가면 공격을 못해서
                    
                    if (!_DragonAnim.GetBool(hashTraceD) && !_DragonAnim.GetBool(hashAttackD)) //공격 애니메이션이 종료되면 trace로 변경
                    {
                        DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제  
                        _DragonAnim.SetBool(hashTraceD, true);
                    }
                    break;
                    
                case State.Attack:
                    if (!_DragonAnim.GetBool(hashAttackD)) //기본 공격
                    {
                        _DragonAnim.SetBool(hashTraceD, false);
                        _DragonAnim.SetBool(hashAttackD, true);
                    }
                    break;

                case State.AtkSp1:
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                    _DragonAgent.SetDestination(transform.position);

                    //애니메이션 초기화
                    InitAni();
                    
                    _DragonAnim.SetTrigger("Dragon_Sp1"); //sp1 시작
                    yield return new WaitUntil(() => !isSp1);
                    break;

                case State.AtkSp2:
                    DisableBasicAttack(); //attack을 제외한 행동을 시��할때 타격판정 해제
                    _DragonAgent.SetDestination(transform.position);
                    StartCoroutine(LookPlayer());

                    //애니메이션 초기화
                    InitAni();
                    
                    _DragonAnim.SetTrigger("Dragon_Sp2"); //sp2 시작
                    yield return new WaitUntil(() => !isSp2);
                    break;

                case State.AtkSp3:
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                    _DragonAgent.SetDestination(transform.position);
                    
                    //애니메이션 초기화
                    InitAni();
                    
                    _DragonAnim.SetTrigger("Dragon_Sp3");
                    yield return new WaitUntil(() => !isSp3);
                    break;

                case State.AtkSp4: //강화공격
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제
                    StartCoroutine(LookPlayer());

                    //애니메이션 초기화
                    InitAni();
                    
                    _DragonAnim.SetTrigger("JumpAttack");
                    yield return new WaitUntil(() => !isSp4);
                    break;

                case State.AtkSp5: //landAttack
                    DisableBasicAttack(); //attack을 제외한 행동을 시작할때 타격판정 해제

                    //애니메이션 초기화
                    InitAni();

                    _DragonAnim.SetTrigger("LandAttack");
                    
                    yield return new WaitUntil(() => !isSp5);
                    break;
                    
                case State.Die:
                    yield break;
            }
            yield return null; //매 프레임마다 호출해서 빠른 처리가 가능하도록 수정
        }
        DropItem();
        DisableHpBar(); 
        DieAni(); //코루틴 종료시 사망 상태이므로 애니메이션 재생
        yield break;
    }
    void InitAni()
    {
        _DragonAnim.SetBool(hashAttackD, false);
        _DragonAnim.SetBool(hashTraceD, false);
    }   
    IEnumerator UseSp1()
    { 
        float playerAtk = (float)Player.Instance.playerStats.atk; //플레이어 공격력 감소
        Player.Instance.playerStats.atk = (int)(playerAtk * 0.9);
        yield return new WaitForSeconds(MonsterSkill[spIndex1].duration); //스킬 지속시간동안 감소된 공격력 유지
        Player.Instance.playerStats.atk = (int)playerAtk;
        EndSilence();
        yield break;
    }
    IEnumerator LookPlayer(float speed = 1.5f)
    {
        if (isRot)
        {
            isRot = false;
            float t = 0;
            while (t < 1f && state != State.GetHit)
            {
                // 플레이어를 바라보도록 부드럽게 회전
                Vector3 direction = (playerTr.position - transform.position).normalized;

                Quaternion lookRotation = Quaternion.LookRotation(direction);

                t += Time.smoothDeltaTime * speed;
                t = Mathf.Clamp01(t);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, t); // 회전 속도 조절
                yield return null;
            }
            isRot = true;
        }
        yield break;
    }

    IEnumerator DieMat()
    {
        float t = 0f;
        float matAmount = mat.GetFloat("_NoiseAmount");
        while (matAmount < 0.6f)
        {
            t += Time.smoothDeltaTime * 0.009f;
            mat.SetFloat("_NoiseAmount", Mathf.Lerp(matAmount, 0.7f, t));
            matAmount = mat.GetFloat("_NoiseAmount");
            yield return null;
        }
        _DragonAnim.SetTrigger("EndDie");  
        yield return new WaitUntil(() =>  _DragonAnim.GetCurrentAnimatorStateInfo(0).IsName("Default"));
        mat.SetFloat("_NoiseAmount", 0.3f);
        gameObject.SetActive(false);
    }
    public void TransMat()
    {
        StartCoroutine(DieMat());
    }
    void DieAni()
    {
        _DragonAgent.isStopped = true;
        _DragonAgent.updateRotation = false;
        _DragonAnim.SetBool(hashTraceD, false);
        _DragonAnim.SetBool(hashAttackD, false);
        _DragonAnim.SetTrigger("Dragon_Die");
    }
    public void StartTrace()
    {
        StartCoroutine(LookPlayer());
    }
    public void StartOfAni() 
    {
        _DragonAgent.isStopped = true;
        _DragonAgent.updateRotation = false;
    }
    
    public void EndOfAni() 
    {
        _DragonAgent.isStopped = false;
        _DragonAgent.updateRotation = true;
    }
    public void EndOfSp1()
    {
        isSp1 = false;
        _DragonAnim.SetBool("EndScreamAttack", true);
        state = State.Trace;
        _DragonAnim.SetBool(hashAttackD, false);
    }
    public void EndOfSp2()
    {
        isSp2 = false;
        _DragonAnim.SetBool("EndFlameAttack", true);
        state = State.Trace;
        _DragonAnim.SetBool(hashAttackD, false);
    }
    public void EndOfSp3()
    {
        isSp3 = false;
        _DragonAnim.SetBool("EndFinalAttack", true);
        state = State.Trace;
    }
    void EndSp4()
    {
        isSp4 = false;
        _DragonAnim.SetBool("EndJumpAttack", true);
        state = State.Trace;
    }
    public void StartBasicAttack()
    {
        if (useSp3) { monsterStat.atk_count++; } //2페이즈일때 공격 횟수를 누적시킴
        StartOfAni();
        StartCoroutine(LookPlayer());
    }
    public void EndBasicAttack()
    {
        EndOfAni();
        _DragonAnim.SetBool(hashAttackD, false);
    }
    public void EnableBasicAttack()
    {
        isAttackApply = true;
    }
    public void DisableBasicAttack()
    {
        isAttackApply = false;
    }
    public void StartScream()
    {
        StartCoroutine(UseSp1());
    }
    public void StartSilence()
    {
        UIManager.Instance.IsSilence = true;
        UIManager.Instance.HUD_Silence.SetActive(true);
        StartOfAni();
    }
    void EndSilence() //지속시간 끝나면 해제하도록 호출
    {
        UIManager.Instance.IsSilence = false;
    }
    public void EndGetHit()
    {
        monsterStat.GetHit = false; //드래곤 ani 종료시 초기화
        _DragonAnim.SetBool("EndGetHit", true);
    }
    public void StartFlameAttack()
    {
        Flame.Play();
    }
    public void EndFlameAttack()
    {
        Flame.Stop();
    }
    public void StartMeteorSkill()
    {
        _DragonAnim.SetBool(hashAttackD, false);
        _DragonAnim.SetBool(hashTraceD, false);
        StartCoroutine(StartSp3());
    }
    IEnumerator StartSp3() //애니메이션에서 호출
    {
        int count = 0;
        float t = 0;

        while(t <= MonsterSkill[spIndex3].duration)
        {
            t += Time.smoothDeltaTime;
            if(t - count > 0f)
            {
                count++;
                SpawnMeteor();
            }
            yield return null;
        }
        _DragonAnim.SetBool("EndFinalAttack", true); //land ani실행
    }
    void SpawnMeteor() //애니메이션에서 호출
    {
        Vector3 pos = playerTr.position + new Vector3(Random.Range(-5f, 5f), 20f, Random.Range(-5f, 5f));
        ObjectPooler.SpawnFromPool("Meteor", pos, Quaternion.identity);
        PlayMeteor();
    }
    public void SetJumpPos() //점프 시작전 시작위치 및 점프위치 설정
    {
        OriPos = transform.position;
        Vector3 distance = transform.position - playerTr.position;
        jumpPos = playerTr.position + distance * 0.1f;
    }
    public void StartJumpAttack()
    {
        EnableBasicAttack();
        StartOfAni();
        StartCoroutine(JumpToPlayer());
    }
    public void ReturnJumpAttack()
    {
        DisableBasicAttack();
        StartCoroutine(LookPlayer());
        StartCoroutine(ReturnToOri());
    }
    IEnumerator JumpToPlayer()
    {
        float t = 0;
        while(t < 1f)
        {
            t += Time.smoothDeltaTime * 1.5f;
            if(jumpPos != null) { transform.position = Vector3.Slerp(transform.position, jumpPos, t); }
            yield return null;
        }
    }
    IEnumerator ReturnToOri()
    {
        float distance = Vector3.Distance(transform.position, OriPos);
        float per = distance / AttackDistance;
        per = Mathf.Clamp(per, 0.75f, 0.85f);
        float t = 0;
        while (t < 1f)
        {
            t += Time.smoothDeltaTime * per;
            if (OriPos != null) { transform.position = Vector3.Slerp(transform.position, OriPos, t); }
            yield return null;
        }
        EndSp4();
    }

    public void StartLandAttack()
    {
        playerTr.LookAt(transform.position); //플레이어가 날아갈때 드래곤을 바라보도록
        Player.Instance.StartFall();
        DisableBasicAttack(); //몬스터 공격판정 비활성화
        StartCoroutine(StartForce());
    }
    public void EndLandAttack()
    {
        StartCoroutine(PlayerCanMove());
    }
    IEnumerator StartForce()
    {
        Player.Instance.CanMove = false; //플레이어 행동제약

        Vector3 forcePos = (playerTr.position - transform.position).normalized;
        forcePos.y = 0.3f;

        Vector3 playerPos = playerTr.position;
        Vector3 middlePos = playerTr.position + forcePos * 12f;

        Vector3 finalPos = middlePos + forcePos * 11f;
        finalPos.y = 0f;

        float t = 0;
        float per;
        while (t < MonsterSkill[spIndex5].duration)
        {
            t += Time.smoothDeltaTime;
            per = t / MonsterSkill[spIndex5].duration;
            
            if(per < 0.4f)
            {
                playerTr.position = Vector3.Lerp(playerPos, middlePos, mainCurve.Evaluate(Mathf.Clamp01(per)));
            }
            else //그래프가 위로 볼록한 2차함수 그래프여서 finalPos랑 middlePos가 반대로 들어감
            {
                playerTr.position = Vector3.Lerp(finalPos, middlePos, mainCurve.Evaluate(Mathf.Clamp01(per)));
            }

            yield return null;
        }
        Player.Instance.CanMove = true;
        Player.Instance.EndFall();
    }
    IEnumerator PlayerCanMove()
    {
        yield return new WaitUntil(() => Player.Instance.CanMove);
        isSp5 = false;
        state = State.Trace;
        _DragonAnim.SetBool("EndLandAttack", true);
        StartCoroutine(LookPlayer());
    }
    public void PlayDragonAttack()
    {
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Attack);
    }
    public void PlayDragonHit()
    {
        AudioManager.Instance.StopDragonSFX(DragonSfx.Attack);
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Hit);
    }
    public void PlayDragonDeath()
    {
        AudioManager.Instance.StopAllDragonSFX();
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Death);
    }
    public void PlayDragonScream()
    {
        AudioManager.Instance.StopDragonSFX(DragonSfx.Attack);
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Scream);
    }
    public void PlayDragonLand()
    {
        AudioManager.Instance.StopDragonSFX(DragonSfx.Attack);
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Land);
    }
    public void PlayDragonFlame()
    {
        AudioManager.Instance.StopDragonSFX(DragonSfx.Attack);
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Flame);
    }
    public void PlayDragonFly()
    {
        AudioManager.Instance.StopDragonSFX(DragonSfx.Attack);
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Fly);
    }
    void PlayMeteor()
    {
        AudioManager.Instance.StopDragonSFX(DragonSfx.Attack);
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Meteor);
    }
    void InitHpBar()
    {
        hpBar = UIManager.Instance.DragonHpBarObj;
        
        HPBarObj = hpBar.transform.Find("BG").gameObject;
        HPBarBgObj = hpBar.transform.Find("HP").gameObject;
    
        HpBarFront = HPBarObj.GetComponent<Image>();
        HpBarBG = HPBarBgObj.GetComponent<Image>();
    }
    void InitComponent()
    {
        InitHpBar(); //HPBar 초기화
            
        DisableHpBar();   //게임 시작시 HPBar 비활성화

        StartCoroutine(LookPlayer(1.5f));

        StartCoroutine(CheckDragonState());
        
        StartCoroutine(DragonAction());
    }

    void InitAnimatorState()
    {
        _DragonAnim.SetBool(hashTraceD, false);
        _DragonAnim.SetBool(hashAttackD, false);
        _DragonAnim.SetBool("EndScreamAttack", false);
        _DragonAnim.SetBool("EndFlameAttack", false);
        _DragonAnim.SetBool("EndFinalAttack", false);
        _DragonAnim.SetBool("EndJumpAttack", false);
        _DragonAnim.SetBool("EndLandAttack", false);
        _DragonAnim.SetBool("EndGetHit", false);

        _DragonAnim.ResetTrigger("Dragon_Die");
        _DragonAnim.ResetTrigger(hashHitD);
        _DragonAnim.ResetTrigger("Dragon_Sp1");
        _DragonAnim.ResetTrigger("Dragon_Sp2");
        _DragonAnim.ResetTrigger("Dragon_Sp3");
        _DragonAnim.ResetTrigger("JumpAttack");
        _DragonAnim.ResetTrigger("LandAttack");

        _DragonAnim.ResetTrigger("EndDie"); 

        _DragonAnim.Play("Default", -1, 0f);
    }
}