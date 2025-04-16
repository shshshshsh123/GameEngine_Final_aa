using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnBoss : MonoBehaviour
{
    public Transform SpawnPos;
    GameObject boss = null;
    // Start is called before the first frame update
    void Start()
    {
        boss = ObjectPooler.SpawnFromPool("Bulkaness", SpawnPos.position, Quaternion.identity);
    }
    void OnDestroy()
    {
        if(boss != null) //게임 종료시에는 null체크 안하면 오류 생겨서 추가
        {
            boss.SetActive(false);
        }
    }
}
