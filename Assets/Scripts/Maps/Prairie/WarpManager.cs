using System.Collections.Generic;
using UnityEngine;

public class WarpManager : Manager<WarpManager>, IWarpManager
{
    [SerializeField] private List<Transform> _warpPoints;

    protected override void Register() => ServiceLocator.Register<IWarpManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IWarpManager>();

    public Transform GetWarpPoint()
    {
        int randomIndex = Random.Range(0, _warpPoints.Count);
        return _warpPoints[randomIndex];
    }
}