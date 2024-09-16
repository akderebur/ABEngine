using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Systems;

class DefaultBVHGroup : BVHGroup
{
    public override void AddVolume(Vector3 otherCenter, float otherRadius)
    {
       
    }
}

public class BVHGroup
{
    public Vector3 Center { get; set; }
    public float Radius { get; set; }
    public bool IsCulled { get; set; }
    
    public virtual void AddVolume(Vector3 otherCenter, float otherRadius)
    {
        if (Radius == 0f)
        {
            Center = otherCenter;
            Radius = otherRadius;
            return;
        }
        // Calculate the distance between the two sphere centers
        Vector3 direction = otherCenter - Center;
        float distance = direction.Length();

        // If one sphere is fully contained within the other, no need to adjust the merged sphere
        if (distance + otherRadius <= Radius)
        {
            return; // Other sphere is inside this one, no changes needed
        }
        else if (distance + Radius <= otherRadius)
        {
            // This sphere is inside the other one, update to the other sphere
            Center = otherCenter;
            Radius = otherRadius;
            return;
        }

        // Otherwise, merge the spheres by adjusting the center and radius to cover both
        float newRadius = (distance + Radius + otherRadius) / 2.0f;
        Vector3 newCenter = Center + direction * ((newRadius - Radius) / distance);
        
        Center = newCenter;
        Radius = newRadius;
    }

    public void ResetVolume()
    {
        Center = Vector3.Zero;
        Radius = 0f;
    }
}

public class BVHSystem : BaseSystem
{
    private readonly ArchetypeQuery<WorldTransform, BVSphere> sphereQuery = Game.GameWorld.Query<WorldTransform, BVSphere>().
            WithDisabled();

    private static Dictionary<string, BVHGroup> bvhGroups;
    private static DefaultBVHGroup defaultBVH = new DefaultBVHGroup();
    public static Frustum frustum;
    
    public BVHSystem()
    {
        bvhGroups = new Dictionary<string, BVHGroup>();
    }
    
    protected override void StartScene()
    {
        
    }

    public override void Update(float gameTime, float deltaTime)
    {
        // Update BVHs
        sphereQuery.ForEachEntity((ref WorldTransform transform, ref BVSphere bvSphere, Entity entity) => {
            float scale = new Vector3(transform.m11, transform.m12, transform.m13).Length();
            bvSphere.bvhGroup.AddVolume(transform.Position, bvSphere.radius * scale);
        });
        
        // Check BVHs
        foreach (var bvhGroupKV in bvhGroups)
        {
            BVHGroup group = bvhGroupKV.Value;
            group.IsCulled = !frustum.IsSphereInFrustum(group.Center, group.Radius);
            group.ResetVolume();
        }
    }

    public static BVHGroup GetDefaultBVHGroup()
    {
        return defaultBVH;
    }

    public static BVHGroup GetBVHGroup(string name)
    {
        if (bvhGroups.TryGetValue(name, out BVHGroup group))
            return group;

        group = new BVHGroup();
        bvhGroups[name] = group;
        return group;
    }
}