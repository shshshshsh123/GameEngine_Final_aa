using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

public class WeaponScript : MonoBehaviour
{
    public enum Type { Hand, Spear, Axe, Pickax, Knife, WoodBow, IronBow, Sword };
    public Type type;
    public LayerMask ApplyMask;
    public VisualEffect HitVFX;
    public bool P_GetHit = false; //피격시 코루틴 호출 방지
    protected Cam camScript;

    BoxCollider hitBox;
    bool isAttack = false; //좌클릭 누르면 true 공격 끝나면 false

    protected virtual void Start()
    {
        SceneLoader.OnSceneLoaded += Initialize;
        StartCoroutine(InitializeAfterFrame());
        camScript = Camera.main.GetComponent<Cam>();
        hitBox = GetComponent<BoxCollider>();
    }

    IEnumerator InitializeAfterFrame()
    {
        // 한 프레임 대기하여 모든 오브젝트가 초기화될 시간을 줌
        yield return new WaitForEndOfFrame();
        Initialize();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (isAttack) //공격 버튼을 눌렀을때만 실행
        {
            //플레이어가 몬스터공격 성공시 motionTrail 연출 실행
            if (Player.Instance.playerStats.isAttack && type != Type.IronBow && type != Type.WoodBow) //나무활이랑 강철활은 화살로 공격하므로 제외
            {
                if (((1 << other.gameObject.layer) & ApplyMask) != 0) //피격대상의 레이어가 monster이면
                {
                    // camScript가 null이거나 파괴되었다면 재초기화
                    if (camScript == null)
                    {
                        Initialize();
                        // 여전히 null이면 효과를 실행하지 않음
                        if (camScript == null)
                        {
                            Debug.LogWarning("카메라 스크립트를 찾을 수 없어 모션 트레일 효과를 실행할 수 없습니다.");
                            return;
                        }
                    }

                    //파티클 실행
                    EnableVFX(other);
                    
                    camScript.StartCoroutine(camScript.HitStop(0.03f, 57, true));

                    isAttack = false; //한번만 공격가능하도록 false로 변경
                }
            }

            if (type == Type.Axe && other.gameObject.CompareTag("Wood"))
            {
                //파티클 실행
                EnableVFX(other);

                other.gameObject.GetComponent<MaterialItems>().AddItem(); //나무 추가
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Wood);

                isAttack = false; //한번만 공격가능하도록 false로 변경
            }
            else if (type == Type.Pickax && (other.gameObject.CompareTag("Rock") || other.gameObject.CompareTag("Coal")))
            {
                //파티클 실행
                EnableVFX(other);

                other.gameObject.GetComponent<MaterialItems>().AddItem(); //돌추가
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Rock);

                isAttack = false; //한번만 공격가능하도록 false로 변경
            }
        }
    }
    protected virtual void OnDestroy()
    {
        SceneLoader.OnSceneLoaded -= Initialize;  
    }
    //씬 변경시 CamScript를 다시 탐색함
    void Initialize()
    {
        try
        {
            if (Camera.main != null)
            {
                camScript = Camera.main.GetComponent<Cam>();
                if (camScript == null)
                {
                    Debug.LogWarning("Cam 스크립트를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("메인 카메라를 찾을 수 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"카메라 초기화 중 오류 발생: {e.Message}");
            camScript = null;
        }
    }
    public void EnableHitBox() //플레이어 피격시 호출
    {
        P_GetHit = true;
        if (type != Type.IronBow && type != Type.WoodBow) //활은 콜라이더가 없어서
        {
            hitBox.enabled = false;
            isAttack = false;
        }
    }
    public IEnumerator Hand()
    {
        if (!P_GetHit)
        {
            isAttack = true;
            hitBox.enabled = true;
            yield return new WaitForSeconds(0.1f);
            hitBox.enabled = false;
            isAttack = false;
            yield return new WaitUntil(() => HitVFX.aliveParticleCount == 0);
            DisableVFX();
        }
    }

    public IEnumerator Spear()
    {
        if (!P_GetHit)
        {
            isAttack = true;
            hitBox.enabled = true;
            yield return new WaitForSeconds(0.1f);
            hitBox.enabled = false;
            isAttack = false;
            yield return new WaitUntil(() => HitVFX.aliveParticleCount == 0);
            DisableVFX();
        }
    }

    public IEnumerator Axe()
    {
        if (!P_GetHit)
        {
            isAttack = true;
            hitBox.enabled = true;
            yield return new WaitForSeconds(0.1f);
            hitBox.enabled = false;
            isAttack = false;
            yield return new WaitUntil(() => HitVFX.aliveParticleCount == 0);
            DisableVFX();
        }
    }
    public IEnumerator Knife()
    {
        if (!P_GetHit)
        {
            isAttack = true;
            hitBox.enabled = true;
            yield return new WaitForSeconds(0.1f);
            hitBox.enabled = false;
            isAttack = false;
            yield return new WaitUntil(() => HitVFX.aliveParticleCount == 0);
            DisableVFX();
        }
    }

    public IEnumerator Sword()
    {
        if (!P_GetHit)
        {
            isAttack = true;
            hitBox.enabled = true;
            yield return new WaitForSeconds(0.1f);
            hitBox.enabled = false;
            isAttack = false;
            yield return new WaitUntil(() => HitVFX.aliveParticleCount == 0);
            DisableVFX();
        }   
    }

    void EnableVFX(Collider other)
    {
        HitVFX.enabled = true;
        Vector3 collisionPoint = other.ClosestPoint(transform.position);
        HitVFX.transform.position = collisionPoint;
        HitVFX.Play();
    }  
    void DisableVFX()
    {
        HitVFX.Stop();
        HitVFX.enabled = false;
    }
}
