using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    public enum SFXType
    {
        PlayerShoot,
        PlayerDamage,
        BossPhaseStart,
        Win,
        Lose,
        ObstacleDestroy,
        RocketFire,
        RocketImpact,
        BoomerangFire
    }

    [Serializable]
    public struct SFXClipMapping
    {
        public                 SFXType   type;
        public                 AudioClip clip;
        [Range(0f, 1f)] public float     volume;
        public                 bool      useRandomPitch;
    }

    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("SFX Configuration")]
        [SerializeField] private List<SFXClipMapping> sfxMappings = new List<SFXClipMapping>();

        private readonly Dictionary<SFXType, SFXClipMapping> _sfxLookup = new Dictionary<SFXType, SFXClipMapping>();

        private void Awake()
        {
            if(sfxAudioSource == null)
                sfxAudioSource = GetComponent<AudioSource>();

            _sfxLookup.Clear();
            foreach(var mapping in sfxMappings)
            {
                if(mapping.clip != null)
                    _sfxLookup.TryAdd(mapping.type, mapping);
            }
        }

        public void PlaySFX(SFXType type)
        {
            if(!_sfxLookup.TryGetValue(type, out var mapping) || mapping.clip == null) return;

            sfxAudioSource.pitch = mapping.useRandomPitch ? UnityEngine.Random.Range(0.92f, 1.08f) : 1f;

            float vol = mapping.volume > 0f ? mapping.volume : 1f;
            sfxAudioSource.PlayOneShot(mapping.clip, vol);
        }

        public void PlayShoot()           => PlaySFX(SFXType.PlayerShoot);
        public void PlayDamage()          => PlaySFX(SFXType.PlayerDamage);
        public void PlayBossPhaseStart()  => PlaySFX(SFXType.BossPhaseStart);
        public void PlayWin()             => PlaySFX(SFXType.Win);
        public void PlayLose()            => PlaySFX(SFXType.Lose);
        public void PlayObstacleDestroy() => PlaySFX(SFXType.ObstacleDestroy);
        public void PlayRocketFire()      => PlaySFX(SFXType.RocketFire);
        public void PlayRocketImpact()    => PlaySFX(SFXType.RocketImpact);
        public void PlayBoomerangFire()   => PlaySFX(SFXType.BoomerangFire);
    }
}
