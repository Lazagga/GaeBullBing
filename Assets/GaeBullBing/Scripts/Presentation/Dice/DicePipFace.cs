using UnityEngine;

namespace GaeBullBing.Presentation.Dice
{
    internal sealed class DicePipFace
    {
        private static Mesh diskMesh;
        private readonly GameObject[] pips;
        private DicePipFace(GameObject[] pips) => this.pips = pips;

        public static DicePipFace Create(Transform parent, Vector3 normal, Material material, float size = .125f)
        {
            var tangent = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .9f ? Vector3.right : Vector3.Cross(Vector3.up, normal).normalized;
            var vertical = Vector3.Cross(normal, tangent).normalized;
            var positions = new[] { Vector2.zero, new Vector2(-.24f,.24f), new Vector2(.24f,.24f), new Vector2(-.24f,0f), new Vector2(.24f,0f), new Vector2(-.24f,-.24f), new Vector2(.24f,-.24f) };
            var objects = new GameObject[positions.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                var pip = new GameObject("Pip", typeof(MeshFilter), typeof(MeshRenderer));
                pip.name = "Pip";
                pip.transform.SetParent(parent, false);
                pip.transform.localPosition = normal * .507f + tangent * positions[i].x + vertical * positions[i].y;
                pip.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, normal);
                pip.transform.localScale = Vector3.one * size;
                pip.GetComponent<MeshFilter>().sharedMesh = GetDiskMesh();
                pip.GetComponent<MeshRenderer>().sharedMaterial = material;
                objects[i] = pip;
            }
            return new DicePipFace(objects);
        }

        public void SetValue(int value)
        {
            foreach (var pip in pips) pip.SetActive(false);
            switch (value)
            {
                case 1: Enable(0); break;
                case 2: Enable(1,6); break;
                case 3: Enable(1,0,6); break;
                case 4: Enable(1,2,5,6); break;
                case 5: Enable(1,2,0,5,6); break;
                case 6: Enable(1,2,3,4,5,6); break;
            }
        }

        private void Enable(params int[] indices) { foreach (var index in indices) pips[index].SetActive(true); }

        private static Mesh GetDiskMesh()
        {
            if (diskMesh != null) return diskMesh;
            const int segments = 20;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            for (var i = 0; i < segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var triangle = i * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = i + 1;
                triangles[triangle + 2] = (i + 1) % segments + 1;
            }
            diskMesh = new Mesh { name = "Dice Pip Disc", vertices = vertices, triangles = triangles };
            diskMesh.RecalculateBounds();
            return diskMesh;
        }
    }
}
