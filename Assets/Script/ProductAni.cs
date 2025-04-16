using UnityEngine;
using UnityEngine.UI;

public class ProductAni : MonoBehaviour
{
    Animator animator;
    Image image;
    Color BaseColor;
    // Start is called before the first frame update
    void Awake()
    {
        RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("Animator/Product");
        if (animatorController != null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = gameObject.AddComponent<Animator>();

            // Animator Controller 설정
            animator.runtimeAnimatorController = animatorController;
            Debug.Log("ProductAnimator assigned successfully.");
        }
        else
        {
            Debug.LogError("Failed to load ProductAnimator from Resources.");
        }
        image = gameObject.GetComponent<Image>();
        BaseColor = image.color;
    }

    private void OnDisable()
    {
        image.fillAmount = 1;
        image.color = BaseColor;
    }
    public void TransSuccess()
    {
        animator.SetBool("CanCreate", true);
    }
    public void TransFail()
    {
        animator.SetBool("CanCreate", true);
    }
}
