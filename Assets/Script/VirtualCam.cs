using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VirtualCam : MonoBehaviour
{
    public Transform player;
    public float moveSpeed;
    public Transform startPos;
    public Transform middlePos;
    public Transform endPos;   // Start is called before the first frame update
    public AnimationCurve curve;
    void Start()
    {
        StartCoroutine(StartMove());
    }

    IEnumerator StartMove()
    {
        while(true)
        {
            float t = 0f;
            while(t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Slerp(startPos.position, middlePos.position, curve.Evaluate(t));
                yield return null;
            }
            t = 0f;
            while(t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Slerp(middlePos.position, endPos.position, curve.Evaluate(t));
                yield return null;
            }
            t = 0f;
            while(t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Slerp(endPos.position, middlePos.position, curve.Evaluate(t));
                yield return null;
            }
            t = 0f;
            while(t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Slerp(middlePos.position, startPos.position, curve.Evaluate(t));
                yield return null;
            }
        }   
    }
}
