using System;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Arch.Core;

namespace ABEngine.ABERuntime
{
    public class MeshAnimatorSystem : BaseSystem
    {
        private readonly QueryDescription animQuery = new QueryDescription().WithAll<Animator, Skeleton>();

        public override void Update(float gameTime, float deltaTime)
        {
            Game.GameWorld.Query(in animQuery, (ref Animator anim, ref Skeleton skeleton, ref Transform transform) =>
            {
                if (!transform.enabled)
                    return;

                anim.Time += deltaTime;
                float animTime = anim.Time;

                bool frameChanged = false;
                bool stateChanged = anim.CheckTransitions();
                anim.CheckTriggers(deltaTime);

                AnimationState curState = anim.GetCurrentState();
                AnimationClip curClip = curState.clip as AnimationClip;
                if (stateChanged)
                {
                    curState.loopStartTime = animTime;
                    curState.lastFrameTime = animTime;
                    curState.curFrame = 0;
                    frameChanged = true;
                }

                curState.normalizedTime = (animTime - curState.loopStartTime) / curState.Length;

                float frameTime = curState.lastFrameTime + curState.SampleFreq;
                while (frameTime <= animTime)
                {
                    curState.curFrame++;
                    frameTime += curState.SampleFreq;
                    frameChanged = true;
                }

                if (frameChanged)
                {
                    frameTime -= curState.SampleFreq;

                    if (curState.curFrame >= curClip.FrameCount)
                    {
                        curState.normalizedTime = 1f;

                        if (curState.IsLooping)
                        {
                            curState.curFrame = 0;
                            curState.loopStartTime = frameTime;
                        }
                        else
                        {
                            curState.completed = true;
                            curState.curFrame = curClip.FrameCount - 1;
                        }
                    }
                    curState.lastFrameTime = frameTime;

                    for (int b = 0; b < skeleton.bones.Length; b++)
                    {
                        Transform bone = skeleton.bones[b];
                        BoneFrameData frameData = curClip.bonesData[b];

                        bone.SetTRS(frameData.framePoses[curState.curFrame], frameData.frameRotations[curState.curFrame], bone.localScale);
                    }
                }
            }
            );
        }
    }
}