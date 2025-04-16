using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardianHit : MonoBehaviour
{
    private ParticleSystem vfx;
    void Start()
    {
        vfx = GetComponent<ParticleSystem>();
    }
    void OnEnable()
    {
        if(vfx != null)
        {
            vfx.Play();
            StartCoroutine(TransDisable());
        }
    }   
    void OnDisable()
    {   
        ObjectPooler.ReturnToPool(gameObject);
    }
    IEnumerator TransDisable()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);    
    }
}
