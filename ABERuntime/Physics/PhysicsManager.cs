using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABEngine.ABERuntime.Components;
using Box2D.NetStandard.Dynamics.Bodies;

namespace ABEngine.ABERuntime.Physics
{
    public static class PhysicsManager
    {
        static CollisionLayer defaultLayer;
        static List<CollisionLayer> collisionLayers = new List<CollisionLayer>();

        private static Queue<Rigidbody> destroyQueue = new Queue<Rigidbody>();
        private static Queue<Rigidbody> createQueue = new Queue<Rigidbody>();

        public static int GetCollisionLayerCount()
        {
            return collisionLayers.Count;
        }


        internal static void AddCollisionLayer(CollisionLayer layer)
        {
            collisionLayers.Add(layer);
        }

        public static CollisionLayer GetDefaultCollisionLayer()
        {
            return defaultLayer;
        }

        public static CollisionLayer GetCollisionLayerByName(string name)
        {
            return collisionLayers.FirstOrDefault(c => c.layerName.Equals(name));
        }

        internal static void CreateBody(Rigidbody rb)
        {
            createQueue.Enqueue(rb);
        }

        internal static void DestroyBody(Rigidbody rb)
        {
            destroyQueue.Enqueue(rb);
        }

        internal static void PreFixedUpdate()
        {
            while (destroyQueue.Count > 0)
            {
                var rb = destroyQueue.Dequeue();
                rb.b2dBody.GetWorld().DestroyBody(rb.b2dBody);
                rb.destroyed = true;
            }

            while (createQueue.Count > 0)
            {
                var rb = createQueue.Dequeue();
                Game.b2dInitSystem.AddRBRuntime(rb.entity);
            }
        }

        internal static void PostFixedUpdate()
        {
           
        }

        internal static void ResetPhysics()
        {
            destroyQueue = new Queue<Rigidbody>();
            collisionLayers.Clear();

            defaultLayer = new CollisionLayer("Default");
        }
    }
}

