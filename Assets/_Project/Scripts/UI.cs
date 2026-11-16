using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ReCraft
{
    /// <summary>프리팹 없이 uGUI를 코드로 구성하는 작은 빌더.</summary>
    public static class UI
    {
        public static readonly Color Bg = new(0.07f, 0.065f, 0.06f, 0.93f);
        public static readonly Color BgLight = new(0.17f, 0.15f, 0.13f, 0.96f);
        public static readonly Color Accent = Palette.Accent;
        public static readonly Color TextCol = new(0.95f, 0.92f, 0.86f);
        public static readonly Color Dim = new(0.62f, 0.59f, 0.55f);
        public static readonly Color Good = new(0.45f, 0.85f, 0.5f);
        public static readonly Color Bad = new(1f, 0.42f, 0.35f);
        public static readonly Vector2 Center = new(0.5f, 0.5f);

        static Font font;
        public static Font Font => font ? font : font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Segoe UI", "Arial" }, 32);

        public static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            if (!UnityEngine.Object.FindFirstObjectByType<EventSystem>())
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color c, bool raycast = true)
        {
            var rt = Rect(parent, name, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color c, TextAnchor align, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box)
        {
            var rt = Rect(parent, "Label", anchor, pivot, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.text = text;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Btn(Transform parent, string text, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Action onClick, Color? bg = null, int fontSize = 24)
        {
            var img = Panel(parent, "Btn", anchor, pivot, pos, size, bg ?? BgLight);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normalColor = new Color(0.82f, 0.82f, 0.82f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.fadeDuration = 0.05f;
            b.colors = colors;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            b.onClick.AddListener(() => { Sfx.Play("click", 0.5f); onClick?.Invoke(); });
            var t = Label(img.transform, text, fontSize, TextCol, TextAnchor.MiddleCenter, Center, Center, Vector2.zero, size - new Vector2(8, 4));
            t.name = "Text";
            return b;
        }

        public static Text TextOf(Button b) => b.transform.Find("Text").GetComponent<Text>();

        public static string Stars(int n, int max = 3)
        {
            return "<color=#ffc24a>" + new string('★', Mathf.Clamp(n, 0, max)) + "</color><color=#555555>" + new string('☆', Mathf.Clamp(max - n, 0, max)) + "</color>";
        }
    }
}
