using Unity.Netcode;
using UnityEngine;

public class testTankCreate : NetworkBehaviour
{
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private GameObject testPlayer;


    TankController tank;
    ulong cid;

    ulong testInt = 0;

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

        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        //var how = ServiceLocator.Get<IUserInfoManager>();
        //var userInfo = how.GetUserInfo();
        //var v = userInfo.userId;
        //Debug.Log($"asdfasdf asdf {v}");

        if (!IsServer) return;

        GameObject t = Instantiate(tankPrefab);
        t.SetActive(true);
        NetworkObject netT = t.GetComponent<NetworkObject>();
        netT.SpawnAsPlayerObject(0);

        GameObject player = Instantiate(testPlayer);
        player.SetActive(true);
        player.name = "[Driver]";
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(0);
        player.GetComponent<RemoteController>().SetControllerData(true, t.GetComponent<teststCode>());
        Debug.Log($"플레이어 생성 완료: {0}");

        player = Instantiate(testPlayer);
        player.SetActive(true);
        player.name = "[Gunner]";
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(1);
        player.GetComponent<RemoteController>().SetControllerData(false, t.GetComponent<teststCode>());
        Debug.Log($"플레이어 생성 완료: {1}");

        Debug.Log($"탱크생성! {0}, {1}");

        
        //tank = t.GetComponent<TankController>();


        //tank.Init(0, 1);



    }

    //private void OnClientConnected(ulong clientID)
    //{   
    //    if (!IsServer) return;
    //
    //    GameObject player = Instantiate(testPlayer);
    //    NetworkObject netObj = player.GetComponent<NetworkObject>();
    //    netObj.SpawnAsPlayerObject(clientID);
    //    Debug.Log($"플레이어 생성 완료: {clientID}");
    //
    //    //일단 테스트코드로 2명까지만 가능하게 작성
    //    Debug.Log($"클라이언트 접속: {clientID}");
    //    if(clientID == 1)
    //    {
    //        Debug.Log($"탱크생성! {cid}, {clientID}");
    //
    //        GameObject t = Instantiate(tankPrefab);
    //        NetworkObject netT = t.GetComponent<NetworkObject>();
    //
    //        netT.Spawn();
    //
    //        tank = t.GetComponent<testTank>();
    //
    //
    //        tank.Init(cid, clientID);
    //    }
    //    else
    //    {
    //        Debug.Log($"Init x");
    //
    //        cid = clientID;
    //    }
    //}




}
