using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum DragonSfx { Attack, Hit, Death, Scream, Land, Flame, Fly, Meteor, Explosion };
public enum GuardianSfx { Attack, Hit, Death, Sp1, Sp2, Sp3 };

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    private static readonly object _lock = new object();
    private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>(); // 로드된 오디오 클립 저장
    private bool isInitialized = false;
    public bool audioInitialized { get; private set; } = false;
    public bool IsAudioLoaded { get; private set; } = false;

    public static AudioManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("AudioManager");
                        _instance = singleton.AddComponent<AudioManager>();
                        DontDestroyOnLoad(singleton);
                    }
                    else
                    {
                        DontDestroyOnLoad(_instance.gameObject);
                    }

                    _instance.Init();
                }

                return _instance;
            }
        }
    }

    [Header("#Bgm")]
    public AudioClip[] bgmClips;
    public float bgmVolume;
    AudioSource bgmPlayer;
    public int bgmIndex = 0;

    [Header("#Sfx")]
    public AudioClip[] sfxClips;
    public float sfxVolume;
    AudioSource[] sfxPlayers;
    public int channels;
    int channelIndex = 0;

    [Header("#Foot Step")]
    public AudioClip[] footStepClips;
    public float footStepVolume;
    AudioSource footStepPlayer;

    [Header("#DragonSFX")]
    public AudioClip[] dragonSFX;
    public float dragonVolume;
    AudioSource[] dragonSFXPlayer;
    int dragonChannel = 0;

    [Header("#GuardianSFX")]
    public AudioClip[] guardianSFX;
    public float guardianVolume;
    AudioSource[] guardianSFXPlayer;
    int guardianChannel = 0;

    public enum Sfx { Hit, Dash, Eat, Sword, Bow, Arrow, Knife, Spear, Wood, Rock, Click, Error };


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
            StartCoroutine(InitializeAudioManager());
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        if (!isInitialized || !audioInitialized || GameManager.Instance == null || GameManager.Instance.data == null)
            return;

        // 데이터 불러오기
        bgmVolume = GameManager.Instance.data.bgmVolume;
        sfxVolume = GameManager.Instance.data.sfxVolume;
        footStepVolume = GameManager.Instance.data.sfxVolume;
        dragonVolume = GameManager.Instance.data.monsterSfxVolume;
        guardianVolume = GameManager.Instance.data.monsterSfxVolume;    

        // 볼륨 적용 (null 체크 추가)
        if (bgmPlayer != null)
        {
            if (bgmIndex == 3) { bgmPlayer.volume = bgmVolume / 1.5f; } //boss 볼륨 감소
            else if (bgmIndex == 1) { bgmPlayer.volume = bgmVolume / 1.2f; } //base 볼륨 감소
            else { bgmPlayer.volume = bgmVolume; }
        }

        if (sfxPlayers != null)
        {
            for (int i = 0; i < channels; i++) 
                if (sfxPlayers[i] != null)
                    sfxPlayers[i].volume = sfxVolume;
        }
        
        if (footStepPlayer != null)
            footStepPlayer.volume = footStepVolume;
        
        if (dragonSFXPlayer != null)
        {
            for (int i = 0; i < dragonSFXPlayer.Length; i++) 
                if (dragonSFXPlayer[i] != null)
                    dragonSFXPlayer[i].volume = dragonVolume;
        }
        
        if (guardianSFXPlayer != null)
        {
            for (int i = 0; i < guardianSFXPlayer.Length; i++) 
                if (guardianSFXPlayer[i] != null)
                    guardianSFXPlayer[i].volume = guardianVolume;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;

            // 오디오 데이터 언로드
            if (bgmClips != null)
                foreach (var clip in bgmClips)
                    if (clip != null) clip.UnloadAudioData();

            if (sfxClips != null)
                foreach (var clip in sfxClips)
                    if (clip != null) clip.UnloadAudioData();

            if (footStepClips != null)
                foreach (var clip in footStepClips)
                    if (clip != null) clip.UnloadAudioData();

            if (dragonSFX != null)
                foreach (var clip in dragonSFX)
                    if (clip != null) clip.UnloadAudioData();

            if (guardianSFX != null)
                foreach (var clip in guardianSFX)
                    if (clip != null) clip.UnloadAudioData();

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "LoadingScene") return;
        
        if(scene.name == "Tree") PlayBGM(0);
        else if(scene.name == "Base") PlayBGM(1);
        else if(scene.name == "Rock") PlayBGM(2);
        else if(scene.name == "Boss") PlayBGM(3);
        else PlayBGM(0);
        
        EnableSFX();
    }

    private void Init()
    {
        if (!gameObject.name.Contains("AudioManager"))
        {
            gameObject.name = "AudioManager";
        }
    }

    public bool AreAllClipsLoaded()
    {
        if (bgmClips == null || sfxClips == null || footStepClips == null || 
            dragonSFX == null || guardianSFX == null)
        {
            Debug.LogWarning("Some audio clip arrays are null");
            return false;
        }

        int totalClips = 0;
        int loadedClips = 0;

        // BGM 클립은 Streaming이므로 null 체크만 수행
        foreach (var clip in bgmClips)
        {
            if (clip != null)
            {
                totalClips++;
                loadedClips++;  // Streaming이므로 항상 로드된 것으로 간주
            }
        }

        // SFX 클립 체크
        foreach (var clip in sfxClips)
        {
            if (clip != null)
            {
                totalClips++;
                if (clip.loadState.Equals(AudioDataLoadState.Loaded))
                    loadedClips++;
            }
        }

        // 발소리 클립 체크
        foreach (var clip in footStepClips)
        {
            if (clip != null)
            {
                totalClips++;
                if (clip.loadState.Equals(AudioDataLoadState.Loaded))
                    loadedClips++;
            }
        }

        // 드래곤 SFX 클립 체크
        foreach (var clip in dragonSFX)
        {
            if (clip != null)
            {
                totalClips++;
                if (clip.loadState.Equals(AudioDataLoadState.Loaded))
                    loadedClips++;
            }
        }

        // 가디언 SFX 클립 체크
        foreach (var clip in guardianSFX)
        {
            if (clip != null)
            {
                totalClips++;
                if (clip.loadState.Equals(AudioDataLoadState.Loaded))
                    loadedClips++;
            }
        }

        if (totalClips != loadedClips)
        {
            Debug.Log($"Audio clips loading progress: {loadedClips}/{totalClips}");
        }

        return totalClips == loadedClips && totalClips > 0;
    }

    private IEnumerator LoadAudioClipInBackground(AudioClip clip, bool isStreaming = false)
    {
        if (clip != null)
        {
            if (!isStreaming && !clip.loadState.Equals(AudioDataLoadState.Loaded))
            {
                clip.LoadAudioData();
                while (!clip.loadState.Equals(AudioDataLoadState.Loaded))
                {
                    yield return null;
                }
                Debug.Log($"Audio clip loaded: {clip.name}");
            }
        }
    }

    private IEnumerator InitializeAudioManager()
    {
        IsAudioLoaded = false;
        Debug.Log("Starting AudioManager initialization...");
        
        // GameManager 초기화 대기
        while (GameManager.Instance == null || GameManager.Instance.data == null)
        {
            yield return null;
        }
        
        AudioInit();
        InitializeVolumes();

        // 모든 오디오 클립을 백그라운드에서 로드
        List<Coroutine> loadCoroutines = new List<Coroutine>();

        // BGM 클립은 Streaming이므로 로드 체크만 수행
        Debug.Log("Checking BGM clips...");
        foreach (var clip in bgmClips)
        {
            if (clip != null)
            {
                loadCoroutines.Add(StartCoroutine(LoadAudioClipInBackground(clip, true)));
            }
        }

        // SFX 클립 로드
        Debug.Log("Loading SFX clips...");
        foreach (var clip in sfxClips)
        {
            if (clip != null)
            {
                loadCoroutines.Add(StartCoroutine(LoadAudioClipInBackground(clip)));
            }
        }

        // 발소리 클립 로드
        Debug.Log("Loading footstep clips...");
        foreach (var clip in footStepClips)
        {
            if (clip != null)
            {
                loadCoroutines.Add(StartCoroutine(LoadAudioClipInBackground(clip)));
            }
        }

        // 드래곤 SFX 클립 로드
        Debug.Log("Loading dragon SFX clips...");
        foreach (var clip in dragonSFX)
        {
            if (clip != null)
            {
                loadCoroutines.Add(StartCoroutine(LoadAudioClipInBackground(clip)));
            }
        }

        // 가디언 SFX 클립 로드
        Debug.Log("Loading guardian SFX clips...");
        foreach (var clip in guardianSFX)
        {
            if (clip != null)
            {
                loadCoroutines.Add(StartCoroutine(LoadAudioClipInBackground(clip)));
            }
        }

        Debug.Log($"Waiting for {loadCoroutines.Count} audio clips to load...");

        // 모든 코루틴이 완료될 때까지 대기
        foreach (var coroutine in loadCoroutines)
        {
            yield return coroutine;
        }

        audioInitialized = true;
        IsAudioLoaded = true;
        Debug.Log("Audio Manager initialization completed successfully!");
    }

    private void InitializeVolumes()
    {
        bgmVolume = GameManager.Instance.data.bgmVolume;
        sfxVolume = GameManager.Instance.data.sfxVolume;
        footStepVolume = GameManager.Instance.data.sfxVolume;
        dragonVolume = GameManager.Instance.data.monsterSfxVolume;
        guardianVolume = GameManager.Instance.data.monsterSfxVolume;
        isInitialized = true;
    }

    void AudioInit()
    {
        // 배경음 초기화
        GameObject bgmObj = new GameObject("BgmPlayer");
        bgmObj.transform.parent = transform;
        bgmPlayer = bgmObj.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;

        // 효과음 초기화
        GameObject sfxObj = new GameObject("SfxPlayer");
        sfxObj.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            sfxPlayers[i] = sfxObj.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake = false;
            sfxPlayers[i].loop = false;
            sfxPlayers[i].volume = sfxVolume;
        }

        // 발소리 초기화
        GameObject footStepObj = new GameObject("FootStepPlayer");
        footStepObj.transform.parent = transform;
        footStepPlayer = footStepObj.AddComponent<AudioSource>();
        footStepPlayer.playOnAwake = false;
        footStepPlayer.loop = false;
        footStepPlayer.volume = footStepVolume;

        // 드래곤 SFX 초기화
        dragonChannel = dragonSFX.Length;
        GameObject dragonObj = new GameObject("DragonPlayer");
        dragonObj.transform.parent = transform;
        dragonSFXPlayer = new AudioSource[dragonChannel];

        for (int i = 0; i < dragonSFXPlayer.Length; i++)
        {
            dragonSFXPlayer[i] = dragonObj.AddComponent<AudioSource>();
            dragonSFXPlayer[i].playOnAwake = false;
            dragonSFXPlayer[i].loop = false;
            dragonSFXPlayer[i].volume = dragonVolume;
        }

        // 가디언 SFX 초기화
        guardianChannel = guardianSFX.Length;
        GameObject guardianObj = new GameObject("GuardianPlayer");
        guardianObj.transform.parent = transform;
        guardianSFXPlayer = new AudioSource[guardianChannel];

        for (int i = 0; i < guardianSFXPlayer.Length; i++)
        {
            guardianSFXPlayer[i] = guardianObj.AddComponent<AudioSource>();
            guardianSFXPlayer[i].playOnAwake = false;
            guardianSFXPlayer[i].loop = false;
            guardianSFXPlayer[i].volume = guardianVolume;
        }
    }

    public void PlayBGM(int index)
    {
        if (!IsAudioLoaded || !audioInitialized)
        {
            Debug.LogWarning($"Cannot play BGM: Audio system not fully initialized (IsAudioLoaded: {IsAudioLoaded}, audioInitialized: {audioInitialized})");
            return;
        }

        StartCoroutine(PlayBGMWhenSceneLoaded(index));
    }

    private IEnumerator PlayBGMWhenSceneLoaded(int index)
    {
        // 씬이 완전히 로딩될 때까지 대기
        while (!SceneManager.GetActiveScene().isLoaded)
        {
            yield return null;
        }

        if (index < 0 || index >= bgmClips.Length)
        {
            Debug.LogError($"Invalid BGM index: {index}. Available range: 0-{bgmClips.Length - 1}");
            yield break;
        }

        if (bgmClips[index] == null)
        {
            Debug.LogError($"BGM clip at index {index} is null");
            yield break;
        }

        bgmPlayer.clip = bgmClips[index];
        bgmPlayer.Play();
        bgmIndex = index;
        Debug.Log($"Playing BGM: {bgmClips[index].name} in scene: {SceneManager.GetActiveScene().name}");
    }

    public void PlaySfx(Sfx sfx)
    {
        if (!IsAudioLoaded || !audioInitialized)
        {
            Debug.LogWarning("Cannot play SFX: Audio system not fully initialized");
            return;
        }

        if(sfx == Sfx.Hit) //Hit 효과음 재생시 모든 효과음 정지
        {
            for(int i = 0; i < sfxPlayers.Length; i++)
            {
                sfxPlayers[i].Stop();
            }
        }
        
        // 빈 채널 찾기
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % sfxPlayers.Length;
            if (sfxPlayers[loopIndex].isPlaying) continue;

            channelIndex = loopIndex;
            sfxPlayers[channelIndex].clip = sfxClips[(int)sfx];
            sfxPlayers[channelIndex].Play();
            break;
        }
    }

    public void PlayFootStep()
    {
        if (footStepPlayer.isPlaying) return;
        string currentScene = SceneManager.GetActiveScene().name;
        int index = 0;  // 0은 흙, 1은 잔디, 2는 돌 소리
        switch (currentScene)
        {
            case "Tree":
                index = 0;
                break;
            case "Base":
                index = 1;
                break;
            case "Rock":
            case "Boss":
                index = 2;
                break;
            default:
                index = 0;
                break;
        }
        footStepPlayer.clip = footStepClips[index];
        footStepPlayer.Play();
    }

    public void StopBGM()
    {
        if (bgmPlayer == null) return;
        bgmPlayer.Stop();
    }

    public void StopFootStep()
    {
        if (!footStepPlayer.isPlaying) return;
        footStepPlayer.Stop();
    }

    public void DisableSFX()
    {
        if(sfxPlayers == null) return;
        sfxPlayers[channelIndex].mute = true;
    }

    public void EnableSFX()
    {
        if(sfxPlayers == null) return;
        sfxPlayers[channelIndex].mute = false;
    }

    public void PlayDragonSFX(DragonSfx sfx)
    {
        //재생중이 아닌 채널을 찾아서 클립을 바꾸고 SFX를 재생함
        for (int i = 0; i < dragonSFXPlayer.Length; i++)
        {
            int loopIndex = (i + dragonChannel) % dragonSFXPlayer.Length;
            if (dragonSFXPlayer[loopIndex].isPlaying) continue;

            dragonChannel = loopIndex;
            dragonSFXPlayer[dragonChannel].clip = dragonSFX[(int)sfx]; 
            dragonSFXPlayer[dragonChannel].Play();
            break;
        }
    }

    public void StopDragonSFX(DragonSfx sfx)
    {
        dragonSFXPlayer[(int)sfx].Stop();
    }

    public void StopAllDragonSFX()
    {
        for (int i = 0; i < dragonSFXPlayer.Length; i++)
        {
            dragonSFXPlayer[i].Stop();
        }
    }

    public void PlayGuardianSFX(GuardianSfx sfx)
    {   
        //재생중이 아닌 채널을 찾아서 클립을 바꾸고 SFX를 재생함
        for (int i = 0; i < guardianSFXPlayer.Length; i++)
        {
            int loopIndex = (i + guardianChannel) % guardianSFXPlayer.Length;
            if (guardianSFXPlayer[loopIndex].isPlaying) continue;

            guardianChannel = loopIndex;
            guardianSFXPlayer[guardianChannel].clip = guardianSFX[(int)sfx];
            guardianSFXPlayer[guardianChannel].Play();
            break;
        }
    }

    public void StopGuardianSFX(GuardianSfx sfx)
    {
        guardianSFXPlayer[(int)sfx].Stop();
    }

    public void StopAllGuardianSFX()
    {
        for (int i = 0; i < guardianSFXPlayer.Length; i++)
        {
            guardianSFXPlayer[i].Stop();
        }
    }
}
