using System;
using System.Threading.Tasks;

public interface IRelayHostManager
{
    public Task<string> StartHost();  // 서버 시작 (방 생성)
    public void StartClient(string joinCode);  // 클라이언트 시작 (방 참여)
    public void Disconnect();
    public ulong GetClientId();
    public void OnHostDisconnectedAddListener(Action callback);
    public void OnHostDisconnectedRemoveListener(Action callback);
}
