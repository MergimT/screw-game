using UnityEngine;

namespace ScrewGame.Presentation
{
    /// <summary>Original procedurally synthesized cues, generated at startup (no third-party audio assets).</summary>
    public sealed class SoundBank : MonoBehaviour
    {
        public enum Cue { Unscrew, Complete, Error, Hint, Win }

        public bool Enabled = true;
        private AudioSource _src;
        private AudioClip[] _clips;

        private void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _clips = new[]
            {
                Tone("unscrew", new[] { 880f, 1320f }, 0.09f, 0.35f),
                Tone("complete", new[] { 660f, 880f, 1100f }, 0.22f, 0.4f),
                Tone("error", new[] { 180f, 150f }, 0.16f, 0.35f),
                Tone("hint", new[] { 990f, 1480f }, 0.18f, 0.3f),
                Tone("win", new[] { 523f, 659f, 784f, 1047f }, 0.6f, 0.45f),
            };
        }

        public void Play(Cue cue)
        {
            if (!Enabled || _src == null) return;
            _src.PlayOneShot(_clips[(int)cue]);
        }

        private static AudioClip Tone(string name, float[] freqs, float seconds, float gain)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(seconds * rate);
            var data = new float[n];
            int seg = n / freqs.Length;
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                int k = Mathf.Min(freqs.Length - 1, i / Mathf.Max(1, seg));
                phase += 2 * Mathf.PI * freqs[k] / rate;
                float t = (float)i / n;
                float env = Mathf.Min(1f, i / 200f) * (1f - t) * (1f - t);
                data[i] = gain * env * (float)(System.Math.Sin(phase) * 0.8 + System.Math.Sin(phase * 2) * 0.2);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
