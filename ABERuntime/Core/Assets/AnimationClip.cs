using System;
using Halak;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Core.Assets
{
	public class AnimationClip : Asset, IClip
    {
        private float _sampleRate;
        public float sampleRate
        {
            get => _sampleRate;
            set
            {
                sampleFreq = 1f / value; clipLength = sampleFreq * frameCount; _sampleRate = value; 
                
            }
        }
        public float sampleFreq { get; internal set; }
        public float clipLength { get; protected set; }
        public int frameCount { get; internal set; }
        
        internal BoneFrameData[] bonesData;
        internal float[] times;

		public AnimationClip()
		{
		}

        internal void Sample(float normalizedTime, Entity[] bones, float transRatio)
        {
            int index = Array.BinarySearch(times, normalizedTime);

            if(transRatio <= 1f)
            {
                if (index < 0)
                    index = ~index;

                // Interpolate current pose and clip pose
                for (int b = 0; b < bones.Length; b++)
                {
                    ref TRS boneTRS = ref bones[b].LocalTransform;
                    BoneFrameData frameData = bonesData[b];

                    boneTRS.Position = Vector3.Lerp(boneTRS.Position, frameData.framePoses[index], transRatio);
                    boneTRS.Rotation = Quaternion.Slerp(boneTRS.Rotation, frameData.frameRotations[index], transRatio);

                    //bone.SetTRS(pos, rot, bone.localScale);
                }
            }
            else if (index >= 0)
            {
                // Exact match
                for (int b = 0; b < bones.Length; b++)
                {
                    ref TRS boneTRS = ref bones[b].LocalTransform;
                    BoneFrameData frameData = bonesData[b];

                    boneTRS.Position = frameData.framePoses[index];
                    boneTRS.Rotation = frameData.frameRotations[index];
                }
            }
            else
            {
                // Interpolate next and prev
                int next = ~index;
                int prev = next - 1;
                float nextTime = times[next];
                float prevTime = times[prev];
                float t = (normalizedTime - prevTime) / (nextTime - prevTime);

                for (int b = 0; b < bones.Length; b++)
                {
                    ref TRS boneTRS = ref bones[b].LocalTransform;
                    BoneFrameData frameData = bonesData[b];

                    boneTRS.Position = Vector3.Lerp(frameData.framePoses[prev], frameData.framePoses[next], t);
                    boneTRS.Rotation = Quaternion.Slerp(frameData.frameRotations[prev], frameData.frameRotations[next], t);
                }
            }
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

