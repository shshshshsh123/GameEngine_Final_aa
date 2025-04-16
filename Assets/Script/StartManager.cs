using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartManager : MonoBehaviour
{   
    public GameObject MainUI;
    public GameObject SetUpUI;
    public GameObject[] settingUI;
    public Scrollbar bgmVolume;
    public Scrollbar sfxVolume;
    public Scrollbar monsterVolume;
    public Text[] etcText;
    // 온오프기능
    public bool shakeOn = true;
    public bool motionTrailOn = true;
    public bool camNorthFix = false;
    private bool isInitialized = false;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.data != null)
        {
            InitializeSettings();
        }
        else
        {
            StartCoroutine(WaitForGameManager());
        }
    }

    private void InitializeSettings()
    {
        shakeOn = GameManager.Instance.data.shakeOn;
        motionTrailOn = GameManager.Instance.data.motionTrailOn;
        camNorthFix = GameManager.Instance.data.camNorthFix;

        // UI 초기값 설정
        bgmVolume.value = GameManager.Instance.data.bgmVolume;
        sfxVolume.value = GameManager.Instance.data.sfxVolume;
        monsterVolume.value = GameManager.Instance.data.monsterSfxVolume;

        // 텍스트 초기값 설정
        etcText[0].text = shakeOn ? "켜짐" : "꺼짐";
        etcText[1].text = motionTrailOn ? "켜짐" : "꺼짐";
        etcText[2].text = camNorthFix ? "켜짐" : "꺼짐";

        isInitialized = true;
        
        //시작 씬일때 배경음악 재생
        StartCoroutine(PlayStartBGM());
    }

    private IEnumerator PlayStartBGM()
    {
        // AudioManager가 초기화될 때까지 대기
        while (!AudioManager.Instance.IsAudioLoaded || !AudioManager.Instance.audioInitialized)
        {
            Debug.Log("Waiting for AudioManager initialization...");
            yield return null;
        }

        Debug.Log("AudioManager is ready, playing start BGM");
        AudioManager.Instance.PlayBGM(0);
    }

    private IEnumerator WaitForGameManager()
    {
        while (GameManager.Instance == null || GameManager.Instance.data == null)
        {
            yield return null;
        }
        InitializeSettings();
    }

    void Update()
    {
        if (!isInitialized)
            return;

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettingUI();
        }
    }

    public void StartGame()
    {
        if (!isInitialized) return;
        GameManager.Instance.StartGame();
    }

    public void SetupGame()
    {
        if (!isInitialized) return;
        MainUI.SetActive(false);
        SetUpUI.SetActive(true);
    }

    public void ExitGame()
    {
        //UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
    
    //setting UI 기능 함수
    public void CloseSettingUI()
    {
        if (!isInitialized) return;

        if (SetUpUI.activeSelf)
        {
            if (settingUI[0].activeSelf) //기본 세팅 ui만 있으면 전체 비활성화
            {
                SetUpUI.SetActive(false);
                MainUI.SetActive(true);
            }
            else //다른 ui가 열려있으면 기본 세팅 ui 오픈
            {
                // UI 다끄고
                for (int i = 0; i < settingUI.Length; i++)
                {
                    settingUI[i].SetActive(false);
                }
                // 선택 UI 키기
                settingUI[0].SetActive(true);
            }
        }
    }

    public void OpenSettingUI(int num)  // 0 = base, 1 = sound, 2 = key, 3 = etc
    {
        if (!isInitialized) return;

        // 기본창에서 닫기버튼 -> 종료
        if (num == 0 && settingUI[0].activeSelf) CloseSettingUI();
        // UI 다끄고
        for (int i = 0; i < settingUI.Length; i++)
        {
            settingUI[i].SetActive(false);
        }
        // 선택 UI 키기
        settingUI[num].SetActive(true);
    }

    public void ChangeBGM()
    {
        if (!isInitialized || GameManager.Instance == null || GameManager.Instance.data == null) return;
        GameManager.Instance.data.bgmVolume = bgmVolume.value;
    }

    public void ChangeSFX()
    {
        if (!isInitialized || GameManager.Instance == null || GameManager.Instance.data == null) return;
        GameManager.Instance.data.sfxVolume = sfxVolume.value;
    }

    public void ChangeMonsterSFX()
    {
        if (!isInitialized || GameManager.Instance == null || GameManager.Instance.data == null) return;
        GameManager.Instance.data.monsterSfxVolume = monsterVolume.value;
    }

    public void TransShake()
    {
        if (!isInitialized || GameManager.Instance == null || GameManager.Instance.data == null) return;
        shakeOn = !shakeOn;
        GameManager.Instance.data.shakeOn = shakeOn;
        etcText[0].text = shakeOn ? "켜짐" : "꺼짐";
    }

    public void TransMotionTrail()
    {
        if (!isInitialized || GameManager.Instance == null || GameManager.Instance.data == null) return;
        motionTrailOn = !motionTrailOn;
        GameManager.Instance.data.motionTrailOn = motionTrailOn;
        etcText[1].text = motionTrailOn ? "켜짐" : "꺼짐";
    }

    public void CamNorthFix()
    {
        if (!isInitialized || GameManager.Instance == null || GameManager.Instance.data == null) return;
        camNorthFix = !camNorthFix;
        GameManager.Instance.data.camNorthFix = camNorthFix;
        etcText[2].text = camNorthFix ? "켜짐" : "꺼짐";
    }
}
