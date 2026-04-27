using Unity.Netcode;
using UnityEngine;

public class testTankCreate : NetworkBehaviour
{
    [SerializeField] private GameObject tankPrefab;

    public override void OnNetworkSpawn()
    {
        GameObject t = Instantiate(tankPrefab);
        NetworkObject netObj = t.GetComponent<NetworkObject>();
        Debug.Log("test1");

        netObj.Spawn();
        Debug.Log("test2");

        testTank tank = t.GetComponent<testTank>();
        tank.Init(OwnerClientId, 1234);
        Debug.Log($"test3 : {OwnerClientId}");
    }

    
}
