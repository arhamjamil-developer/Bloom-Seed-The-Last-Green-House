using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Bloombots
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BloomCardView : MonoBehaviour
    {
        [Header("Card")]
        [SerializeField] private string cardId = "cactus-guard";
        [SerializeField] private string displayName = "Cactus Guard";
        [SerializeField] private string role = "Lane guard";
        [SerializeField, TextArea(1, 3)] private string description = "Blocks bugs and counters on contact.";
        [SerializeField] private Sprite portrait;
        [SerializeField] private Sprite roleIcon;
        [SerializeField] private Color accent = new(0.25f, 0.86f, 0.53f);
        [SerializeField] private bool numericCostBadge;

        [Header("Deployment")]
        [SerializeField, Min(0)] private int sunCost = 2;
        [SerializeField, Min(0)] private float rechargeSeconds = 4;
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private UnityEvent<BloomCardView> onPlacementRequested = new();

        [Header("Inspector Preview")]
        [SerializeField, Min(0)] private int previewSun = 5;
        [SerializeField, Min(0)] private float previewRechargeRemaining;
        [SerializeField] private bool previewHasOpenTile = true;

        [SerializeField, HideInInspector] private UnityEngine.UI.Button selectButton;
        [SerializeField, HideInInspector] private UnityEngine.UI.Image portraitImage;
        [SerializeField, HideInInspector] private UnityEngine.UI.Image roleImage;
        [SerializeField, HideInInspector] private UnityEngine.UI.Image chargeFill;
        [SerializeField, HideInInspector] private TMP_Text nameLabel;
        [SerializeField, HideInInspector] private TMP_Text roleLabel;
        [SerializeField, HideInInspector] private TMP_Text descriptionLabel;
        [SerializeField, HideInInspector] private TMP_Text costLabel;
        [SerializeField, HideInInspector] private TMP_Text statusLabel;

        private bool previewDirty;
        private bool hasRuntimeState;
        private int availableSun;
        private float rechargeRemaining;
        private bool hasOpenTile;
        private bool guideLocked;

        public string CardId => cardId;
        public GameObject UnitPrefab => unitPrefab;
        public int SunCost => sunCost;
        public float RechargeSeconds => rechargeSeconds;
        public UnityEvent<BloomCardView> OnPlacementRequested => onPlacementRequested;
        public bool CanRequestPlacement =>
            hasRuntimeState && !guideLocked && availableSun >= sunCost && rechargeRemaining <= 0 && hasOpenTile;

        private void OnEnable()
        {
            if (Application.IsPlaying(gameObject) && selectButton != null)
                selectButton.onClick.AddListener(RequestPlacement);

            RefreshView();
        }

        private void OnDisable()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(RequestPlacement);
        }

        private void OnValidate() => previewDirty = true;

        private void Update()
        {
            if (!previewDirty)
                return;

            previewDirty = false;
            RefreshView();
        }

        /// <summary>Updates the card from the current wallet, cooldown and placement rules.</summary>
        public void SetAvailability(int currentSun, float remainingSeconds, bool canPlaceOnBoard, bool lockedByGuide = false)
        {
            availableSun = Mathf.Max(0, currentSun);
            rechargeRemaining = Mathf.Max(0, remainingSeconds);
            hasOpenTile = canPlaceOnBoard;
            guideLocked = lockedByGuide;
            hasRuntimeState = true;
            RefreshView();
        }

        private void RequestPlacement()
        {
            if (CanRequestPlacement)
                onPlacementRequested.Invoke(this);
        }

        [ContextMenu("Refresh Card Preview")]
        public void RefreshView()
        {
            if (selectButton == null)
                return;

            bool playing = Application.IsPlaying(gameObject);
            int sun = playing ? availableSun : previewSun;
            float remaining = playing ? rechargeRemaining : previewRechargeRemaining;
            bool openTile = playing ? hasOpenTile : previewHasOpenTile;
            float charge = rechargeSeconds > 0 ? 1 - Mathf.Clamp01(remaining / rechargeSeconds) : 1;
            bool shortOnSun = sun < sunCost && (!playing || hasRuntimeState && !guideLocked);

            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            portraitImage.color = shortOnSun ? new Color(.60f, .60f, .60f) : Color.white;
            roleImage.sprite = roleIcon;
            roleImage.enabled = roleIcon != null;
            nameLabel.text = displayName;
            roleLabel.text = role;
            descriptionLabel.text = description;
            costLabel.text = numericCostBadge ? sunCost.ToString() : $"SUN  {sunCost}";
            if (numericCostBadge)
                costLabel.color = shortOnSun ? new Color(1, .74f, .57f) : Color.white;

            chargeFill.color = accent;
            chargeFill.enabled = charge > 0;
            chargeFill.rectTransform.anchorMax = new Vector2(charge, 1);

            selectButton.interactable = playing ? CanRequestPlacement : sun >= sunCost && remaining <= 0 && openTile;
            statusLabel.enabled = !numericCostBadge || shortOnSun;
            if (playing && !hasRuntimeState)
                statusLabel.text = "Waiting for board";
            else if (playing && guideLocked)
                statusLabel.text = "Follow guide";
            else if (sun < sunCost)
                statusLabel.text = $"NEED {sunCost - sun} SUN";
            else if (remaining > 0)
                statusLabel.text = $"Charging {remaining:0.0}s";
            else if (!openTile)
                statusLabel.text = "No open tile";
            else
                statusLabel.text = "Ready to place";
        }
    }
}
