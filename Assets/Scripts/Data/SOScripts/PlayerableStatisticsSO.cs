using UnityEngine;

[CreateAssetMenu(fileName = "PlayerableStatisticsSO", menuName = "Scriptable Objects/PlayerableStatisticsSO")]
public sealed class PlayerableStatisticsSO : ScriptableObject
{
    [Header("전차 관련")]
    [SerializeField] int _vechicleMaximumHP;
    [SerializeField] float _vechicleMoveSpeed;
    [SerializeField] float _vechicleRotationSpeed;
    [SerializeField] float _vechicleReloadtime;
    [SerializeField] float _turretVerticalRotationSpeed;
    [SerializeField] float _turretHorizontalRotationSpeed;


    [Tooltip("포신 최대 하강 각도")] [SerializeField] float _turretMaximumDepressionAngle;

    [Tooltip("포신 최대 상승 각도")] [SerializeField] float _turretMaximumElevationAngle;

    [Header("투사체 관련")]
    [SerializeField] int _projectileDamage;
    [SerializeField] int _projectileFlightSpeed;
    [SerializeField] float _projectileMaximumDamageRange;
    [SerializeField] float _projectileMaximumDinstance;

    [Header("이동 객체 이름")]
    [SerializeField] string _vechicleName;

    public int VechicleMaximumHP
    {
        get => _vechicleMaximumHP;
        private set => _vechicleMaximumHP = value;
    }
    public float VechicleMoveSpeed
    {
        get => _vechicleMoveSpeed;
        private set => _vechicleMoveSpeed = value;
    }
    public float VechicleRotationSpeed
    {
        get => _vechicleRotationSpeed;
        private set => _vechicleRotationSpeed = value;
    }
    public float VechicleReloadtime
    {
        get => _vechicleReloadtime;
        private set => _vechicleReloadtime = value;
    }

    public float TurretVerticalRotationSpeed
    {
        get => _turretVerticalRotationSpeed;
        private set => _turretVerticalRotationSpeed = value;
    }

    public float TurretHorizontalRotationSpeed
    {
        get => _turretHorizontalRotationSpeed;
        private set => _turretHorizontalRotationSpeed = value;
    }

    /// <summary>
    /// 포신 최소 하강 각도
    /// </summary>
    public float TurretMaximumDepressionAngle
    {
        get => _turretMaximumDepressionAngle;
        private set => _turretMaximumDepressionAngle = value;
    }
    /// <summary>
    /// 포신 최대 상승 각도
    /// </summary>
    public float TurretMaximumElevationAngle
    {
        get => _turretMaximumElevationAngle;
        private set => _turretMaximumElevationAngle = value;
    }

    public int ProjectileDamage
    {
        get => _projectileDamage;
        private set => _projectileDamage = value;
    }
    public int ProjectileFlightSpeed
    {
        get => _projectileFlightSpeed;
        private set => _projectileFlightSpeed = value;
    }
    public float ProjectileMaximumDamageableRange
    {
        get => _projectileMaximumDamageRange;
        private set => _projectileMaximumDamageRange = value;
    }
    public float ProjectileMaximumDinstance
    {
        get => _projectileMaximumDinstance;
        private set => _projectileMaximumDinstance = value;
    }
}
