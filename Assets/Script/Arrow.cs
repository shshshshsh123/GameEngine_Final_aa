using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Arrow : WeaponScript
{
    Rigidbody rb;
    float TrailWidth;
    [Header("Arrow Setup")]
    public float FireSpeed = 20f;
    public TrailRenderer trail;

    //중복 타격 방지용 변수
    bool isHit = false;

    //오브젝트 비활성화 방지용 변수
    bool isTrailEnable = false;
    bool isEnable = false;

    void OnDisable()
    {
        HitVFX.enabled = false;
        ObjectPooler.ReturnToPool(gameObject);    // 한 객체에 한번만 
    }

    private void OnEnable()
    {
        if(SceneManager.GetActiveScene().name != "StartScene" && SceneManager.GetActiveScene().name != "LoadingScene")
        {
            if(camScript == null)
            {
                camScript = Camera.main.GetComponent<Cam>();
            }
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }
            isEnable = false; //TransEnable bool 값 초기화
            isTrailEnable = false;//TrailEnable bool 값 초기화
            isHit = false; //활성화 될때 초기화
            trail.enabled = true;
            if (Player.Instance.FirePos != null)
            {
                rb.AddForce(Player.Instance.FirePos.forward * FireSpeed, ForceMode.Impulse);
            }
            StartCoroutine(WaitAndShoot());
        }
    }
    protected override void Start()
    {
        base.Start();
        TrailWidth = trail.startWidth;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        //arrow 적중시 motiontrail 실행
        if (!isHit && ((1 << other.gameObject.layer) & ApplyMask) != 0) //중복타격하지 않았을때 실행
        {
            isHit = true;
            camScript.StartCoroutine(camScript.HitStop(0.04f, 58, true));
            rb.constraints = RigidbodyConstraints.FreezeAll;
            other.gameObject.GetComponent<Monster>().TakeDamage(Player.Instance.playerStats.atk + Player.Instance.playerStats.weapon_atk);

            //Trail 비활성화
            StartCoroutine(TrailEnable());

            //Hit 이펙트 실행
            HitVFX.enabled = true;
            Vector3 collisionPoint = other.ClosestPoint(transform.position);
            HitVFX.transform.position = collisionPoint;
            HitVFX.Play();
        }
    }
    private void Initialize()
    {
        StopAllCoroutines();
        //씬이 로드되면 Trail과 rb속성 초기화
        trail.enabled = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.velocity = Vector3.zero;
        trail.startWidth = TrailWidth;
        gameObject.SetActive(false);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "LoadingScene")
        {
            Initialize();
        }
    }


    IEnumerator WaitAndShoot() //일정 시간이 지나면 사라지도록 하는 함수
    {
        yield return new WaitForSeconds(1f);;
        if (trail.enabled) { StartCoroutine(TrailEnable()); }
        if (gameObject.activeSelf) { StartCoroutine(TransEnable()); }
    }
    IEnumerator TrailEnable()
    {
        if (!isTrailEnable)
        {
            isTrailEnable = true;
            float t = 0f;
            while (t < 1f && trail.enabled)
            {
                t += 3f * Time.smoothDeltaTime;
                trail.startWidth = Mathf.Lerp(TrailWidth, 0, t);
                yield return null;
            }
            rb.constraints = RigidbodyConstraints.None;
            rb.velocity = Vector3.zero;
            trail.startWidth = TrailWidth;
            trail.enabled = false;
        }
    }
    IEnumerator TransEnable()
    {
        if (!isEnable)
        {
            isEnable = true;
            while (trail.enabled) //trail이 비활성화 될때까지 실행
            {
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
    //파티클 종료시 호출
    void OnParticleSystemStopped()
    {
        StartCoroutine(TransEnable());
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 메모리 누수 방지를 위해 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
