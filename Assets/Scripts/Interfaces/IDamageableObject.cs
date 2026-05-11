using System.Numerics;

public interface IDamageableObject
{
    public void TakeDamaged(int dmg, PlayerTeamEnum enemy);

    public void ExplosionDamaged(Vector3 expsPos, int dmg, PlayerTeamEnum enemy);
}
