using System.Collections;
using UnityEngine;

public class MeteorHit : MonoBehaviour
{
    void OnDisable()
    {   
        ObjectPooler.ReturnToPool(gameObject);
    }

    void OnEnable()
    {
        StartCoroutine(TransDisable());
    }

    IEnumerator TransDisable()
    {
        yield return new WaitForSeconds(2f);
        if (gameObject.activeSelf) { gameObject.SetActive(false); }
    }
}
