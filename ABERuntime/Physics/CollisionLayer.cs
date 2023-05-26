using System;
namespace ABEngine.ABERuntime.Physics
{
	public class CollisionLayer
	{
        public ushort categoryBits { get; set; }
        public ushort maskBits { get; set; }
		public int layerIndex { get; set; }
		public string layerName { get; set; }

		public CollisionLayer(string layerName)
		{
			this.layerName = layerName;
			layerIndex = PhysicsManager.GetCollisionLayerCount();
			categoryBits = (ushort)(1 << layerIndex);
			maskBits = 0xFFFF;

			PhysicsManager.AddCollisionLayer(this);
		}

		public void ExcludeCollisionLayer(CollisionLayer other)
		{
			int otherCategory = (ushort)(1 << other.layerIndex);
            maskBits = (ushort)(maskBits & ~otherCategory);
        }

		public void IncludeCollisionLayer(CollisionLayer other)
		{
            int otherCategory = (ushort)(1 << other.layerIndex);
            maskBits = (ushort)(maskBits | otherCategory);
        }
    }
}

