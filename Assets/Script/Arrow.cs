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

    //�ߺ� Ÿ�� ������ ����
    bool isHit = false;

    //������Ʈ ��Ȱ��ȭ ������ ����
    bool isTrailEnable = false;
    bool isEnable = false;

    void OnDisable()
    {
        HitVFX.enabled = false;
        ObjectPooler.ReturnToPool(gameObject);    // �� ��ü�� �ѹ��� 
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
            isEnable = false; //TransEnable bool �� �ʱ�ȭ
            isTrailEnable = false;//TrailEnable bool �� �ʱ�ȭ
            isHit = false; //Ȱ��ȭ �ɶ� �ʱ�ȭ
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
        //arrow ���߽� motiontrail ����
        if (!isHit && ((1 << other.gameObject.layer) & ApplyMask) != 0) //�ߺ�Ÿ������ �ʾ����� ����
        {
            isHit = true;
            camScript.StartCoroutine(camScript.HitStop(0.04f, 58, true));
            rb.constraints = RigidbodyConstraints.FreezeAll;
            other.gameObject.GetComponent<Monster>().TakeDamage(Player.Instance.playerStats.atk + Player.Instance.playerStats.weapon_atk);

            //Trail ��Ȱ��ȭ
            StartCoroutine(TrailEnable());

            //Hit ����Ʈ ����
            HitVFX.enabled = true;
            Vector3 collisionPoint = other.ClosestPoint(transform.position);
            HitVFX.transform.position = collisionPoint;
            HitVFX.Play();
        }
    }
    private void Initialize()
    {
        StopAllCoroutines();
        //���� �ε�Ǹ� Trail�� rb�Ӽ� �ʱ�ȭ
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


    IEnumerator WaitAndShoot() //���� �ð��� ������ ��������� �ϴ� �Լ�
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
            while (trail.enabled) //trail�� ��Ȱ��ȭ �ɶ����� ����
            {
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
    //��ƼŬ ����� ȣ��
    void OnParticleSystemStopped()
    {
        StartCoroutine(TransEnable());
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // �޸� ���� ������ ���� �̺�Ʈ ����
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
