using System;
using Halak;
using System.Numerics;
using ABEngine.ABERuntime.Components;

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
        internal float[] times;

		public AnimationClip()
		{
		}

        internal void Sample(float normalizedTime, Transform[] bones, float transRatio)
        {
            int index = Array.BinarySearch(times, normalizedTime);

            if(transRatio <= 1f)
            {
                if (index < 0)
                    index = ~index;

                // Interpolate current pose and clip pose
                for (int b = 0; b < bones.Length; b++)
                {
                    Transform bone = bones[b];
                    BoneFrameData frameData = bonesData[b];

                    Vector3 pos = Vector3.Lerp(bone.localPosition, frameData.framePoses[index], transRatio);
                    Quaternion rot = Quaternion.Slerp(bone.localRotation, frameData.frameRotations[index], transRatio);

                    bone.SetTRS(pos, rot, bone.localScale);
                }
            }
            else if (index >= 0)
            {
                // Exact match
                for (int b = 0; b < bones.Length; b++)
                {
                    Transform bone = bones[b];
                    BoneFrameData frameData = bonesData[b];

                    bone.SetTRS(frameData.framePoses[index], frameData.frameRotations[index], bone.localScale);
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
                    Transform bone = bones[b];
                    BoneFrameData frameData = bonesData[b];

                    Vector3 pos = Vector3.Lerp(frameData.framePoses[prev], frameData.framePoses[next], t);
                    Quaternion rot = Quaternion.Slerp(frameData.frameRotations[prev], frameData.frameRotations[next], t);

                    bone.SetTRS(pos, rot, bone.localScale);
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

