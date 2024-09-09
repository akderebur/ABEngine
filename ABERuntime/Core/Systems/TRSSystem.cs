using System.Numerics;
using ABEngine.ABERuntime.Components;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Systems;

public class TRSSystem : BaseSystem
{
    private readonly ArchetypeQuery<TRS, WorldTransform> rootQuery = Game.GameWorld.Query<TRS, WorldTransform>().WithoutAllTags(Tags.Get<ChildTag>());

    public override void Update(float gameTime, float deltaTime)
    {
        rootQuery.ForEachEntity(Execute);
    }

    private void Execute(ref TRS transform, ref WorldTransform worldTransform, Entity ent)
    {
        CalculateMatrix(ref transform, ref worldTransform, ent, false);
    }

    private void CalculateMatrix(ref TRS transform, ref WorldTransform worldTransform, in Entity ent, bool isDirty)
    {
        bool needsRecalc = transform.IsDirty || isDirty;
        if (needsRecalc)
        {
            worldTransform.matrix = Matrix4x4.CreateScale(transform.Scale) * 
                                    Matrix4x4.CreateFromQuaternion(transform.Rotation) * 
                                    Matrix4x4.CreateTranslation(transform.Position);
            if (!ent.Parent.IsNull)
                worldTransform.matrix *= ent.Parent.GetComponent<WorldTransform>().matrix;
        }
        
        if (ent.ChildCount > 0)
        {
            foreach (var childEnt in ent.ChildEntities)
            {   
                CalculateMatrix(ref ent.LocalTransform, ref ent.GetComponent<WorldTransform>(), childEnt, needsRecalc);
            }
        }
    }
}