using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloombots
{
    [DisallowMultipleComponent]
    public sealed class CardHandPager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform heroesPage;
        [SerializeField] private RectTransform plantsPage;
        [SerializeField] private Image heroesTab;
        [SerializeField] private Image plantsTab;
        [SerializeField] private Sprite selectedTab;
        [SerializeField] private Sprite unselectedTab;

        private const float SwipeThreshold = 0.12f;
        private const float SnapSpeed = 6f;
        private bool dragging;
        private int dragPointer;
        private float pressX;
        private float pressPosition;
        private float position;
        private bool lockedToHeroes;

        public int ActivePage { get; private set; }

        private void OnEnable()
        {
            position = ActivePage;
            ApplyPagePositions();
            RefreshTabs();
        }

        private void OnDisable() => dragging = false;

        private void LateUpdate()
        {
            if (!dragging)
                position = Mathf.MoveTowards(position, ActivePage, SnapSpeed * Time.unscaledDeltaTime);
            ApplyPagePositions();
        }

        public void ShowHeroes() => SelectPage(0);

        public void ShowPlants() => SelectPage(1);

        public void SelectPage(int page, bool immediate = false)
        {
            if (lockedToHeroes && page != 0)
                return;
            ActivePage = Mathf.Clamp(page, 0, 1);
            dragging = false;
            if (immediate || !Application.isPlaying)
            {
                position = ActivePage;
                ApplyPagePositions();
            }
            RefreshTabs();
        }

        public void LockToHeroes(bool locked)
        {
            lockedToHeroes = locked;
            if (locked)
                SelectPage(0, immediate: true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (lockedToHeroes || dragging || eventData.button != PointerEventData.InputButton.Left || viewport == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewport, eventData.pressPosition, eventData.pressEventCamera, out var press))
                return;

            dragPointer = eventData.pointerId;
            pressX = press.x;
            pressPosition = position;
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!TryGetTravel(eventData, out float travel))
                return;

            position = Mathf.Clamp01(pressPosition + travel);
            ApplyPagePositions();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging || dragPointer != eventData.pointerId)
                return;

            int next = ActivePage;
            if (TryGetTravel(eventData, out float travel) && Mathf.Abs(travel) >= SwipeThreshold)
                next += travel > 0 ? 1 : -1;

            SelectPage(next);
        }

        private bool TryGetTravel(PointerEventData eventData, out float travel)
        {
            travel = 0;
            if (!dragging || dragPointer != eventData.pointerId || viewport == null || viewport.rect.width <= 0
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewport, eventData.position, eventData.pressEventCamera, out var cursor))
                return false;

            travel = (pressX - cursor.x) / viewport.rect.width;
            return true;
        }

        private void ApplyPagePositions()
        {
            if (viewport == null || heroesPage == null || plantsPage == null)
                return;

            float width = viewport.rect.width;
            heroesPage.anchoredPosition = new Vector2(-position * width, 0);
            plantsPage.anchoredPosition = new Vector2(-position * width, 0);
        }

        private void RefreshTabs()
        {
            if (heroesTab != null)
                heroesTab.sprite = ActivePage == 0 ? selectedTab : unselectedTab;
            if (plantsTab != null)
                plantsTab.sprite = ActivePage == 1 ? selectedTab : unselectedTab;
        }
    }
}

