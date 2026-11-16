using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Bloombots
{
    internal sealed class BloomFeedback : MonoBehaviour
    {
        private static BloomFeedback instance;
        private AudioSource music, effects;
        private AudioClip plant, impact;
#if UNITY_IOS && !UNITY_EDITOR
        private float nextImpact;

        [DllImport("__Internal")]
        private static extern void BloomSeedImpact();
#endif

        internal static float SoundVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(BloomPreferences.Sound, 1));
        internal static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(BloomPreferences.Music, 1));
        internal static bool VibrationEnabled => PlayerPrefs.GetInt(BloomPreferences.Vibration, 1) != 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance != null)
                return;

            var root = new GameObject("Bloom audio");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<BloomFeedback>();
            instance.music = root.AddComponent<AudioSource>();
            instance.effects = root.AddComponent<AudioSource>();
            instance.music.playOnAwake = instance.effects.playOnAwake = false;
            instance.music.loop = true;
            instance.music.spatialBlend = instance.effects.spatialBlend = 0;
            instance.music.clip = Resources.Load<AudioClip>("BloomAudio/Greenhouse");
            instance.plant = Resources.Load<AudioClip>("BloomAudio/Plant");
            instance.impact = Resources.Load<AudioClip>("BloomAudio/CoreImpact");
            instance.effects.volume = SoundVolume;
            instance.UpdateMusic();
        }

        internal static void SetSound(float value)
        {
            PlayerPrefs.SetFloat(BloomPreferences.Sound, Mathf.Clamp01(value));
            if (instance != null)
                instance.effects.volume = SoundVolume;
        }

        internal static void SetMusic(float value)
        {
            PlayerPrefs.SetFloat(BloomPreferences.Music, Mathf.Clamp01(value));
            if (instance != null)
                instance.UpdateMusic();
        }

        private void UpdateMusic()
        {
            music.volume = MusicVolume;
            if (music.volume <= 0)
                music.Stop();
            else if (music.clip != null && !music.isPlaying)
                music.Play();
        }

        internal static void ToggleVibration() => SetVibration(!VibrationEnabled);

        internal static void SetVibration(bool enabled) => PlayerPrefs.SetInt(BloomPreferences.Vibration, enabled ? 1 : 0);

        internal static void Planted()
        {
            if (instance != null)
                instance.Play(instance.plant);
        }

        internal static void CoreHit()
        {
            if (instance == null)
                return;
            instance.Play(instance.impact);
#if UNITY_IOS && !UNITY_EDITOR
            if (VibrationEnabled && Time.unscaledTime >= instance.nextImpact)
            {
                instance.nextImpact = Time.unscaledTime + .14f;
                BloomSeedImpact();
            }
#endif
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && effects.volume > 0)
                effects.PlayOneShot(clip);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                PlayerPrefs.Save();
        }
    }
}
