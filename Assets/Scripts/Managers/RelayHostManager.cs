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
            // Relay 서버에 공간 할당
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // 다른 플레이어가 접속할 Join Code 생성
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // UnityTransport 에 Relay 서버 정보 주입
            RelayServerData serverData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

            // Host 시작
            NetworkManager.Singleton.StartHost();
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Relay] Host 시작 실패: {e.Message}");
            throw;
        }
    }

    private async Task StartClientWithRelayAsync(string joinCode)
    {
        try
        {
            // Join Code 로 Allocation 참가
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // UnityTransport 에 Relay 서버 정보 주입
            RelayServerData serverData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

            // Client 시작
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Relay] Client 접속 실패: {e.Message}");
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
        if (string.IsNullOrEmpty(joinCode)) return;

        try
        {
            await StartClientWithRelayAsync(joinCode);
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            ServiceLocator.Get<IUserInfoManager>()?.SetClientId(clientId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayHostManager] Client 접속 오류: {e.Message}");
        }
    }

    public void Disconnect() => NetworkManager.Singleton.Shutdown();

    public ulong GetClientId() => NetworkManager.Singleton.LocalClientId;
    public void OnHostDisconnectedAddListener(Action callback) => _onHostDisconnected += callback;
    public void OnHostDisconnectedRemoveListener(Action callback) => _onHostDisconnected += callback;

    private void HostDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            _onHostDisconnected?.Invoke();
        }
    }
}