using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class DjPerformerController : MonoBehaviour
    {
        private Vector3 basePosition;
        private Quaternion baseRotation;

        private void Awake()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            float beat = Time.time * 2.15f;
            transform.position = basePosition + Vector3.up * (Mathf.Sin(beat) * 0.012f);
            transform.rotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(beat * 0.52f) * 1.2f, 0f);
        }
    }
}
