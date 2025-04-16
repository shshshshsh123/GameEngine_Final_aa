using System.Collections.Generic;
using UnityEngine;

public class MaterialItems : MonoBehaviour
{
    private int BaseCount; //씬 재로딩시 초기화할 초기 갯수
    public int count;  // n회 타격시 오브젝트 파괴
    public int quantity;    // 1회 타격시 마다 획득수량
    public int exp;  // 획득경험치량

    public bool isCheck = false;
    public float openT = 0.0f;

    private Dictionary<string, object> getExp;

    public enum ItemType
    {
        Wood = 0,
        Grass = 1,
        Rock = 2,
        Coal = 3
    };

    public ItemType type;
    Item CurrentMat = new Item();
    void Start()
    {
        foreach (var expAmount in GameManager.Instance.getExp)
        {
            if (expAmount["tag"].ToString() == type.ToString())
            {
                getExp = expAmount;
                break;
            }
        }

        isCheck = false;
        switch (type)
        {
            case ItemType.Wood:
                CurrentMat = ItemManager.Instance.mat.MatData[0];
                count = 3;
                quantity = 3;
                break;
            case ItemType.Grass:
                CurrentMat = ItemManager.Instance.mat.MatData[1];
                count = 1;
                quantity = 2;
                break;
            case ItemType.Rock:
                CurrentMat = ItemManager.Instance.mat.MatData[2];
                count = 5;
                quantity = 3;
                break;
            case ItemType.Coal:
                CurrentMat = ItemManager.Instance.mat.MatData[3];
                count = 4;
                quantity = 2;
                break;
        }
        BaseCount = count;
        exp = int.Parse(getExp["exp"].ToString());
    }

    void Update()
    {
        if (type == ItemType.Grass) AddPlant();
    }

    public void AddItem()
    {
        count--;
        //자원 오브젝트를 끝까지 캐면 아이템을 quantity만큼 추가함
        if (count <= 0)
        {
            InventoryManager.Instance.AddItem(CurrentMat, quantity);
            UIManager.Instance.TransActiveInven();
            ExpManager.Instance.ManageLevel(exp);
            gameObject.SetActive(false);
        }
    }

    void AddPlant()
    {
        if (isCheck && Player.Instance.actionAxis)
        {
            openT += Time.deltaTime;
            if (openT > 3.0f)
            {
                openT = 0.0f;
                AddItem();
            }
        }
        else
        {
            if (openT > 0f) openT = 0.0f;   // action키 때면
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player")) isCheck = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player")) isCheck = false;
    }

    private void OnDisable()
    {
        count = BaseCount; //씬 재로딩시 갯수 초기화
        ObjectPooler.ReturnToPool(gameObject);
    }
}
