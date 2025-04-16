using System.Collections;
using UnityEngine;

public class Cam : MonoBehaviour
{
    // 카메라
    Camera mainCamera;
    [Header("Camera Setting")]
    [SerializeField] Vector3 camOffset;
    [SerializeField][Range(0.0f, 20.0f)] float smoothness = 5f;
    private float OriginCamY;
    [Header("MiniMapCam")]
    [SerializeField] GameObject minimapCamera;
    [SerializeField] GameObject InvCamera;
    
    //모션 트레일 중복 금지
    [SerializeField] public bool PlayMotionTrail = false;
    
    //플레이어
    GameObject playerObj;
    Player player;
    Vector3 playerLastPos;
    // 온오프기능
    public bool shakeOn = true;
    public bool motionTrailOn = true;
    public bool camNorthFix = false;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        
        //씬 로딩될때마다 찾아야 해서
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
        // if (Vector3.Distance(playerLastPos, playerObj.transform.position) > 0.01f) return;    // 움직이지 않으면 카메라 멈추기

        // Vector3 CamPos = Vector3.Lerp(mainCamera.transform.position,
        //     playerObj.transform.position + camOffset, smoothness * Time.deltaTime);
        // CamPos.y = OriginCamY;              //카메라가 y축으로 이동할때 collider때문에 떨리는 현상을 없애기 위해 추가함
        // mainCamera.transform.position = CamPos;
    }
    void Map()
    {
        // 미니맵 카메라이동
        Vector3 minimapCamDir = playerObj.transform.position;
        minimapCamDir.y = 100.0f;   // y는 고정
        minimapCamera.transform.position = minimapCamDir;
        // 미니맵 카메라 회전
        if (camNorthFix) minimapCamera.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);   // 카메라 북향 고정
        else minimapCamera.transform.rotation = Quaternion.Euler(90.0f, playerObj.transform.eulerAngles.y, 0.0f);    // 회전
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
        while (t < ShakeTime) //초기 위치로 이동
        {
            t += Time.smoothDeltaTime * 1.5f;
            transform.position = Vector3.Slerp(transform.position, playerObj.transform.position + camOffset, t);
            yield return null;
        }
        transform.position = playerObj.transform.position + camOffset;
    }
    
    //경직효과
    public IEnumerator HitStop(float MotionAmount = 0.1f, int ZoomFOV = 55, bool IsAttack = false) //isAttack : 공격상태인지 대쉬 상태인지
    {
        if (!PlayMotionTrail) //motion trail이 실행상태가 아니면
        {
            if(IsAttack) 
            {
                player.playerStats.AttackInvinci = true; //플레이어 공격 무적 실행
                StartCoroutine(EnableAttack(1.5f));
            }
            PlayMotionTrail = true;
            if (motionTrailOn) { Time.timeScale = 0; } //경직효과 on 상태면
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
            player.playerStats.isAttack = false; //플레이어 공격 상태 해제
        }
    }
    public IEnumerator EnableAttack(float WaitT)
    {
        float t = 0;
        while(t < WaitT)
        {
            t += Time.smoothDeltaTime;
            if (!player.playerStats.AttackInvinci) //대기시간동안 공격무적이 해제 됐으면 코루틴 종료
            {
                yield break;
            }
            yield return null;
        }
        player.playerStats.AttackInvinci = false; //몬스터에서 무적 해제를 못했을 경우를 대비해서 시간이 지나면 무적 해제
    }
}