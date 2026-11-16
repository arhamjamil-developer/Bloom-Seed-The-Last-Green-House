using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Bloombots
{
    public sealed class StorybookView : MonoBehaviour
    {
        public enum Section { Opening, Map, Restoration }

        [Serializable]
        private struct Page
        {
            public Sprite art;
            public string title;
            [TextArea] public string copy;
        }

        [SerializeField] private Page[] opening, map;
        [SerializeField] private Page restoration;
        [SerializeField] private UnityEngine.UI.Image illustration;
        [SerializeField] private TMP_Text chapter, title, copy, progress, primaryLabel, secondaryLabel;
        [SerializeField] private UnityEngine.UI.Button primary;

        private MainMenuController menu;
        private Section section;
        private int index;
        private RectTransform levelPicker;
        private Texture productionCast;
        private TMP_FontAsset displayFace;
        private readonly UnityEngine.UI.Button[] gardenButtons = new UnityEngine.UI.Button[RelayGarden.Names.Length];

        public Selectable Primary => primary;

        internal void ApplyTypography(TMP_FontAsset displayFont, Texture cast)
        {
            productionCast = cast;
            displayFace = displayFont;
            ProductionPanel.Square(primary, ProductionPanel.Blue);
            var secondary = secondaryLabel.GetComponentInParent<UnityEngine.UI.Button>();
            if (secondary != null)
                ProductionPanel.Square(secondary, ProductionPanel.Red);
            var border = ProductionPanel.Box(transform, "Story title border", .175f, .848f, .92f, .933f);
            border.SetSiblingIndex(chapter.transform.GetSiblingIndex());
            ProductionPanel.Fill(border, ProductionPanel.Ink);
            ProductionPanel.Cast(ProductionPanel.Box(border, "Hero portrait strip", .63f, 0, 1, 1), cast, 2, count: 1);
            ProductionPanel.Title(chapter, displayFont, 23, 39);
            ProductionPanel.Place(chapter.rectTransform, .20f, .851f, .63f, .93f);
            MainMenuController.SetDisplayFont(title, displayFont, 34, 62, wrap: true);
            title.rectTransform.anchorMin = new Vector2(.065f, .285f);
            title.rectTransform.anchorMax = new Vector2(.935f, .367f);
            copy.rectTransform.anchorMin = new Vector2(.09f, .179f);
            copy.rectTransform.anchorMax = new Vector2(.91f, .284f);
            copy.fontSizeMin = 28;
            copy.fontSizeMax = 42;
            copy.overflowMode = TextOverflowModes.Truncate;
            primaryLabel.fontSizeMin = 26;
            primaryLabel.fontSizeMax = 37;
            primaryLabel.overflowMode = TextOverflowModes.Truncate;
            secondaryLabel.fontSizeMin = 24;
            secondaryLabel.overflowMode = TextOverflowModes.Truncate;
        }

        public void Show(MainMenuController owner, Section current, int garden = 0)
        {
            menu = owner;
            section = current;
            index = current == Section.Opening ? 0 : garden;
            if (levelPicker != null)
                levelPicker.gameObject.SetActive(false);
            primary.gameObject.SetActive(true);
            Render();
        }

        public void Next()
        {
            if (section == Section.Opening && index == opening.Length - 1)
                menu.StartStory();
            else if (section == Section.Map)
                menu.StartGarden(index);
            else if (section == Section.Restoration)
                menu.ShowHome();
            else
            {
                index++;
                Render();
            }
        }

        public void Secondary()
        {
            if (section == Section.Opening)
                menu.StartStory();
            else if (section == Section.Restoration)
                menu.OpenMap();
            else
                ShowLevelPicker(levelPicker == null || !levelPicker.gameObject.activeSelf);
        }

        public void Back()
        {
            if (levelPicker != null && levelPicker.gameObject.activeSelf)
                ShowLevelPicker(false);
            else
                menu.ShowHome();
        }

        private void ShowLevelPicker(bool show)
        {
            if (levelPicker == null)
                BuildLevelPicker();
            if (show)
            {
                for (int i = 0; i < gardenButtons.Length; i++)
                    gardenButtons[i].GetComponentInChildren<TMP_Text>().text =
                        RelayGarden.Names[i] + (RelayGarden.IsRestored(i) ? "\nRestored" : "");
            }
            levelPicker.gameObject.SetActive(show);
            primary.gameObject.SetActive(!show);
            secondaryLabel.text = show ? "CLOSE" : "LEVEL SELECT";
            EventSystem.current?.SetSelectedGameObject(show ? gardenButtons[index].gameObject : primary.gameObject);
        }

        private void BuildLevelPicker()
        {
            levelPicker = ProductionPanel.Box(transform, "Level Selection", .085f, .143f, .915f, .84f);
            ProductionPanel.Fill(levelPicker, ProductionPanel.Ink, hit: true);
            ProductionPanel.Cast(ProductionPanel.Box(levelPicker, "Hero portrait strip", 0, .74f, 1, 1), productionCast);
            ProductionPanel.Rule(levelPicker, 0, ProductionPanel.Brass);
            var heading = ProductionPanel.Box(levelPicker, "Title", .07f, .634f, .94f, .737f)
                .gameObject.AddComponent<TextMeshProUGUI>();
            heading.font = title.font;
            heading.text = "Level Select";
            heading.raycastTarget = false;
            ProductionPanel.Title(heading, displayFace, 27, 49);

            for (int i = 0; i < gardenButtons.Length; i++)
            {
                int garden = i;
                var button = Instantiate(primary, levelPicker);
                button.name = RelayGarden.Names[i];
                button.gameObject.SetActive(true);
                button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    index = garden;
                    ShowLevelPicker(false);
                    Render();
                });
                float y = .50f - i * .145f;
                ProductionPanel.Place((RectTransform)button.transform, .07f, y, .93f, y + .128f);
                ProductionPanel.Square(button, i % 2 == 0 ? ProductionPanel.Blue : ProductionPanel.Red);
                var label = button.GetComponentInChildren<TMP_Text>();
                ProductionPanel.Place(label.rectTransform, .065f, .08f, .72f, .92f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = ProductionPanel.Paper;
                label.fontSizeMin = 22;
                label.fontSizeMax = 40;
                label.textWrappingMode = TextWrappingModes.Normal;
                var art = ProductionPanel.Box(button.transform, "Garden illustration", .75f, 0, 1, 1);
                var sprite = map[i].art;
                if (sprite != null)
                {
                    var texture = sprite.texture;
                    Rect rect = sprite.rect;
                    ProductionPanel.Crop(art, texture, new Rect(rect.x / texture.width, rect.y / texture.height,
                        rect.width / texture.width, rect.height / texture.height));
                }
                art.SetAsFirstSibling();
                gardenButtons[i] = button;
            }
        }

        private void Render()
        {
            var page = section == Section.Opening ? opening[index]
                : section == Section.Restoration && index == 0 ? restoration : map[index];
            illustration.sprite = page.art;
            title.text = page.title;
            copy.text = page.copy;
            if (section == Section.Opening)
            {
                chapter.text = "THE SEED THAT SANG";
                progress.text = $"{index + 1} / {opening.Length}";
                primaryLabel.text = index == opening.Length - 1 ? "START DAWN NURSERY" : "NEXT";
                secondaryLabel.text = "SKIP INTRO";
            }
            else if (section == Section.Map)
            {
                chapter.text = "THE RELAY GARDENS";
                progress.text = RelayGarden.IsRestored(index) ? "RESTORED" : "3 WAVES";
                copy.text = RelayGarden.Briefs[index];
                primaryLabel.text = RelayGarden.IsRestored(index) ? "REPLAY LEVEL" : "PLAY LEVEL";
                secondaryLabel.text = "LEVEL SELECT";
            }
            else
            {
                chapter.text = RelayGarden.Names[index].ToUpperInvariant();
                if (index != 0)
                {
                    title.text = "The relay is restored";
                    copy.text = "Water is flowing through this garden again. Visit the map to choose another relay.";
                }
                progress.text = $"{RelayGarden.RestoredCount()} OF {RelayGarden.Names.Length} RESTORED";
                primaryLabel.text = "BACK TO MENU";
                secondaryLabel.text = "VIEW GARDENS";
            }
        }
    }
}
