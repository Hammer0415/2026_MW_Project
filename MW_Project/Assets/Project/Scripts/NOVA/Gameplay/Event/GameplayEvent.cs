using System;
using UnityEngine;

[Serializable]
public struct GameplayEvent
{
    [Header("Component")]
    [Tooltip("GameplayTag 스크립트")]
    [SerializeField] private GameplayTag eventTag;

    public GameplayTag EventTag => eventTag;

    public NovaActor Instigator { get; }
    public NovaActor Target { get; }

    public GameplayEvent(GameplayTag eventTag, NovaActor instigator = null, NovaActor target = null)
    {
        this.eventTag = eventTag;
        Instigator = instigator;
        Target = target;
    }
}
