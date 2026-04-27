using System.Collections.Generic;

public interface IDatabaseBackend
{
    public void SaveUserAsync(string userId);
    public void RemoveUserAsync(string userId);
    public bool ValidateDuplicateUserIdAsync(string userId);
}
