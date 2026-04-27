using System.Collections.Generic;

public interface IDatabaseBackend
{
    public void SaveUser(string username);
    public void RemoveUser(string username);
    public bool ValidateDuplicateUserId(string username);
}
