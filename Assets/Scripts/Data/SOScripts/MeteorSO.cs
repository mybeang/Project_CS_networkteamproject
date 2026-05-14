using UnityEngine;

[CreateAssetMenu(fileName = "Meteor", menuName = "Scriptable Objects/MeteorSO")]
public class MeteorSO : ScriptableObject
{
    [Header("기본 수치")]
    public int meteorDamage;
    public int meteorDamageRange;
    public int meteorMaxSpawnMeteor;
    public int meteorDropSpeed;

    [Header("빛 크기")]
    public Vector3 meteorSize3D;

    [Header("꼬리 화염")]
    public float meteorFireTailSize;

    [Header("바람 크기")]
    public Vector3 meteorWindSize;
    [Header("폭발 크기")]
    public float meteorExplosionSize;

    [Header("소환 가능 범위")]
    public int meteorMaxHorizontalRange;
    public int meteorMinHorizontalRange;
    public int meteorMaxVerticalRange;
    public int meteorMinVerticalRange;

    [Header("1업 당 증가 수치")]
    public int upPerMeteorDamage;
    public int upPerMeteorDamageRange;
    public int upPerMeteorMaxSpawnMeteor;

    public MeteorSO UpMeteor(int stage)
    {
        MeteorSO so = this;
        so.meteorDamage = meteorDamage + upPerMeteorDamage * stage;
        so.meteorDamageRange = meteorDamageRange + upPerMeteorDamageRange * stage;
        so.meteorMaxSpawnMeteor = meteorMaxSpawnMeteor + upPerMeteorMaxSpawnMeteor * stage;
        return so;
    }
}
