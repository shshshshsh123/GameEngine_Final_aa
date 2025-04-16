using UnityEngine;

public class Meteor : Monster
{
    public GameObject Hit;
    Rigidbody rb;
    int skillIndex;
    Transform player;
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);
    }

    protected override void Start()
    {
        base.Start();
        player = Player.Instance.transform;
        rb = GetComponent<Rigidbody>();
        skillIndex = MonsterSkill.FindIndex(x => x.SpTag == "D_Sp3");
    }
    protected override void Update()
    {   
        //몬스터의 스킬 관련 스크립트에서는 update처리 x
    }
    protected override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //플레이어가 대쉬 무적 상태가 아니면
            if (!Player.Instance.playerStats.dashInvincibility)
            {
                AttackDamage(MonsterSkill[skillIndex].atk);
                StartCoroutine(CamScript.ShakeCam(1.0f));
            }
            else //대쉬 무적때 충돌하면 모션트레일 실행
            {
                CamScript.StartCoroutine(CamScript.HitStop(0.07f, 55, false));
            }
        }

        if (other.gameObject.CompareTag("Ground"))
        {
            SpawnMeteorHit();
            PlayMeteorExplosion();
        }
    }
    void SpawnMeteorHit()
    {
        Vector3 SpawnPos = transform.position;
        SpawnPos.y = 0;
        rb.velocity = Vector3.zero;
        ObjectPooler.SpawnFromPool("MeteorHit", SpawnPos, Quaternion.identity);
        gameObject.SetActive(false);
    }
    public void PlayMeteorExplosion()
    {
        AudioManager.Instance.StopDragonSFX(DragonSfx.Attack);
        AudioManager.Instance.PlayDragonSFX(DragonSfx.Explosion);
    }
}