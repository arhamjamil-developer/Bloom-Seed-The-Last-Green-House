using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloombots
{
    public sealed class ArenaHeaderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text day, sun, phase, health, hint, visor;
        [SerializeField] private UnityEngine.UI.Button pauseButton, visorButton;
        private int lastSun = -1;
        private UnityEngine.UI.Image hintBacking;

        public void Bind(Action pause, Action toggleVisor)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(.016f, .905f);
            rect.anchorMax = new Vector2(.984f, .993f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var plate = new GameObject("Hint backing", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            plate.layer = hint.gameObject.layer;
            var plateRect = (RectTransform)plate.transform;
            plateRect.SetParent(hint.transform.parent, false);
            plateRect.SetSiblingIndex(hint.transform.GetSiblingIndex());
            plateRect.anchorMin = new Vector2(.035f, -.53f);
            plateRect.anchorMax = new Vector2(.732f, -.025f);
            plateRect.offsetMin = plateRect.offsetMax = Vector2.zero;
            hintBacking = plate.GetComponent<UnityEngine.UI.Image>();
            hintBacking.color = new Color(.035f, .095f, .11f, .89f);
            hintBacking.raycastTarget = false;
            pauseButton.onClick.AddListener(() => pause());
            visorButton.onClick.AddListener(() => toggleVisor());
        }

        public void Show(string dayText, string phaseText, string healthText, string tip, int sunAmount,
            bool night, bool visorActive, float charge)
        {
            SetText(day, dayText);
            SetText(phase, phaseText);
            SetText(health, healthText);
            SetText(hint, tip);
            hintBacking.enabled = !string.IsNullOrEmpty(tip);
            if (lastSun != sunAmount)
            {
                lastSun = sunAmount;
                sun.text = $"SUN DROPS  {sunAmount}";
            }
            if (visorButton.gameObject.activeSelf != night)
                visorButton.gameObject.SetActive(night);
            if (night)
                SetText(visor, $"VISOR {(visorActive ? "ON" : "OFF")}  {Mathf.CeilToInt(charge * 100)}%");
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label.text != value)
                label.text = value;
        }
    }
}
