using System.Numerics;

namespace ABEngine.ABERuntime;

public class Frustum
{
    public Vector4 LeftPlane;
    public Vector4 RightPlane;
    public Vector4 BottomPlane;
    public Vector4 TopPlane;
    public Vector4 NearPlane;
    public Vector4 FarPlane;

    public Frustum(in Matrix4x4 viewProjectionMatrix)
    {
        // Left Plane: row 4 + row 1
        LeftPlane = new Vector4(
            viewProjectionMatrix.M14 + viewProjectionMatrix.M11,
            viewProjectionMatrix.M24 + viewProjectionMatrix.M21,
            viewProjectionMatrix.M34 + viewProjectionMatrix.M31,
            viewProjectionMatrix.M44 + viewProjectionMatrix.M41
        );

        // Right Plane: row 4 - row 1
        RightPlane = new Vector4(
            viewProjectionMatrix.M14 - viewProjectionMatrix.M11,
            viewProjectionMatrix.M24 - viewProjectionMatrix.M21,
            viewProjectionMatrix.M34 - viewProjectionMatrix.M31,
            viewProjectionMatrix.M44 - viewProjectionMatrix.M41
        );

        // Bottom Plane: row 4 + row 2
        BottomPlane = new Vector4(
            viewProjectionMatrix.M14 + viewProjectionMatrix.M12,
            viewProjectionMatrix.M24 + viewProjectionMatrix.M22,
            viewProjectionMatrix.M34 + viewProjectionMatrix.M32,
            viewProjectionMatrix.M44 + viewProjectionMatrix.M42
        );

        // Top Plane: row 4 - row 2
        TopPlane = new Vector4(
            viewProjectionMatrix.M14 - viewProjectionMatrix.M12,
            viewProjectionMatrix.M24 - viewProjectionMatrix.M22,
            viewProjectionMatrix.M34 - viewProjectionMatrix.M32,
            viewProjectionMatrix.M44 - viewProjectionMatrix.M42
        );

        // Near Plane: row 3
        NearPlane = new Vector4(
            viewProjectionMatrix.M13,
            viewProjectionMatrix.M23,
            viewProjectionMatrix.M33,
            viewProjectionMatrix.M43
        );

        // Far Plane: row 4 - row 3
        FarPlane = new Vector4(
            viewProjectionMatrix.M14 - viewProjectionMatrix.M13,
            viewProjectionMatrix.M24 - viewProjectionMatrix.M23,
            viewProjectionMatrix.M34 - viewProjectionMatrix.M33,
            viewProjectionMatrix.M44 - viewProjectionMatrix.M43
        );
    }
    
    public bool IsSphereInFrustum(Vector3 center, float radius)
    {
        return IsSphereInPlane(LeftPlane, center, radius) &&
               IsSphereInPlane(RightPlane, center, radius) &&
               IsSphereInPlane(BottomPlane, center, radius) &&
               IsSphereInPlane(TopPlane, center, radius) &&
               IsSphereInPlane(NearPlane, center, radius) &&
               IsSphereInPlane(FarPlane, center, radius);
    }
    
    bool IsSphereInPlane(in Vector4 plane, in Vector3 center, in float radius)
    {
        // Plane equation: Ax + By + Cz + D
        float distance = plane.X * center.X + plane.Y * center.Y + plane.Z * center.Z + plane.W;

        // If the distance is less than -radius, the sphere is completely behind the plane and thus outside the frustum
        return distance >= -radius;
    }
    
}
