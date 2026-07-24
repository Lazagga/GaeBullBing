using UnityEngine;

namespace GaeBullBing.Presentation.Board
{
    public sealed class HoverMarkerView : MonoBehaviour
    {
        [SerializeField] private float amplitude = .07f;
        [SerializeField] private float frequency = 3.2f;
        private Vector3 baseLocalPosition;
        private float phase;

        public static HoverMarkerView Create(
            Transform parent,
            Vector3 localPosition,
            Color color,
            int sortingLayerId)
        {
            var markerObject = new GameObject("Position Marker");
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.localPosition = localPosition;
            markerObject.transform.localScale = new Vector3(.2f, .14f, 1f);
            markerObject.transform.localRotation = Quaternion.identity;

            var renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateTriangleSprite();
            renderer.color = color;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = 32760;

            var marker = markerObject.AddComponent<HoverMarkerView>();
            marker.baseLocalPosition = localPosition;
            marker.phase = Random.value * Mathf.PI * 2f;
            return marker;
        }

        private void Update()
        {
            var offset = Mathf.Sin(Time.time * frequency + phase) * amplitude;
            transform.localPosition = baseLocalPosition + Vector3.up * offset;
        }

        private static Sprite CreateTriangleSprite()
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Position Marker",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var halfWidth = Mathf.RoundToInt((y + 1) * .5f);
                var center = (size - 1) * .5f;
                for (var x = 0; x < size; x++)
                    pixels[y * size + x] = Mathf.Abs(x - center) <= halfWidth
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }
    }
}
