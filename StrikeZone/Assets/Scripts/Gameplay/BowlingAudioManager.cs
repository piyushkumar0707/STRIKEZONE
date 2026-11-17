using StrikeZone.Core;
using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Plays bowling feedback sounds and supplies small synthesized placeholders.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BowlingAudioManager : MonoBehaviour
    {
        [SerializeField] private BowlingGameManager _gameManager;
        [SerializeField] private BowlingController _bowlingController;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _powerLockClip;
        [SerializeField] private AudioClip _ballReleaseClip;
        [SerializeField] private AudioClip _pinHitClip;
        [SerializeField] private AudioClip _strikeClip;
        [SerializeField] private AudioClip _spareClip;

        private BowlingPin[] _pins;
        private float _lastPinSoundTime;

        private void Awake()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            if (_gameManager == null) _gameManager = FindAnyObjectByType<BowlingGameManager>();
            if (_bowlingController == null) _bowlingController = FindAnyObjectByType<BowlingController>();
            _pins = FindObjectsByType<BowlingPin>(FindObjectsInactive.Include);

            if (_powerLockClip == null) _powerLockClip = MakeTone("GeneratedPowerLock", .08f, 1050f, 640f, 10f);
            if (_ballReleaseClip == null) _ballReleaseClip = MakeWhoosh("GeneratedBallRelease", .3f);
            if (_pinHitClip == null) _pinHitClip = MakeImpact("GeneratedPinHit", .17f);
            if (_strikeClip == null) _strikeClip = MakeStinger("GeneratedStrike", true);
            if (_spareClip == null) _spareClip = MakeStinger("GeneratedSpare", false);
        }

        private void OnEnable()
        {
            if (_gameManager != null) _gameManager.OnRollResolved += HandleRollResolved;
            if (_bowlingController != null)
            {
                _bowlingController.OnPowerLocked += HandlePowerLocked;
                _bowlingController.OnBallReleased += HandleBallReleased;
            }

            if (_pins != null)
                foreach (var pin in _pins)
                    if (pin != null) pin.OnImpact += HandlePinImpact;
        }

        private void OnDisable()
        {
            if (_gameManager != null) _gameManager.OnRollResolved -= HandleRollResolved;
            if (_bowlingController != null)
            {
                _bowlingController.OnPowerLocked -= HandlePowerLocked;
                _bowlingController.OnBallReleased -= HandleBallReleased;
            }

            if (_pins != null)
                foreach (var pin in _pins)
                    if (pin != null) pin.OnImpact -= HandlePinImpact;
        }

        private void HandlePowerLocked(float power) => Play(_powerLockClip, .45f + power * .35f);
        private void HandleBallReleased() => Play(_ballReleaseClip, .7f);

        private void HandleRollResolved(int frame, int roll, int pins, StrikeZone.Logic.ScoreSnapshot snapshot)
        {
            if (snapshot == null || frame < 1 || frame > snapshot.Frames.Length) return;
            var score = snapshot.Frames[frame - 1];
            if (score.IsStrike && roll == 1) Play(_strikeClip, .85f);
            else if (score.IsSpare && roll == 2) Play(_spareClip, .75f);
        }

        private void HandlePinImpact(float impactSpeed)
        {
            if (Time.unscaledTime - _lastPinSoundTime < .08f || impactSpeed < 1.1f) return;
            _lastPinSoundTime = Time.unscaledTime;
            Play(_pinHitClip, Mathf.Clamp01(impactSpeed / 9f));
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && _audioSource != null) _audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static AudioClip MakeTone(string name, float duration, float startFrequency, float endFrequency, float decay)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var progress = t / duration;
                var frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                var envelope = Mathf.Exp(-decay * t) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * .3f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip MakeImpact(string name, float duration)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-27f * t);
                var low = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(230f, 105f, t / duration) * t);
                var clack = Mathf.Sin(2f * Mathf.PI * 940f * t) * Mathf.Exp(-45f * t);
                var noise = (Mathf.PerlinNoise(i * .071f, .37f) * 2f - 1f) * Mathf.Exp(-38f * t);
                samples[i] = (low * .52f + clack * .2f + noise * .28f) * envelope * .42f;
            }

            return CreateClip(name, samples, sampleRate);
        }

        private static AudioClip MakeWhoosh(string name, float duration)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var phase = Mathf.Clamp01(t / duration);
                var envelope = Mathf.Sin(Mathf.PI * phase) * Mathf.Exp(-2.3f * phase);
                var rumble = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(175f, 62f, phase) * t);
                var air = Mathf.PerlinNoise(i * .034f, .71f) * 2f - 1f;
                samples[i] = (rumble * .56f + air * .22f) * envelope * .32f;
            }

            return CreateClip(name, samples, sampleRate);
        }

        private static AudioClip MakeStinger(string name, bool strike)
        {
            const int sampleRate = 22050;
            var duration = strike ? .78f : .52f;
            var notes = strike ? new[] { 587f, 740f, 880f, 1175f } : new[] { 494f, 659f, 784f };
            var step = duration / notes.Length;
            var sampleCount = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / step));
                var noteTime = t - noteIndex * step;
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(noteTime / step)) * Mathf.Exp(-2.4f * noteTime);
                var fundamental = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * noteTime);
                var harmonic = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 2f * noteTime) * .22f;
                samples[i] = (fundamental * .72f + harmonic) * envelope * .26f;
            }

            return CreateClip(name, samples, sampleRate);
        }

        private static AudioClip CreateClip(string name, float[] samples, int sampleRate)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
