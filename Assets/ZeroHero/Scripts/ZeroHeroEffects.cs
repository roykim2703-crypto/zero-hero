using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        void AddEffect(Sprite sprite, Vector2 pos, Color color, float life, float scale, Vector2 velocity, float grow)
        {
            if (effects.Count > 450) return;
            var sr = Sprite("Particle", sprite, color, pos, Vector2.one * scale, 28);
            effects.Add(new Effect { sprite = sr, velocity = velocity, life = life, maxLife = life, scale = scale, grow = grow });
        }
        void Burst(Vector2 pos, Color color, int amount)
        {
            for (int i = 0; i < amount; i++) AddEffect(art.square, pos, color, Range(.2f, .6f), Range(.05f, .12f), new Vector2(Range(-3, 3), Range(-3, 3)), -.08f);
        }
        void TickEffects(float dt)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var e = effects[i]; e.life -= dt;
                if (e.life <= 0) { Destroy(e.sprite.gameObject); effects.RemoveAt(i); continue; }
                e.sprite.transform.position += (Vector3)(e.velocity * dt); e.scale += e.grow * dt;
                e.sprite.transform.localScale = Vector3.one * Mathf.Max(.01f, e.scale);
                Color c = e.sprite.color; c.a = e.life / e.maxLife; e.sprite.color = c;
            }
            for (int i = floating.Count - 1; i >= 0; i--)
            { floating[i].life -= dt; floating[i].pos += Vector2.up * dt * .75f; if (floating[i].life <= 0) floating.RemoveAt(i); }
        }
        void MakeAudio()
        {
            int rate = 22050;
            float[] frequencies = { 660, 180, 65, 420, 880, 95, 1200 };
            for (int i = 0; i < frequencies.Length; i++)
            {
                float length = i == 2 ? .45f : i == 4 ? .4f : .11f;
                var samples = new float[(int)(rate * length)];
                for (int n = 0; n < samples.Length; n++)
                {
                    float t = (float)n / rate, envelope = Mathf.Pow(1 - t / length, 2);
                    float frequency = frequencies[i] * (i == 4 ? 1 + Mathf.Floor(t * 10) * .25f : 1 - t * 1.3f);
                    samples[n] = Mathf.Sin(t * frequency * Mathf.PI * 2) * envelope * .38f;
                    if (i == 2 || i == 5) samples[n] += Mathf.Sin(n * 73.123f) * envelope * .14f;
                }
                var clip = AudioClip.Create("Zero sound " + i, samples.Length, 1, rate, false); clip.SetData(samples, 0); clips.Add(clip);
            }
        }
        void PlaySound(int index) { if (!muted && audioSource != null) audioSource.PlayOneShot(clips[index]); }
    }
}
