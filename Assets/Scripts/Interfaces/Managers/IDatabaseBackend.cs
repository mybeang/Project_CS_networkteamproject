using System;
using System.Threading.Tasks;
using Firebase.Database;

public interface IDatabaseBackend
{
    // User
    public void SaveUserAsync(string userId);
    public void RemoveUserAsync(string userId);
    public Task<bool> ValidateDuplicateUserIdAsync(string userId);
    public void RegisterUserDisconnectHandler(string userId);
    // JoinCode
    public void SetJoinCodeAsync(string roomId, string joinCode);
    public Task<string> GetJoinCodeAsync(string roomId);
    public void RemoveJoinCodeAsync(string roomId);
    // MapNumber
    public void SetMapNumberAsync(string roomId, int mapNumber);
    public Task<string> GetMapNumberAsync(string roomId);
    public void RemoveMapNumberAsync(string roomId);
    public void RegisterRemoveRoomHandler(string roomId);
    public void RegisterMapNumberValueChangedHandler(string roomId, EventHandler<ValueChangedEventArgs> callback);
    public void UnregisterMapNumberValueChangedHandler(string roomId, EventHandler<ValueChangedEventArgs> callback);
}
