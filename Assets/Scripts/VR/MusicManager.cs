using UnityEngine;

namespace HackathonVR
{
    /// <summary>
    /// Manages background music across scenes.
    /// Persists between scenes using DontDestroyOnLoad.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        [Header("Music Settings")]
        [SerializeField] private AudioClip[] musicTracks;
        [SerializeField] private float volume = 0.5f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool shuffle = false;
        
        [Header("Fade Settings")]
        [SerializeField] private float fadeInDuration = 2f;
        [SerializeField] private float fadeOutDuration = 1f;
        
        private AudioSource audioSource;
        private static MusicManager instance;
        private int currentTrackIndex = 0;
        private bool isFading = false;
        
        public static MusicManager Instance => instance;
        
        private void Awake()
        {
            // Singleton pattern - persist across scenes
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            SetupAudioSource();
            LoadMusicFromResources();
        }
        
        private void Start()
        {
            // Check if we should play music (from menu)
            if (playOnStart || PlayerPrefs.GetInt("PlayMusic", 0) == 1)
            {
                PlayerPrefs.SetInt("PlayMusic", 0);
                PlayMusic();
            }
        }
        
        private void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.playOnAwake = false;
        }
        
        private void LoadMusicFromResources()
        {
            // If no tracks assigned, try to load from Musiques folder
            if (musicTracks == null || musicTracks.Length == 0)
            {
                var loadedTracks = Resources.LoadAll<AudioClip>("Musiques");
                if (loadedTracks.Length > 0)
                {
                    musicTracks = loadedTracks;
                    Debug.Log($"[MusicManager] Loaded {loadedTracks.Length} tracks from Resources");
                }
            }
        }
        
        public void PlayMusic()
        {
            if (musicTracks == null || musicTracks.Length == 0)
            {
                Debug.LogWarning("[MusicManager] No music tracks available");
                return;
            }
            
            if (shuffle)
            {
                currentTrackIndex = Random.Range(0, musicTracks.Length);
            }
            
            audioSource.clip = musicTracks[currentTrackIndex];
            audioSource.Play();
            
            StartCoroutine(FadeIn());
            
            Debug.Log($"[MusicManager] Playing: {audioSource.clip.name}");
        }
        
        public void StopMusic()
        {
            StartCoroutine(FadeOutAndStop());
        }
        
        public void NextTrack()
        {
            currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Length;
            StartCoroutine(CrossfadeToTrack(currentTrackIndex));
        }
        
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (!isFading)
            {
                audioSource.volume = volume;
            }
        }
        
        private System.Collections.IEnumerator FadeIn()
        {
            isFading = true;
            audioSource.volume = 0f;
            
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = (elapsed / fadeInDuration) * volume;
                yield return null;
            }
            
            audioSource.volume = volume;
            isFading = false;
        }
        
        private System.Collections.IEnumerator FadeOutAndStop()
        {
            isFading = true;
            float startVolume = audioSource.volume;
            float elapsed = 0f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = startVolume * (1f - elapsed / fadeOutDuration);
                yield return null;
            }
            
            audioSource.Stop();
            audioSource.volume = volume;
            isFading = false;
        }
        
        private System.Collections.IEnumerator CrossfadeToTrack(int trackIndex)
        {
            yield return StartCoroutine(FadeOutAndStop());
            
            audioSource.clip = musicTracks[trackIndex];
            audioSource.Play();
            
            yield return StartCoroutine(FadeIn());
        }
    }
}
