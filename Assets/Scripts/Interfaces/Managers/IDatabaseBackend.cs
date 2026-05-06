using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDatabaseBackend
{
    public void SaveUserAsync(string userId);
    public void RemoveUserAsync(string userId);
    public Task<bool> ValidateDuplicateUserIdAsync(string userId);
    public void RegisterDisconnectHandler(string userId);
    public void SetJoinCodeAsync(string roomId, string joinCode);
    public Task<string> GetJoinCodeAsync(string roomId);
    public void RemoveJoinCodeAsync(string roomId);
}
