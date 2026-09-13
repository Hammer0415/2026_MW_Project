using System;
using UnityEngine;

[Serializable]
public class NovaEffect
{
    [SerializeField] private GameplayTag effectTag;
    [SerializeField] private float value;

    public GameplayTag EffectTag => effectTag;
    public float Value => value;

    // Attribute 변경을 표현하는 데이터 객체
    public NovaEffect(GameplayTag effectTag, float value)
    {
        this.effectTag = effectTag;
        this.value = value;
    }
}
