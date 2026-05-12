using UnityEngine;

public interface IImpactForce
{
    /// <summary>
    /// 폭발 힘 / 폭발 위치 / 폭발 범위 / 위로 띄우는 힘(폭발에 휘말렸을 때 위로 뜨는 힘을 넣어서 포물 선을 그릴 수 있게)
    /// </summary>
    /// <param name="explosionForce"></param>
    /// <param name="explosionPosition"></param>
    /// <param name="explosionRadius"></param>
    /// <param name="upwardsModifier"></param>
    public void ImpactPhysic(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier);
}
