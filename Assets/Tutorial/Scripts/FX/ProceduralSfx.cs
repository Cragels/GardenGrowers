using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenGrowers.FX
{
    /// <summary>Sounds the tutorial can make without any audio files.</summary>
    public enum Sfx
    {
        /// <summary>Two bright bell notes. Step complete.</summary>
        Chime,
        /// <summary>Four rising notes. Item received, big moments.</summary>
        Reward,
        /// <summary>Speech bubble or UI popping in.</summary>
        Pop,
        Clap,
        /// <summary>Gogo's stick tapping the ground.</summary>
        Tock,
        /// <summary>Time passing.</summary>
        Whoosh,
        /// <summary>A hint appearing.</summary>
        Tick,
        /// <summary>Landing after a hop.</summary>
        Thud
    }

    /// <summary>Makes the tutorial's sound effects in code, since the project has no sound files yet.</summary>
    public static class ProceduralSfx
    {
        private static readonly Dictionary<Sfx, AudioClip> Cache = new Dictionary<Sfx, AudioClip>();

        public static AudioClip Get(Sfx type)
        {
            if (Cache.TryGetValue(type, out AudioClip clip) && clip != null) return clip;
            clip = Build(type);
            Cache[type] = clip;
            return clip;
        }

        private static int SampleRate
        {
            get
            {
                int rate = AudioSettings.outputSampleRate;
                return rate > 0 ? rate : 44100;
            }
        }

        private static AudioClip Build(Sfx type)
        {
            int sr = SampleRate;
            float[] data;
            switch (type)
            {
                case Sfx.Chime:
                    data = Bells(sr, 0.9f, 0.32f, new[] { 1318.5f, 1975.5f }, new[] { 0f, 0.11f });
                    break;
                case Sfx.Reward:
                    data = Bells(sr, 1.1f, 0.3f, new[] { 1046.5f, 1318.5f, 1568f, 2093f }, new[] { 0f, 0.07f, 0.14f, 0.21f });
                    break;
                case Sfx.Pop:
                    data = Sweep(sr, 0.08f, 520f, 1150f, 0.03f);
                    break;
                case Sfx.Clap:
                    data = Clap(sr);
                    break;
                case Sfx.Tock:
                    data = Tock(sr);
                    break;
                case Sfx.Whoosh:
                    data = Whoosh(sr, 0.9f);
                    break;
                case Sfx.Tick:
                    data = Sweep(sr, 0.03f, 2100f, 2100f, 0.008f);
                    break;
                default:
                    data = Sweep(sr, 0.22f, 130f, 65f, 0.07f);
                    break;
            }

            Normalise(data, 0.8f);
            AudioClip clip = AudioClip.Create("Sfx_" + type, data.Length, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float[] Bells(int sr, float length, float decay, float[] notes, float[] starts)
        {
            var data = new float[Mathf.CeilToInt(length * sr)];
            float[] partials = { 1f, 2f, 3.01f };
            float[] partialGain = { 1f, 0.3f, 0.12f };

            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.FloorToInt(starts[n] * sr);
                for (int i = start; i < data.Length; i++)
                {
                    float t = (i - start) / (float)sr;
                    float envelope = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t / decay);
                    float sample = 0f;
                    for (int p = 0; p < partials.Length; p++)
                    {
                        sample += partialGain[p] * Mathf.Sin(2f * Mathf.PI * notes[n] * partials[p] * t) * Mathf.Exp(-t * p * 3f);
                    }
                    data[i] += sample * envelope;
                }
            }

            return data;
        }

        private static float[] Sweep(int sr, float length, float fromHz, float toHz, float decay)
        {
            var data = new float[Mathf.CeilToInt(length * sr)];
            double phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sr;
                float frequency = Mathf.Lerp(fromHz, toHz, t / length);
                phase += 2.0 * Math.PI * frequency / sr;
                float envelope = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t / decay);
                data[i] = (float)Math.Sin(phase) * envelope;
            }
            return data;
        }

        private static float[] Clap(int sr)
        {
            var data = new float[Mathf.CeilToInt(0.16f * sr)];
            var random = new System.Random(7);
            float[] bursts = { 0f, 0.008f, 0.016f };
            float previous = 0f;

            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sr;
                float envelope = 0f;
                foreach (float b in bursts)
                {
                    if (t >= b) envelope += Mathf.Exp(-(t - b) / (b > 0.01f ? 0.03f : 0.006f)) * (b > 0.01f ? 1f : 0.6f);
                }

                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float bright = noise - previous; // simple high-pass
                previous = noise;
                data[i] = bright * envelope;
            }
            return data;
        }

        private static float[] Tock(int sr)
        {
            var data = new float[Mathf.CeilToInt(0.1f * sr)];
            var random = new System.Random(3);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sr;
                float body = Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 1850f * t);
                float click = t < 0.002f ? (float)(random.NextDouble() * 2.0 - 1.0) : 0f;
                data[i] = (body * Mathf.Exp(-t / 0.022f)) + click * 0.5f;
            }
            return data;
        }

        private static float[] Whoosh(int sr, float length)
        {
            var data = new float[Mathf.CeilToInt(length * sr)];
            var random = new System.Random(11);
            float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sr;
                float u = t / length;
                float cutoff = Mathf.Lerp(250f, 3200f, Mathf.Sin(u * Mathf.PI));
                float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / sr);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                low += alpha * (noise - low);
                data[i] = low * Mathf.Sin(u * Mathf.PI);
            }
            return data;
        }

        private static void Normalise(float[] data, float peakTarget)
        {
            float peak = 0f;
            foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            if (peak <= 0f) return;
            float gain = peakTarget / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= gain;
        }
    }
}
