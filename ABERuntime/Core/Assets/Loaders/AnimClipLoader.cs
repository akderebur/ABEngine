using System;
using System.IO;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Assets
{ 
	internal class AnimClipLoader : AssetLoader
	{
        internal override Asset LoadAssetRAW(byte[] data)
        {
            AnimationClip clip = new AnimationClip();

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                int frameC = br.ReadInt32();
                float frameDiv = frameC - 1;
                var times = new float[frameC];
                for (int f = 0; f < frameC; f++)
                {
                    float normTime = f / frameDiv;
                    if (f == frameC - 1)
                        normTime = 1f;
                    times[f] = normTime;
                }
                clip.times = times;

                clip.FrameCount = frameC;
                clip.SampleRate = br.ReadSingle();

                int boneC = br.ReadInt32();
                clip.bonesData = new BoneFrameData[boneC];
                BoneFrameData boneFrame = new BoneFrameData();

                for (int b = 0; b < boneC; b++)
                {
                    boneFrame.framePoses = new Vector3[frameC];
                    boneFrame.frameRotations = new Quaternion[frameC];

                    // Translation X
                    if(br.ReadByte() == 1) // Constant
                    {
                        float constVal = br.ReadSingle();
                        for (int i = 0; i < frameC; i++)
                            boneFrame.framePoses[i].X = constVal;
                    }
                    else
                    {
                        for (int i = 0; i < frameC; i++)
                            boneFrame.framePoses[i].X = br.ReadSingle();
                    }

                    // Translation Y
                    if (br.ReadByte() == 1) // Constant
                    {
                        float constVal = br.ReadSingle();
                        for (int i = 0; i < frameC; i++)
                            boneFrame.framePoses[i].Y = constVal;
                    }
                    else
                    {
                        for (int i = 0; i < frameC; i++)
                            boneFrame.framePoses[i].Y = br.ReadSingle();
                    }

                    // Translation Z
                    if (br.ReadByte() == 1) // Constant
                    {
                        float constVal = br.ReadSingle();
                        for (int i = 0; i < frameC; i++)
                            boneFrame.framePoses[i].Z = constVal;
                    }
                    else
                    {
                        for (int i = 0; i < frameC; i++)
                            boneFrame.framePoses[i].Z = br.ReadSingle();
                    }

                    // Rotation X
                    if (br.ReadByte() == 1) // Constant
                    {
                        float constVal = br.ReadSingle();
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].X = constVal;
                    }
                    else
                    {
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].X = br.ReadSingle();
                    }

                    // Rotation Y
                    if (br.ReadByte() == 1) // Constant
                    {
                        float constVal = br.ReadSingle();
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].Y = constVal;
                    }
                    else
                    {
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].Y = br.ReadSingle();
                    }

                    // Rotation Z
                    if (br.ReadByte() == 1) // Constant
                    {
                        float constVal = br.ReadSingle();
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].Z = constVal;
                    }
                    else
                    {
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].Z = br.ReadSingle();
                    }

                    // Rotation W
                    if (br.ReadByte() == 1) // Constant
                    {
                        float constVal = br.ReadSingle();
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].W = constVal;
                    }
                    else
                    {
                        for (int i = 0; i < frameC; i++)
                            boneFrame.frameRotations[i].W = br.ReadSingle();
                    }

                    clip.bonesData[b] = boneFrame;
                }
            }

            return clip;
        }
    }
}

