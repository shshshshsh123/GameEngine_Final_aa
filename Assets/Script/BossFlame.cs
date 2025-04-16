using System.Collections;
using UnityEngine;

public class BossFlame : Monster
{
    int skillIndex = 0;
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        //flame은 objectPooler에 없기 때문에 처리 x
    }
    protected override void Update()
    {
        //몬스터의 스킬 관련 스크립트에서는 update처리 x
    }
    protected override void Start()
    {
        base.Start();
        skillIndex = MonsterSkill.FindIndex(x => x.SpTag == "D_Sp2");
    }
    private void OnParticleCollision(GameObject other)
    {
        if (!IsFlame && other.CompareTag("Player")) //flame에 처음 타격 당하면
        {
            StartCoroutine(CanHitFlame(0.5f));
            //플레이어가 대쉬 무적 상태가 아니면
            if (!Player.Instance.playerStats.dashInvincibility)
            {
                AttackDamage(MonsterSkill[skillIndex].atk); 
                StartCoroutine(CamScript.ShakeCam(1.0f));
            }
            else //플레이어가 대쉬 무적일때 충돌하면 모션트레일 실행
            {
                CamScript.StartCoroutine(CamScript.HitStop(0.07f, 55, false));
            }
        }
    }
    IEnumerator CanHitFlame(float t = 0.3f)
    {
        if (!IsFlame)
        {
            IsFlame = true;
            yield return new WaitForSeconds(t);
            IsFlame = false;
        }
    }
}
