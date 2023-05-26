using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Box2D.NetStandard.Dynamics.Bodies;

namespace ABEngine.ABERuntime.Physics
{
    public static class PhysicsManager
    {
        static CollisionLayer defaultLayer;
        static List<CollisionLayer> collisionLayers = new List<CollisionLayer>();

        private static Queue<Body> destroyQueue = new Queue<Body>();

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

        internal static void DestroyBody(Body b2dBody)
        {
            destroyQueue.Enqueue(b2dBody);
        }

        internal static void PostFixedUpdate()
        {
            while (destroyQueue.Count > 0)
            {
                var body = destroyQueue.Dequeue();
                body.GetWorld().DestroyBody(body);
            }
        }

        internal static void ResetPhysics()
        {
            destroyQueue = new Queue<Body>();
            collisionLayers.Clear();

            defaultLayer = new CollisionLayer("Default");
        }
    }
}

