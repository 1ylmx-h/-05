using System;
using UnityEngine;

namespace GlobalDeformationDemo
{
    public enum DeformationMode
    {
        Original,
        Taper,
        Twist,
        Bend,
        FreeForm
    }

    [RequireComponent(typeof(MeshFilter))]
    public sealed class MeshDeformer : MonoBehaviour
    {
        public event Action MeshChanged;
        public event Action LatticeChanged;

        public Mesh Mesh { get; private set; }
        public Vector3[] OriginalVertices { get; private set; }
        public Bounds OriginalBounds { get; private set; }
        public Vector3[] LatticePoints { get; private set; }
        public int LatticeResolution { get; private set; } = 3;
        public DeformationMode Mode { get; private set; }

        private MeshFilter meshFilter;
        private Vector3[] workingVertices;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        public void SetMesh(Mesh source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (Mesh != null)
            {
                Destroy(Mesh);
            }

            Mesh = Instantiate(source);
            Mesh.name = source.name + " (Editable)";
            meshFilter.sharedMesh = Mesh;
            OriginalVertices = Mesh.vertices;
            workingVertices = new Vector3[OriginalVertices.Length];
            OriginalBounds = Mesh.bounds;
            BuildLattice(LatticeResolution);
            ResetMesh();
        }

        public void ResetMesh()
        {
            EnsureReady();
            Array.Copy(OriginalVertices, workingVertices, OriginalVertices.Length);
            ApplyWorkingVertices();
            Mode = DeformationMode.Original;
        }

        public void ApplyTaper(float amount)
        {
            EnsureReady();
            for (int index = 0; index < OriginalVertices.Length; index++)
            {
                workingVertices[index] = DeformationMath.Taper(OriginalVertices[index], OriginalBounds, amount);
            }
            ApplyWorkingVertices();
            Mode = DeformationMode.Taper;
        }

        public void ApplyTwist(float degrees)
        {
            EnsureReady();
            for (int index = 0; index < OriginalVertices.Length; index++)
            {
                workingVertices[index] = DeformationMath.Twist(OriginalVertices[index], OriginalBounds, degrees);
            }
            ApplyWorkingVertices();
            Mode = DeformationMode.Twist;
        }

        public void ApplyBend(float degrees)
        {
            EnsureReady();
            for (int index = 0; index < OriginalVertices.Length; index++)
            {
                workingVertices[index] = DeformationMath.Bend(OriginalVertices[index], OriginalBounds, degrees);
            }
            ApplyWorkingVertices();
            Mode = DeformationMode.Bend;
        }

        public void BuildLattice(int resolution)
        {
            EnsureReadyForLattice();
            LatticeResolution = Mathf.Clamp(resolution, 2, 5);
            LatticePoints = DeformationMath.CreateRegularLattice(OriginalBounds, LatticeResolution);
            LatticeChanged?.Invoke();
        }

        public void EnterFreeFormMode()
        {
            EnsureReady();
            Mode = DeformationMode.FreeForm;
            ApplyFreeForm();
        }

        public void ResetLattice()
        {
            BuildLattice(LatticeResolution);
            EnterFreeFormMode();
        }

        public void SetLatticePoint(int index, Vector3 localPosition)
        {
            EnsureReady();
            if (index < 0 || index >= LatticePoints.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            LatticePoints[index] = localPosition;
            Mode = DeformationMode.FreeForm;
            ApplyFreeForm();
        }

        public void ApplyFreeForm()
        {
            EnsureReady();
            for (int index = 0; index < OriginalVertices.Length; index++)
            {
                workingVertices[index] = DeformationMath.FreeFormDeform(
                    OriginalVertices[index],
                    OriginalBounds,
                    LatticePoints,
                    LatticeResolution);
            }
            ApplyWorkingVertices();
        }

        private void ApplyWorkingVertices()
        {
            Mesh.vertices = workingVertices;
            Mesh.RecalculateNormals();
            Mesh.RecalculateBounds();
            MeshChanged?.Invoke();
        }

        private void EnsureReady()
        {
            if (Mesh == null || OriginalVertices == null)
            {
                throw new InvalidOperationException("Load a mesh before applying deformation.");
            }
        }

        private void EnsureReadyForLattice()
        {
            if (Mesh == null)
            {
                return;
            }
        }
    }
}
