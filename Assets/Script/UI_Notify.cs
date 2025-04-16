using UnityEngine;

public class UI_Notify : MonoBehaviour
{
    Animator animator;
    private void Start()
    {
        RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("Animator/Notify");
        if (animatorController != null)
        {
            animator = GetComponent<Animator>();    
        }
        else
        {
            Debug.LogError("Failed to load NotifyAnimator from Resources.");
        }
    }
    public void EndOfAni()
    {
        animator.SetBool("UTT", false);
    }
}
