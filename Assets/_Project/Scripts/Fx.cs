using UnityEngine;

namespace ReCraft
{
    /// <summary>파티클 · 카메라 흔들림 (Juice).</summary>
    public class Fx : MonoBehaviour
    {
        static Fx I;
        ParticleSystem dust, mist, spark;
        float shakeAmp, shakeTime, shakeDur;
        public static Vector3 ShakeOffset { get; private set; }

        public static void Init(GameObject host)
        {
            I = host.AddComponent<Fx>();
            I.dust = I.MakeSystem("DustFx", false, 0.5f, 0.006f, 0.25f, 0.6f);
            I.mist = I.MakeSystem("MistFx", false, 0.35f, 0.012f, 0.15f, -0.05f);
            I.spark = I.MakeSystem("SparkFx", true, 0.6f, 0.007f, 0.3f, 0.15f);
        }

        ParticleSystem MakeSystem(string name, bool additive, float life, float size, float speed, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.004f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mat.Particle(additive);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Color c, int count, float spread)
        {
            if (!ps) return;
            var shape = ps.shape;
            shape.radius = spread;
            var p = new ParticleSystem.EmitParams { position = pos, startColor = c, applyShapeToPosition = true };
            ps.Emit(p, count);
        }

        public static void Dust(Vector3 pos, Color c, int count = 4) { if (I) Emit(I.dust, pos, c, count, 0.006f); }
        public static void Mist(Vector3 pos, Color c, int count = 4) { if (I) Emit(I.mist, pos, c, count, 0.012f); }
        public static void Spark(Vector3 pos, Color c, int count = 14, float spread = 0.02f) { if (I) Emit(I.spark, pos, c, count, spread); }

        public static void Shake(float amp, float dur = 0.18f)
        {
            if (!I) return;
            I.shakeAmp = Mathf.Max(I.shakeAmp, amp);
            I.shakeDur = dur;
            I.shakeTime = dur;
        }

        void Update()
        {
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDur);
                ShakeOffset = Random.insideUnitSphere * shakeAmp * k;
                if (shakeTime <= 0f) { ShakeOffset = Vector3.zero; shakeAmp = 0f; }
            }
        }
    }
}
