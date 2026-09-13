using UnityEngine;
using System.Collections;

public class MG_Audio : MonoBehaviour
{
    //=====Singleton=====//
    public static MG_Audio Instance { get; private set; }
    //===================//

    //=====Components=====//
    private MG_Game gameManager = null;
    //====================//

    //=====AudioSources=====//
    [Header("Audio Sources")]
    [Tooltip("배경음 오디오 소스")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("사운드 이펙트 오디오 소스")]
    [SerializeField] private AudioSource sfxSource;
    //======================//

    //=====BGMSettings=====//
    [Header("BGM")]
    [Tooltip("비전투 배경 음악")]
    [SerializeField] private AudioClip[] defaultBGMClip;
    [Tooltip("전투 시 배경 음악")]
    [SerializeField] private AudioClip[] battleBGMClip;
    [Tooltip("BGM 전환 페이드 시간")]
    [SerializeField] private float bgmFadeDuration = 0.0f;
    [Tooltip("게임 시작 시 비전투 BGM 재생")]
    [SerializeField] private bool playDefaultBGMOnStart = true;
    //=====================//

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

    //=====VolumeSettings=====//
    [Header("Volume")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float bgmVolume = 1.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float sfxVolume = 1.0f;
    //========================//
    
    private enum BGMState
    {
        None,
        Default,
        Battle
    }

    private BGMState currentBGMState = BGMState.None;

    private Coroutine bgmCoroutine = null;

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

    private void Start()
    {
        InitStartSetup();

        if (playDefaultBGMOnStart) PlayDefaultBGM();
    }

    //======================================//
    // BGM
    //======================================//
    public void PlayDefaultBGM()
    {
        if (defaultBGMClip == null || defaultBGMClip.Length == 0) return;

        if (currentBGMState == BGMState.Default) return;

        AudioClip clip = GetRandomClip(defaultBGMClip);

        if (clip == null) return;

        StartBGMTransition(clip, BGMState.Default);
    }

    public void PlayBattleBGM()
    {
        if (battleBGMClip == null || battleBGMClip.Length == 0) return;

        if (currentBGMState == BGMState.Battle) return;

        AudioClip clip = GetRandomClip(battleBGMClip);

        if (clip == null) return;

        StartBGMTransition(clip, BGMState.Battle);
    }

    public void StopBGM()
    {
        if (bgmSource == null) return;
        
        if (bgmCoroutine != null) StopCoroutine(bgmCoroutine);

        bgmCoroutine = StartCoroutine(FadeOutBGM());
    }

    private void StartBGMTransition(AudioClip newClip, BGMState newState)
    {
        if (bgmSource == null || newClip == null) return;

        if (bgmCoroutine != null)
        {
            StopCoroutine(bgmCoroutine);
        }

        bgmCoroutine = StartCoroutine(ChangeBGM(newClip, newState));
    }

    private IEnumerator ChangeBGM(AudioClip newClip, BGMState newState)
    {
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            float timer = 0.0f;

            while (timer < bgmFadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / bgmFadeDuration;
                bgmSource.volume = Mathf.Lerp(startVolume, 0.0f, t);

                yield return null;
            }

            bgmSource.Stop();
        }

        bgmSource.clip = newClip;
        bgmSource.volume = 0.0f;

        bgmSource.Play();

        float fadeTimer = 0.0f;
        float targetVolume = masterVolume * bgmVolume;

        while (fadeTimer < bgmFadeDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float t = fadeTimer / bgmFadeDuration;
            bgmSource.volume = Mathf.Lerp(0.0f, targetVolume, t);

            yield return null;
        }

        bgmSource.volume = targetVolume;
        currentBGMState = newState;
        bgmCoroutine = null;

        while (bgmSource.isPlaying) yield return null;

        PlayNextBGM(newState);
    }

    private void PlayNextBGM(BGMState state)
    {
        AudioClip[] clips = null;

        if (state == BGMState.Battle) clips = battleBGMClip;
        else if (state == BGMState.Default) clips = defaultBGMClip;

        if (clips == null || clips.Length == 0) return;

        AudioClip nextClip = GetRandomClip(clips);

        if (nextClip == null) return;

        StartBGMTransition(nextClip, state);
    }

    private IEnumerator FadeOutBGM()
    {
        if (!bgmSource.isPlaying) yield break;

        float startVolume = bgmSource.volume;
        float timer = 0.0f;

        while (timer < bgmFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / bgmFadeDuration;
            bgmSource.volume = Mathf.Lerp(startVolume, 0.0f, t);

            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = 0.0f;

        currentBGMState = BGMState.None;

        bgmCoroutine = null;
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;

        return clips[Random.Range(0, clips.Length)];
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
    // Volume
    //======================================//
    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;

        sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
    }

    //======================================//
    private void InitStartSetup()
    {
        if (gameManager == null) gameManager = MG_Game.Instance;

        if (gameManager == null) Debug.LogError("MG_Game을 찾을 수 없습니다.", this);
    }
    //======================================//
}
