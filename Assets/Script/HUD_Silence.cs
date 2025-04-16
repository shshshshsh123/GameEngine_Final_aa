using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUD_Silence : MonoBehaviour
{
    public Vector3 pos;
    RawImage icon;
    bool IsTransColor = false;
    private void OnEnable()
    {
        IsTransColor = false;
        if(icon != null) icon.color = Color.white;   
    }
    private void Start()
    {
        icon = GetComponent<RawImage>();
        icon.color = Color.white;
    }
    private void Update()
    {
        if (!UIManager.Instance.IsSilence) //침묵상태면 HUD실행
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
                icon.color = Color.Lerp(Color.white, Color.clear, t);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}