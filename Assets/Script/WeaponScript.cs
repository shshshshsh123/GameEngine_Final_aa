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
    public bool P_GetHit = false; //�ǰݽ� �ڷ�ƾ ȣ�� ����
    protected Cam camScript;

    BoxCollider hitBox;
    bool isAttack = false; //��Ŭ�� ������ true ���� ������ false

    protected virtual void Start()
    {
        SceneLoader.OnSceneLoaded += Initialize;
        StartCoroutine(InitializeAfterFrame());
        camScript = Camera.main.GetComponent<Cam>();
        hitBox = GetComponent<BoxCollider>();
    }

    IEnumerator InitializeAfterFrame()
    {
        // �� ������ ����Ͽ� ��� ������Ʈ�� �ʱ�ȭ�� �ð��� ��
        yield return new WaitForEndOfFrame();
        Initialize();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (isAttack) //���� ��ư�� ���������� ����
        {
            //�÷��̾ ���Ͱ��� ������ motionTrail ���� ����
            if (Player.Instance.playerStats.isAttack && type != Type.IronBow && type != Type.WoodBow) //����Ȱ�̶� ��öȰ�� ȭ��� �����ϹǷ� ����
            {
                if (((1 << other.gameObject.layer) & ApplyMask) != 0) //�ǰݴ���� ���̾ monster�̸�
                {
                    // camScript�� null�̰ų� �ı��Ǿ��ٸ� ���ʱ�ȭ
                    if (camScript == null)
                    {
                        Initialize();
                        // ������ null�̸� ȿ���� �������� ����
                        if (camScript == null)
                        {
                            Debug.LogWarning("ī�޶� ��ũ��Ʈ�� ã�� �� ���� ��� Ʈ���� ȿ���� ������ �� �����ϴ�.");
                            return;
                        }
                    }

                    //��ƼŬ ����
                    EnableVFX(other);
                    
                    camScript.StartCoroutine(camScript.HitStop(0.03f, 57, true));

                    isAttack = false; //�ѹ��� ���ݰ����ϵ��� false�� ����
                }
            }

            if (type == Type.Axe && other.gameObject.CompareTag("Wood"))
            {
                //��ƼŬ ����
                EnableVFX(other);

                other.gameObject.GetComponent<MaterialItems>().AddItem(); //���� �߰�
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Wood);

                isAttack = false; //�ѹ��� ���ݰ����ϵ��� false�� ����
            }
            else if (type == Type.Pickax && (other.gameObject.CompareTag("Rock") || other.gameObject.CompareTag("Coal")))
            {
                //��ƼŬ ����
                EnableVFX(other);

                other.gameObject.GetComponent<MaterialItems>().AddItem(); //���߰�
                AudioManager.Instance.PlaySfx(AudioManager.Sfx.Rock);

                isAttack = false; //�ѹ��� ���ݰ����ϵ��� false�� ����
            }
        }
    }
    protected virtual void OnDestroy()
    {
        SceneLoader.OnSceneLoaded -= Initialize;  
    }
    //�� ����� CamScript�� �ٽ� Ž����
    void Initialize()
    {
        try
        {
            if (Camera.main != null)
            {
                camScript = Camera.main.GetComponent<Cam>();
                if (camScript == null)
                {
                    Debug.LogWarning("Cam ��ũ��Ʈ�� ã�� �� �����ϴ�.");
                }
            }
            else
            {
                Debug.LogWarning("���� ī�޶� ã�� �� �����ϴ�.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ī�޶� �ʱ�ȭ �� ���� �߻�: {e.Message}");
            camScript = null;
        }
    }
    public void EnableHitBox() //�÷��̾� �ǰݽ� ȣ��
    {
        P_GetHit = true;
        if (type != Type.IronBow && type != Type.WoodBow) //Ȱ�� �ݶ��̴��� ���
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
