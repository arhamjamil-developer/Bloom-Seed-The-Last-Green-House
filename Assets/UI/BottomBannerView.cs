using TMPro;
using UnityEngine;

namespace Bloombots
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BottomBannerView : MonoBehaviour
    {
        [SerializeField, Min(0)] private int sun = 50;
        [SerializeField, Range(0, 6)] private int chargedPips = 2;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private TMP_Text sunLabel;
        [SerializeField] private TMP_Text sunCaption;
        [SerializeField] private UnityEngine.UI.Image sunFill;
        [SerializeField] private UnityEngine.UI.Image[] pips;
        [SerializeField] private Sprite chargedPip;
        [SerializeField] private Sprite emptyPip;
        [SerializeField] private Canvas canvas;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool displayDirty;
        private int sunScale = 12, recentSpend;
        private float shownFill, spendUntil;

        private static readonly Color FullSun = new(1, .81f, .29f);
        private static readonly Color LowSun = new(1, .46f, .22f);

        private void OnEnable()
        {
            lastScreenSize = default;
            ResetSunMeter(sun);
            RefreshSafeArea();
            RefreshDisplay();
        }

        private void OnValidate() => displayDirty = true;

        private void Update()
        {
            RefreshSafeArea();
            if (displayDirty)
                RefreshDisplay();
            if (Application.IsPlaying(gameObject))
            {
                float target = (float)sun / sunScale;
                if (!Mathf.Approximately(shownFill, target))
                {
                    shownFill = Mathf.MoveTowards(shownFill, target, Time.unscaledDeltaTime * .9f);
                    DrawSunMeter();
                }
                if (recentSpend > 0 && Time.unscaledTime >= spendUntil)
                {
                    recentSpend = 0;
                    UpdateSunCaption();
                }
            }
        }

        public void SetDisplay(int availableSun, int mergePips)
        {
            sun = Mathf.Max(0, availableSun);
            sunScale = Mathf.Max(sunScale, sun);
            chargedPips = Mathf.Clamp(mergePips, 0, pips?.Length ?? 0);
            RefreshDisplay();
        }

        internal void ResetSunMeter(int balance)
        {
            sun = Mathf.Max(0, balance);
            sunScale = Mathf.Max(12, sun);
            shownFill = (float)sun / sunScale;
            recentSpend = 0;
            DrawSunMeter();
            UpdateSunCaption();
        }

        internal void ShowSunSpend(int cost)
        {
            if (cost <= 0)
                return;
            recentSpend += cost;
            spendUntil = Time.unscaledTime + .85f;
            UpdateSunCaption();
        }

        private void DrawSunMeter()
        {
            if (sunFill == null)
                return;
            sunFill.enabled = shownFill > 0;
            sunFill.rectTransform.anchorMax = new Vector2(shownFill, 1);
            sunFill.color = Color.Lerp(LowSun, FullSun, shownFill);
        }

        private void UpdateSunCaption()
        {
            if (sunCaption == null)
                return;
            sunCaption.text = recentSpend > 0 ? $"−{recentSpend} SUN" : "SUN DROPS";
            sunCaption.color = recentSpend > 0 ? new Color(1, .68f, .37f) : new Color(1, .94f, .75f);
        }

        private void RefreshDisplay()
        {
            displayDirty = false;
            if (sunLabel != null)
                sunLabel.text = sun.ToString();
            if (!Application.IsPlaying(gameObject))
                ResetSunMeter(sun);

            if (pips == null)
                return;

            for (int i = 0; i < pips.Length; i++)
                if (pips[i] != null)
                    pips[i].sprite = i < chargedPips ? chargedPip : emptyPip;
        }

        private void RefreshSafeArea()
        {
            if (safeArea == null || canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                return;

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var area = Screen.safeArea;
            if (screenSize.x == 0 || screenSize.y == 0 || (area == lastSafeArea && screenSize == lastScreenSize))
                return;

            lastSafeArea = area;
            lastScreenSize = screenSize;
            safeArea.anchorMin = new Vector2(area.xMin / screenSize.x, area.yMin / screenSize.y);
            safeArea.anchorMax = new Vector2(area.xMax / screenSize.x, area.yMax / screenSize.y);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }
    }
}
