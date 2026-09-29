using System;
using UnityEngine;

namespace GardenGrowers.Dialogue
{
    /// <summary>Makes Animal Crossing style vowel blips in code, so no audio files are needed.</summary>
    public static class BabbleSynth
    {
        public const float BaseFrequency = 245f;
        public const float Duration = 0.095f;

        // The 3 formants (Hz) that make a, e, i, o, u sound different. Softened so nothing is shrill.
        private static readonly Vector3[] Formants =
        {
            new Vector3(850f, 1220f, 2810f),
            new Vector3(610f, 1900f, 2600f),
            new Vector3(380f, 2250f, 2950f),
            new Vector3(560f, 900f, 2600f),
            new Vector3(420f, 860f, 2500f)
        };

        private static readonly Vector3 Bandwidths = new Vector3(110f, 140f, 200f);

        private static AudioClip[] cached;

        /// <summary>The five vowel clips (a, e, i, o, u). Built once, then cached.</summary>
        public static AudioClip[] VowelClips
        {
            get
            {
                if (cached == null || cached.Length == 0 || cached[0] == null) cached = BuildAll();
                return cached;
            }
        }

        /// <summary>Picks a vowel for any letter, always the same one, so words sound steady.</summary>
        public static int VowelIndexFor(char letter)
        {
            switch (char.ToLowerInvariant(letter))
            {
                case 'a': return 0;
                case 'e': return 1;
                case 'i':
                case 'y': return 2;
                case 'o': return 3;
                case 'u':
                case 'w': return 4;
                default: return Mathf.Abs(char.ToLowerInvariant(letter) * 7) % Formants.Length;
            }
        }

        private static AudioClip[] BuildAll()
        {
            int sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate <= 0) sampleRate = 44100;

            var clips = new AudioClip[Formants.Length];
            for (int v = 0; v < Formants.Length; v++) clips[v] = BuildVowel(v, sampleRate);
            return clips;
        }

        private static AudioClip BuildVowel(int vowel, int sampleRate)
        {
            int count = Mathf.CeilToInt(Duration * sampleRate);
            var data = new float[count];

            Vector3 formant = Formants[vowel];
            int harmonics = Mathf.Max(1, Mathf.FloorToInt(5000f / BaseFrequency));
            var amplitudes = new float[harmonics + 1];
            for (int k = 1; k <= harmonics; k++)
            {
                float frequency = k * BaseFrequency;
                float amplitude = Resonance(frequency, formant.x, Bandwidths.x)
                                  + 0.6f * Resonance(frequency, formant.y, Bandwidths.y)
                                  + 0.3f * Resonance(frequency, formant.z, Bandwidths.z);
                amplitudes[k] = amplitude / Mathf.Sqrt(k);
            }

            var phases = new double[harmonics + 1];
            var random = new System.Random(1234 + vowel);
            const float attack = 0.006f;
            const float release = 0.035f;
            float peak = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;

                // A quick upward chirp at the start makes it sound chatty and cute.
                float glide = 1f + 0.07f * Mathf.Exp(-t * 45f) - 0.04f * (t / Duration);
                double step = 2.0 * Math.PI * BaseFrequency * glide / sampleRate;

                float sample = 0f;
                for (int k = 1; k <= harmonics; k++)
                {
                    phases[k] += step * k;
                    sample += amplitudes[k] * (float)Math.Sin(phases[k]);
                }

                // A tiny breathy "consonant" at the start of each blip.
                if (t < 0.008f) sample += ((float)random.NextDouble() * 2f - 1f) * 0.25f * (1f - t / 0.008f);

                float envelope = Mathf.Min(1f, t / attack)
                                 * Mathf.Clamp01((Duration - t) / release)
                                 * (1f - 0.25f * t / Duration);

                data[i] = sample * envelope;
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            float gain = peak > 0f ? 0.85f / peak : 1f;
            for (int i = 0; i < count; i++) data[i] = (float)Math.Tanh(data[i] * gain * 1.2f) * 0.9f;

            AudioClip clip = AudioClip.Create("Babble_" + "aeiou"[vowel], count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Resonance(float frequency, float centre, float bandwidth)
        {
            float x = (frequency - centre) / bandwidth;
            return 1f / (1f + x * x);
        }
    }
}
