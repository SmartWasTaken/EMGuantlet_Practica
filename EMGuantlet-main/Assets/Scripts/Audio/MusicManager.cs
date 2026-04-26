using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Componentes")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("Listas de Reproducción")]
    [SerializeField] private AudioClip[] calmTracks;

    [Header("Configuración Procedural")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float minSilenceBetweenTracks = 5f;
    [SerializeField] private float maxSilenceBetweenTracks = 15f;

    private bool isPlayingProcedural = false;
    private Coroutine musicRoutine;
    private AudioClip lastPlayedClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false;
            musicSource.playOnAwake = false;
        }

        if (musicMixerGroup != null)
        {
            musicSource.outputAudioMixerGroup = musicMixerGroup;
        }
    }

    private void Start()
    {
        StartProceduralMusic();
    }

    /// <summary>
    /// Arranca el motor de música infinita.
    /// </summary>
    public void StartProceduralMusic()
    {
        isPlayingProcedural = true;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(ProceduralMusicFlow());
    }

    /// <summary>
    /// Para la música con un fundido suave.
    /// </summary>
    public void StopMusic()
    {
        isPlayingProcedural = false;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        StartCoroutine(FadeOut(musicSource, fadeDuration));
    }

    private IEnumerator ProceduralMusicFlow()
    {
        while (isPlayingProcedural)
        {
            if (calmTracks == null || calmTracks.Length == 0)
            {
                Debug.LogWarning("[MusicManager] No hay canciones en la lista. El motor se detiene.");
                yield break;
            }

            AudioClip nextClip = ChooseRandomTrack();
            lastPlayedClip = nextClip;

            musicSource.clip = nextClip;
            musicSource.volume = 0f;
            musicSource.Play();

            yield return StartCoroutine(FadeIn(musicSource, fadeDuration, 1f));

            float waitTimeForSong = nextClip.length - fadeDuration;
            if (waitTimeForSong > 0)
            {
                yield return new WaitForSeconds(waitTimeForSong);
            }

            yield return StartCoroutine(FadeOut(musicSource, fadeDuration));

            float silenceDuration = Random.Range(minSilenceBetweenTracks, maxSilenceBetweenTracks);
            yield return new WaitForSeconds(silenceDuration);
        }
    }

    private AudioClip ChooseRandomTrack()
    {
        if (calmTracks.Length == 1) return calmTracks[0];

        AudioClip chosen;
        do
        {
            chosen = calmTracks[Random.Range(0, calmTracks.Length)];
        }
        while (chosen == lastPlayedClip);

        return chosen;
    }

    private IEnumerator FadeIn(AudioSource source, float duration, float targetVolume)
    {
        float currentTime = 0;
        source.volume = 0;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(0, targetVolume, currentTime / duration);
            yield return null;
        }
        source.volume = targetVolume;
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float currentTime = 0;
        float startVolume = source.volume;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0, currentTime / duration);
            yield return null;
        }
        source.volume = 0;
        source.Stop();
    }
}