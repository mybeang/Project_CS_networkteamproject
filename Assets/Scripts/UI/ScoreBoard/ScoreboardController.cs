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

    private int _team1score = 0;
    private int _team2score = 0;
    private int _team3score = 0;
    private int _team4score = 0;

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
        _team1score = score[0];
        _team2score = score[1];
        _team3score = score[2];
        _team4score = score[3];
    }

    private void OnEnable()
    {
        _team1.text = _team1score.ToString();
        _team2.text = _team2score.ToString();
        _team3.text = _team3score.ToString();
        _team4.text = _team4score.ToString();
    }

}
