using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class testTankCreate : NetworkBehaviour
{
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private GameObject testPlayer;


    testTank tank;
    bool bbbb = false;
    ulong cid;
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        GameObject t = Instantiate(tankPrefab);
        NetworkObject netObj = t.GetComponent<NetworkObject>();

        netObj.Spawn();

        tank = t.GetComponent<testTank>();
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientID)
    {
        if (!IsServer) return;
        GameObject player = Instantiate(testPlayer);
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientID);


        Debug.Log($"클라이언트 접속: {clientID}");
        if(clientID == 1)
        {
            Debug.Log($"Init!! {cid}, {clientID}");

            tank.Init(cid, clientID);
        }
        else
        {
            Debug.Log($"Init x");

            cid = clientID;
        }
        bbbb = !bbbb;
    }



}
