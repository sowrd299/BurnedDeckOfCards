using System;
using System.Collections;
using UnityEngine;

namespace Ashworld
{
    public class AttackAnimationView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform animatedBody; // The part of the card that moves
        [SerializeField] private ArrowView arrow;

        [Header("Settings")]
        [SerializeField] private float rotateSpeed = 10f;
        [SerializeField] private float rotateDuration = 0.2f;
        [SerializeField] private float punchDistance = 1.5f;
        [SerializeField] private float punchDuration = 0.3f;

        private void Awake() {
            arrow.SetVisible(false);
        }

        public IEnumerator PlayAttackerAnim(Transform targetPlayerCard, Action onHit, Action onComplete)
        {
            if (animatedBody == null)
            {
                onHit?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            Vector3 originalPos = animatedBody.localPosition;
            Quaternion originalRot = animatedBody.localRotation;

            // 1. Show Arrow and Point At
            if (arrow != null)
            {
                arrow.SetVisible(true);
                arrow.PointAt(targetPlayerCard.position);
            }

            // 2. Rotate toward target
            Vector3 direction = targetPlayerCard.position - animatedBody.position;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // Assumes card vertical is +Y
            Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);

            float elapsed = 0;
            while (elapsed < rotateDuration)
            {
                elapsed += Time.deltaTime;
                animatedBody.localRotation = Quaternion.Slerp(originalRot, targetRot, elapsed / rotateDuration);
                yield return null;
            }
            animatedBody.localRotation = targetRot;

            // 3. Punch Toward (Apex)
            Vector3 punchTarget = originalPos + animatedBody.up * punchDistance;
            
            elapsed = 0;
            float halfPunch = punchDuration / 2f;
            while (elapsed < halfPunch)
            {
                elapsed += Time.deltaTime;
                animatedBody.localPosition = Vector3.Lerp(originalPos, punchTarget, elapsed / halfPunch);
                yield return null;
            }
            animatedBody.localPosition = punchTarget;

            // HIT!
            onHit?.Invoke();

            // 4. Return
            elapsed = 0;
            while (elapsed < halfPunch)
            {
                elapsed += Time.deltaTime;
                animatedBody.localPosition = Vector3.Lerp(punchTarget, originalPos, elapsed / halfPunch);
                yield return null;
            }
            animatedBody.localPosition = originalPos;

            // 5. Reset
            if (arrow != null) arrow.SetVisible(false);
            
            elapsed = 0;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                animatedBody.localRotation = Quaternion.Slerp(targetRot, originalRot, elapsed / 0.1f);
                yield return null;
            }
            animatedBody.localRotation = originalRot;

            onComplete?.Invoke();
        }
    }
}
