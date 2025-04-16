using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class SceneLoader : MonoBehaviour
{
    public static event Action OnSceneLoaded;

    static string nextScene;
    static int tipNum = 1;
    public Texture2D[] LoadingBG;
    public RawImage loadingBG;
    public Image loadingBar;
    public Text tipText;
    int index = 0;

    public static void LoadScene(string sceneName)
    {
        nextScene = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    void Start()
    {
        if(!GameManager.Instance.isStartScene) UIManager.Instance.DragonHpBarObj.SetActive(false);
        
        // AudioManager �ʱ�ȭ �� BGM ����
        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.DisableSFX();
        }

        tipNum = tipNum == 2 ? 0 : tipNum + 1;
        switch (tipNum)
        {
            case 0:
                tipText.text = "Tip!\t�÷��̾� ������ ���� ü�°� ���ݷ��� ��½�Ű����.";
                loadingBG.texture = LoadingBG[0];
                break;  
            case 1:
                tipText.text = "Tip!\t���ڸ� ���� ��� ��Ḧ ȹ���ϼ���.";
                loadingBG.texture = LoadingBG[1];
                break;
            case 2:
                tipText.text = "Tip!\t�����⸦ ���� ���� ������ ȸ���ϼ���.";
                loadingBG.texture = LoadingBG[2];
                break;
        }
        
        switch (nextScene)
        {
            case "Tree":
                index = 0;
                break;
            case "Base":
                index = 1;
                break;
            case "Rock":
                index = 2;
                break;
            case "Boss":
                index = 3;
                break;
            default:
                index = 0;
                break;
        }

        StartCoroutine(LoadSceneProcess());
    }


    IEnumerator LoadSceneProcess()
    {
        // AudioManager �ε� �Ϸ� ���
        while (AudioManager.Instance == null || !AudioManager.Instance.IsAudioLoaded)
        {
            yield return null;
        }

        // ��� ����� Ŭ���� �ε�� ������ �߰� ���
        while (!AudioManager.Instance.AreAllClipsLoaded())
        {
            yield return null;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;    // �� ��ȯ�� �������� ����
        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;

            if (op.progress < 0.90f)
            {   // 90�ۼ�Ʈ������ ��¥�ε�
                loadingBar.fillAmount = op.progress;
            }
            else
            {   // ����ũ�ε�
                timer += Time.unscaledDeltaTime * 0.7f;
                loadingBar.fillAmount = Mathf.Lerp(0.9f, 1f, timer);

                if (loadingBar.fillAmount >= 1f && op.progress >= 0.9f)
                {
                    // ���� ���� Ȱ��ȭ
                    op.allowSceneActivation = true;

                    // ���� ������ �ε�� ������ ���
                    while (!op.isDone)
                    {
                        yield return null;
                    }

                    // �߰� ��� �ð����� ��� ������Ʈ�� ��ũ��Ʈ�� �ʱ�ȭ�ǵ��� ����
                    yield return new WaitForSeconds(0.5f);

                    // ���� ��� ������Ʈ�� Awake�� Start�� ������ �� �ֵ��� �� ������ ���
                    yield return new WaitForEndOfFrame();

                    OnSceneLoaded?.Invoke();   
                    //���� ���� �ƴϸ� ����
                    if(!GameManager.Instance.isStartScene) 
                    {
                        UIManager.Instance.DragonHpBarObj.SetActive(false);
                    }
                    
                    yield break;
                }
            }
        }
    }
}
