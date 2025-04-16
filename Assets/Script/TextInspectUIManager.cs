using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Explain
{
    public string name;
    public string tag;
    public string ex;

}
public class TextInspectUIManager : MonoBehaviour
{
    [Header("Object Name UI")]
    [SerializeField] private Text objectNameText;
    [SerializeField] private GameObject objectNameBG;

    [Header("Object Details Settings")]
    [SerializeField] private Text objectDetailsText;
    [SerializeField] private GameObject objectDetailsBG;
    
    private List<Dictionary<string, object>> explainData = new List<Dictionary<string, object>>();
    public List<Explain> explain = new List<Explain>();

    public bool isEnable = false; //ui가 활성화 됐는지 

    private Image NameBG;
    private Image DetailBG;

    private Color oriColor;
    private Color emptyColor;
    private Color nameColor;
    private Color emptyNameColor;
    private Color textColor;
    private Color emptyTextColor;

    private static TextInspectUIManager _instance;
    private static readonly object _lock = new object();

    public static TextInspectUIManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TextInspectUIManager>();

                    if (_instance == null)
                    {
                        var singleton = new GameObject("TextInspectUIManager");
                        _instance = singleton.AddComponent<TextInspectUIManager>();
                    }

                    _instance.Init();
                }

                return _instance;
            }
        }
    }

    private void Init()
    {
        if (!gameObject.name.Contains("TextInspectUIManager"))
        {
            gameObject.name = "TextInspectUIManager";
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Init();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Start()
    {
        InitExplain();
        
        NameBG = objectNameBG.GetComponent<Image>();
        DetailBG = objectDetailsBG.GetComponent<Image>();

        oriColor = DetailBG.color;
        emptyColor = oriColor;
        emptyColor.a = 0f;

        nameColor = NameBG.color;
        emptyNameColor = nameColor;
        emptyNameColor.a = 0f;

        textColor = objectDetailsText.color;
        emptyTextColor = textColor;
        emptyTextColor.a = 0f;
        
        objectNameBG.SetActive(false);
        objectDetailsBG.SetActive(false);
    }
    public IEnumerator FadeUI(float speed)
    {
        //시작시 색상, bool, 활성화상태 설정
        objectNameBG.SetActive(true);
        objectDetailsBG.SetActive(true);
        
        isEnable = true;

        float elapsedTime = 0f;

        DetailBG.color = emptyColor;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.unscaledDeltaTime * speed;
            NameBG.color = Color.Lerp(emptyNameColor, nameColor, elapsedTime * 1.1f);
            objectNameText.color = Color.Lerp(emptyTextColor, textColor, elapsedTime * 1.1f);
            DetailBG.color = Color.Lerp(emptyColor, oriColor, elapsedTime);
            objectDetailsText.color = Color.Lerp(emptyTextColor, textColor, elapsedTime);
            yield return null;
        }
        DetailBG.color = oriColor;
        NameBG.color = nameColor;
        objectDetailsText.color = textColor;
        objectNameText.color = textColor;

        yield return new WaitUntil(() => Input.GetMouseButtonDown(1));
        
        elapsedTime = 0f;

        DetailBG.color = oriColor;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.unscaledDeltaTime * speed;
            NameBG.color = Color.Lerp(nameColor, emptyNameColor, elapsedTime * 1.1f);
            objectNameText.color = Color.Lerp(textColor, emptyTextColor, elapsedTime * 1.1f);
            DetailBG.color = Color.Lerp(oriColor, emptyColor, elapsedTime);
            objectDetailsText.color = Color.Lerp(textColor, emptyTextColor, elapsedTime);
            yield return null;
        }
        //종료시 색상, bool, 활성화상태 설정
        DetailBG.color = emptyColor;
        NameBG.color = emptyNameColor;
        objectDetailsText.color = emptyTextColor;
        objectNameText.color = emptyTextColor;
        
        isEnable = false;

        objectNameBG.SetActive(false);
        objectDetailsBG.SetActive(false);
    }
    public void ShowObjectDetails(string objectName, string newInfo)
    {
        objectNameText.text = objectName;
        objectDetailsText.text = newInfo;
        
        StartCoroutine(FadeUI(1.5f));
    }

    void InitExplain()
    {
        foreach (var stringData in GameManager.Instance.ExplainData)
        {
            explainData.Add(stringData);
        }
        
        for (int i = 0; i < explainData.Count; i++)
        {
            Explain newItem = new Explain();
            newItem.name = explainData[i]["name"].ToString();
            newItem.tag = explainData[i]["tag"].ToString();
            newItem.ex = explainData[i]["explain"].ToString();
            explain.Add(newItem);
        }
    }

    public void EnableBG()
    {
        StopAllCoroutines();
        NameBG.color = emptyColor;
        DetailBG.color = emptyColor;

        objectNameText.color = emptyColor;
        objectDetailsText.color = emptyColor;

        isEnable = false;
    }
}