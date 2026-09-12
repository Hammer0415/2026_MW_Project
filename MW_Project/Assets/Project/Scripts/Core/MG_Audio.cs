using UnityEngine;

public class MG_Audio : MonoBehaviour
{
    //=====Singleton=====//
    public static MG_Audio Instance { get; private set; }
    //===================//

    //=====AudioSources=====//
    [Header("Audio Sources")]
    [Tooltip("배경음 오디오 소스")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("사운드 이펙트 오디오 소스")]
    [SerializeField] private AudioSource sfxSource;
    //======================//

    //=====SFXSettings=====//
    [Header("Player SFX")]
    [Tooltip("플레이어 기본 공격 사운드 이펙트")]
    [SerializeField] private AudioClip playerDefaultAttackClip;
    [Tooltip("플레이어 대쉬 사운드 이펙트")]
    [SerializeField] private AudioClip dodgeClip;
    [Tooltip("플레이어 발자국 사운드 이펙트")]
    [SerializeField] private AudioClip footstepClip;
    [Header("Enemy SFX")]
    [Tooltip("적 기본 공격 사운드 이펙트")]
    [SerializeField] private AudioClip enemyDefaultAttackClip;
    [Header("UI SFX")]
    [Tooltip("UI 클릭 사운드 이펙트")]
    [SerializeField] private AudioClip uiClickClip;
    //====================//

    //===== Volume =====//
    [Header("Volume")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float bgmVolume = 1.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float sfxVolume = 1.0f;
    //==================//    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    //======================================//
    // SFX
    //======================================//
    public void PlayPlayerDefaultAttack()
    {
        PlaySFX(playerDefaultAttackClip);
    }

    public void PlayDodge()
    {
        PlaySFX(dodgeClip);
    }

    public void PlayFootstep()
    {
        PlaySFX(footstepClip);
    }

    public void PlayEnemyDefaultAttack()
    {
        PlaySFX(enemyDefaultAttackClip);
    }

    public void PlayUIClick()
    {
        PlaySFX(uiClickClip);
    }

    //======================================//
    // BGM
    //======================================//
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
    }

    //======================================//
    // Volume
    //======================================//
    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;

        sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
    }
}
