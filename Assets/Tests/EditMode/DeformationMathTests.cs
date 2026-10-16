using NUnit.Framework;
using UnityEngine;

namespace GlobalDeformationDemo.Tests
{
    public sealed class DeformationMathTests
    {
        private static readonly Bounds Bounds = new Bounds(Vector3.up, new Vector3(2f, 2f, 2f));

        [Test]
        public void Taper_ZeroAmount_IsIdentity()
        {
            Vector3 vertex = new Vector3(0.7f, 1.4f, -0.3f);
            Assert.That(Vector3.Distance(vertex, DeformationMath.Taper(vertex, Bounds, 0f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void Twist_ZeroAngle_IsIdentity()
        {
            Vector3 vertex = new Vector3(0.7f, 1.4f, -0.3f);
            Assert.That(Vector3.Distance(vertex, DeformationMath.Twist(vertex, Bounds, 0f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void Bend_ZeroAngle_IsIdentity()
        {
            Vector3 vertex = new Vector3(0.7f, 1.4f, -0.3f);
            Assert.That(Vector3.Distance(vertex, DeformationMath.Bend(vertex, Bounds, 0f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void RegularFfdLattice_ReproducesOriginalVertex()
        {
            Vector3 vertex = new Vector3(0.35f, 1.45f, -0.6f);
            Vector3[] lattice = DeformationMath.CreateRegularLattice(Bounds, 3);
            Vector3 result = DeformationMath.FreeFormDeform(vertex, Bounds, lattice, 3);
            Assert.That(Vector3.Distance(vertex, result), Is.LessThan(0.0001f));
        }

        [Test]
        public void MovingFfdCorner_MovesMatchingBoundsCorner()
        {
            const int n = 3;
            Vector3[] lattice = DeformationMath.CreateRegularLattice(Bounds, n);
            int corner = DeformationMath.Index(n - 1, n - 1, n - 1, n);
            lattice[corner] += new Vector3(0.5f, 0.25f, -0.1f);
            Vector3 result = DeformationMath.FreeFormDeform(Bounds.max, Bounds, lattice, n);
            Assert.That(Vector3.Distance(lattice[corner], result), Is.LessThan(0.0001f));
        }

        [Test]
        public void ObjParser_TriangulatesQuad()
        {
            const string obj = "v 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nf 1 2 3 4\n";
            Mesh mesh = RuntimeObjLoader.Parse(obj, "Quad");
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.triangles.Length, Is.EqualTo(6));
            Object.DestroyImmediate(mesh);
        }
    }
}
