using System.Collections.Generic;
using Bloombots.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloombots
{
    internal sealed class EnemyGuideView
    {
        private static readonly (int Kind, string Name, string Info)[] Entries =
        {
            (-1, "Rust Titan", "Every fifth Endless wave. Lands near your defenders, then destroys up to three on the red plots. Move heroes or defeat it before it blows."),
            (9, "Scrap Beetle", "Common lane attacker. Block it up front and use ranged defenders behind your guard."),
            (10, "Bolt Brute", "Heavy and slow. Hits blockers hard and deals 3 damage if it reaches the Seed Core."),
            (11, "Dust Moth", "Fast flier that slips past its first blocker. Keep a second line of defense."),
            (12, "Drill Mole", "Burrows past its first blocker and briefly breaks the plot beneath that defender."),
            (13, "Bronze Rampart", "Armored Bolt Brute variant. Slow, durable, and dangerous to blockers."),
            (14, "Coil Wasp", "Dust Moth variant. It can bypass the first defender in its lane."),
            (15, "Ruby Skitter", "Scrap Beetle variant. Use a guard to stop its lane advance."),
            (16, "Kiln Borer", "Drill Mole variant. It bypasses its first blocker and damages that plot.")
        };

        private readonly ScrollRect scroll;
        private readonly RectTransform banner, section, jump;
        private readonly TextMeshProUGUI heading, jumpLabel;
        private readonly (RectTransform Rect, TextMeshProUGUI Title, TextMeshProUGUI Info)[] rows;
        private readonly List<Sprite> portraits = new();

        public EnemyGuideView(ScrollRect scroll, RectTransform banner, TMP_FontAsset actionFont,
            TMP_FontAsset displayFont, TMP_FontAsset bodyFont)
        {
            this.scroll = scroll;
            this.banner = banner;
            RectTransform content = CreateRect("How to Play Content", scroll.viewport);
            scroll.content = content;
            banner.SetParent(content, false);

            jump = CreateRect("Jump to Enemy Guide", content);
            var button = jump.gameObject.AddComponent<UnityEngine.UI.Button>();
            ProductionPanel.Square(button, ProductionPanel.Blue);
            button.onClick.AddListener(JumpToGuide);
            jumpLabel = Label(jump, "View Enemy Guide", actionFont, Color.white, TextAlignmentOptions.Center);
            Fill(jumpLabel.rectTransform, 0, 0, 1, 1);

            section = CreateRect("Enemy Guide", content);
            heading = Label(section, "Enemy Guide", displayFont != null ? displayFont : actionFont,
                new Color32(225, 248, 183, 255), TextAlignmentOptions.Center);
            var assets = Resources.Load<ArenaAssets>("BloombotsArena");
            var atlas = JsonUtility.FromJson<ArenaAssets.Atlas>(assets.frames.text);
            rows = new (RectTransform, TextMeshProUGUI, TextMeshProUGUI)[Entries.Length];
            for (int i = 0; i < Entries.Length; i++)
            {
                var entry = Entries[i];
                var rect = CreateRect(entry.Name, section);
                var background = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
                background.color = i == 0 ? ProductionPanel.Red : ProductionPanel.Blue;
                background.raycastTarget = false;

                Sprite sprite = assets.rustTitan;
                if (entry.Kind >= 0)
                {
                    sprite = atlas.sheets[entry.Kind].frames[0].Build(assets.sheets[entry.Kind]);
                    portraits.Add(sprite);
                }
                RectTransform portrait = CreateRect("Sprite", rect);
                Fill(portrait, .025f, .10f, .26f, .90f);
                var portraitImage = portrait.gameObject.AddComponent<UnityEngine.UI.Image>();
                portraitImage.sprite = sprite;
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;

                var title = Label(rect, entry.Name, actionFont, new Color32(255, 218, 141, 255), TextAlignmentOptions.BottomLeft);
                var info = Label(rect, entry.Info, bodyFont, Color.white, TextAlignmentOptions.TopLeft);
                rows[i] = (rect, title, info);
            }
        }

        public float Layout(float width, float bannerHeight)
        {
            float jumpHeight = width * .12f;
            Place(jump, width, jumpHeight, 0);
            jumpLabel.fontSize = width * .047f;
            Place(banner, width, bannerHeight, jumpHeight + width * .018f);

            float y = width * .13f;
            Place(heading.rectTransform, width, y, 0);
            heading.fontSize = width * .063f;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                float rowWidth = width * .97f;
                float padding = width * .025f;
                float titleHeight = width * .09f;
                row.Title.fontSize = width * .059f;
                row.Info.fontSize = width * .050f;
                float textHeight = row.Info.GetPreferredValues(row.Info.text, rowWidth * .67f, float.PositiveInfinity).y;
                float height = Mathf.Max(width * .25f, titleHeight + textHeight + padding * 3);
                Place(row.Rect, rowWidth, height, y);
                Fill(row.Title.rectTransform, .29f, 1 - (titleHeight + padding) / height, .96f, 1 - padding / height);
                Fill(row.Info.rectTransform, .29f, padding / height, .96f, 1 - (titleHeight + padding * 1.4f) / height);
                y += height + width * .015f;
            }

            float beforeBanner = jumpHeight + width * .018f;
            Place(section, width, y, beforeBanner + bannerHeight);
            return beforeBanner + y;
        }

        private void JumpToGuide()
        {
            scroll.StopMovement();
            float scrollable = scroll.content.rect.height - scroll.viewport.rect.height;
            if (scrollable > 0)
                scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(-section.anchoredPosition.y / scrollable);
        }

        public void Dispose()
        {
            foreach (var portrait in portraits)
                Object.Destroy(portrait);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            Place(rect, 0, 0, 0);
            return rect;
        }

        private static TextMeshProUGUI Label(RectTransform parent, string value, TMP_FontAsset font,
            Color color, TextAlignmentOptions alignment)
        {
            var label = CreateRect("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = value;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static void Place(RectTransform rect, float width, float height, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0, -y);
        }

        private static void Fill(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
