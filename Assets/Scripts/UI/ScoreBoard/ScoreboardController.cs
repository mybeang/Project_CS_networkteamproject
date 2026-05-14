using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreboardController : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI _team1;
    [SerializeField] TextMeshProUGUI _team2;
    [SerializeField] TextMeshProUGUI _team3;
    [SerializeField] TextMeshProUGUI _team4;

    private void Start()
    {
        ServiceLocator.Get<IGameManager>().OnChangeScore += ScoreListenerClientRpc;
    }

    private void OnDestroy() => ServiceLocator.Get<IGameManager>().OnChangeScore -= ScoreListenerClientRpc;

    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void ScoreListenerClientRpc(string scoreStringData)
    {
        Debug.Log($"[ScoreboardController] [{name}] {scoreStringData}");
        int[] score = new int [4];
        for (int i = 0; i < score.Length; i++)
            score[i] = int.Parse(scoreStringData.Split(',')[i]);
            
        Debug.Log($"[ScoreboardController] [{name}] in score");
        _team1.text = score[0].ToString();
        _team2.text = score[1].ToString();
        _team3.text = score[2].ToString();
        _team4.text = score[3].ToString();
    }
}
