using System;
using System.Collections;
using UnityEngine;

namespace Ashworld
{
    public class FireEffectView : MonoBehaviour
    {
        [SerializeField] private Transform fireVisual;
        [SerializeField] private float peakScale = 1.5f;
        [SerializeField] private float duration = 0.5f;

        private void Awake()
        {
            if (fireVisual != null)
                fireVisual.localScale = Vector3.zero;
        }

        public IEnumerator PlayFire(Action onComplete)
        {
            if (fireVisual == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Scale Up
            float elapsed = 0;
            float halfDuration = duration;
            
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                fireVisual.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * peakScale, t);
                yield return null;
            }

            fireVisual.localScale = Vector3.one * peakScale;

            onComplete?.Invoke();
        }
    }
}
