using Firebase;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;


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
    protected override void Unregister() => ServiceLocator.Unregister<IDatabaseBackend>();
    
    public void SaveUserAsync(string userId)
    {
        _db.RootReference.Child($"users/{userId}").SetValueAsync(true).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] User data saved successfully: {userId}");
            else if (task.IsCanceled) Debug.LogError("[DB] SaveUserAsync was canceled.");
            else Debug.LogError("[DB] SaveUserAsync encountered an error: " + task.Exception);
        });
    }

    public void RemoveUserAsync(string userId)
    {
        _db.RootReference.Child($"users/{userId}").RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"[DB] User data removed successfully: {userId}");
            else if (task.IsCanceled) Debug.LogError("[DB] RemoveUserAsync was canceled.");
            else Debug.LogError("[DB] RemoveUserAsync encountered an error: " + task.Exception);
        });
    }

    public bool ValidateDuplicateUserIdAsync(string userId)
    {   // 중복이면 true !!
        _db.RootReference.Child($"users/{userId}").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot dataSnapshot = task.Result;
                if (dataSnapshot.Exists) return true;
            }
            return true;
        });
        return false;
    }
}
