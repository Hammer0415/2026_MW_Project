using System.Collections;
using UnityEngine;

[AddComponentMenu("NOVA/Core/Game Manager")]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    [Tooltip("게임 상태 컴포넌트. 비어 있으면 같은 오브젝트에서 찾는다.")]
    [SerializeField] private GameState gameState;

    [Header("Battle")]
    [Tooltip("전투가 끝난 뒤 기본 상태로 돌아가기까지 대기 시간")]
    [SerializeField] private float battleExitDelay = 3f;
    [ReadOnly]
    [Tooltip("현재 전투 중인지")]
    [SerializeField] private bool isBattle;
    [ReadOnly]
    [Tooltip("전투에 참여 중인 적 수")]
    [SerializeField] private int battleEnemyCount;

    public bool IsBattle => isBattle;
    public bool IsPlayerDead => gameState && gameState.IsPlayerDead;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!gameState) gameState = GetComponent<GameState>();
        if (!gameState) gameState = gameObject.AddComponent<GameState>();
    }

    // 적이 전투에 진입했을 때 호출한다.
    public void EnterBattle()
    {
        battleEnemyCount++;
        isBattle = battleEnemyCount > 0;

        if (gameState) gameState.SetState(PlayState.InBattle);
    }

    // 적이 전투에서 빠졌을 때 호출한다.
    public void ExitBattle()
    {
        battleEnemyCount = Mathf.Max(battleEnemyCount - 1, 0);
        isBattle = battleEnemyCount > 0;

        StartCoroutine(WaitBattleExit());
    }

    // 플레이어 사망 상태를 기록한다.
    public void SetPlayerDead()
    {
        if (gameState) gameState.SetState(PlayState.PlayerDead);
    }

    // 전투 적 수가 0이 되면 일정 시간 뒤 Playing 상태로 되돌린다.
    private IEnumerator WaitBattleExit()
    {
        yield return new WaitForSeconds(battleExitDelay);

        if (isBattle) yield break;
        if (gameState && gameState.IsPlayerDead) yield break;

        if (gameState) gameState.SetState(PlayState.Playing);
    }
}
