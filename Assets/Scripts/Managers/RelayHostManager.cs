using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;


public class RelayHostManager : Manager<RelayHostManager>, IRelayHostManager
{
    private Action _onHostDisconnected;
    private bool _isQuit;
    
    protected override async void Init() => await UnityServiceInitialize.Processing();
    protected override void Register()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += HostDisconnected;
        ServiceLocator.Register<IRelayHostManager>(this);
    }

    protected override void Unregister()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= HostDisconnected;
        ServiceLocator.Unregister<IRelayHostManager>();
    }

    private async Task<string> StartHostWithRelayAsync(int maxConnections = 7)
    {
        try
        {
            Debug.Log("[RelayHostManager] Relay 서버에 공간 할당"); 
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            Debug.Log("[RelayHostManager] 다른 플레이어가 접속할 Join Code 생성");
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("[RelayHostManager] UnityTransport 에 Relay 서버 정보 주입");
            RelayServerData serverData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

            Debug.Log("[RelayHostManager] Host 시작");
            NetworkManager.Singleton.StartHost();
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayHostManager] Host 시작 실패: {e.Message}");
            throw;
        }
    }

    private async Task StartClientWithRelayAsync(string joinCode)
    {
        try
        {
            Debug.Log("[RelayHostManager] Join Code 로 Allocation 참가");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            Debug.Log("[RelayHostManager] UnityTransport 에 Relay 서버 정보 주입");
            RelayServerData serverData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

            Debug.Log("[RelayHostManager] Client 시작");
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayHostManager] Client 접속 실패: {e.Message}");
            throw;
        }
    }

    public async Task<string> StartHost()
    {
        try
        {
            string joinCode = await StartHostWithRelayAsync();
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            ServiceLocator.Get<IUserInfoManager>()?.SetClientId(clientId);
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayHostManager] Host 시작 오류: {e.Message}");
            return default;
        }
    }

    public async void StartClient(string joinCode)
    {
        Debug.Log($"[RelayHostManager] Client ...  ");
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"[RelayHostManager] Client ... Join Code is Empty ");
            return;
        }

        try
        {
            Debug.Log($"[RelayHostManager] Client ... Try to connect ");
            await StartClientWithRelayAsync(joinCode);
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            ServiceLocator.Get<IUserInfoManager>()?.SetClientId(clientId);
            Debug.Log($"[RelayHostManager] Client ... Done ");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayHostManager] Client 접속 오류: {e.Message}");
            return;
        }
    }

    public void Disconnect()
    {
        _isQuit = true;
        NetworkManager.Singleton.Shutdown();
        Debug.Log($"[RelayHostManager] Disconnect ");
    }

    public ulong GetClientId() => NetworkManager.Singleton.LocalClientId;
    public void OnHostDisconnectedAddListener(Action callback) => _onHostDisconnected += callback;
    public void OnHostDisconnectedRemoveListener(Action callback) => _onHostDisconnected -= callback;

    private void HostDisconnected(ulong clientId)
    {
        Debug.Log($"[RelayHostManager] Somebody disconnected: c;{clientId} vs s;{NetworkManager.ServerClientId}");
        if (NetworkManager.Singleton.LocalClientId != NetworkManager.ServerClientId)
        {  // 내가 서버가 아닌 경우
            Debug.Log("[RelayHostManager] Somebody disconnected ... I am client.");
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {   // 서버가 터지면 clientID 는 나로 나옴.
                if (_isQuit)
                {
                    Debug.Log("[RelayHostManager] Somebody disconnected ... I quited.");
                    _isQuit = false;
                    return;
                }
                Debug.Log("[RelayHostManager] Somebody disconnected ... The server is boom.");
                _onHostDisconnected?.Invoke();
            }
        }
        else
        {
            Debug.Log("[RelayHostManager] Somebody disconnected ... I am server.");
            _isQuit = false;
        }
    }
}