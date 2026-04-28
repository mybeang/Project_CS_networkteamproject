using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class testTankCreate : NetworkBehaviour
{
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private GameObject testPlayer;


    testTank tank;
    ulong cid;
    public override void OnNetworkSpawn()
    {
        //if (!IsServer) return;
        //GameObject t = Instantiate(tankPrefab);
        //NetworkObject netObj = t.GetComponent<NetworkObject>();
        //
        //netObj.Spawn();
        //
        //tank = t.GetComponent<testTank>();
    }

    private void Start()
    {
        Debug.Log("Start!");

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientID)
    {
        if (!IsServer) return;

        GameObject player = Instantiate(testPlayer);
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientID);
        Debug.Log($"플레이어 생성 완료: {clientID}");

        //일단 테스트코드로 2명까지만 가능하게 작성
        Debug.Log($"클라이언트 접속: {clientID}");
        if(clientID == 1)
        {
            Debug.Log($"탱크생성! {cid}, {clientID}");

            GameObject t = Instantiate(tankPrefab);
            NetworkObject netT = t.GetComponent<NetworkObject>();

            netT.Spawn();

            tank = t.GetComponent<testTank>();


            tank.Init(cid, clientID);
        }
        else
        {
            Debug.Log($"Init x");

            cid = clientID;
        }
    }



}
