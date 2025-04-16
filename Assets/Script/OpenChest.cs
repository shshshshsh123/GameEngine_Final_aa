using UnityEngine;

public class OpenChest : MonoBehaviour
{
    //상자 상호작용 변수
    bool isFirstTouch = true;
    bool isTouch = false;
    float OpenT = 0f;
    Animator animator;

    //상자 비활성화 변수
    bool isOpen = false;
    float AlphaT = 0f;
    public Material BoxMat;
    public GameObject coin;
    // Start is called before the first frame update
    void Start()
    {
        RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("Animator/Chest");
        if (animatorController != null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = gameObject.AddComponent<Animator>();

            // Animator Controller 설정
            animator.runtimeAnimatorController = animatorController;
            Debug.Log("ChestAnimator assigned successfully.");
        }
        else
        {
            Debug.LogError("Failed to load ChestAnimator from Resources.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        ActionChest();
        DisableChest();
    }
    private void OnApplicationQuit()
    {
        BoxMat.color = Color.white;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "Player")
        {
            if (!isTouch)
            {
                isTouch = true;
            }
        }
    }
    void ActionChest()
    {
        if (isTouch && !isOpen)
        {
            if (Player.Instance.actionAxis) //상호작용 키를 누르고 있으면
            {
                if (isFirstTouch)
                {
                    isFirstTouch = false;
                    animator.SetBool("isEmpty", false); //exit하는 파라미터 초기화
                    animator.SetTrigger("TryOpen");
                }
                OpenT += Time.deltaTime;
                if (OpenT > 2f) //2초동안 누르고 있으면 상자 열기
                {
                    OpenT = 0f;
                    animator.SetTrigger("Open");
                }
            }
            else
            {
                if (OpenT > 0f) //상호작용 키를 때면
                {
                    isTouch = false;
                    isFirstTouch = true;
                    OpenT = 0f;
                    animator.SetBool("isEmpty", true);
                }
            }
        }
    }

    void DisableChest()
    {
        if (isOpen)
        {
            AlphaT += 0.05f * Time.deltaTime;
            BoxMat.color = Color.Lerp(BoxMat.color, Color.black, AlphaT);
            if(coin.activeSelf)
            {
                coin.SetActive(false);
            }
            if(BoxMat.color.r <= 0.1f)
            {
                BoxMat.color = Color.white;
                AlphaT = 0f;
                isOpen = false;
                gameObject.SetActive(false);
            }
        }
    }
    public void FinishOpen() //오픈 애니메이션 끝나면 실행
    {
        isOpen = true;
        int rand = Random.Range(0, 5) + 4;

        Item item = new Item();
        item = ItemManager.Instance.mat.MatData[rand];
        InventoryManager.Instance.AddItem(item, 1);

        UIManager.Instance.TransActiveInven();
        UIManager.Instance.ChestUiOpen(rand - 4);
    }
}
