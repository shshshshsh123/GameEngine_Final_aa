using System.Collections;
using UnityEngine;

public class Cam : MonoBehaviour
{
    // ī�޶�
    Camera mainCamera;
    [Header("Camera Setting")]
    [SerializeField] Vector3 camOffset;
    [SerializeField][Range(0.0f, 20.0f)] float smoothness = 5f;
    private float OriginCamY;
    [Header("MiniMapCam")]
    [SerializeField] GameObject minimapCamera;
    [SerializeField] GameObject InvCamera;
    
    //��� Ʈ���� �ߺ� ����
    [SerializeField] public bool PlayMotionTrail = false;
    
    //�÷��̾�
    GameObject playerObj;
    Player player;
    Vector3 playerLastPos;
    // �¿������
    public bool shakeOn = true;
    public bool motionTrailOn = true;
    public bool camNorthFix = false;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        
        //�� �ε��ɶ����� ã�ƾ� �ؼ�
        playerObj = Player.Instance.gameObject;
        player = Player.Instance;
        
        transform.position = playerObj.transform.position + camOffset;
        mainCamera.transform.LookAt(playerObj.transform.position);
        OriginCamY = mainCamera.transform.position.y;

        shakeOn = GameManager.Instance.data.shakeOn;
        motionTrailOn = GameManager.Instance.data.motionTrailOn;
        camNorthFix = GameManager.Instance.data.camNorthFix;
    }

    // Update is called once per frame
    void Update()
    {
        CamMove();
        Map();
        MoveInv();
    }
    void CamMove()
    {
        transform.position = playerObj.transform.position + camOffset;
        // playerLastPos = playerObj.transform.position;
        // if (Vector3.Distance(playerLastPos, playerObj.transform.position) > 0.01f) return;    // �������� ������ ī�޶� ���߱�

        // Vector3 CamPos = Vector3.Lerp(mainCamera.transform.position,
        //     playerObj.transform.position + camOffset, smoothness * Time.deltaTime);
        // CamPos.y = OriginCamY;              //ī�޶� y������ �̵��Ҷ� collider������ ������ ������ ���ֱ� ���� �߰���
        // mainCamera.transform.position = CamPos;
    }
    void Map()
    {
        // �̴ϸ� ī�޶��̵�
        Vector3 minimapCamDir = playerObj.transform.position;
        minimapCamDir.y = 100.0f;   // y�� ����
        minimapCamera.transform.position = minimapCamDir;
        // �̴ϸ� ī�޶� ȸ��
        if (camNorthFix) minimapCamera.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);   // ī�޶� ���� ����
        else minimapCamera.transform.rotation = Quaternion.Euler(90.0f, playerObj.transform.eulerAngles.y, 0.0f);    // ȸ��
    }

    void MoveInv()
    {
        if(UIManager.Instance.InvenUI.activeSelf)
        {
            Vector3 playerMove = player.moveDir * 2f;
            Vector3 InvMoveDir = playerObj.transform.position + playerObj.transform.forward * 2;
            Vector3 LookPos = playerObj.transform.position;
            InvMoveDir.y = 1f;
            LookPos.y = 1f;
            InvCamera.transform.position = InvMoveDir;
            InvCamera.transform.LookAt(LookPos);
        }
    }

    public IEnumerator ShakeCam(float ShakeAmount, float ShakeTime = 1f)
    {
        if (!shakeOn) yield break;
        float t = 0;

        for (int i = 0; i < 5; i++)
        {
            Vector3 transPos = new Vector3(Random.Range(-ShakeAmount, ShakeAmount), Random.Range(-ShakeAmount, ShakeAmount), 0);
            while (t < ShakeTime)
            {
                t += Time.smoothDeltaTime * (smoothness + 5f);
                transform.position = Vector3.Slerp(transform.position, playerObj.transform.position + camOffset + transPos, t);
                yield return null;
            }
            t = 0;
        }
        while (t < ShakeTime) //�ʱ� ��ġ�� �̵�
        {
            t += Time.smoothDeltaTime * 1.5f;
            transform.position = Vector3.Slerp(transform.position, playerObj.transform.position + camOffset, t);
            yield return null;
        }
        transform.position = playerObj.transform.position + camOffset;
    }
    
    //����ȿ��
    public IEnumerator HitStop(float MotionAmount = 0.1f, int ZoomFOV = 55, bool IsAttack = false) //isAttack : ���ݻ������� �뽬 ��������
    {
        if (!PlayMotionTrail) //motion trail�� ������°� �ƴϸ�
        {
            if(IsAttack) 
            {
                player.playerStats.AttackInvinci = true; //�÷��̾� ���� ���� ����
                StartCoroutine(EnableAttack(1.5f));
            }
            PlayMotionTrail = true;
            if (motionTrailOn) { Time.timeScale = 0; } //����ȿ�� on ���¸�
            float t = 0;
            float CurrentFOV = Camera.main.fieldOfView;
            while (t < MotionAmount)
            {
                t += Time.unscaledDeltaTime;
                if(shakeOn) Camera.main.fieldOfView = Mathf.Lerp(CurrentFOV, ZoomFOV, t / MotionAmount);
                yield return null;
            }
            t = 0;
            while (t < MotionAmount)
            {
                t += Time.unscaledDeltaTime;
                if (shakeOn) Camera.main.fieldOfView = Mathf.Lerp(ZoomFOV, CurrentFOV, t / MotionAmount);
                yield return null;
            }
            Time.timeScale = 1;
            yield return new WaitForSeconds(0.5f);
            PlayMotionTrail = false;
            player.playerStats.isAttack = false; //�÷��̾� ���� ���� ����
        }
    }
    public IEnumerator EnableAttack(float WaitT)
    {
        float t = 0;
        while(t < WaitT)
        {
            t += Time.smoothDeltaTime;
            if (!player.playerStats.AttackInvinci) //���ð����� ���ݹ����� ���� ������ �ڷ�ƾ ����
            {
                yield break;
            }
            yield return null;
        }
        player.playerStats.AttackInvinci = false; //���Ϳ��� ���� ������ ������ ��츦 ����ؼ� �ð��� ������ ���� ����
    }
}