using System;
using UnityEngine;

namespace GaeBullBing.Presentation.Monsters
{
    public sealed class OverflowIndicatorView : MonoBehaviour
    {
        private Action<bool> hoverChanged;
        public void Initialize(int hiddenCount, Action<bool> callback)
        {
            hoverChanged = callback;
            var text = gameObject.AddComponent<TextMesh>();
            text.text = $"! +{hiddenCount}"; text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center; text.characterSize = .12f;
            text.fontSize = 48; text.color = new Color(1f, .25f, .15f);
            var renderer = GetComponent<MeshRenderer>(); renderer.sortingOrder = 30;
            var collider = gameObject.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.55f, .35f);
        }
        private void OnMouseEnter() => hoverChanged?.Invoke(true);
        private void OnMouseExit() => hoverChanged?.Invoke(false);
        private void OnDisable() => hoverChanged?.Invoke(false);
    }
}
