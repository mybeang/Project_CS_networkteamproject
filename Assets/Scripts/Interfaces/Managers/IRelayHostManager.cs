using System;
using System.Threading.Tasks;

public interface IRelayHostManager
{
    // Relay
    public Task<string> StartHostWithRelayAsync(int maxConnections = 7);
    public Task StartClientWithRelayAsync(string joinCode);
    
    // 클라이언트 접속시 Callback 함수 추가 및 제거
    public void AddListenerToClientConnectedCallback(Action<ulong> listener);
    public void RemoveListenerFromClientConnectedCallback(Action<ulong> listener);
    
    // 클라이언트 접속 종료시 Callback 함수 추가 및 제거
    public void AddListenerToClientDisconnectedCallback(Action<ulong> listener);
    public void RemoveListenerFromClientDisconnectedCallback(Action<ulong> listener);
    
    // 서버 자체가 시작시 Callback 함수 추가 및 제거
    public void AddListenerToServerStartCallback(Action listener);
    public void RemoveListenerFromServerStartCallback(Action listener);
    
    public void StartHost();  // 서버 시작 (방 생성)
    public void StartClient(string joinCode);  // 클라이언트 시작 (방 참여)
    public void Disconnect();
}
