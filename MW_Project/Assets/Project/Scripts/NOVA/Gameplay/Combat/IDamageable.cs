public interface IDamageable
{
    bool IsDead { get; }

    void ApplyDamage(DamageInfo damageInfo);
}
