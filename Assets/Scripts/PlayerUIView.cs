using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ashworld
{
    public class PlayerUIView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject turnIndicator;
        [SerializeField] private List<Image> actionViews;

        [Header("Visual Settings")]
        [SerializeField] private float inactiveAlpha = 0.4f;
        [SerializeField] private float lerpSpeed = 8f;

        private Coroutine[] actionCoroutines;

        private void Awake()
        {
            if (actionViews != null && actionViews.Count > 0)
            {
                actionCoroutines = new Coroutine[actionViews.Count];
            }
        }

        /// <summary>
        /// Updates the player's UI to reflect turn status and actions.
        /// </summary>
        public void SetTurnInfo(int actionsRemaining, bool isPlayerTurn)
        {
            // Turn indicator
            if (turnIndicator != null)
                turnIndicator.SetActive(isPlayerTurn);

            // Update action views
            for (int i = 0; i < actionViews.Count; i++)
            {
                if (actionViews[i] == null) continue;

                bool available = (i < actionsRemaining) && isPlayerTurn;

                if (actionCoroutines[i] != null)
                    StopCoroutine(actionCoroutines[i]);

                actionCoroutines[i] = StartCoroutine(
                    LerpActionView(actionViews[i], available)
                );
            }
        }

        private IEnumerator LerpActionView(Image sr, bool available)
        {
            if (sr == null) yield break;

            float targetAlpha = available ? 1f : inactiveAlpha;
            Quaternion targetRot = available
                ? Quaternion.Euler(0f, 0f, 180f)
                : Quaternion.identity;

            while (true)
            {
                // Lerp rotation
                sr.transform.localRotation = Quaternion.Lerp(
                    sr.transform.localRotation,
                    targetRot,
                    Time.deltaTime * lerpSpeed);

                // Lerp alpha
                Color c = sr.color;
                c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * lerpSpeed);
                sr.color = c;

                // Stop when close enough
                if (Quaternion.Angle(sr.transform.localRotation, targetRot) < 0.5f &&
                    Mathf.Abs(sr.color.a - targetAlpha) < 0.01f)
                {
                    sr.transform.localRotation = targetRot;
                    c.a = targetAlpha;
                    sr.color = c;
                    break;
                }

                yield return null;
            }
        }
    }
}
