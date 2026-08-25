using UnityEngine;

namespace CandyShop
{
    // Procedural placeholder SFX (spec section 11 allows shipping silent, but these tiny
    // synthesized tones make the 音效 toggle meaningful without any audio assets).
    public static class Sfx
    {
        private static AudioSource _source;
        private static AudioClip _pop;
        private static AudioClip _thud;
        private static AudioClip _ding;
        private static AudioClip _power;

        private static void Ensure()
        {
            if (_source != null) return;
            var go = new GameObject("SfxPlayer");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _pop = Tone("sfx_pop", 620f, 0.09f);
            _thud = Tone("sfx_thud", 150f, 0.16f);
            _ding = Tone("sfx_ding", 920f, 0.28f);
            _power = Sweep("sfx_power", 300f, 900f, 0.25f);
        }

        private static AudioClip Tone(string name, float freq, float duration)
        {
            int rate = 22050;
            int samples = Mathf.CeilToInt(rate * duration);
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-5f * t) * Mathf.Min(1f, t * 40f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * 0.35f * envelope;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Sweep(string name, float from, float to, float duration)
        {
            int rate = 22050;
            int samples = Mathf.CeilToInt(rate * duration);
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            var data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(from, to, t);
                phase += 2f * Mathf.PI * freq / rate;
                float envelope = Mathf.Exp(-3f * t);
                data[i] = Mathf.Sin(phase) * 0.3f * envelope;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static void Play(AudioClip clip)
        {
            if (clip == null) return;
            var save = SaveDataService.Current;
            if (save == null || !save.sfxEnabled) return;
            Ensure();
            _source.PlayOneShot(clip);
        }

        public static void Pop() => Play(_pop ?? LazyPop());
        public static void Thud() => Play(_thud ?? LazyThud());
        public static void Ding() => Play(_ding ?? LazyDing());
        public static void Power() => Play(_power ?? LazyPower());

        // Lazily build clips even if Play was called before Ensure ran.
        private static AudioClip LazyPop() { Ensure(); return _pop; }
        private static AudioClip LazyThud() { Ensure(); return _thud; }
        private static AudioClip LazyDing() { Ensure(); return _ding; }
        private static AudioClip LazyPower() { Ensure(); return _power; }
    }
}
