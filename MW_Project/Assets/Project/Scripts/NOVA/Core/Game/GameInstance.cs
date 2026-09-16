using UnityEngine;

[AddComponentMenu("NOVA/Core/Game Instance")]
public class GameInstance : MonoBehaviour
{
    public static GameInstance Instance { get; private set; }

    [Header("References")]
    [Tooltip("현재 씬의 GameManager. 비어 있으면 씬에서 찾는다.")]
    [SerializeField] private GameManager gameManager;
    [Tooltip("현재 씬의 GameState. 비어 있으면 씬에서 찾는다.")]
    [SerializeField] private GameState gameState;

    public GameManager GameManager => gameManager;
    public GameState GameState => gameState;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>();
        if (!gameState) gameState = FindFirstObjectByType<GameState>();
    }
}
