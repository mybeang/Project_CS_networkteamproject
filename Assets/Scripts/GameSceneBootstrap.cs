using System;
using UnityEngine;

public class GameSceneBootstrap : MonoBehaviour
{
    private void OnEnable()
    {
        ServiceLocator.Get<IGameManager>().StartGame();
    }
}