using UnityEngine;

public enum PlayState
{
    Playing,
    InBattle,
    Paused,
    PlayerDead
}

[AddComponentMenu("NOVA/Core/Game State")]
public class GameState : MonoBehaviour
{
    [Header("State")]
    [ReadOnly]
    [Tooltip("현재 게임 상태")]
    [SerializeField] private PlayState currentState = PlayState.Playing;

    public PlayState CurrentState => currentState;
    public bool IsBattle => currentState == PlayState.InBattle;
    public bool IsPlayerDead => currentState == PlayState.PlayerDead;

    // 게임 상태를 변경한다.
    public void SetState(PlayState state)
    {
        currentState = state;
    }
}
