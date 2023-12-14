using System;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABERuntime.Physics
{
	public struct CollisionData
	{
		public Rigidbody rigidbodyA { get; set; }
        public Rigidbody rigidbodyB { get; set; }
		public CollisionType collisionType { get; set; }
    }

    public enum CollisionType
	{
		Enter,
		Exit
	}
}

