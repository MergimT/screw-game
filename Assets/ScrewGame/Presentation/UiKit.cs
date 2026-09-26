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
            Stretch((RectTransform)go.transform);
            return t;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color? color = null, float fontSize = 40f)
        {
            var go = new GameObject("Button " + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color ?? new Color(1f, 1f, 1f, 0.95f);
            var b = go.GetComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            b.colors = colors;
            b.onClick.AddListener(() => onClick());
            var label = Label(go.transform, text, fontSize);
            label.margin = new Vector4(12, 6, 12, 6);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 120f;
            le.preferredHeight = 120f;
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
