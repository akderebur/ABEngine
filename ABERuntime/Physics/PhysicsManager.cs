using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Box2D.NetStandard.Dynamics.Bodies;

namespace ABEngine.ABERuntime.Physics
{
    public static class PhysicsManager
    {
        static List<string> collisionLayers;

        static string layersPath;

        private static List<Body> destroyQueue = new List<Body>();

        internal static void InitSettings()
        {
            layersPath = Game.AssetPath + "PhysicsLayers.abconfig";
            //if (!File.Exists(layersPath))
            //{
            //    File.WriteAllText(layersPath, "Default");
            //}

            collisionLayers = new List<string>();
            //foreach (var line in File.ReadAllLines(layersPath))
            //{
            //    string layerName = line.Trim();
            //    if (!collisionLayers.Contains(layerName))
            //        collisionLayers.Add(layerName);
            //}
        }

        public static void AddCollisionLayer(string layerName)
        {
            layerName = layerName.Trim();
            if (!collisionLayers.Contains(layerName))
            {
                collisionLayers.Add(layerName);

                if (!File.Exists(layersPath))
                {
                    File.WriteAllText(layersPath, "Default");
                }

                using (StreamWriter w = File.AppendText(layersPath))
                {
                    w.Write("\n" + layerName);
                }
            }
        }

        public static int GetColliisonLayerID(string layerName)
        {
            return collisionLayers.IndexOf(layerName);
        }

        internal static void DestroyBody(Body b2dBody)
        {
            destroyQueue.Add(b2dBody);
        }

        internal static void PostFixedUpdate()
        {
            while (destroyQueue.Count > 0)
            {
                var body = destroyQueue.First();
                body.GetWorld().DestroyBody(body);
                destroyQueue.RemoveAt(0);
            }
        }

        internal static void ResetPhysics()
        {
            destroyQueue = new List<Body>();

        }
    }
}

