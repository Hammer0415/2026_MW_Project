using System;

public static class GameplayEventBus
{
    public static event Action<GameplayEvent> OnEventRaised;

    // 게임플레이 이벤트를 모든 구독자에게 전달한다.
    public static void Raise(GameplayEvent gameplayEvent)
    {
        OnEventRaised?.Invoke(gameplayEvent);
    }

    // 태그, 시전자, 대상으로 이벤트를 만들어 전달한다.
    public static void Raise(GameplayTag eventTag, NovaActor instigator = null, NovaActor target = null)
    {
        Raise(new GameplayEvent(eventTag, instigator, target));
    }
}
