using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace Ashworld
{
    public class ChapterAnimationView : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool reverseAnimationDirection; // False: Top->Bot, True: Bot->Top
        [SerializeField] private string victorySuffix = "draw closer to victory";

        [Header("References")]
        [SerializeField] private TextMeshPro chapterTitleText;
        [SerializeField] private TextMeshPro descriptionText;

        [Header("Animation Settings")]
        [SerializeField] private float slideDuration = 0.5f;
        [SerializeField] private float stayDuration = 1.0f;
        [SerializeField] private float offscreenOffset = 10f;

        private Vector3 screenCenter;
        private Vector3 screenStart;
        private Vector3 screenEnd;

        private void Awake()
        {
            screenCenter = transform.position;
            
            float direction = reverseAnimationDirection ? -1f : 1f;
            screenStart = screenCenter + new Vector3(0, offscreenOffset * direction, 0);
            screenEnd = screenCenter + new Vector3(0, -offscreenOffset * direction, 0);

            // Hide initially
            transform.position = screenStart;
        }

        public IEnumerator PlayTransition(int chapterNum, string cardName, Action onCovered)
        {
            // 1. Setup Text
            if (chapterTitleText != null) chapterTitleText.text = "Chapter " + chapterNum;
            if (descriptionText != null) descriptionText.text = $"{cardName}\n-\n{victorySuffix}";

            // 2. Slide In
            yield return StartCoroutine(MoveTo(screenCenter, slideDuration));

            // 3. Callback while covered
            onCovered?.Invoke();

            // 4. Stay
            yield return new WaitForSeconds(stayDuration);

            // 5. Slide Out
            yield return StartCoroutine(MoveTo(screenEnd, slideDuration));

            // 6. Reset to start for next time
            transform.position = screenStart;
        }

        private IEnumerator MoveTo(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.position = target;
        }
    }
}
