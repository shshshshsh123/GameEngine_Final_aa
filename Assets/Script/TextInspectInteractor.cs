using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TextInspectInteractor : MonoBehaviour
{
    private TextInspectItem textItem;

    EventSystem eventSystem;
    GraphicRaycaster graphicRaycaster;
    void Initialize()
    {
        if(SceneManager.GetActiveScene().name == "LoadingScene") return;
        eventSystem = GameObject.FindWithTag("Event").GetComponent<EventSystem>();  
    }
    private void Start()
    {
        graphicRaycaster = UIManager.Instance.MainUI.GetComponent<GraphicRaycaster>();
        SceneLoader.OnSceneLoaded += Initialize; 
        Initialize();
        
        eventSystem = GameObject.FindWithTag("Event").GetComponent<EventSystem>();  
    }
    private void Update()
    {
        if(SceneManager.GetActiveScene().name != "LoadingScene")
        {
            if (UIManager.Instance.ProductUI.activeSelf)
            {
                StartRay();
            }
        }
    }
    void Destroy()
    {
        SceneLoader.OnSceneLoaded -= Initialize;
    }

    void StartRay()
    {
        if (Input.GetMouseButtonDown(1) && !TextInspectUIManager.Instance.isEnable) //우클릭 and 현재 ui가 안열렸으면
        {
            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = Input.mousePosition
            };
            
            // Raycast 결과를 저장할 리스트
            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerData, results);

            if (results.Count > 0) //현재 eventsystem에 작동중인 오브젝트가 있을때만 실행
            {
                var readableItem = results[0].gameObject.GetComponent<TextInspectItem>();
                if (readableItem != null)
                {
                    textItem = readableItem;
                    textItem.ShowDetails();
                }
                else
                {
                    ClearText();
                }
            }
        }
    }
    void ClearText()
    {
        if (textItem != null)
        {
            textItem = null;
        }
    }
}