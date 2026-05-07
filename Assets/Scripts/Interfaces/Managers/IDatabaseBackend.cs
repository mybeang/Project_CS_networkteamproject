using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDatabaseBackend
{
    public void SaveUserAsync(string userId);
    public void RemoveUserAsync(string userId);
    public Task<bool> ValidateDuplicateUserIdAsync(string userId);
    public void RegisterDisconnectHandler(string userId);
}
