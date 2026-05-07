using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public static class UnityServiceInitialize
{
    private static Task _isInitializing;
    
    public static async Task Processing()
    {
        if (_isInitializing != null)
        {
            await _isInitializing;
            return;
        }

        _isInitializing = InternalInitializeAsync();
        await _isInitializing;
    }

    private static async Task InternalInitializeAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            string profile = $"{Guid.NewGuid().ToString().Substring(0, 8)}";;
            var options = new InitializationOptions();
            options.SetProfile(profile);
            await UnityServices.InitializeAsync(options);
        }
        
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}