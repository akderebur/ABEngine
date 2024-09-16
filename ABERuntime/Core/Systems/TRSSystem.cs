using System.Numerics;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Systems;

public class TRSSystem : BaseSystem
{
    private readonly ArchetypeQuery<TRS, WorldTransform> flatQuery = Game.GameWorld.Query<TRS, WorldTransform>().
                                                                     WithoutAllComponents(ComponentTypes.Get<TreeNode>()).
                                                                     WithoutAllTags(Tags.Get<ChildTag>());
    
    private readonly ArchetypeQuery<TRS, WorldTransform> rootQuery = Game.GameWorld.Query<TRS, WorldTransform>().
                                                                     AllComponents(ComponentTypes.Get<TreeNode>()).
                                                                     WithoutAllTags(Tags.Get<ChildTag>());
    

    private static WorldTransform transformID = new WorldTransform() { matrix = Matrix4x4.Identity };
    public override void Update(float gameTime, float deltaTime)
    {
        flatQuery.ForEachEntity(ExecuteFlat);
        rootQuery.ForEachEntity(ExecuteRoot);
    }

    private static void ExecuteFlat(ref TRS transform, ref WorldTransform worldTransform, Entity ent)
    {
        CalculateFlat(ref transform, ref worldTransform);
    }
    
    private static void ExecuteRoot(ref TRS transform, ref WorldTransform worldTransform, Entity ent)
    {
        CalculateHierarchy(ref transform, ref worldTransform, ent, ref transformID, false, false);
    }

    private static void CalculateFlat(ref TRS transform, ref WorldTransform worldTransform)
    {
        if (transform.IsDirty)
        {
            if (transform.IsRecalc)
            {
                worldTransform.matrix = Matrix4x4.CreateScale(transform.Scale) *
                                        Matrix4x4.CreateFromQuaternion(transform.Rotation) *
                                        Matrix4x4.CreateTranslation(transform.Position);
            }
            else
            {
                Vector3 position = transform.Position;
                worldTransform.matrix.M41 = position.X;
                worldTransform.matrix.M42 = position.Y;
                worldTransform.matrix.M43 = position.Z;
            }

            transform.ResetDirty();
        }
    }
    
    private static void CalculateHierarchy(ref TRS transform, ref WorldTransform worldTransform, in Entity ent, ref WorldTransform parTrans, bool hasParent, bool isParentDirty)
    {
        bool needsRecalc = isParentDirty || transform.IsDirty;
        if (needsRecalc)
        {
            if (transform.IsRecalc)
            {
                worldTransform.matrix = Matrix4x4.CreateScale(transform.Scale) *
                                        Matrix4x4.CreateFromQuaternion(transform.Rotation) *
                                        Matrix4x4.CreateTranslation(transform.Position);
            }
            else
            {
                Vector3 position = transform.Position;
                worldTransform.matrix.M41 = position.X;
                worldTransform.matrix.M42 = position.Y;
                worldTransform.matrix.M43 = position.Z;
            }

            if (hasParent)
                worldTransform.matrix *= parTrans.matrix;
            
            transform.ResetDirty();
        }

        /*foreach (var childEnt in ent.ChildEntities)
        {
            CalculateHierarchy(ref childEnt.LocalTransform, ref childEnt.WorldTransform, childEnt, ref worldTransform, true, needsRecalc);
        }*/
        if (ent.TryGetComponent(out TreeNode treeNode))
        {
            foreach (var childEnt in treeNode.GetChildEntities(Game.GameWorld))
            {
                CalculateHierarchy(ref childEnt.LocalTransform, ref childEnt.WorldTransform, childEnt, ref worldTransform, true, needsRecalc);
            }
        }
    }

    private void CalculateParent()
    {

    }
}