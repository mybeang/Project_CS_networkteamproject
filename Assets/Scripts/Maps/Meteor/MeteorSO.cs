using UnityEngine;

public class MeteorSO : ScriptableObject
{
    [SerializeField] private int _meteorDamage;
    [SerializeField] private int _meteorCount;
    [SerializeField] private int _meteorDamageRange;
    [SerializeField] private int _meteorSpawnRange;

    public int MeteorDamage
    {
        get { return _meteorDamage; }
    }

    public int MeteorCount
    {
        get { return _meteorDamage; }
    }

    public int MeteorDamageRange
    {
        get { return _meteorDamage; }
    }

    public int MeteorSpawnRange
    {
        get { return _meteorDamage; }
    }
}
