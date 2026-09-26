using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.Presentation
{
    /// <summary>Gentle breathing scale for a call-to-action.</summary>
    public sealed class Pulse : MonoBehaviour
    {
        public float Amount = 0.035f;
        public float Speed = 3.2f;

        private void Update()
        {
            float k = 1f + Mathf.Sin(Time.unscaledTime * Speed) * Amount;
            transform.localScale = new Vector3(k, k, 1f);
        }
    }

    /// <summary>Overshooting scale-in after a delay.</summary>
    public sealed class PopIn : MonoBehaviour
    {
        public float Delay;
        public float Seconds = 0.35f;
        private float _t;

        private void OnEnable()
        {
            _t = 0f;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01((_t - Delay) / Seconds);
            float s = k <= 0f ? 0f : 1f + 2.7f * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f);
            transform.localScale = new Vector3(s, s, 1f);
            if (k >= 1f) enabled = false;
        }
    }

    /// <summary>Falling, tumbling UI confetti; destroys itself when every piece has left the screen.</summary>
    public sealed class Confetti : MonoBehaviour
    {
        private RectTransform[] _pieces;
        private Vector2[] _velocity;
        private float[] _spin;
        private float _height;

        public static void Burst(RectTransform parent, int count)
        {
            var go = new GameObject("Confetti", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            UiKit.Stretch(rt);
            go.AddComponent<Confetti>().Spawn(rt, count);
        }

        private void Spawn(RectTransform root, int count)
        {
            var size = root.rect.size;
            if (size.x <= 0f) size = new Vector2(1080f, 1920f);
            _height = size.y;
            _pieces = new RectTransform[count];
            _velocity = new Vector2[count];
            _spin = new float[count];
            for (int i = 0; i < count; i++)
            {
                var p = new GameObject("Piece", typeof(RectTransform), typeof(Image));
                p.transform.SetParent(root, false);
                var img = p.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = Palette.Screw[i % Palette.Screw.Length];
                if (i % 4 == 0) img.sprite = UiKit.Star();
                var r = (RectTransform)p.transform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = i % 4 == 0 ? new Vector2(44f, 44f) : new Vector2(Random.Range(18f, 28f), Random.Range(30f, 46f));
                r.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * size.x, size.y * 0.5f + Random.Range(20f, 700f));
                r.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                _pieces[i] = r;
                _velocity[i] = new Vector2(Random.Range(-90f, 90f), -Random.Range(380f, 720f));
                _spin[i] = Random.Range(-360f, 360f);
            }
        }

        private void Update()
        {
            if (_pieces == null) return;
            float dt = Time.unscaledDeltaTime;
            bool alive = false;
            for (int i = 0; i < _pieces.Length; i++)
            {
                var r = _pieces[i];
                var pos = r.anchoredPosition + _velocity[i] * dt;
                pos.x += Mathf.Sin(Time.unscaledTime * 3f + i) * 40f * dt;
                r.anchoredPosition = pos;
                r.Rotate(0f, 0f, _spin[i] * dt);
                float flip = Mathf.Abs(Mathf.Cos(Time.unscaledTime * 5f + i * 0.7f));
                r.localScale = new Vector3(1f, 0.35f + 0.65f * flip, 1f);
                if (pos.y > -_height * 0.5f - 80f) alive = true;
            }
            if (!alive) Destroy(gameObject);
        }
    }
}
