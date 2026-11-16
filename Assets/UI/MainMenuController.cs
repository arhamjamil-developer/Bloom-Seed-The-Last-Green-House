using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bloombots
{
    internal static class BloomPreferences
    {
        public const string Motion = "Bloombots.Motion";
        public const string Flash = "Bloombots.Flash";
        public const string BestDay = "Bloombots.BestDay";
        public const string DawnRestored = "Bloombots.DawnRestored";
        public const string Sound = "Bloombots.Sound";
        public const string Music = "Bloombots.Music";
        public const string Vibration = "Bloombots.Vibration";
    }

    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string SceneName = "Main Menu";
        private const string ArenaScene = "SampleScene";
        private static bool tutorialRequested;
        private static int storyRequested = -1;
        private static int restorationRequested = -1;

        [Header("Authored layout")]
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private UnityEngine.UI.Image backdrop;
        [SerializeField] private AspectRatioFitter backdropFit;
        [SerializeField] private Sprite phoneBackdrop, tabletBackdrop;
        [SerializeField] private UnityEngine.UI.Image heroCast;
        [SerializeField] private GameObject home, deck, tutorial, settings, exit, loading;
        [SerializeField] private UnityEngine.UI.Button playButton, deckBack, tutorialBack, settingsBack, stayButton, quitButton;
        [SerializeField] private TMP_Text exitMessage, bestDay;
        [SerializeField] private TMP_Text homeRecord;
        [SerializeField] private StorybookView storybook;
        [SerializeField] private UnityEngine.UI.Image loadingFill;
        [SerializeField] private TMP_Text loadingPercent, loadingTitle;

        [Header("Deck and tutorial banners")]
        [SerializeField] private ScrollRect deckScroll, tutorialScroll;
        [SerializeField] private UnityEngine.UI.Image deckArt, tutorialArt;
        [SerializeField] private Sprite heroesBanner, plantsBanner, fullDeckBanner, phoneTutorial, tabletTutorial;
        [SerializeField] private UnityEngine.UI.Button heroesTab, plantsTab;

        [Header("Audio and vibration")]
        [SerializeField] private Slider soundSlider, musicSlider;
        [SerializeField] private TMP_Text soundValue, musicValue, vibrationValue;

        private GameObject current;
        private bool tablet, plantsSelected, sceneLoading, preferencesDirty;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;
        private AsyncOperation arenaLoad;
        private int lastLoadPercent = -1;
        private EnemyGuideView enemyGuide;
        private TMP_FontAsset displayFont;

        private void Start()
        {
            ApplyTypography();
            soundSlider.SetValueWithoutNotify(BloomFeedback.SoundVolume);
            musicSlider.SetValueWithoutNotify(BloomFeedback.MusicVolume);
            RefreshSettingsLabels();
            int record = PlayerPrefs.GetInt(BloomPreferences.BestDay, 0);
            bestDay.text = record > 0 ? $"BEST DAY  {record}" : "BEST DAY  —";
            if (homeRecord != null)
                homeRecord.text = record > 0 ? $"BEST DAY  {record}" : "BEST DAY  —";
            RefreshLayout();
            ShowHome();
            if (restorationRequested >= 0)
            {
                int restored = restorationRequested;
                restorationRequested = -1;
                OpenStorybook(StorybookView.Section.Restoration, restored);
            }
        }

        private void ApplyTypography()
        {
            displayFont = Resources.Load<TMP_FontAsset>("Fredoka-Bold SDF");
            if (displayFont == null)
                return;

            Texture cast = heroCast != null && heroCast.sprite != null ? heroCast.sprite.texture : null;
            foreach (var panel in new[] { deck, tutorial, settings, exit })
            {
                var frame = panel.transform.Find("Gold Frame") as RectTransform;
                if (frame != null)
                {
                    ProductionPanel.Fill(frame, ProductionPanel.Ink);
                    ProductionPanel.Rule(frame, 1, ProductionPanel.Brass);
                    ProductionPanel.Rule(frame, 0, ProductionPanel.Brass);
                }
                foreach (var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    ProductionPanel.Square(button, button == quitButton ? ProductionPanel.Red : ProductionPanel.Blue);
                    var buttonLabel = button.GetComponentInChildren<TMP_Text>(true);
                    if (buttonLabel != null)
                        buttonLabel.color = ProductionPanel.Paper;
                }
                foreach (var label in panel.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.name != "Title")
                        continue;
                    ProductionPanel.Title(label, displayFont, 27, panel == exit ? 57 : 55);
                    if (panel == exit)
                    {
                        label.textWrappingMode = TextWrappingModes.Normal;
                        if (frame != null)
                        {
                            ProductionPanel.Place(label.rectTransform, .10f, .65f, .90f, .80f);
                            ProductionPanel.Cast(ProductionPanel.Box(frame, "Hero title border", 0, .81f, 1, 1), cast, 2);
                        }
                        continue;
                    }
                    var banner = (RectTransform)label.transform.parent;
                    ProductionPanel.Fill(banner, ProductionPanel.Blue);
                    ProductionPanel.Place(label.rectTransform, .045f, .10f, .57f, .9f);
                    ProductionPanel.Cast(ProductionPanel.Box(banner, "Hero title border", .59f, 0, 1, 1),
                        cast, panel == deck ? 0 : panel == tutorial ? 1 : 2, count: 1);
                }
            }
            if (loading.transform.Find("Gold Frame") is RectTransform loadingFrame)
            {
                ProductionPanel.Fill(loadingFrame, ProductionPanel.Ink);
                ProductionPanel.Rule(loadingFrame, 0, ProductionPanel.Brass);
            }
            SetDisplayFont(loadingTitle, displayFont, 28, 42);
            loadingTitle.color = ProductionPanel.Paper;
            SetDisplayFont(playButton.GetComponentInChildren<TMP_Text>(true), displayFont, 38, 76);
            storybook?.ApplyTypography(displayFont, cast);
        }

        internal static void SetDisplayFont(TMP_Text label, TMP_FontAsset font, float minimum, float maximum,
            bool wrap = false)
        {
            if (label == null || font == null)
                return;
            label.font = font;
            label.fontSharedMaterial = font.material;
            label.fontStyle = FontStyles.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = minimum;
            label.fontSizeMax = maximum;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
        }

        private void LateUpdate()
        {
            RefreshLayout();
            if (sceneLoading || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
                return;
            if (current == home)
                OpenExit();
            else
                ShowHome();
        }

        private void RefreshLayout()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            Rect area = Screen.safeArea;
            if (screen.x <= 0 || screen.y <= 0 || area.width <= 0 || area.height <= 0 ||
                (screen == lastScreen && area == lastSafeArea))
                return;

            lastScreen = screen;
            lastSafeArea = area;
            safeArea.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            safeArea.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            tablet = area.width / area.height >= .62f;
            backdrop.sprite = tablet ? tabletBackdrop : phoneBackdrop;
            backdropFit.aspectRatio = backdrop.sprite.rect.width / backdrop.sprite.rect.height;
            if (heroCast != null && heroCast.sprite != null)
            {
                const float width = .61f;
                const float feet = .355f;
                float height = width * area.width / area.height * heroCast.sprite.rect.height / heroCast.sprite.rect.width;
                heroCast.rectTransform.anchorMin = new Vector2(.5f - width * .5f, feet);
                heroCast.rectTransform.anchorMax = new Vector2(.5f + width * .5f, feet + height);
            }
            heroesTab.gameObject.SetActive(!tablet);
            plantsTab.gameObject.SetActive(!tablet);
            var deckBounds = (RectTransform)deckScroll.transform;
            deckBounds.anchorMax = new Vector2(deckBounds.anchorMax.x, tablet ? .846f : .782f);

            if (current == deck || current == tutorial)
                RefreshBanner();
        }

        public void ShowHome()
        {
            SavePreferences();
            Open(home, playButton);
        }

        public void OpenDeck()
        {
            plantsSelected = false;
            Open(deck, tablet ? deckBack : heroesTab);
            RefreshBanner();
        }

        public void OpenTutorial()
        {
            Open(tutorial, tutorialBack);
            RefreshBanner();
        }

        public void OpenSettings() => Open(settings, settingsBack);
        public void ShowHeroes() => SelectDeck(false);
        public void ShowPlants() => SelectDeck(true);

        private void SelectDeck(bool plants)
        {
            plantsSelected = plants;
            RefreshBanner();
        }

        private void RefreshBanner()
        {
            bool showDeck = current == deck;
            ScrollRect scroll = showDeck ? deckScroll : tutorialScroll;
            UnityEngine.UI.Image image = showDeck ? deckArt : tutorialArt;
            Sprite sprite = showDeck
                ? (tablet ? fullDeckBanner : plantsSelected ? plantsBanner : heroesBanner)
                : (tablet ? tabletTutorial : phoneTutorial);

            Canvas.ForceUpdateCanvases();
            Vector2 available = scroll.viewport.rect.size;
            float ratio = sprite.rect.width / sprite.rect.height;
            float width = tablet && !showDeck ? Mathf.Min(available.x, available.y * ratio) : available.x;
            image.sprite = sprite;
            float bannerHeight = width / ratio;
            float guideHeight = 0;
            if (!showDeck)
            {
                enemyGuide ??= new EnemyGuideView(scroll, image.rectTransform, bestDay.font, displayFont, exitMessage.font);
                guideHeight = enemyGuide.Layout(width, bannerHeight);
            }
            scroll.content.sizeDelta = new Vector2(width, bannerHeight + guideHeight);
            scroll.content.anchoredPosition = Vector2.zero;
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
            var inactiveTab = new Color(.70f, .74f, .77f);
            heroesTab.image.color = plantsSelected ? inactiveTab : Color.white;
            plantsTab.image.color = plantsSelected ? Color.white : inactiveTab;
        }

        public void SetSound(float value)
        {
            BloomFeedback.SetSound(value);
            preferencesDirty = true;
            RefreshSettingsLabels();
        }

        public void SetMusic(float value)
        {
            BloomFeedback.SetMusic(value);
            preferencesDirty = true;
            RefreshSettingsLabels();
        }

        public void ToggleVibration()
        {
            BloomFeedback.ToggleVibration();
            preferencesDirty = true;
            RefreshSettingsLabels();
        }

        public void ResetSettings()
        {
            soundSlider.SetValueWithoutNotify(1);
            musicSlider.SetValueWithoutNotify(1);
            BloomFeedback.SetSound(1);
            BloomFeedback.SetMusic(1);
            BloomFeedback.SetVibration(true);
            preferencesDirty = true;
            RefreshSettingsLabels();
            SavePreferences();
        }

        private void RefreshSettingsLabels()
        {
            soundValue.text = $"{Mathf.RoundToInt(BloomFeedback.SoundVolume * 100)}%";
            musicValue.text = $"{Mathf.RoundToInt(BloomFeedback.MusicVolume * 100)}%";
            vibrationValue.text = BloomFeedback.VibrationEnabled ? "ON" : "OFF";
        }

        public void OpenExit()
        {
            bool mobile = Application.isMobilePlatform;
            quitButton.gameObject.SetActive(!mobile);
            if (mobile)
            {
                var buttonBounds = (RectTransform)stayButton.transform;
                buttonBounds.anchorMin = new Vector2(.31f, buttonBounds.anchorMin.y);
                buttonBounds.anchorMax = new Vector2(.69f, buttonBounds.anchorMax.y);
            }
            exitMessage.text = mobile ? "Use your device’s Home gesture to leave the game."
                : "Close Bloom Heroes To Save Seed?";
            Open(exit, stayButton);
        }

        public void QuitGame()
        {
            SavePreferences();
            if (!Application.isMobilePlatform)
                Application.Quit();
        }

        public void Play()
        {
            if (storybook == null)
                StartEndless();
            else
                OpenStorybook(StorybookView.Section.Opening);
        }

        public void PlayTutorial() => LoadArena(tutorialRun: true);

        public void StartEndless() => LoadArena();

        public void StartStory() => StartGarden(0);

        internal void StartGarden(int garden)
        {
            if (garden >= 0 && garden < RelayGarden.Names.Length)
                LoadArena(garden: garden);
        }

        public void OpenMap() => OpenStorybook(StorybookView.Section.Map);

        private void OpenStorybook(StorybookView.Section section, int garden = 0)
        {
            if (storybook == null || sceneLoading)
                return;
            storybook.Show(this, section, garden);
            Open(storybook.gameObject, storybook.Primary);
        }

        internal static bool TakeTutorialRequest()
        {
            bool requested = tutorialRequested;
            tutorialRequested = false;
            return requested;
        }

        internal static int TakeStoryRequest()
        {
            int requested = storyRequested;
            storyRequested = -1;
            return requested;
        }

        internal static void ShowRestorationOnLoad(int garden) => restorationRequested = garden;

        private void LoadArena(bool tutorialRun = false, int garden = -1)
        {
            if (sceneLoading)
                return;

            SavePreferences();
            tutorialRequested = tutorialRun;
            storyRequested = garden;
            sceneLoading = true;
            Open(loading, null);
            loadingTitle.text = garden >= 0 ? RelayGarden.Names[garden] : "OPENING THE GREENHOUSE";
            lastLoadPercent = -1;
            ShowLoadProgress(0);
            StartCoroutine(LoadArenaScene());
        }

        private IEnumerator LoadArenaScene()
        {
            yield return null;
            arenaLoad = SceneManager.LoadSceneAsync(ArenaScene);
            if (arenaLoad == null)
            {
                sceneLoading = tutorialRequested = false;
                storyRequested = -1;
                ShowHome();
                yield break;
            }

            arenaLoad.allowSceneActivation = false;
            float shown = 0;
            while (shown < 1)
            {
                // Unity reports 0.9 when the scene is ready for activation.
                float ready = Mathf.Clamp01(arenaLoad.progress / .9f);
                shown = Mathf.MoveTowards(shown, ready, Time.unscaledDeltaTime * 1.8f);
                ShowLoadProgress(shown);
                yield return null;
            }
            loadingTitle.text = "READY TO DEFEND";
            yield return null;
            arenaLoad.allowSceneActivation = true;
            arenaLoad = null;
        }

        private void ShowLoadProgress(float amount)
        {
            loadingFill.enabled = amount > 0;
            loadingFill.rectTransform.anchorMax = new Vector2(amount, 1);
            int percent = Mathf.FloorToInt(amount * 100);
            if (percent != lastLoadPercent)
            {
                lastLoadPercent = percent;
                loadingPercent.SetText("{0:0}%", percent);
            }
        }

        private void Open(GameObject panel, Selectable focus)
        {
            if (current != null)
                current.SetActive(false);
            current = panel;
            current.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(focus != null ? focus.gameObject : null);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SavePreferences();
        }

        private void OnDestroy()
        {
            enemyGuide?.Dispose();
            if (arenaLoad != null)
                arenaLoad.allowSceneActivation = true;
        }

        private void SavePreferences()
        {
            if (!preferencesDirty)
                return;
            PlayerPrefs.Save();
            preferencesDirty = false;
        }
    }
}
