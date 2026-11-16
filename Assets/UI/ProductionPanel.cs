using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloombots
{
    internal static class ProductionPanel
    {
        public static readonly Color32 Ink = new(15, 28, 43, 255);
        public static readonly Color32 Blue = new(35, 65, 91, 255);
        public static readonly Color32 Red = new(98, 49, 53, 255);
        public static readonly Color32 Brass = new(185, 142, 91, 255);
        public static readonly Color32 Paper = new(246, 233, 210, 255);

        public static RectTransform Box(Transform parent, string name, float left, float bottom, float right, float top)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            Place(rect, left, bottom, right, top);
            return rect;
        }

        public static void Place(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static Image Fill(RectTransform rect, Color color, bool hit = false)
        {
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = null;
            image.overrideSprite = null;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = hit;
            return image;
        }

        public static void Rule(Transform parent, float y, Color color)
        {
            var rect = Box(parent, "Brass rule", 0, y, 1, y);
            rect.sizeDelta = new Vector2(0, 2);
            Fill(rect, color);
        }

        public static void Cast(Transform parent, Texture texture, int lead = 0, int count = 3)
        {
            if (texture == null)
                return;
            for (int i = 0; i < count; i++)
            {
                int hero = (i + lead) % 3;
                float x = hero switch { 0 => .499f, 1 => .287f, _ => .724f };
                float width = hero == 2 ? .243f : .214f;
                float left = i / (float)count;
                float right = (i + 1) / (float)count;
                var panel = Box(parent, "Production portrait", left, 0, right, 1);
                Fill(panel, hero == 2 ? Red : Blue);
                Crop(panel, texture, new Rect(x, .60f, width, .40f));
                Fill(Box(panel, "Color wash", 0, 0, 1, 1),
                    hero == 2 ? new Color(.36f, .09f, .13f, .24f) : new Color(.06f, .20f, .38f, .23f));
                var seam = Box(panel, "Portrait divider", 0, 0, 0, 1);
                seam.sizeDelta = new Vector2(2, 0);
                Fill(seam, Brass);
            }
            Rule(parent, 0, Brass);
            Rule(parent, 1, Brass);
        }

        public static void Crop(RectTransform parent, Texture texture, Rect uv)
        {
            var portrait = Box(parent, "Art crop", 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            portrait.texture = texture;
            portrait.uvRect = uv;
            portrait.raycastTarget = false;
            var fitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = uv.width * texture.width / (uv.height * texture.height);
            parent.gameObject.AddComponent<RectMask2D>();
        }

        public static void Square(Button button, Color32 fill)
        {
            var graphic = Fill((RectTransform)button.transform, fill, true);
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            var states = button.colors;
            states.normalColor = Color.white;
            states.highlightedColor = new Color(.88f, .94f, 1);
            states.selectedColor = states.highlightedColor;
            states.pressedColor = new Color(.65f, .76f, .87f);
            button.colors = states;
            Rule(button.transform, 1, Brass);
        }

        public static void Title(TMP_Text label, TMP_FontAsset font, float minimum, float maximum)
        {
            MainMenuController.SetDisplayFont(label, font, minimum, maximum);
            label.color = Paper;
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }
}
