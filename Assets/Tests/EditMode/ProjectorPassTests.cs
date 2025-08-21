using NUnit.Framework;
using UnityEngine;
using Game.Level;

namespace EditModeTests.Level
{
    public class ProjectorPassTests
    {
        private GameObject sourceRoot;
        private GameObject projectedRoot;
        private GameObject testObject;
        private ProjectorPass projectorPass;

        [SetUp]
        public void Setup()
        {
            // Create test hierarchy
            sourceRoot = new GameObject("SourceRoot");
            projectedRoot = new GameObject("ProjectedRoot");
            testObject = new GameObject("TestObject");
            testObject.transform.SetParent(sourceRoot.transform);
            
            // Add MeshFilter and MeshRenderer
            var meshFilter = testObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateTestMesh();
            var meshRenderer = testObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateTestMaterial();
            
            projectorPass = new ProjectorPass();
        }

        [TearDown]
        public void TearDown()
        {
            if (sourceRoot) Object.DestroyImmediate(sourceRoot);
            if (projectedRoot) Object.DestroyImmediate(projectedRoot);
        }

        private Mesh CreateTestMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[] 
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0)
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            return mesh;
        }

        private Material CreateTestMaterial()
        {
            return new Material(Shader.Find("Standard"));
        }

        [Test]
        public void ProjectorPass_Run_CreatesClone()
        {
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 0f,
                copyMaterials = true,
                projectedLayer = -1
            };

            projectorPass.Run(ProjectionAxis.FlattenZ, context);

            Assert.That(projectedRoot.transform.childCount, Is.EqualTo(1));
            var clone = projectedRoot.transform.GetChild(0).gameObject;
            Assert.That(clone.name, Is.EqualTo("Clone_TestObject"));
        }

        [Test]
        public void ProjectorPass_IdempotentRebuild_NoDuplicates()
        {
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 0f,
                copyMaterials = true,
                projectedLayer = -1
            };

            // First run
            projectorPass.Run(ProjectionAxis.FlattenZ, context);
            Assert.That(projectedRoot.transform.childCount, Is.EqualTo(1));

            // Clear and run again
            projectorPass.Clear(projectedRoot.transform);
            projectorPass.Run(ProjectionAxis.FlattenZ, context);
            
            // Should still have only one child
            Assert.That(projectedRoot.transform.childCount, Is.EqualTo(1));
        }

        [Test]
        public void ProjectorPass_CopyMaterials_On_CopiesMaterials()
        {
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 0f,
                copyMaterials = true,
                projectedLayer = -1
            };

            projectorPass.Run(ProjectionAxis.FlattenZ, context);

            var clone = projectedRoot.transform.GetChild(0).gameObject;
            var cloneMR = clone.GetComponent<MeshRenderer>();
            var sourceMR = testObject.GetComponent<MeshRenderer>();
            
            Assert.That(cloneMR.sharedMaterials, Is.EqualTo(sourceMR.sharedMaterials));
        }

        [Test]
        public void ProjectorPass_CopyMaterials_Off_DoesNotCopyMaterials()
        {
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 0f,
                copyMaterials = false,
                projectedLayer = -1
            };

            projectorPass.Run(ProjectionAxis.FlattenZ, context);

            var clone = projectedRoot.transform.GetChild(0).gameObject;
            var cloneMR = clone.GetComponent<MeshRenderer>();
            
            // Should not have copied materials
            Assert.That(cloneMR.sharedMaterials, Is.Not.EqualTo(testObject.GetComponent<MeshRenderer>().sharedMaterials));
        }

        [Test]
        public void ProjectorPass_ProjectedLayer_AssignsCorrectLayer()
        {
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 0f,
                copyMaterials = true,
                projectedLayer = 8 // User layer 8
            };

            projectorPass.Run(ProjectionAxis.FlattenZ, context);

            var clone = projectedRoot.transform.GetChild(0).gameObject;
            Assert.That(clone.layer, Is.EqualTo(8));
        }

        [Test]
        public void ProjectorPass_ProjectedLayer_KeepsSourceLayer()
        {
            testObject.layer = 5; // Set specific source layer
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 0f,
                copyMaterials = true,
                projectedLayer = -1 // Keep source layer
            };

            projectorPass.Run(ProjectionAxis.FlattenZ, context);

            var clone = projectedRoot.transform.GetChild(0).gameObject;
            Assert.That(clone.layer, Is.EqualTo(5));
        }

        [Test]
        public void ProjectorPass_FlattenZ_CorrectPlaneFlattening()
        {
            testObject.transform.position = new Vector3(5f, 10f, 15f);
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 20f,
                planeX = 0f,
                copyMaterials = true,
                projectedLayer = -1
            };

            projectorPass.Run(ProjectionAxis.FlattenZ, context);

            var clone = projectedRoot.transform.GetChild(0).gameObject;
            var clonePos = clone.transform.position;
            
            Assert.That(clonePos.x, Is.EqualTo(5f)); // X preserved
            Assert.That(clonePos.y, Is.EqualTo(10f)); // Y preserved
            Assert.That(clonePos.z, Is.EqualTo(20f)); // Z flattened to planeZ
        }

        [Test]
        public void ProjectorPass_FlattenX_CorrectPlaneFlattening()
        {
            testObject.transform.position = new Vector3(5f, 10f, 15f);
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 25f,
                copyMaterials = true,
                projectedLayer = -1
            };

            projectorPass.Run(ProjectionAxis.FlattenX, context);

            var clone = projectedRoot.transform.GetChild(0).gameObject;
            var clonePos = clone.transform.position;
            
            Assert.That(clonePos.x, Is.EqualTo(25f)); // X flattened to planeX
            Assert.That(clonePos.y, Is.EqualTo(10f)); // Y preserved
            Assert.That(clonePos.z, Is.EqualTo(15f)); // Z preserved
        }

        [Test]
        public void ProjectorPass_CollidersCloned_BoxCollider()
        {
            var boxCollider = testObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(1, 2, 3);
            boxCollider.size = new Vector3(4, 5, 6);
            boxCollider.isTrigger = true;
            
            var context = new ProjectorPassContext
            {
                sourceRoot = sourceRoot.transform,
                projectedRoot = projectedRoot.transform,
                planeZ = 0f,
                planeX = 0f,
                copyMaterials = true,
                projectedLayer = -1
            };

            projectorPass.Run(ProjectionAxis.FlattenZ, context);

            var clone = projectedRoot.transform.GetChild(0).gameObject;
            var cloneBoxCollider = clone.GetComponent<BoxCollider>();
            
            Assert.That(cloneBoxCollider, Is.Not.Null);
            Assert.That(cloneBoxCollider.center, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(cloneBoxCollider.size, Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(cloneBoxCollider.isTrigger, Is.True);
        }
    }
}