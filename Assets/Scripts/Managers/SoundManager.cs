using UnityEngine;
using TopDownShooter.Core;

namespace TopDownShooter.Managers
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

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

        public void PlaySfx(string sfxName)
        {
            // Placeholder for SFX playback using Debug.Log as requested
            // Debug.Log($"[SFX] Play Sound: {sfxName}");
        }

        public void PlayMusic(string musicName)
        {
             // Placeholder for Music playback
            Debug.Log($"[Music] Play Music: {musicName}");
        }
        
        // Example method if there's an explicit enum or ID later
        public void PlayClip(AudioClip clip)
        {
             if (clip != null)
             {
                 Debug.Log($"[SFX] Play Clip: {clip.name}");
             }
        }
    }
}
