using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.UI
{
    public sealed class DiceFacePipGraphic : MaskableGraphic
    {
        [SerializeField, Range(1, 6)] private int value = 1;
        [SerializeField] private Color faceColor = Color.white;
        [SerializeField] private Color pipColor = Color.black;
        [SerializeField] private Color borderColor = Color.black;

        public void SetValue(int faceValue)
        {
            value = Mathf.Clamp(faceValue, 1, 6);
            SetVerticesDirty();
        }

        public void SetInverted(bool inverted)
        {
            faceColor = inverted ? new Color(.04f, .04f, .04f, 1f) : Color.white;
            pipColor = inverted ? Color.white : Color.black;
            borderColor = inverted ? Color.white : Color.black;
            SetVerticesDirty();
        }

public void SetColors(Color dieColor, bool blackPips)
        {
            faceColor = dieColor;
            pipColor = blackPips ? Color.black : Color.white;
            borderColor = pipColor;
            SetVerticesDirty();
        }


        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            AddQuad(vertexHelper, rect, faceColor);

            var border = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * .035f);
            AddQuad(vertexHelper, new Rect(rect.xMin, rect.yMin, rect.width, border), borderColor);
            AddQuad(vertexHelper, new Rect(rect.xMin, rect.yMax - border, rect.width, border), borderColor);
            AddQuad(vertexHelper, new Rect(rect.xMin, rect.yMin, border, rect.height), borderColor);
            AddQuad(vertexHelper, new Rect(rect.xMax - border, rect.yMin, border, rect.height), borderColor);

            var left = rect.xMin + rect.width * .28f;
            var right = rect.xMax - rect.width * .28f;
            var bottom = rect.yMin + rect.height * .28f;
            var top = rect.yMax - rect.height * .28f;
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * .075f;

            if (value == 1 || value == 3 || value == 5) AddCircle(vertexHelper, center, radius, pipColor);
            if (value >= 2)
            {
                AddCircle(vertexHelper, new Vector2(left, top), radius, pipColor);
                AddCircle(vertexHelper, new Vector2(right, bottom), radius, pipColor);
            }
            if (value >= 4)
            {
                AddCircle(vertexHelper, new Vector2(right, top), radius, pipColor);
                AddCircle(vertexHelper, new Vector2(left, bottom), radius, pipColor);
            }
            if (value == 6)
            {
                AddCircle(vertexHelper, new Vector2(left, center.y), radius, pipColor);
                AddCircle(vertexHelper, new Vector2(right, center.y), radius, pipColor);
            }
        }

        private static void AddQuad(VertexHelper vertexHelper, Rect rect, Color color)
        {
            var start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddCircle(VertexHelper vertexHelper, Vector2 center, float radius, Color color)
        {
            const int segments = 16;
            var start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, color, Vector2.zero);
            for (var index = 0; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                vertexHelper.AddVert(
                    center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    color,
                    Vector2.zero);
            }
            for (var index = 0; index < segments; index++)
                vertexHelper.AddTriangle(start, start + index + 1, start + index + 2);
        }
    }
}
