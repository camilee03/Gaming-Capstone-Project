using UnityEngine;
using UnityEngine.Pool;

public class TileSpawner : MonoBehaviour
{

    public GameObject plainTile;

    ObjectPool<GameObject> upPool;
    ObjectPool<GameObject> leftPool;
    ObjectPool<GameObject> rightPool;
    ObjectPool<GameObject> downPool;
    public ObjectPool<GameObject>[] pools;

    MainRoomPG mainRoom;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        upPool = new ObjectPool<GameObject>(CreatePool, GetPool, ReleasePool, DestroyPool, true, 5, 20);
        downPool = new ObjectPool<GameObject>(CreatePool, GetPool, ReleasePool, DestroyPool, true, 5, 20);
        leftPool = new ObjectPool<GameObject>(CreatePool, GetPool, ReleasePool, DestroyPool, true, 5, 20);
        rightPool = new ObjectPool<GameObject>(CreatePool, GetPool, ReleasePool, DestroyPool, true, 5, 20);

        pools = new ObjectPool<GameObject>[4] { upPool, downPool, rightPool, leftPool };

        mainRoom = GetComponent<MainRoomPG>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GetPool(GameObject tile)
    {
        tile.gameObject.SetActive(true);
    }

    void ReleasePool(GameObject tile)
    {
        tile.gameObject.SetActive(false);
    }
    GameObject CreatePool()
    {
        GameObject tile = Instantiate(plainTile, mainRoom.position, mainRoom.rotation, mainRoom.parent.transform);

        return Instantiate(tile, mainRoom.parent.transform);
    }

    void DestroyPool(GameObject tile)
    {
        Destroy(tile);
    }
}
