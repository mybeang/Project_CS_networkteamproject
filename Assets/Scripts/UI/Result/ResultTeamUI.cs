using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultTeamUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _gunnerName;
    [SerializeField] private TextMeshProUGUI _driverName;
    [SerializeField] private Image _winnerImg;

    public void SetData(TeamInfo team)
    {
        foreach (var player in team.players)
        {
            if (player.role == PlayerRole.Driver) _driverName.text = player.userId;
            else _gunnerName.text = player.userId;
        }
    }
    public void SetWinner() => _winnerImg.gameObject.SetActive(true);
    public void UnsetWinner() => _winnerImg.gameObject.SetActive(false);
}
