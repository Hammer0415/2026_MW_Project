using System;
using UnityEngine;

[Serializable]
public class NovaEffect
{
    [Header("Effect")]
    [Tooltip("이 효과가 어떤 Attribute를 바꾸는지 나타내는 태그")]
    [SerializeField] private GameplayTag effectTag;
    [Tooltip("적용할 값. 데미지면 양수, 회복이면 양수로 두고 호출 쪽에서 해석한다.")]
    [SerializeField] private float value;

    public GameplayTag EffectTag => effectTag;
    public float Value => value;

    // Attribute 변경을 표현하는 데이터 객체를 만든다.
    public NovaEffect(GameplayTag effectTag, float value)
    {
        this.effectTag = effectTag;
        this.value = value;
    }
}
