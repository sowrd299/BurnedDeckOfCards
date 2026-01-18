using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashworld
{
    public class InputManager : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] private float followSpeed = 15f;
        [SerializeField] private float snapSpeed = 12f;
        [SerializeField] private float dragOffsetZ = -1f; // bring card above board while dragging

        private Camera mainCam;
        private CardView draggingCard;
        private Vector3 dragOffset;
        private Vector3 originalPosition;
        private bool isDragging;

        public delegate bool OnCardViewDroppedCallback(CardView view);

        // Registered callbacks per zone
        private readonly Dictionary<CardZoneView, OnCardViewDroppedCallback> dropCallbacks = new();

        private void Awake()
        {
            mainCam = Camera.main;
        }

        private void Update()
        {
            HandleMouseInput();
            UpdateDragging();
        }

        private void HandleMouseInput()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                var hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

                if (hit.collider != null)
                {
                    var cardView = hit.collider.GetComponent<CardView>();
                    if (cardView != null)
                    {
                        StartDragging(cardView, mouseWorld);
                    }
                }
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
            {
                EndDragging();
            }
        }

        private void StartDragging(CardView card, Vector2 mouseWorld)
        {
            draggingCard = card;
            originalPosition = card.transform.position;
            dragOffset = card.transform.position - (Vector3)mouseWorld;
            isDragging = true;
        }

        private void UpdateDragging()
        {
            if (!isDragging || draggingCard == null) return;

            Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3 targetPos = (Vector3)mouseWorld + dragOffset;
            targetPos.z = dragOffsetZ;

            draggingCard.transform.position = Vector3.Lerp(
                draggingCard.transform.position,
                targetPos,
                Time.deltaTime * followSpeed);
        }

        private void EndDragging()
        {
            isDragging = false;

            // Check what zone we’re over
            Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            var hits = Physics2D.RaycastAll(mouseWorld, Vector2.zero);

            foreach (var hit in hits) {
                if (hit.collider != null)
                {
                    Debug.Log($"Placed on collider {hit.collider.gameObject.name}...");
                    var zone = hit.collider.GetComponent<CardZoneView>();
                    if (zone != null && dropCallbacks.TryGetValue(zone, out var callback))
                    {
                        Debug.Log("Collider is Zone...");
                        if (callback.Invoke(draggingCard)) {
                            // Snap into zone
                            Debug.Log("Zone accepted the card!");
                            SnapCardToZone(draggingCard, zone);
                            draggingCard = null;
                            return;
                        }
                    }
                }
            }

            // If no zone hit, return to original pos
            ReturnCardToOrigin(draggingCard);
            draggingCard = null;
        }

        private void SnapCardToZone(CardView card, CardZoneView zone)
        {
            if (card == null) return;

            StartCoroutine(LerpToPosition(card.transform, zone.GetDropPosition(card.Card), snapSpeed));
        }

        private void ReturnCardToOrigin(CardView card)
        {
            if (card == null) return;

            StartCoroutine(LerpToPosition(card.transform, originalPosition, snapSpeed));
        }

        private System.Collections.IEnumerator LerpToPosition(Transform obj, Vector3 target, float speed)
        {
            while (Vector3.Distance(obj.position, target) > 0.01f)
            {
                obj.position = Vector3.Lerp(obj.position, target, Time.deltaTime * speed);
                yield return null;
            }
            obj.position = target;
        }

        /// <summary>
        /// Register a callback for when a card is dropped in this zone.
        /// </summary>
        public void RegisterZoneCallback(CardZoneView zone, OnCardViewDroppedCallback callback)
        {
            if (zone == null) return;
            dropCallbacks[zone] = callback;
        }

        /// <summary>
        /// Unregister a zone callback.
        /// </summary>
        public void UnregisterZoneCallback(CardZoneView zone)
        {
            if (zone == null) return;
            if (dropCallbacks.ContainsKey(zone))
                dropCallbacks.Remove(zone);
        }
    }
}
