using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GlobalDeformationDemo
{
    public sealed class FfdControlPoint : MonoBehaviour
    {
        public int Index;
    }

    public sealed class FfdLatticeView : MonoBehaviour
    {
        private readonly List<FfdControlPoint> handles = new List<FfdControlPoint>();
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private MeshDeformer deformer;
        private Camera sceneCamera;
        private Transform latticeRoot;
        private Material lineMaterial;
        private Material handleMaterial;
        private FfdControlPoint selected;
        private Plane dragPlane;
        private bool visible;

        public void Initialize(MeshDeformer meshDeformer, Camera cameraToUse)
        {
            deformer = meshDeformer;
            sceneCamera = cameraToUse;
            latticeRoot = new GameObject("FFD Lattice").transform;
            latticeRoot.SetParent(transform, false);

            Shader lineShader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(lineShader) { name = "FFD Grid Material", color = new Color(0.1f, 0.9f, 1f, 0.8f) };
            Shader handleShader = Shader.Find("Standard");
            handleMaterial = new Material(handleShader) { name = "FFD Point Material", color = new Color(1f, 0.55f, 0.05f) };

            deformer.LatticeChanged += Rebuild;
            Rebuild();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (deformer != null)
            {
                deformer.LatticeChanged -= Rebuild;
            }
            if (lineMaterial != null) Destroy(lineMaterial);
            if (handleMaterial != null) Destroy(handleMaterial);
        }

        private void Update()
        {
            if (!visible || sceneCamera == null || Input.GetMouseButton(1))
            {
                return;
            }

            if (Input.GetMouseButtonDown(0)
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
                float nearest = float.MaxValue;
                foreach (RaycastHit hit in hits)
                {
                    FfdControlPoint handle = hit.collider.GetComponent<FfdControlPoint>();
                    if (handle != null && hit.distance < nearest)
                    {
                        nearest = hit.distance;
                        selected = handle;
                    }
                }

                if (selected != null)
                {
                    dragPlane = new Plane(-sceneCamera.transform.forward, selected.transform.position);
                }
            }

            if (Input.GetMouseButton(0) && selected != null)
            {
                Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
                if (dragPlane.Raycast(ray, out float distance))
                {
                    Vector3 worldPosition = ray.GetPoint(distance);
                    Vector3 localPosition = latticeRoot.InverseTransformPoint(worldPosition);
                    selected.transform.localPosition = localPosition;
                    deformer.SetLatticePoint(selected.Index, localPosition);
                    UpdateLines();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                selected = null;
            }
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (latticeRoot != null)
            {
                latticeRoot.gameObject.SetActive(value);
            }
        }

        public void Rebuild()
        {
            if (deformer == null || latticeRoot == null || deformer.LatticePoints == null)
            {
                return;
            }

            for (int index = latticeRoot.childCount - 1; index >= 0; index--)
            {
                DestroyImmediateSafe(latticeRoot.GetChild(index).gameObject);
            }
            handles.Clear();
            lines.Clear();

            float maximumSize = Mathf.Max(
                deformer.OriginalBounds.size.x,
                deformer.OriginalBounds.size.y,
                deformer.OriginalBounds.size.z);
            float pointSize = Mathf.Max(0.025f, maximumSize * 0.035f);

            for (int index = 0; index < deformer.LatticePoints.Length; index++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"FFD Point {index}";
                sphere.transform.SetParent(latticeRoot, false);
                sphere.transform.localPosition = deformer.LatticePoints[index];
                sphere.transform.localScale = Vector3.one * pointSize;
                sphere.GetComponent<MeshRenderer>().sharedMaterial = handleMaterial;
                FfdControlPoint handle = sphere.AddComponent<FfdControlPoint>();
                handle.Index = index;
                handles.Add(handle);
            }

            int n = deformer.LatticeResolution;
            for (int axis = 0; axis < 3; axis++)
            {
                for (int a = 0; a < n; a++)
                {
                    for (int b = 0; b < n; b++)
                    {
                        GameObject lineObject = new GameObject($"FFD Line {axis}-{a}-{b}");
                        lineObject.transform.SetParent(latticeRoot, false);
                        LineRenderer line = lineObject.AddComponent<LineRenderer>();
                        line.useWorldSpace = false;
                        line.sharedMaterial = lineMaterial;
                        line.startColor = lineMaterial.color;
                        line.endColor = lineMaterial.color;
                        line.startWidth = maximumSize * 0.008f;
                        line.endWidth = maximumSize * 0.008f;
                        line.positionCount = n;
                        line.numCapVertices = 2;
                        lines.Add(line);
                    }
                }
            }
            UpdateLines();
            SetVisible(visible);
        }

        public void SyncFromDeformer()
        {
            if (deformer == null || deformer.LatticePoints == null || handles.Count != deformer.LatticePoints.Length)
            {
                return;
            }
            for (int index = 0; index < handles.Count; index++)
            {
                handles[index].transform.localPosition = deformer.LatticePoints[index];
            }
            UpdateLines();
        }

        private void UpdateLines()
        {
            if (deformer == null || lines.Count == 0)
            {
                return;
            }

            int n = deformer.LatticeResolution;
            int lineIndex = 0;
            for (int axis = 0; axis < 3; axis++)
            {
                for (int a = 0; a < n; a++)
                {
                    for (int b = 0; b < n; b++)
                    {
                        LineRenderer line = lines[lineIndex++];
                        for (int step = 0; step < n; step++)
                        {
                            int i = axis == 0 ? step : a;
                            int j = axis == 1 ? step : (axis == 0 ? a : b);
                            int k = axis == 2 ? step : b;
                            int pointIndex = DeformationMath.Index(i, j, k, n);
                            line.SetPosition(step, handles[pointIndex].transform.localPosition);
                        }
                    }
                }
            }
        }

        private static void DestroyImmediateSafe(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
