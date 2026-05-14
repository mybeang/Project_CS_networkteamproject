using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;


public class ChildKey
{
    public const string USERS = "users";
    public const string ROOMS = "rooms";
    public const string JOINCODES = "joinCodes";
    public const string MAPNUM = "mapNumber";
}


public class DatabaseBackend : Manager<DatabaseBackend>, IDatabaseBackend
{
    private FirebaseApp _app;
    private FirebaseDatabase _db;
    
    protected override void Init()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => 
        {
            if (task.Result == DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                _app = FirebaseApp.DefaultInstance;
                _db = FirebaseDatabase.DefaultInstance;
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
                // Set a flag here to indicate whether Firebase is ready to use by your app.
                Debug.Log("Firebase dependencies check success");
            }
            else
            {
                Debug.LogWarning($"Could not resolve all Firebase dependencies: {task.Result}");
                // Firebase Unity SDK is not safe to use here.
                _app = null;
                _db = null;
            }
        });
    }
    
    protected override void Register() => ServiceLocator.Register<IDatabaseBackend>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IDatabaseBackend>(this);
    
    public void SaveUserAsync(string userId)
    {
        _db.RootReference.Child($"{ChildKey.USERS}/{userId}").SetValueAsync(true).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] User data saved successfully: {userId}");
            else if (task.IsCanceled) Debug.LogError("[DB] SaveUserAsync was canceled.");
            else Debug.LogError("[DB] SaveUserAsync encountered an error: " + task.Exception);
        });
    }

    public void RemoveUserAsync(string userId)
    {
        _db.RootReference.Child($"{ChildKey.USERS}/{userId}").RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] User data removed successfully: {userId}");
            else if (task.IsCanceled) Debug.LogError("[DB] RemoveUserAsync was canceled.");
            else Debug.LogError("[DB] RemoveUserAsync encountered an error: " + task.Exception);
        });
    }

    public async Task<bool> ValidateDuplicateUserIdAsync(string userId)
    {   // 중복이면 true !!
        try
        {
            DataSnapshot dataSnapshot = await _db.RootReference.Child($"{ChildKey.USERS}/{userId}").GetValueAsync();
            if (dataSnapshot.Exists)
            {
                Debug.Log($"[DB] {userId} was duplicated.");
                return true;
            }
            return false;
        }
        catch (FirebaseException e)
        {
            Debug.LogError("[DB] Could not validate duplicated user.");
            return false;
        }
    }
    
    
    
    public void RegisterUserDisconnectHandler(string userId)
    {
        _db.RootReference.Child($"{ChildKey.USERS}/{userId}").OnDisconnect().RemoveValue().ContinueWithOnMainThread(task => {
            if (task.IsCompleted) {
                Debug.Log($"[DB] Disconnect handler registered for: {userId}");
            }
        });
    }

    public void RegisterRemoveRoomHandler(string roomId)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}"; 
        _db.RootReference.Child(path).OnDisconnect().RemoveValue().ContinueWithOnMainThread(task => {
            if (task.IsCompleted) {
                Debug.Log($"[DB] Disconnect handler registered for: {roomId}");
            }
        });
    }

    public void SetJoinCodeAsync(string roomId, string joinCode)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.JOINCODES}";
        _db.RootReference.Child(path).SetValueAsync(joinCode).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] JoinCode data saved successfully: {joinCode}");
            else if (task.IsCanceled) Debug.LogError("[DB] SetJoinCodeAsync was canceled.");
            else Debug.LogError("[DB] SetJoinCodeAsync encountered an error: " + task.Exception);
        });
    }

    public async Task<string> GetJoinCodeAsync(string roomId)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.JOINCODES}";
        DataSnapshot dataSnapshot = await _db.RootReference.Child(path).GetValueAsync();
        if (dataSnapshot.Exists)
        {
            Debug.Log($"[DB] GetJoinCodeAsync successful: {dataSnapshot.Value} ");
            return dataSnapshot.Value as string;
        }
        return null;
    }

    public void RemoveJoinCodeAsync(string roomId)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.JOINCODES}";
        _db.RootReference.Child(path).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] JoinCode data removed successfully: {roomId}");
            else if (task.IsCanceled) Debug.LogError("[DB] RemoveJoinCodeAsync was canceled.");
            else Debug.LogError("[DB] RemoveJoinCodeAsync encountered an error: " + task.Exception);
        });
    }

    public void SetMapNumberAsync(string roomId, int mapNumber)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.MAPNUM}";
        _db.RootReference.Child(path).SetValueAsync(mapNumber).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] MapNumber data saved successfully: {mapNumber}");
            else if (task.IsCanceled) Debug.LogError("[DB] SetMapNumberAsync was canceled.");
            else Debug.LogError("[DB] SetMapNumberAsync encountered an error: " + task.Exception);
        });
    }
    
    public void UpdateMapNumberAsync(string roomId, int mapNumber)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.MAPNUM}";
        _db.RootReference.Child(path).SetValueAsync(mapNumber).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] MapNumber data saved successfully: {mapNumber}");
            else if (task.IsCanceled) Debug.LogError("[DB] SetMapNumberAsync was canceled.");
            else Debug.LogError("[DB] SetMapNumberAsync encountered an error: " + task.Exception);
        });
    }

    public async Task<string> GetMapNumberAsync(string roomId)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.MAPNUM}";
        DataSnapshot dataSnapshot = await _db.RootReference.Child(path).GetValueAsync();
        if (dataSnapshot.Exists)
        {
            Debug.Log($"[DB] GetMapNumberAsync successful: {dataSnapshot.Value} ");
            return dataSnapshot.Value as string;
        }
        return null;  // fail number
    }
    
    public void RemoveMapNumberAsync(string roomId)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.MAPNUM}";
        _db.RootReference.Child(path).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] MapNumber data removed successfully: {roomId}");
            else if (task.IsCanceled) Debug.LogError("[DB] MapNumber was canceled.");
            else Debug.LogError("[DB] MapNumber encountered an error: " + task.Exception);
        });
    }
    
    public void RegisterMapNumberValueChangedHandler(string roomId, EventHandler<ValueChangedEventArgs> callback)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.MAPNUM}";
        _db.RootReference.Child(path).ValueChanged += callback;
    }
    
    public void UnregisterMapNumberValueChangedHandler(string roomId, EventHandler<ValueChangedEventArgs> callback)
    {
        string path = $"{ChildKey.ROOMS}/{roomId}/{ChildKey.MAPNUM}";
        _db.RootReference.Child(path).ValueChanged -= callback;
    }
}

