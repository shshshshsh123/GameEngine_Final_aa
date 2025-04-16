using System.Collections.Generic;
using UnityEngine;

public class CreateMap : MonoBehaviour
{
    const int plantSize = 2;
    const int monSize = 3; //spider, scout 추가시 3으로 변경
    public int[] SpawnSize = new int[plantSize];
    public string[] objTag = new string[plantSize];
    public int[] monSpawnSize = new int[monSize];
    public string[] monObjTag = new string[monSize];
    [SerializeField] private AnimationCurve distributionCurve;
    public Transform PlayerSpawn;
    [Header("Spawn Dis")]
    public float monSpawnDis;
    public float PlayerSpawnDis;
    public float NoSpawnDis;
    public float minDistance; //스폰간의 거리
    public float spawnRange; //스폰 크기 (Grond scale의 4배가 적당함
    Vector3[][] SpawnPos;
    Vector3[][] monSpawnPos;

    //생성된 오브젝트 저장
    List<GameObject> plantList = new List<GameObject>();    
    List<GameObject> monList = new List<GameObject>();

    void Start()
    {
        SpawnPos = new Vector3[plantSize][];
        monSpawnPos = new Vector3[monSize][];
        
        InitSpawnPos();

        CalPlantSpawnPos();
        CalMonSpawnPos();

        SpawnObject();
    }
    void OnDestroy()
    {
        DeactivateObjects(); //씬 변경시 생성된 오브젝트 모두 비활성화
    }
    
    private void DeactivateObjects()
    {
        for (int i = 0; i < plantList.Count; i++)
        {
            if(plantList[i] != null)
            {
                plantList[i].SetActive(false);
            }
        }

        for (int i = 0; i < monList.Count; i++)
        {
            if(monList[i] != null)
            {
                monList[i].SetActive(false);
            }
        }
    }
    void CalPlantSpawnPos()
    {
        for (int index = 0; index < plantSize; index++)
        {
            int CalSpawnSize = SpawnSize[index];
            int SpawnIndex = 0;
            int attempts = 0;
            int maxAttempts = 0;
            maxAttempts += CalSpawnSize * 10;

            while (SpawnIndex < CalSpawnSize && attempts < maxAttempts)
            {
                // 분포도를 반영한 랜덤 위치 생성
                Vector3 randomPosition = GeneratePlantPos();

                // 최소 거리 검증
                bool isFarEnough = true;

                if (Vector3.Distance(randomPosition, Vector3.zero) < NoSpawnDis) { isFarEnough = false; }//상자 스폰 근처에 생성x
                if (Vector3.Distance(randomPosition, PlayerSpawn.position) < PlayerSpawnDis) { isFarEnough = false; } //플레이어 스폰 근처에 생성x
                else
                {
                    for (int i = 0; i <= index; i++)
                    {
                        for (int j = 0; j < SpawnSize[index]; j++) //다른 plant오브젝트의 위치와도 비교하기 위해 2중 for문을 사용함
                        {
                            Vector3 pos = SpawnPos[i][j];
                            if (pos != Vector3.zero && Vector3.Distance(randomPosition, pos) < minDistance) //0,0,0 좌표는 앞에서 계산하므로 
                            {
                                isFarEnough = false;
                                break;
                            }
                        }
                        if (!isFarEnough) break;
                    }
                }

                if (isFarEnough)
                {
                    // 조건을 만족하면 오브젝트 위치 저장
                    SpawnPos[index][SpawnIndex] = randomPosition;
                    SpawnIndex++;
                }

                attempts++;
            }

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("Maximum attempts reached. Some objects may not have been spawned.");
            }
        }
    }

    void CalMonSpawnPos(){
        for (int index = 0; index < monSize; index++)
        {
            int CalSpawnSize = monSpawnSize[index];
            int SpawnIndex = 0;
            int attempts = 0;
            int maxAttempts = 0;
            maxAttempts += CalSpawnSize * 10;

            while (SpawnIndex < CalSpawnSize && attempts < maxAttempts)
            {
                // 분포도를 반영한 랜덤 위치 생성
                Vector3 randomPosition = GeneratePlantPos();

                // 최소 거리 검증
                bool isFarEnough = true;

                if (Vector3.Distance(randomPosition, Vector3.zero) < NoSpawnDis) { isFarEnough = false; }//상자 스폰 근처에 생성x
                if (Vector3.Distance(randomPosition, PlayerSpawn.position) < PlayerSpawnDis) { isFarEnough = false; } //플레이어 스폰 근처에 생성x
                else
                {
                    for (int i = 0; i < index; i++) 
                    {
                        for(int j = 0; j < SpawnSize[i]; j++)//plant와의 거리를 비교
                        {
                            Vector3 PlantPos = SpawnPos[i][j];
                            if(PlantPos != Vector3.zero && Vector3.Distance(randomPosition, PlantPos) < monSpawnDis) //plant와의 거리가 너무 가까우면 생성x
                            {
                                isFarEnough = false;
                                break;
                            }
                        }   
                        if (!isFarEnough) break;

                        for (int j = 0; j < monSpawnSize[index]; j++) //mon과의 거리를 비교
                        {
                            Vector3 pos = monSpawnPos[i][j];
                            
                            if (pos != Vector3.zero && Vector3.Distance(randomPosition, pos) < monSpawnDis) //mon과의 거리가 너무 가까우면 생성x
                            {
                                isFarEnough = false;
                                break;
                            }
                        }
                        if (!isFarEnough) break;
                    }
                }

                if (isFarEnough)
                {
                    // 조건을 만족하면 오브젝트 위치 저장
                    monSpawnPos[index][SpawnIndex] = randomPosition;
                    SpawnIndex++;
                }

                attempts++;
            }
        }
    }
    Vector3 GeneratePlantPos()
    {
        //반지름 230의 영역에 그래프에따른 분포도의 위치를 반환함
        float x = Mathf.Lerp(-spawnRange, spawnRange, distributionCurve.Evaluate(UnityEngine.Random.value));
        float z = Mathf.Lerp(-spawnRange, spawnRange, distributionCurve.Evaluate(UnityEngine.Random.value));
        return new Vector3(x, 0f, z);
    }
    void InitSpawnPos()
    {
        for (int i = 0; i < plantSize; i++)
        {
            SpawnPos[i] = new Vector3[SpawnSize[i]];
            for (int j = 0; j < SpawnSize[i]; j++)
            {
                SpawnPos[i][j] = Vector3.zero;
            }
        }
        for (int i = 0; i < monSize; i++)
        {
            monSpawnPos[i] = new Vector3[monSpawnSize[i]];
            for (int j = 0; j < monSpawnSize[i]; j++)
            {
                monSpawnPos[i][j] = Vector3.zero;
            }
        }
    }

    private void SpawnObject()
    {
        for (int i = 0; i < plantSize; i++)
        {
            for (int j = 0; j < SpawnSize[i]; j++)
            {
                GameObject obj = ObjectPooler.SpawnFromPool(objTag[i], SpawnPos[i][j], Quaternion.identity);
                plantList.Add(obj);
            }
        }
        
        for (int i = 0; i < monSize; i++)
        {
            for (int j = 0; j < monSpawnSize[i]; j++)
            {
                GameObject obj = ObjectPooler.SpawnFromPool(monObjTag[i], monSpawnPos[i][j], Quaternion.identity);
                monList.Add(obj);
            }
        }
    }
}
