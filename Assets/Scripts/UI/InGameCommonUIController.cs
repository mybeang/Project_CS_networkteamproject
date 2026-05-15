using TMPro;
using Unity.Netcode;
using UnityEngine;

public class InGameCommonUIController : NetworkManager<InGameCommonUIController>, IInGameCommonUIController
{
    [Header("For RespawnUI")]
    [SerializeField] private GameObject _respawnUIPrefab;
    [SerializeField] private TextMeshProUGUI _respawnTimeText;
    
    [Header("For LoadingUI")]
    [SerializeField] private GameObject _LoadingUIPrefab;

    protected override void Register() => ServiceLocator.Register<IInGameCommonUIController>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IInGameCommonUIController>(this);

    public override void OnNetworkSpawn()
    {
        Debug.Log("[InGameCommonUIController] OnNetworkSpawn");
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        ServiceLocator.Get<IGameManager>().AddRespawnCounterHandler(userInfo.teamNum, UpdateRespawnTimeText);
    }

    public void SetRespawnUIActive(PlayerTeamEnum team, bool enable) => SetRespawnActiveClientRpc(team, enable);
    public void SetLoadingUIActive(bool enable) => SetLoadingUIActiveClientRpc(enable);

    [ClientRpc]
    private void SetRespawnActiveClientRpc(PlayerTeamEnum team, bool enable)
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (userInfo.teamNum == team) _respawnUIPrefab.SetActive(enable);
    }

    [ClientRpc]
    private void SetLoadingUIActiveClientRpc(bool enable)
    {
        _LoadingUIPrefab.SetActive(enable);
    }

    private void UpdateRespawnTimeText(int oldVal, int newVal)
    {
        _respawnTimeText.text = $"남은 시간: {newVal} s";
    }
    
}
