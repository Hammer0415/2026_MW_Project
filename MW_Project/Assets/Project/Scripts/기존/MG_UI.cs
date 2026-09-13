using UnityEngine;
using UnityEngine.UI;

public class MG_UI : MonoBehaviour
{
    //=====Components=====//
    private AC_Player player = null;
    //====================//

    //=====PlayerUI=====//
    [Header("Player UIs")]
    [SerializeField] private Slider playerHpBar = null;
    [SerializeField] private Slider playerSteminaBar = null;
    //==================//

    private void Start()
    {
        StartInitSetup();
    }

    private void Update()
    {
        HandlePlayerUI();
    }

    private void HandlePlayerUI()
    {
        if (player)
        {
            if (playerHpBar) playerHpBar.value = player.HP();
            if (playerSteminaBar) playerSteminaBar.value = player.Stemina();
        }
    }

    //======================================//
    private void StartInitSetup()
    {
        if (player == null) player = AC_Player.Instance;

        if (player == null) Debug.LogError("AC_Player를 찾을 수 없습니다.");
    }
}
