using UnityEngine;

public struct DamageInfo
{
    public NovaActor Instigator;
    public NovaActor Target;
    public float Amount;
    public Vector3 HitPoint;
    public Vector3 HitNormal;
    public GameplayTag DamageTag;
    public bool CanBePerfectDodged;

    public DamageInfo(NovaActor instigator, float amount)
    {
        Instigator = instigator;
        Target = null;
        Amount = amount;
        HitPoint = Vector3.zero;
        HitNormal = Vector3.forward;
        DamageTag = default;
        CanBePerfectDodged = false;
    }
}
