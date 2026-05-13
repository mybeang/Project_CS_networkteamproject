using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RespawnUIController : NetworkManager<RespawnUIController>, IRespawnUIController
{
    [SerializeField] private GameObject _respawnUIPrefab;
    [SerializeField] private TextMeshProUGUI _respawnTimeText;
    protected override void Register() => ServiceLocator.Register<IRespawnUIController>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IRespawnUIController>();
    
    private void OnEnable()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("[RespawnUIController] OnNetworkSpawn");
        ServiceLocator.PrintServices();
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        ServiceLocator.Get<IGameManager>().AddRespawnCounterHandler(userInfo.teamNum, UpdateRespawnTimeText);
    }

    public void SetActive(PlayerTeamEnum team, bool enable)
    {
        SetActiveClientRpc(team, enable);
    }
    
    [ClientRpc]
    public void SetActiveClientRpc(PlayerTeamEnum team, bool enable)
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (userInfo.teamNum == team) _respawnUIPrefab.SetActive(enable);
    }

    private void UpdateRespawnTimeText(int oldVal, int newVal)
    {
        _respawnTimeText.text = $"남은 시간: {newVal} s";
    }
    
}
