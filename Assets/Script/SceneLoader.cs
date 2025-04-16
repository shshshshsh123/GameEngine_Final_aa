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
        
        // AudioManager 초기화 및 BGM 정지
        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.DisableSFX();
        }

        tipNum = tipNum == 2 ? 0 : tipNum + 1;
        switch (tipNum)
        {
            case 0:
                tipText.text = "Tip!\t플레이어 레벨을 높여 체력과 공격력을 상승시키세요.";
                loadingBG.texture = LoadingBG[0];
                break;  
            case 1:
                tipText.text = "Tip!\t상자를 열어 희귀 재료를 획득하세요.";
                loadingBG.texture = LoadingBG[1];
                break;
            case 2:
                tipText.text = "Tip!\t구르기를 통해 적의 공격을 회피하세요.";
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
        // AudioManager 로딩 완료 대기
        while (AudioManager.Instance == null || !AudioManager.Instance.IsAudioLoaded)
        {
            yield return null;
        }

        // 모든 오디오 클립이 로드될 때까지 추가 대기
        while (!AudioManager.Instance.AreAllClipsLoaded())
        {
            yield return null;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;    // 씬 전환을 수동으로 제어
        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;

            if (op.progress < 0.90f)
            {   // 90퍼센트까지는 진짜로딩
                loadingBar.fillAmount = op.progress;
            }
            else
            {   // 페이크로딩
                timer += Time.unscaledDeltaTime * 0.7f;
                loadingBar.fillAmount = Mathf.Lerp(0.9f, 1f, timer);

                if (loadingBar.fillAmount >= 1f && op.progress >= 0.9f)
                {
                    // 다음 씬을 활성화
                    op.allowSceneActivation = true;

                    // 씬이 완전히 로드될 때까지 대기
                    while (!op.isDone)
                    {
                        yield return null;
                    }

                    // 추가 대기 시간으로 모든 오브젝트와 스크립트가 초기화되도록 보장
                    yield return new WaitForSeconds(0.5f);

                    // 씬의 모든 오브젝트가 Awake와 Start를 실행할 수 있도록 한 프레임 대기
                    yield return new WaitForEndOfFrame();

                    OnSceneLoaded?.Invoke();   
                    //시작 씬이 아니면 실행
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
