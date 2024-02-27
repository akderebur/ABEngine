using System;
using Halak;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Assets
{
	public class AnimationClip : Asset, IClip
    {
        private float _sampleRate;
        public float SampleRate { get { return _sampleRate; } set { SampleFreq = 1f / value; ClipLength = SampleFreq * FrameCount; _sampleRate = value; } }
        public float SampleFreq { get; internal set; }
        public float ClipLength { get; protected set; }
        public int FrameCount { get; internal set; }

        public string ClipAssetPath => throw new NotImplementedException();

        internal BoneFrameData[] bonesData;

		public AnimationClip()
		{
		}

        internal override JValue SerializeAsset()
        {
            JsonObjectBuilder assetEnt = new JsonObjectBuilder(200);
            assetEnt.Put("TypeID", 4);
            assetEnt.Put("FileHash", (long)fPathHash);
            return assetEnt.Build();
        }
    }

    internal struct BoneFrameData
    {
        internal Vector3[] framePoses;
        internal Quaternion[] frameRotations;
    }
}

