using UnityEngine;

namespace GaeBullBing.Presentation.Board
{
    internal sealed class BoardCharacterShadow
    {
        private const float Width = .52f;
        private static Sprite sharedSprite;
        private readonly SpriteRenderer renderer;

        private BoardCharacterShadow(SpriteRenderer renderer)
        {
            this.renderer = renderer;
        }

        public static BoardCharacterShadow Create(Transform parent, int sortingLayerId)
        {
            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(parent, false);
            shadowObject.transform.localScale = Vector3.one * Width;
            var renderer = shadowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite();
            renderer.color = new Color(0f, 0f, 0f, .45f);
            renderer.sortingLayerID = sortingLayerId;
            return new BoardCharacterShadow(renderer);
        }

        public void Set(Vector3 groundPosition)
        {
            if (renderer == null) return;
            renderer.transform.position = groundPosition;
            renderer.sortingOrder = BoardDepthSorting.GetOrder(groundPosition, -20);
        }

        public void SetVisible(bool visible)
        {
            if (renderer != null) renderer.enabled = visible;
        }

        private static Sprite GetSprite()
        {
            if (sharedSprite != null) return sharedSprite;
            const int width = 64;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Character Shadow Ellipse",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            var centerX = (width - 1) * .5f;
            var centerY = (height - 1) * .5f;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var dx = (x - centerX) / centerX;
                var dy = (y - centerY) / centerY;
                pixels[y * width + x] = dx * dx + dy * dy <= 1f
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            sharedSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 64f);
            return sharedSprite;
        }
    }
}
