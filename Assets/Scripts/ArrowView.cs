using UnityEngine;

namespace Ashworld
{
    public class ArrowView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer arrowRenderer;
        [SerializeField] private Transform root;

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public void PointAt(Vector3 targetWorldPosition)
        {
            Vector3 direction = (targetWorldPosition - root.position);
            direction.z = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                root.up = direction.normalized;
            }
        }

        public void SetColor(Color color)
        {
            if (arrowRenderer != null)
                arrowRenderer.color = color;
        }

        public void SetDirection(Vector3 direction)
        {
            direction.z = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                root.up = direction.normalized;
            }
        }
    }
}
