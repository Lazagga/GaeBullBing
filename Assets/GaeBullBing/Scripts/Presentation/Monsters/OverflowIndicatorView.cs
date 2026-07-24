using UnityEngine;

namespace GaeBullBing.Presentation.Monsters
{
    public sealed class OverflowIndicatorView : MonoBehaviour
    {
public void Initialize(int hiddenCount)
        {
            var text = gameObject.AddComponent<TextMesh>();
            text.text = $"! +{hiddenCount}";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = .12f;
            text.fontSize = 48;
            text.color = new Color(1f, .25f, .15f);
            var renderer = GetComponent<MeshRenderer>();
            renderer.sortingOrder = 32760;
        }



    }
}
