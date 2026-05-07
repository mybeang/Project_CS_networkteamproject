using System.Collections.Generic;
using System.Threading.Tasks;

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
    public void RegisterRemoveRoomHandler(string roomId);
}
