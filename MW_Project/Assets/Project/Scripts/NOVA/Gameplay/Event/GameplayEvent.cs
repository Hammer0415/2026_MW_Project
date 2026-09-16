using System;
using UnityEngine;

[Serializable]
public struct GameplayEvent
{
    [Header("Event")]
    [Tooltip("이벤트 종류를 나타내는 태그")]
    [SerializeField] private GameplayTag eventTag;

    public GameplayTag EventTag => eventTag;
    public NovaActor Instigator { get; }
    public NovaActor Target { get; }
    public float Value { get; }

    public GameplayEvent(GameplayTag eventTag, NovaActor instigator = null, NovaActor target = null, float value = 0f)
    {
        this.eventTag = eventTag;
        Instigator = instigator;
        Target = target;
        Value = value;
    }
}
