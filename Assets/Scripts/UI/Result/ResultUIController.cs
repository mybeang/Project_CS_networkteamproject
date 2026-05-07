using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ResultUIController : MonoBehaviour
{
    [Header("Team Panels")]
    [SerializeField] private ResultTeamUI _team1;
    [SerializeField] private ResultTeamUI _team2;
    [SerializeField] private ResultTeamUI _team3;
    [SerializeField] private ResultTeamUI _team4;

    [Header("Button")]
    [SerializeField] private Button _confirmButton;
    
    private TeamInfo[] _teams;
    private List<(int score, ResultTeamUI ui)> _scoreBoards = new();

    private void OnEnable()
    {
        _confirmButton.onClick.AddListener(GoToLobby);
        UpdateUI();
    }

    private void OnDisable()
    {
        _confirmButton.onClick.RemoveListener(GoToLobby);
        UnsetAllWinnerFlags();
        UnsetAllScoreBoards();
    }

    private void UpdateUI()
    {
        if (_teams == null) return;
        GetTeams();
        SetData();
        CandidateWinner();
    }
    
    private void GetTeams() => _teams = ServiceLocator.Get<IGameManager>()?.GetTeams();

    private void SetUIData(TeamInfo team, ResultTeamUI teamUI)
    {
        teamUI.SetData(team);
        _scoreBoards.Add((score:team.GetScore(), ui:teamUI));
        teamUI.gameObject.SetActive(true);
    }
    
    private void SetData()
    {
        foreach (var team in _teams)
        {
            switch (team.GetTeamNum())
            {
                case PlayerTeamEnum.firstTeam:
                    SetUIData(team, _team1);
                    break;
                case PlayerTeamEnum.secondTeam:
                    SetUIData(team, _team2);
                    break;
                case PlayerTeamEnum.thirdTeam:
                    SetUIData(team, _team3);
                    break;
                case PlayerTeamEnum.fourthTeam:
                    SetUIData(team, _team4);
                    break;
            }
        }
    }

    private void CandidateWinner()
    {
        _scoreBoards.Sort((a, b) => a.score.CompareTo(b.score));
        _scoreBoards.Reverse();
        int winnerScore = _scoreBoards[0].score;  // top score
        foreach (var (score, ui) in _scoreBoards)
            if (winnerScore == score) ui.SetWinner();
    }

    private void UnsetAllWinnerFlags()
    {
        _team1.UnsetWinner();
        _team2.UnsetWinner();
        _team3.UnsetWinner();
        _team4.UnsetWinner();
    }
    
    private void UnsetAllScoreBoards()
    {
        _team1.gameObject.SetActive(false);
        _team1.gameObject.SetActive(false);
        _team1.gameObject.SetActive(false);
        _team1.gameObject.SetActive(false);
    }

    private void GoToLobby()
    {
        var loader = ServiceLocator.Get<ILocalSceneLoader>();
        loader.LoadScene("LobbyRoom");
    }

# if UNITY_EDITOR
    [ContextMenu("Test Score")]
    private void TestScore()
    {
        var team1 = new TeamInfo(PlayerTeamEnum.firstTeam, PlayerableVehicleEnum.tank)
        {
            players = new List<PlayerInfo>()
            {
                new()
                {
                    userId = "jadeJJang",
                    clientId = 0,
                    role = PlayerRole.Gunner
                },
                new()
                {
                    userId = "DokimekiJoYoung",
                    clientId = 1,
                    role = PlayerRole.Driver
                }
            }
        };
        team1.SetScore(1000);
        
        var team2 = new TeamInfo(PlayerTeamEnum.secondTeam, PlayerableVehicleEnum.tank)
        {
            players = new List<PlayerInfo>()
            {
                new()
                {
                    userId = "YoungMin",
                    clientId = 2,
                    role = PlayerRole.Gunner
                },
                new()
                {
                    userId = "Taeho",
                    clientId = 3,
                    role = PlayerRole.Driver
                }
            }
        };
        team2.SetScore(900);
        
        var team3 = new TeamInfo(PlayerTeamEnum.thirdTeam, PlayerableVehicleEnum.tank)
        {
            players = new List<PlayerInfo>()
            {
                new()
                {
                    userId = "TaeWook",
                    clientId = 4,
                    role = PlayerRole.Gunner
                },
                new()
                {
                    userId = "WonTak",
                    clientId = 5,
                    role = PlayerRole.Driver
                }
            }
        };
        team3.SetScore(600);
        
        var team4 = new TeamInfo(PlayerTeamEnum.fourthTeam, PlayerableVehicleEnum.tank)
        {
            players = new List<PlayerInfo>()
            {
                new()
                {
                    userId = "Chaebh",
                    clientId = 6,
                    role = PlayerRole.Gunner
                },
                new()
                {
                    userId = "GaeYoung",
                    clientId = 7,
                    role = PlayerRole.Driver
                }
            }
        };
        team4.SetScore(500);
        
        _teams = new []{team1, team2, team3};
        SetData();
        CandidateWinner();
    }
#endif
}
