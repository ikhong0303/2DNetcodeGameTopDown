using UnityEngine;
using System.Collections.Generic;

namespace IsaacLike.Net
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        [HideInInspector] public AudioSource source;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private Sound[] musicTracks;

        [Header("Sound Effects")]
        [SerializeField] private Sound[] soundEffects;

        [Header("Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 1f;

        private Dictionary<string, Sound> _musicDict = new Dictionary<string, Sound>();
        private Dictionary<string, Sound> _sfxDict = new Dictionary<string, Sound>();
        private AudioSource _currentMusic;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSounds();
        }

        private void InitializeSounds()
        {
            foreach (Sound music in musicTracks)
            {
                music.source = gameObject.AddComponent<AudioSource>();
                music.source.clip = music.clip;
                music.source.volume = music.volume * musicVolume * masterVolume;
                music.source.pitch = music.pitch;
                music.source.loop = music.loop;

                _musicDict[music.name] = music;
            }

            foreach (Sound sfx in soundEffects)
            {
                sfx.source = gameObject.AddComponent<AudioSource>();
                sfx.source.clip = sfx.clip;
                sfx.source.volume = sfx.volume * sfxVolume * masterVolume;
                sfx.source.pitch = sfx.pitch;
                sfx.source.loop = sfx.loop;

                _sfxDict[sfx.name] = sfx;
            }
        }

        public void PlayMusic(string name, bool fadeIn = false)
        {
            if (!_musicDict.ContainsKey(name))
            {
                Debug.LogWarning($"Music '{name}' not found!");
                return;
            }

            if (_currentMusic != null && _currentMusic.isPlaying)
            {
                _currentMusic.Stop();
            }

            Sound music = _musicDict[name];
            _currentMusic = music.source;

            if (fadeIn)
            {
                StartCoroutine(FadeIn(music.source, 1f));
            }
            else
            {
                music.source.Play();
            }
        }

        public void StopMusic(bool fadeOut = false)
        {
            if (_currentMusic == null) return;

            if (fadeOut)
            {
                StartCoroutine(FadeOut(_currentMusic, 1f));
            }
            else
            {
                _currentMusic.Stop();
            }
        }

        public void PlaySFX(string name, float volumeMultiplier = 1f)
        {
            if (!_sfxDict.ContainsKey(name))
            {
                Debug.LogWarning($"SFX '{name}' not found!");
                return;
            }

            Sound sfx = _sfxDict[name];
            sfx.source.volume = sfx.volume * sfxVolume * masterVolume * volumeMultiplier;
            sfx.source.Play();
        }

        public void PlaySFXAtPosition(string name, Vector3 position, float spatialBlend = 1f)
        {
            if (!_sfxDict.ContainsKey(name))
            {
                Debug.LogWarning($"SFX '{name}' not found!");
                return;
            }

            Sound sfx = _sfxDict[name];

            GameObject tempGO = new GameObject($"TempAudio_{name}");
            tempGO.transform.position = position;

            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = sfx.clip;
            tempSource.volume = sfx.volume * sfxVolume * masterVolume;
            tempSource.pitch = sfx.pitch;
            tempSource.spatialBlend = spatialBlend;
            tempSource.Play();

            Destroy(tempGO, sfx.clip.length + 0.1f);
        }

        private System.Collections.IEnumerator FadeIn(AudioSource source, float duration)
        {
            source.volume = 0;
            source.Play();

            float targetVolume = source.volume;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(0, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }

        private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            foreach (var music in _musicDict.Values)
            {
                if (music.source != null)
                {
                    music.source.volume = music.volume * musicVolume * masterVolume;
                }
            }

            foreach (var sfx in _sfxDict.Values)
            {
                if (sfx.source != null)
                {
                    sfx.source.volume = sfx.volume * sfxVolume * masterVolume;
                }
            }
        }
    }
}
