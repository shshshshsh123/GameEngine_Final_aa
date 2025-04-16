using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HUD_Spider : MonoBehaviour
{
    void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);
    }
    void Start()
    {
        SceneLoader.OnSceneLoaded += Initialize;
        Initialize();
    }
    void OnDestroy()
    {
        SceneLoader.OnSceneLoaded -= Initialize;
    }   
    void Initialize()
    {
        gameObject.SetActive(false);
    }
}
