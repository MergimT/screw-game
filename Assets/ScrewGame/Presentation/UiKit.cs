using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.Presentation
{
    /// <summary>Minimal runtime uGUI/TextMeshPro builders so screens are code-owned and diffable.</summary>
    public static class UiKit
    {
        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = color.a > 0f;
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            return rt;
        }

        public static RectTransform Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = Palette.Ink;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            t.outlineWidth = 0.22f;
            t.outlineColor = Palette.Outline;
            Stretch((RectTransform)go.transform);
            return t;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color? color = null, float fontSize = 40f)
        {
            var go = new GameObject("Button " + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color ?? Palette.Secondary;
            Round(img);
            var lip = go.AddComponent<Shadow>();
            lip.effectColor = new Color(0f, 0f, 0f, 0.28f);
            lip.effectDistance = new Vector2(0f, -9f);
            var rim = go.AddComponent<Outline>();
            rim.effectColor = new Color(1f, 1f, 1f, 0.9f);
            rim.effectDistance = new Vector2(4f, -4f);
            var b = go.GetComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            b.colors = colors;
            b.onClick.AddListener(() => onClick());
            var label = Label(go.transform, text, fontSize);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.margin = new Vector4(12, 6, 12, 6);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 120f;
            le.preferredHeight = 120f;
            return b;
        }

        private static Sprite _rounded;

        /// <summary>Applies a generated 9-sliced rounded-rectangle sprite.</summary>
        public static void Round(Image img, float cornerScale = 1f)
        {
            if (_rounded == null)
            {
                const int size = 96, r = 40;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(r - x - 0.5f, x + 0.5f - (size - r)));
                    float dy = Mathf.Max(0f, Mathf.Max(r - y - 0.5f, y + 0.5f - (size - r)));
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
            }
            img.sprite = _rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.4f / cornerScale;
        }

        private static Sprite _star;

        /// <summary>Generated anti-aliased five-point star sprite.</summary>
        public static Sprite Star()
        {
            if (_star != null) return _star;
            const int size = 128;
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 0.48f : 0.21f;
                pts[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.47f + Mathf.Sin(a) * r) * size;
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                    if (Inside(pts, new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f))) hits++;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 16f));
            }
            tex.Apply();
            _star = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _star;
        }

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        public static Image StarImage(Transform parent, Color color)
        {
            var go = new GameObject("Star", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Star();
            img.color = color;
            img.raycastTarget = false;
            img.preserveAspect = true;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.3f);
            sh.effectDistance = new Vector2(0f, -6f);
            return img;
        }

        /// <summary>Round booster button with a caption and a count bubble; returns the button and the bubble label.</summary>
        public static Button Booster(Transform parent, string caption, Action onClick, Color color, out TextMeshProUGUI count)
        {
            var holder = new GameObject("Booster " + caption, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var b = Button(holder.transform, caption, onClick, color, 34f);
            var brt = (RectTransform)b.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(170f, 170f);
            Round(b.GetComponent<Image>(), 2.9f);
            var bubble = Panel(b.transform, "Count", Palette.Badge);
            Round(bubble.GetComponent<Image>(), 1.2f);
            bubble.GetComponent<Image>().raycastTarget = false;
            bubble.anchorMin = bubble.anchorMax = new Vector2(1f, 1f);
            bubble.sizeDelta = new Vector2(70f, 70f);
            bubble.anchoredPosition = new Vector2(-12f, -12f);
            var rim = bubble.gameObject.AddComponent<Outline>();
            rim.effectColor = Color.white;
            rim.effectDistance = new Vector2(3f, -3f);
            count = Label(bubble, "0", 34f);
            count.fontStyle = FontStyles.Bold;
            count.margin = Vector4.zero;
            return b;
        }

        public static void SetText(Button b, string text) => b.GetComponentInChildren<TextMeshProUGUI>().text = text;

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing = 24f, int pad = 48)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.childAlignment = TextAnchor.MiddleCenter;
            return v;
        }

        public static HorizontalLayoutGroup Row(RectTransform rt, float spacing = 20f)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = true;
            return h;
        }

        public static TextMeshProUGUI Heading(Transform parent, string text, float size, float height)
        {
            var go = new GameObject("Heading", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            var t = Label(go.transform, text, size);
            return t;
        }
    }

    /// <summary>Keeps its RectTransform inside Screen.safeArea (notches, home indicator).</summary>
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _last;

        private void Update()
        {
            var sa = Screen.safeArea;
            if (sa == _last || Screen.width == 0 || Screen.height == 0) return;
            _last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
