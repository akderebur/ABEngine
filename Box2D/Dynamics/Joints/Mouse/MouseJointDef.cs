using System.Numerics;

namespace Box2D.NetStandard.Dynamics.Joints.Mouse
{
    /// <summary>
    ///  Mouse joint definition. This requires a world target point,
    ///  tuning parameters, and the time step.
    /// </summary>
    public class MouseJointDef : JointDef
    {
        /// <summary>
        ///  The damping ratio. 0 = no damping, 1 = critical damping.
        /// </summary>
        public float DampingRatio;

        /// <summary>
        ///  The response speed.
        /// </summary>
        public float FrequencyHz;

        /// <summary>
        ///  The maximum constraint force that can be exerted
        ///  to move the candidate body. Usually you will express
        ///  as some multiple of the weight (multiplier * mass * gravity).
        /// </summary>
        public float MaxForce;

        /// <summary>
        ///  The initial world target point. This is assumed
        ///  to coincide with the body anchor initially.
        /// </summary>
        public Vector2 Target;

        public MouseJointDef()
        {
            FrequencyHz = 5.0f;
            DampingRatio = 0.7f;
        }
    }
}