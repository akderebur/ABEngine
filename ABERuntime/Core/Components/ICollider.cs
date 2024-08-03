using System.Numerics;

namespace ABEngine.ABERuntime.Components
{
	public interface ICollider
	{
        Box2D.NetStandard.Collision.AABB ToB2D(Transform transform);

        bool CheckCollisionMouse(Transform transform, Vector2 mousePos);
    }
}

