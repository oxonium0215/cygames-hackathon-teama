using NUnit.Framework;
using UnityEngine;
using Game.Projection;

namespace EditModeTests.Projection
{
    public class DepenetrationSolverTests
    {
        private DepenetrationSolver solver;
        private GameObject testObject;
        private BoxCollider testCollider;
        
        [SetUp]
        public void Setup()
        {
            // Create test object with collider
            testObject = new GameObject("TestPlayer");
            testCollider = testObject.AddComponent<BoxCollider>();
            testCollider.size = Vector3.one;
            
            // Initialize solver with test parameters
            solver = new DepenetrationSolver(
                penetrationSkin: 0.003f,
                overlapBoxInflation: 0.98f,
                maxResolveStep: 2.0f,
                maxResolveTotal: 8.0f,
                groundSkin: 0.05f
            );
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
                Object.DestroyImmediate(testObject);
        }
        
        [Test]
        public void DepenetrationSolver_NoOverlaps_ReturnsFalse()
        {
            // Position object in free space
            testObject.transform.position = Vector3.up * 10f;
            LayerMask groundMask = 1; // Default layer
            
            bool moved = solver.ResolveVerticalOverlapUpwards(testCollider, testObject.transform, 
                groundMask, 6, false);
            
            Assert.IsFalse(moved);
        }
        
        [Test]
        public void DepenetrationSolver_NullCollider_ReturnsFalse()
        {
            bool moved = solver.ResolveVerticalOverlapUpwards(null, testObject.transform, 1, 6, false);
            
            Assert.IsFalse(moved);
        }
        
        [Test]
        public void DepenetrationSolver_NullTransform_ReturnsFalse()
        {
            bool moved = solver.ResolveVerticalOverlapUpwards(testCollider, null, 1, 6, false);
            
            Assert.IsFalse(moved);
        }
        
        [Test]
        public void DepenetrationSolver_WithSimpleOverlap_MovesUpward()
        {
            // Create ground object
            GameObject ground = new GameObject("Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            ground.transform.position = Vector3.zero;
            groundCollider.size = new Vector3(10f, 1f, 10f);
            ground.layer = 0; // Default layer
            
            try
            {
                // Position player overlapping with ground
                testObject.transform.position = new Vector3(0f, 0.5f, 0f); // Overlapping
                LayerMask groundMask = 1; // Default layer mask
                
                Vector3 initialPos = testObject.transform.position;
                
                bool moved = solver.ResolveVerticalOverlapUpwards(testCollider, testObject.transform, 
                    groundMask, 6, false);
                
                // Should have moved upward to resolve overlap
                Assert.IsTrue(moved);
                Assert.That(testObject.transform.position.y, Is.GreaterThan(initialPos.y));
            }
            finally
            {
                Object.DestroyImmediate(ground);
            }
        }
        
        [Test]
        public void DepenetrationSolver_IterationCap_LimitsIterations()
        {
            // Create multiple overlapping objects
            GameObject ground1 = new GameObject("Ground1");
            BoxCollider groundCollider1 = ground1.AddComponent<BoxCollider>();
            ground1.transform.position = Vector3.zero;
            groundCollider1.size = new Vector3(10f, 1f, 10f);
            
            GameObject ground2 = new GameObject("Ground2"); 
            BoxCollider groundCollider2 = ground2.AddComponent<BoxCollider>();
            ground2.transform.position = new Vector3(0f, 1.5f, 0f);
            groundCollider2.size = new Vector3(10f, 1f, 10f);
            
            try
            {
                // Position player overlapping with both
                testObject.transform.position = new Vector3(0f, 1.0f, 0f);
                LayerMask groundMask = 1;
                
                // Use very low iteration count
                bool moved = solver.ResolveVerticalOverlapUpwards(testCollider, testObject.transform, 
                    groundMask, 1, false);
                
                // Should still attempt to resolve even with low iteration count
                Assert.IsTrue(moved);
            }
            finally
            {
                Object.DestroyImmediate(ground1);
                Object.DestroyImmediate(ground2);
            }
        }
        
        [Test]
        public void DepenetrationSolver_ConservativeFallback_UsesHighestBound()
        {
            // Create ground object
            GameObject ground = new GameObject("Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            ground.transform.position = new Vector3(0f, 0f, 0f);
            groundCollider.size = new Vector3(10f, 2f, 10f); // Height of 2, so top at y=1
            ground.layer = 0;
            
            try
            {
                // Position player overlapping
                testObject.transform.position = new Vector3(0f, 0.5f, 0f);
                LayerMask groundMask = 1;
                
                Vector3 initialPos = testObject.transform.position;
                
                bool moved = solver.ResolveVerticalOverlapUpwards(testCollider, testObject.transform, 
                    groundMask, 6, true); // Enable conservative fallback
                
                Assert.IsTrue(moved);
                // Should be positioned above the ground's top bound
                Assert.That(testObject.transform.position.y, Is.GreaterThan(1f));
            }
            finally
            {
                Object.DestroyImmediate(ground);
            }
        }
        
        [Test]
        public void DepenetrationSolver_LayerMask_FiltersCorrectly()
        {
            // Create ground on layer 8
            GameObject ground = new GameObject("Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            ground.transform.position = Vector3.zero;
            groundCollider.size = new Vector3(10f, 1f, 10f);
            ground.layer = 8;
            
            try
            {
                // Position player overlapping
                testObject.transform.position = new Vector3(0f, 0.5f, 0f);
                
                // Use layer mask that doesn't include layer 8
                LayerMask groundMask = 1; // Only includes layer 0
                
                bool moved = solver.ResolveVerticalOverlapUpwards(testCollider, testObject.transform, 
                    groundMask, 6, false);
                
                // Should not move since ground is on different layer
                Assert.IsFalse(moved);
            }
            finally
            {
                Object.DestroyImmediate(ground);
            }
        }
    }
}