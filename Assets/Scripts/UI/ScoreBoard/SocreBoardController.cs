using TMPro;
using UnityEngine;

public class SocreBoardController : MonoBehaviour
{
    [SerializeField] Canvas _scoreBoard;
    [SerializeField] TextMeshProUGUI _team1;
    [SerializeField] TextMeshProUGUI _team2;
    [SerializeField] TextMeshProUGUI _team3;
    [SerializeField] TextMeshProUGUI _team4;

    private int _team1score;
    private int _team2score;
    private int _team3score;
    private int _team4score;

    private void Awake()
    {
        ServiceLocator.Get<IGameManager>().OnChangeScore += ScoreListener;
    }

    private void OnDestroy()
    {
        ServiceLocator.Get<IGameManager>().OnChangeScore -= ScoreListener;
    }

    private void ScoreListener(int[] score)
    {
        _team1score = score[0];
        _team2score = score[1];
        _team3score = score[2];
        _team4score = score[3];
    }

    public void OnScoreBoardEnable()
    {
        _scoreBoard.enabled = true;
        _team1.text = _team1score.ToString();
        _team2.text = _team2score.ToString();
        _team3.text = _team3score.ToString();
        _team4.text = _team4score.ToString();
    }

    public void OnScoreBoardDisable()
    {
        _scoreBoard.enabled = false;
    }

}
