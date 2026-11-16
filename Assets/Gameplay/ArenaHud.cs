using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloombots.Gameplay
{
    internal sealed class ArenaHud
    {
        private static readonly Color Cream = new(1, .94f, .75f);
        private static readonly Color Gold = new(.89f, .64f, .24f);
        private readonly TMP_FontAsset font;
        private readonly ArenaHeaderView header;
        private readonly TMP_Text summaryTitle, summaryText;
        private readonly RectTransform modal;
        private readonly RectTransform choices, pauseControls, restartControls, leaveControls, storyControls;
        private readonly TMP_Text retryLabel;
        private readonly TMP_Text[] choiceTitles = new TMP_Text[3];
        private readonly TMP_Text[] choiceLabels = new TMP_Text[3];
        private readonly RectTransform safeArea, tray, sunCounter, mergeControl, hudBounds;
        private readonly RectTransform dragLayer, liftedRect;
        private readonly UnityEngine.UI.Image liftedUnit, dragBadge;
        private readonly TMP_Text dragLabel;
        private readonly Vector3[] corners = new Vector3[4];
        public RectTransform GuideParent => safeArea;

        public ArenaHud(BottomBannerView banner, EndlessArena game, ArenaAssets assets)
        {
            font = assets.uiLabels != null ? assets.uiLabels : TMP_Settings.defaultFontAsset;
            var safe = banner.transform.Find("SafeArea") ?? banner.transform;
            safeArea = (RectTransform)safe;
            tray = safe.Find("BottomBanner") as RectTransform;
            sunCounter = tray != null ? tray.Find("SunCounter") as RectTransform : null;
            mergeControl = tray != null ? tray.Find("MergeButton") as RectTransform : null;
            var boardHit = Box(banner.transform, "Arena input", 0, 0, 1, 1);
            boardHit.SetAsFirstSibling();
            var hit = boardHit.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.canvasRenderer.cullTransparentMesh = false;
            boardHit.gameObject.AddComponent<ArenaBoardInput>().Arena = game;

            header = UnityEngine.Object.Instantiate(assets.header, safe, false);
            header.Bind(game.TogglePause, game.ToggleVisor);
            hudBounds = (RectTransform)header.transform;

            modal = Box(safe, "Run dialog", 0, 0, 1, 1);
            var shade = modal.gameObject.AddComponent<Image>();
            shade.color = new Color(.02f, .07f, .10f, .80f);
            var panel = Box(modal, "Production dialog", .075f, .155f, .925f, .845f);
            ProductionPanel.Fill(panel, ProductionPanel.Ink);
            ProductionPanel.Cast(Box(panel, "Hero portrait strip", 0, .81f, 1, 1), assets.productionCast, lead: 1);
            ProductionPanel.Rule(panel, 0, ProductionPanel.Brass);
            summaryTitle = Label(panel, "Title", .09f, .718f, .91f, .805f, 52);
            ProductionPanel.Title(summaryTitle, Resources.Load<TMP_FontAsset>("Fredoka-Bold SDF"), 26, 52);
            summaryText = Label(panel, "Details", .09f, .638f, .91f, .718f, 38);
            summaryText.alignment = TextAlignmentOptions.MidlineLeft;
            if (assets.uiBody != null)
                summaryText.font = assets.uiBody;
            choices = Box(panel, "Harvest choices", .028f, .09f, .972f, .633f);
            for (int i = 0; i < choiceLabels.Length; i++)
            {
                int index = i;
                float y = .68f - .32f * i;
                var button = MakeButton(choices, "Upgrade " + (i + 1), "", .08f, y, .92f, y + .28f,
                    () => game.ChooseUpgrade(index), out choiceTitles[i], i == 0 ? ProductionPanel.Blue : ProductionPanel.Ink);
                choiceTitles[i].rectTransform.anchorMin = new Vector2(.10f, .50f);
                choiceTitles[i].rectTransform.anchorMax = new Vector2(.90f, .83f);
                choiceLabels[i] = Label(button.transform, "Upgrade effect", .11f, .18f, .89f, .51f, 32);
                if (assets.uiBody != null)
                    choiceLabels[i].font = assets.uiBody;
                choiceLabels[i].color = Gold;
            }

            pauseControls = Box(panel, "Pause controls", .025f, .09f, .975f, .628f);
            MakeButton(pauseControls, "Resume", "RESUME", .13f, .81f, .87f, .99f, game.TogglePause, out _);
            MakeSlider(pauseControls, "Sound", .61f, .80f, BloomFeedback.SoundVolume, BloomFeedback.SetSound);
            MakeSlider(pauseControls, "Music", .41f, .60f, BloomFeedback.MusicVolume, BloomFeedback.SetMusic);
            TMP_Text vibration = null;
            MakeButton(pauseControls, "Vibration", VibrationText(), .20f, .21f, .80f, .39f, () =>
            {
                BloomFeedback.ToggleVibration();
                vibration.text = VibrationText();
            }, out vibration, ProductionPanel.Ink);
            MakeButton(pauseControls, "Main menu", "MAIN MENU", .13f, .01f, .87f, .18f, ConfirmLeave, out _, ProductionPanel.Red);

            restartControls = Box(panel, "Restart controls", .045f, .09f, .955f, .615f);
            MakeButton(restartControls, "Restart", "REPLANT THE GREENHOUSE", .09f, .49f, .91f, .83f,
                game.RestartRun, out retryLabel);
            MakeButton(restartControls, "Main menu", "MAIN MENU", .09f, .11f, .91f, .42f,
                game.ReturnToMainMenu, out _, ProductionPanel.Ink);

            leaveControls = Box(panel, "Confirm return", .045f, .09f, .955f, .615f);
            MakeButton(leaveControls, "Keep playing", "STAY HERE", .09f, .49f, .91f, .83f, Pause, out _);
            MakeButton(leaveControls, "Leave run", "RETURN TO MENU", .09f, .11f, .91f, .42f,
                game.ReturnToMainMenu, out _, ProductionPanel.Red);
            storyControls = Box(panel, "Story clear controls", .045f, .09f, .955f, .615f);
            MakeButton(storyControls, "View restoration", "SEE THE GARDEN", .09f, .38f, .91f, .74f,
                game.ReturnToMainMenu, out _);
            modal.gameObject.SetActive(false);

            dragLayer = Box(banner.transform, "Card lift overlay", 0, 0, 1, 1);
            liftedRect = Box(dragLayer, "Lifted gameplay sprite", .5f, .5f, .5f, .5f);
            liftedUnit = liftedRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            liftedUnit.raycastTarget = false;
            var badge = Box(liftedRect, "Placement status", .5f, 1, .5f, 1);
            badge.pivot = new Vector2(.5f, 0);
            badge.sizeDelta = new Vector2(230, 42);
            badge.anchoredPosition = new Vector2(0, -7);
            dragBadge = badge.gameObject.AddComponent<UnityEngine.UI.Image>();
            dragBadge.raycastTarget = false;
            dragLabel = Label(badge, "Status text", .04f, 0, .96f, 1, 25);
            dragLayer.gameObject.SetActive(false);
        }

        public Rect ArenaViewport()
        {
            safeArea.GetWorldCorners(corners);
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 maximum = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            float bottom = Mathf.Max(TopOf(tray), Mathf.Max(TopOf(sunCounter), TopOf(mergeControl)));
            // Reserve the visor strip so switching to night never shifts the grid.
            hudBounds.GetWorldCorners(corners);
            float hudBottom = RectTransformUtility.WorldToScreenPoint(null, corners[0]).y;
            float top = Mathf.Min(hudBottom, Mathf.Lerp(minimum.y, maximum.y, .838f));
            float padding = (maximum.y - minimum.y) * .012f;
            return Rect.MinMaxRect(minimum.x / Screen.width, (bottom + padding) / Screen.height,
                maximum.x / Screen.width, (top - padding) / Screen.height);
        }

        private float TopOf(RectTransform rect)
        {
            if (rect == null)
                return Screen.height * .25f;
            rect.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(null, corners[2]).y;
        }

        public void ShowPlacement(Sprite sprite, Vector2 screenPoint, Vector2 pixelSize, string message, Color badgeColor)
        {
            if (!dragLayer.gameObject.activeSelf)
                dragLayer.gameObject.SetActive(true);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPoint, null, out var local))
                return;

            liftedUnit.sprite = sprite;
            liftedUnit.color = new Color(1, 1, 1, .94f);
            liftedRect.pivot = sprite.pivot / sprite.rect.size;
            liftedRect.localPosition = local;
            liftedRect.sizeDelta = Vector2.Scale(pixelSize, dragLayer.rect.size / new Vector2(Screen.width, Screen.height));
            dragBadge.color = badgeColor;
            SetText(dragLabel, message);
        }

        public void HidePlacement() => dragLayer.gameObject.SetActive(false);

        public void Refresh(string dayText, string phaseText, string healthText, string tip, int sunAmount,
            bool night, bool active, float charge) =>
            header.Show(dayText, phaseText, healthText, tip, sunAmount, night, active, charge);

        public void HideDialog() => modal.gameObject.SetActive(false);

        public void Pause()
        {
            Dialog("Game paused", "Your defenders are holding their positions.", false, true, false);
        }

        public void Harvest(string[] options)
        {
            Dialog("Wave cleared", "Choose one harvest upgrade for the next wave.", true, false, false);
            for (int i = 0; i < choiceLabels.Length; i++)
            {
                var parts = options[i].Split("  ·  ", 2, StringSplitOptions.None);
                choiceTitles[i].text = parts[0];
                choiceLabels[i].text = parts.Length > 1 ? parts[1] : "";
            }
        }

        public void Lost(int survived, int best, bool storyRun)
        {
            retryLabel.text = storyRun ? "RETRY LEVEL" : "REPLANT THE GREENHOUSE";
            Dialog("One sprout saved", storyRun ? "Pip saved a seed. Try this garden again."
                    : $"Days survived  {survived}     Best  {best}\nPip saved a seed for another run.",
                false, false, true);
        }

        public void StoryComplete(string garden)
        {
            Dialog("Garden restored", $"Water is flowing through {garden} again.", false, false, false);
            storyControls.gameObject.SetActive(true);
        }

        private void ConfirmLeave()
        {
            Dialog("Leave this run?", "You will start a fresh run the next time you play.", false, false, false);
            leaveControls.gameObject.SetActive(true);
        }

        private void Dialog(string title, string detail, bool harvest, bool pause, bool restart)
        {
            modal.SetAsLastSibling();
            summaryTitle.text = title;
            summaryText.text = detail;
            choices.gameObject.SetActive(harvest);
            pauseControls.gameObject.SetActive(pause);
            restartControls.gameObject.SetActive(restart);
            leaveControls.gameObject.SetActive(false);
            storyControls.gameObject.SetActive(false);
            modal.gameObject.SetActive(true);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label.text != value)
                label.text = value;
        }

        private static string VibrationText() => BloomFeedback.VibrationEnabled ? "VIBRATION  ON" : "VIBRATION  OFF";

        private void MakeSlider(Transform parent, string name, float bottom, float top, float value, Action<float> change)
        {
            float middle = (bottom + top) * .5f;
            var title = Label(parent, name, .13f, middle, .67f, top, 34);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.text = name;
            var percent = Label(parent, name + " volume", .69f, middle, .87f, top, 32);
            percent.alignment = TextAlignmentOptions.MidlineRight;
            percent.text = $"{Mathf.RoundToInt(value * 100)}%";
            var root = Box(parent, name + " slider", .13f, bottom, .87f, middle);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.canvasRenderer.cullTransparentMesh = false;
            var track = Box(root, "Track", 0, .30f, 1, .70f);
            ProductionPanel.Fill(track, new Color32(37, 53, 70, 255));
            var pad = Box(root, "Travel", .055f, .37f, .945f, .63f);
            var fill = Box(pad, "Fill", 0, 0, 1, 1);
            ProductionPanel.Fill(fill, ProductionPanel.Blue);
            var knob = Box(pad, "Knob", 0, 0, 0, 1);
            knob.sizeDelta = new Vector2(16, 14);
            var handle = ProductionPanel.Fill(knob, ProductionPanel.Brass);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = knob;
            slider.targetGraphic = handle;
            slider.value = value;
            slider.onValueChanged.AddListener(v =>
            {
                change(v);
                percent.text = $"{Mathf.RoundToInt(v * 100)}%";
            });
        }

        private Button MakeButton(Transform parent, string name, string text, float x0, float y0, float x1, float y1,
            Action click, out TMP_Text label, Color32? skin = null)
        {
            var rect = Box(parent, name, x0, y0, x1, y1);
            var button = rect.gameObject.AddComponent<Button>();
            ProductionPanel.Square(button, skin ?? ProductionPanel.Blue);
            button.onClick.AddListener(() => click());
            label = Label(rect, "Text", .12f, .16f, .88f, .86f, 44);
            label.text = text;
            return button;
        }

        private TMP_Text Label(Transform parent, string name, float x0, float y0, float x1, float y1, float size)
        {
            var text = Box(parent, name, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = size * .5f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Box(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
