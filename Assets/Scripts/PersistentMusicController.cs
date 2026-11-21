using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentMusicController : MonoBehaviour
{
    private static PersistentMusicController instance;

    [Header("musica principal (persistente)")]
    public AudioClip defaultMusic;
    [Range(0f, 1f)] public float maxVolume = 0.5f;

    [Header("escena especial")]
    [Tooltip("nombre exacto de la escena con musica diferente")]
    [SerializeField] private string specialSceneName = "04_Leaderboard";
    [SerializeField] private AudioClip specialSceneMusic;

    [Header("ajustes de transicion")]
    [Tooltip("duracion del desvanecimiento en segundos")]
    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource mainAudioSource;
    private AudioSource specialAudioSource;
    private Coroutine activeFadeCoroutine;

    void Awake()
    {
        // singleton para mantener un unico controlador de musica entre escenas
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // canal 1: musica principal
        mainAudioSource = gameObject.AddComponent<AudioSource>();
        mainAudioSource.clip = defaultMusic;
        mainAudioSource.loop = true;
        mainAudioSource.volume = maxVolume;
        if (defaultMusic != null) mainAudioSource.Play();

        // canal 2: musica secundaria para la escena especial
        specialAudioSource = gameObject.AddComponent<AudioSource>();
        specialAudioSource.clip = specialSceneMusic;
        specialAudioSource.loop = true;
        specialAudioSource.volume = 0f;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // gestiona la transicion de musica al cargar una escena
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }

        if (scene.name == specialSceneName)
        {
            if (specialSceneMusic != null && !specialAudioSource.isPlaying)
            {
                specialAudioSource.Play();
            }
            activeFadeCoroutine = StartCoroutine(Crossfade(mainAudioSource, specialAudioSource, fadeDuration));
        }
        else
        {
            if (defaultMusic != null && !mainAudioSource.isPlaying)
            {
                mainAudioSource.Play();
            }
            activeFadeCoroutine = StartCoroutine(Crossfade(specialAudioSource, mainAudioSource, fadeDuration));
        }
    }

    // hace crossfade gradual entre la pista saliente y la entrante
    private IEnumerator Crossfade(AudioSource fadeOutSource, AudioSource fadeInSource, float duration)
    {
        if (duration <= 0f)
        {
            fadeOutSource.volume = 0f;
            fadeInSource.volume = maxVolume;
            yield break;
        }

        float timer = 0f;
        float startFadeOutVol = fadeOutSource.volume;
        float startFadeInVol = fadeInSource.volume;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            fadeOutSource.volume = Mathf.Lerp(startFadeOutVol, 0f, progress);
            fadeInSource.volume = Mathf.Lerp(startFadeInVol, maxVolume, progress);

            yield return null;
        }

        fadeOutSource.volume = 0f;
        fadeInSource.volume = maxVolume;

        if (fadeOutSource == specialAudioSource)
        {
            specialAudioSource.Pause();
        }
        else if (fadeOutSource == mainAudioSource)
        {
            mainAudioSource.Pause();
        }
    }
}