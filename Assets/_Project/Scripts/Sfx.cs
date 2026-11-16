using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReCraft
{
    /// <summary>절차적으로 합성한 효과음. 외부 오디오 에셋 없이 손맛(ASMR)을 낸다.</summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx I;
        const int Rate = 44100;
        readonly Dictionary<string, AudioClip> clips = new();
        readonly Dictionary<string, AudioSource> loops = new();
        readonly Dictionary<string, float> loopTarget = new();
        AudioSource one;
        System.Random rng = new(7);

        public static void Init(GameObject host)
        {
            I = host.AddComponent<Sfx>();
            I.one = host.AddComponent<AudioSource>();
            I.one.playOnAwake = false;
            I.Build();
        }

        float Noise() => (float)rng.NextDouble() * 2f - 1f;

        AudioClip Make(string name, float dur, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            clips[name] = clip;
            return clip;
        }

        void MakeLoop(string name, float dur, Func<float, float> f)
        {
            var clip = Make(name, dur, f);
            // 이음매 클릭 제거: 끝을 처음과 교차 페이드
            int n = clip.samples, fade = Rate / 20;
            var d = new float[n];
            clip.GetData(d, 0);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                d[n - fade + i] = d[n - fade + i] * (1f - k) + d[i] * k;
            }
            clip.SetData(d, 0);
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip; src.loop = true; src.volume = 0f; src.playOnAwake = false;
            loops[name] = src;
            loopTarget[name] = 0f;
        }

        static float Env(float t, float decay) => Mathf.Exp(-t * decay);
        static float Sin(float t, float hz) => Mathf.Sin(t * hz * Mathf.PI * 2f);

        void Build()
        {
            float lp = 0f;
            Make("tick", 0.05f, t => (Noise() * 0.6f + Sin(t, 2400) * 0.4f) * Env(t, 90));
            Make("click", 0.06f, t => (Noise() * 0.3f + Sin(t, 1200) * 0.5f) * Env(t, 70));
            Make("pop", 0.16f, t => Sin(t, 300 - t * 900) * Env(t, 22) * 0.8f + Noise() * Env(t, 60) * 0.25f);
            Make("snap", 0.22f, t => Sin(t, 110) * Env(t, 26) * 0.9f + (Noise() * 0.5f + Sin(t, 1800) * 0.3f) * Env(t, 80) + (t > 0.05f ? Sin(t, 900) * Env(t - 0.05f, 60) * 0.3f : 0f));
            Make("drop", 0.12f, t => (Sin(t, 3200) * 0.4f + Sin(t, 5100) * 0.3f) * Env(t, 45));
            Make("error", 0.22f, t => Mathf.Sign(Sin(t, 130)) * 0.25f * Env(t, 8));
            Make("cash", 0.7f, t => (Sin(t, 1318) * Env(t, 7) + (t > 0.09f ? Sin(t, 1760) * Env(t - 0.09f, 6) : 0f) + (t > 0.18f ? Sin(t, 2637) * Env(t - 0.18f, 5) : 0f)) * 0.3f);
            Make("chime", 0.8f, t => (Sin(t, 880) + Sin(t, 1320) * 0.6f + Sin(t, 1760) * 0.3f) * Env(t, 5) * 0.28f);
            Make("sparkle", 0.5f, t => (Sin(t, 2093) * Env(t, 9) + (t > 0.07f ? Sin(t, 3136) * Env(t - 0.07f, 9) : 0f)) * 0.22f);
            Make("whoosh", 0.35f, t => { lp += (Noise() - lp) * 0.08f; return lp * Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI) * 1.6f; });
            Make("bell", 2.2f, t => Mathf.Sign(Sin(t, 22)) > 0 ? (Sin(t, 1900) * 0.5f + Sin(t, 2850) * 0.3f + Sin(t, 4100) * 0.2f) * 0.35f * (1f - t / 2.4f) : 0f);
            Make("beep", 1.6f, t =>
            {
                float[] notes = { 659, 784, 988, 1319, 988, 1319, 1568, 2093 };
                int k = Mathf.Min(notes.Length - 1, (int)(t / 0.18f));
                float lt = t - k * 0.18f;
                return Mathf.Sign(Sin(t, notes[k])) * 0.14f * Env(lt, 6);
            });
            Make("clunk", 0.9f, t => Sin(t, 90) * Env(t, 14) * 0.9f + Noise() * Env(t, 50) * 0.4f + (t > 0.55f ? (Sin(t, 1400) * 0.4f + Noise() * 0.3f) * Env(t - 0.55f, 30) : 0f));
            Make("melody", 9.5f, Melody);

            lp = 0f;
            float lp2 = 0f;
            MakeLoop("sand", 1.2f, t => { lp += (Noise() - lp) * 0.35f; return (lp - (lp2 += (lp - lp2) * 0.02f)) * (0.75f + 0.25f * Sin(t, 9)) * 0.9f; });
            MakeLoop("cloth", 1.2f, t => { lp += (Noise() - lp) * 0.06f; return lp * (0.6f + 0.4f * Sin(t, 3.3f)) * 1.4f; });
            MakeLoop("spray", 1.0f, t => { float nz = Noise(); lp += (nz - lp) * 0.5f; return (nz - lp) * 0.55f; });
            MakeLoop("drill", 0.8f, t => (Mathf.Repeat(t * 170f, 1f) - 0.5f) * 0.35f + Sin(t, 340) * 0.12f + Noise() * 0.08f);
            MakeLoop("hum", 1.0f, t => Sin(t, 100) * 0.25f + Sin(t, 200) * 0.1f + Sin(t, 50) * 0.2f);
            MakeLoop("static", 1.0f, t => { lp += (Noise() - lp) * 0.3f; return lp * 0.25f + Sin(t, 440 + 30 * Sin(t, 5)) * 0.08f; });
            MakeLoop("ultra", 1.0f, t => Sin(t, 3100 + 200 * Sin(t, 12)) * 0.06f + Sin(t, 60) * 0.2f + Noise() * 0.04f);
        }

        // 엘리제를 위하여 (퍼블릭 도메인) 도입부를 오르골 음색으로
        static float Melody(float t)
        {
            float[] hz = { 659.3f, 622.3f, 659.3f, 622.3f, 659.3f, 493.9f, 587.3f, 523.3f, 440f, 261.6f, 329.6f, 440f, 493.9f, 329.6f, 415.3f, 493.9f, 523.3f, 329.6f, 659.3f, 622.3f, 659.3f, 622.3f, 659.3f, 493.9f, 587.3f, 523.3f, 440f, 261.6f, 329.6f, 440f, 493.9f, 329.6f, 523.3f, 493.9f, 440f };
            float[] len = { 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1, 3 };
            const float unit = 0.21f;
            float s = 0f, start = 0f;
            for (int i = 0; i < hz.Length; i++)
            {
                float lt = t - start;
                if (lt >= 0f && lt < 2.5f)
                    s += (Mathf.Sin(lt * hz[i] * 2f * Mathf.PI * 2f) + 0.35f * Mathf.Sin(lt * hz[i] * 4f * Mathf.PI * 2f)) * Mathf.Exp(-lt * 2.6f) * 0.2f;
                start += len[i] * unit;
            }
            return s;
        }

        public static void Play(string name, float volume = 1f, float pitch = 1f)
        {
            if (!I || !I.clips.TryGetValue(name, out var c)) return;
            I.one.pitch = pitch;
            I.one.PlayOneShot(c, volume);
        }

        /// <summary>루프 사운드의 목표 음량을 정한다 (0이면 서서히 꺼짐).</summary>
        public static void Loop(string name, float volume, float pitch = 1f)
        {
            if (!I || !I.loops.TryGetValue(name, out var src)) return;
            I.loopTarget[name] = volume;
            src.pitch = pitch;
        }

        void Update()
        {
            foreach (var kv in loops)
            {
                var src = kv.Value;
                float target = loopTarget[kv.Key];
                src.volume = Mathf.MoveTowards(src.volume, target, Time.unscaledDeltaTime * 6f);
                if (src.volume > 0.001f && !src.isPlaying) src.Play();
                else if (src.volume <= 0.001f && src.isPlaying && target <= 0f) src.Stop();
            }
        }

        public static void StopLoops()
        {
            if (!I) return;
            var keys = new List<string>(I.loopTarget.Keys);
            foreach (var k in keys) I.loopTarget[k] = 0f;
        }
    }
}
