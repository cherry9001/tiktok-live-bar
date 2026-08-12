using UnityEngine;

namespace TikTokLiveGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        private SpriteRenderer target;
        private Sprite[] frames = System.Array.Empty<Sprite>();
        private float framesPerSecond = 12f;
        private float playbackSpeed = 1f;
        private float clock;
        private int frameIndex;

        private void Awake() => target = GetComponent<SpriteRenderer>();

        public void SetFrames(Sprite[] nextFrames, float fps = 12f)
        {
            frames = nextFrames ?? System.Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(1f, fps);
            clock = Random.value / framesPerSecond;
            frameIndex = 0;
            target.sprite = frames.Length > 0 ? frames[0] : null;
        }

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Clamp(speed, 0.5f, 3f);
        }

        private void Update()
        {
            if (frames.Length < 2) return;
            clock += Time.deltaTime * playbackSpeed;
            float frameDuration = 1f / framesPerSecond;
            while (clock >= frameDuration)
            {
                clock -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
                target.sprite = frames[frameIndex];
            }
        }
    }
}
