using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUD_Slow : MonoBehaviour
{
   
    public Vector3 pos;
    RawImage icon;
    bool IsTransColor = false;
    private void OnEnable()
    {
        IsTransColor = false;
        if(icon != null) icon.color = Color.red;   
    }
    private void Start()
    {
        icon = GetComponent<RawImage>();
        icon.color = Color.red;
    }
    private void Update()
    {
        if (!UIManager.Instance.IsSlow) //침묵상태면 HUD실행
        {
            StartCoroutine(TransEnable());
        }
        transform.position = Player.Instance.transform.position + pos;  
    }

    IEnumerator TransEnable()
    {
        if (!IsTransColor)
        {
            IsTransColor = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.smoothDeltaTime;
                icon.color = Color.Lerp(Color.red, Color.clear, t);
                if(UIManager.Instance.IsSlow) //slow HUD가 사라지던중에 다시 isSlow가 참이되면 재시작
                {
                    IsTransColor = false;
                    yield break;
                }
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
