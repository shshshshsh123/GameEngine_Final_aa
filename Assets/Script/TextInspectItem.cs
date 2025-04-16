using UnityEngine;

public class TextInspectItem : MonoBehaviour
{
    [Header("Show Information Selection")]
    [SerializeField] private bool showObjectName;
    [SerializeField] private bool showObjectDetails;

    [Header("Text Parameters")]
    private string objectName = "Generic Object";

    private string objectDetails = "This is a description, please fill in the inspector";

    private void Start()
    {
        InitItem();
    }
    public void InitItem()
    {
        Explain tmpEx = new Explain();

        tmpEx = TextInspectUIManager.Instance.explain.Find(x => x.tag.Equals(gameObject.name));

        objectName = tmpEx.name;
        objectDetails = tmpEx.ex;
    }

    public void ShowDetails()
    {
        if (showObjectDetails)
        {
            TextInspectUIManager.Instance.ShowObjectDetails(objectName, objectDetails);
        }
    }
}