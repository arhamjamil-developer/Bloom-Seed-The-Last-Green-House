using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloombots
{
    public sealed class FieldGuideView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image teacher;
        [SerializeField] private Sprite dubuCard, arhamCard;
        [SerializeField] private TMP_Text heading, instructions, stepCounter, nextLabel;
        [SerializeField] private UnityEngine.UI.Button nextButton, skipButton;
        private float panelAspect;
        private Vector2 lastSafeSize;

        public void Bind(Action next, Action skip)
        {
            panelAspect = GetComponent<AspectRatioFitter>().aspectRatio;
            FitToSafeArea();
            var panel = (RectTransform)transform;
            ProductionPanel.Fill(panel, ProductionPanel.Ink, hit: true);
            ProductionPanel.Rule(panel, 1, ProductionPanel.Brass);
            ProductionPanel.Rule(panel, 0, ProductionPanel.Brass);
            ProductionPanel.Square(nextButton, ProductionPanel.Blue);
            ProductionPanel.Square(skipButton, ProductionPanel.Red);
            nextButton.onClick.AddListener(() => next());
            skipButton.onClick.AddListener(() => skip());
        }

        public void Show(int step, bool arham, string title, string message, string action = null)
        {
            teacher.sprite = arham ? arhamCard : dubuCard;
            heading.text = title;
            instructions.text = message;
            stepCounter.text = $"{step} / 3";
            nextButton.gameObject.SetActive(action != null);
            if (action != null)
                nextLabel.text = action;
            gameObject.SetActive(true);
        }

        private void LateUpdate() => FitToSafeArea();

        private void FitToSafeArea()
        {
            var rect = (RectTransform)transform;
            if (panelAspect <= 0 || rect.parent is not RectTransform safe)
                return;

            Vector2 size = safe.rect.size;
            if (size == lastSafeSize || size.x <= 0 || size.y <= 0)
                return;

            lastSafeSize = size;
            float halfWidth = .5f * Mathf.Min(.95f, size.y * .220f * panelAspect / size.x);
            rect.anchorMin = new Vector2(.5f - halfWidth, .888f);
            rect.anchorMax = new Vector2(.5f + halfWidth, .888f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, rect.sizeDelta.y);
        }

    }
}
