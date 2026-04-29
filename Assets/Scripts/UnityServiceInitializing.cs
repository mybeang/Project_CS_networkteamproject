using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public static class UnityServiceInitialize
{
    public static async Task Processing()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
            await UnityServices.InitializeAsync();
        
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}