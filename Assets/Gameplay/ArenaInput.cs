using UnityEngine;
using UnityEngine.EventSystems;

namespace Bloombots.Gameplay
{
    internal sealed class ArenaBoardInput : MonoBehaviour, IPointerClickHandler
    {
        public EndlessArena Arena { private get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Arena.BoardTap(eventData.position);
        }
    }

    internal sealed class ArenaCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private enum Route { None, Pending, Page, Board }
        private Route route;
        private int pointer;
        private EndlessArena arena;
        private BloomCardView card;
        private CardHandPager pager;

        public void Bind(EndlessArena game, BloomCardView view)
        {
            arena = game;
            card = view;
            pager = view.GetComponentInParent<CardHandPager>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (route != Route.None || eventData.button != PointerEventData.InputButton.Left)
                return;
            pointer = eventData.pointerId;
            route = Route.Pending;
            ResolveRoute(eventData);
        }

        private void ResolveRoute(PointerEventData eventData)
        {
            var delta = eventData.position - eventData.pressPosition;
            if (delta.y > 0 && delta.y >= Mathf.Abs(delta.x) * .95f)
            {
                if (!arena.BeginCardDrag(card, pointer, eventData.position))
                    return;
                route = Route.Board;
            }
            else if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && pager != null)
            {
                route = Route.Page;
                pager.OnBeginDrag(eventData);
            }
            if (route != Route.Pending)
                eventData.eligibleForClick = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointer)
                return;
            if (route == Route.Pending)
                ResolveRoute(eventData);
            if (route == Route.Page)
                pager.OnDrag(eventData);
            else if (route == Route.Board)
                arena.MoveCardDrag(pointer, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointer)
                return;
            if (route == Route.Page)
                pager.OnEndDrag(eventData);
            else if (route == Route.Board)
                arena.EndCardDrag(pointer, eventData.position);
            route = Route.None;
        }

        private void OnDisable()
        {
            if (route == Route.Board)
                arena.CancelCardDrag(pointer);
            route = Route.None;
        }
    }
}
