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
            // 회중시계: 태엽 감는 소리 → 초당 5번 째깍 (틱/톡 음높이 교대)
            Make("ticking", 3.2f, t =>
            {
                float s = 0f;
                if (t < 0.5f) s += (Mathf.Repeat(t * 30f, 1f) < 0.15f ? Noise() * 0.25f : 0f) * (1f - t / 0.5f);
                float tt = t - 0.5f;
                if (tt >= 0f)
                {
                    int k = (int)(tt * 5f);
                    float lt = tt - k / 5f;
                    s += (Sin(lt, k % 2 == 0 ? 4200 : 3300) * 0.35f + Noise() * 0.15f) * Env(lt, 160);
                }
                return s;
            });
            // 로봇 팔: 서보 모터 윙— 소리가 오르내리다 확인 비프 두 번
            Make("robot", 3.2f, t =>
            {
                float s = 0f;
                if (t < 2.6f)
                {
                    float hz = 320f + 140f * Mathf.Sin(t * 2.4f);
                    float amp = Mathf.Clamp01(t * 4f) * Mathf.Clamp01((2.6f - t) * 4f);
                    s += (Mathf.Repeat(t * hz, 1f) - 0.5f) * 0.18f * amp + Sin(t, hz * 2f) * 0.05f * amp;
                }
                foreach (float b in new[] { 2.65f, 2.85f })
                {
                    float bt = t - b;
                    if (bt >= 0f && bt < 0.12f) s += Mathf.Sign(Sin(bt, 1760)) * 0.12f;
                }
                return s;
            });
            // 타자기: 불규칙한 타닥 소리 → 끝에 '띵!' + 캐리지가 돌아가는 소리
            var hits = new float[] { 0.05f, 0.14f, 0.21f, 0.33f, 0.4f, 0.47f, 0.6f, 0.68f, 0.77f, 0.85f, 0.98f, 1.06f, 1.13f, 1.25f, 1.33f, 1.4f };
            Make("typing", 2.4f, t =>
            {
                float s = 0f;
                foreach (float h in hits)
                {
                    float lt = t - h;
                    if (lt >= 0f && lt < 0.06f) s += (Noise() * 0.55f + Sin(lt, 1500) * 0.35f + Sin(lt, 180) * 0.4f) * Env(lt, 70);
                }
                float bt = t - 1.55f;
                if (bt >= 0f) s += (Sin(bt, 2350) * 0.5f + Sin(bt, 3520) * 0.25f) * Env(bt, 4) * 0.6f;
                float ct = t - 1.75f;
                if (ct >= 0f && ct < 0.45f) s += Noise() * 0.18f * Mathf.Sin(ct / 0.45f * Mathf.PI) + Mathf.Sign(Sin(ct, 40)) * 0.05f;
                return s;
            });
            // LP: 지직거리는 잡음 위로 부드러운 화음 진행
            Make("record", 4.2f, t =>
            {
                float crackle = Noise() * 0.035f + (rng.NextDouble() < 0.0012 ? Noise() * 0.5f : 0f);
                float[][] chords = { new[] { 261.6f, 329.6f, 392f }, new[] { 220f, 261.6f, 329.6f }, new[] { 174.6f, 220f, 261.6f }, new[] { 196f, 246.9f, 293.7f } };
                float fade = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((4.2f - t) / 0.5f);
                int k = Mathf.Min(3, (int)(t / 1.05f));
                float lt = t - k * 1.05f;
                float m = 0f;
                foreach (float hz in chords[k]) m += Sin(t, hz) * 0.07f + Sin(t, hz * 2f) * 0.02f;
                m *= 0.6f + 0.4f * Env(lt, 2.5f);
                float mel = Sin(t, chords[k][2] * 2f) * 0.06f * Env(Mathf.Repeat(t, 0.35f), 6f);
                return (m + mel) * fade + crackle;
            });
            Make("arcade", 3.0f, t =>
            {
                // 오락실 칩튠: 코인 효과음 → 짧은 아르페지오 팡파르
                float[] notes = { 988, 1319, 0, 523, 659, 784, 1047, 784, 1047, 1319, 1568, 2093 };
                float step = t < 0.24f ? 0.12f : 0.2f;
                int k = t < 0.24f ? (int)(t / 0.12f) : 2 + (int)((t - 0.24f) / 0.2f);
                if (k >= notes.Length || notes[k] <= 0f) return 0f;
                float lt = t < 0.24f ? t - k * 0.12f : t - 0.24f - (k - 2) * 0.2f;
                float sq = Mathf.Sign(Sin(t, notes[k]));
                float tri = Mathf.PingPong(t * notes[k] * 0.5f * 4f, 2f) - 1f;
                return (sq * 0.12f + tri * 0.06f) * Env(lt, 5f) * (lt < step ? 1f : 0f);
            });

            lp = 0f;
            float lp2 = 0f;
            MakeLoop("sand", 1.2f, t => { lp += (Noise() - lp) * 0.35f; return (lp - (lp2 += (lp - lp2) * 0.02f)) * (0.75f + 0.25f * Sin(t, 9)) * 0.9f; });
            MakeLoop("cloth", 1.2f, t => { lp += (Noise() - lp) * 0.06f; return lp * (0.6f + 0.4f * Sin(t, 3.3f)) * 1.4f; });
            MakeLoop("spray", 1.0f, t => { float nz = Noise(); lp += (nz - lp) * 0.5f; return (nz - lp) * 0.55f; });
            MakeLoop("drill", 0.8f, t => (Mathf.Repeat(t * 170f, 1f) - 0.5f) * 0.35f + Sin(t, 340) * 0.12f + Noise() * 0.08f);
            // 단기통 엔진 공회전: 초당 11번 '툴' 하는 폭발음 + 배기 잡음
            MakeLoop("engine", 1.0f, t =>
            {
                float ph = Mathf.Repeat(t * 11f, 1f) / 11f;
                return (Sin(ph, 52) * 0.7f + Sin(ph, 104) * 0.25f + Noise() * 0.25f) * Env(ph, 38) + Sin(t, 33) * 0.08f;
            });
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
