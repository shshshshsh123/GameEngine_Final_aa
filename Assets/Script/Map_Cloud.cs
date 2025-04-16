using UnityEngine;

public class Map_Cloud : MonoBehaviour
{
    public Material mat;
    private void Start()
    {
        Renderer tmpRenderer = gameObject.GetComponent<Renderer>();
        if(tmpRenderer != null){ mat = tmpRenderer.material; }
    }
    private void Update()
    {
        mat.SetFloat("_UnscaledTime", Time.unscaledTime);
    }
}
