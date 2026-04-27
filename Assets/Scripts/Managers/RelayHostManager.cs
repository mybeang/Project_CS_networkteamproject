using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;


public class RelayHostManager : Manager, IRelayHostManager
{
    protected override void Register() => ServiceLocator.Register<IRelayHostManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IRelayHostManager>();

    public async Task<string> StartHostWithRelayAsync(int maxConnections = 7)
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

    public async Task StartClientWithRelayAsync(string joinCode)
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

    public void AddListenerToClientConnectedCallback(Action<ulong> listener)
        => NetworkManager.Singleton.OnClientConnectedCallback += listener;

    public void RemoveListenerFromClientConnectedCallback(Action<ulong> listener)
        => NetworkManager.Singleton.OnClientConnectedCallback -= listener;

    public void AddListenerToClientDisconnectedCallback(Action<ulong> listener)
        => NetworkManager.Singleton.OnClientDisconnectCallback += listener;

    public void RemoveListenerFromClientDisconnectedCallback(Action<ulong> listener)
        => NetworkManager.Singleton.OnClientDisconnectCallback -= listener;

    public void AddListenerToServerStartCallback(Action listener)
        => NetworkManager.Singleton.OnServerStarted += listener;

    public void RemoveListenerFromServerStartCallback(Action listener)
        => NetworkManager.Singleton.OnServerStarted -= listener;

    public async void StartHost()
    {
        try
        {
            string joinCode = await StartHostWithRelayAsync();
            // ToDo. Unity Lobby 관련 코드 추가시 JoinCode 처리 추가할 것.
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayHostManager] Host 시작 오류: {e.Message}");
        }
    }

    public async void StartClient(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode)) return;

        try
        {
            await StartClientWithRelayAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayHostManager] Client 접속 오류: {e.Message}");
        }
    }

    public void Disconnect() => NetworkManager.Singleton.Shutdown();
}