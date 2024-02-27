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

                bool stateChanged = anim.CheckTransitions();
                anim.CheckTriggers(deltaTime);

                AnimationState curState = anim.GetCurrentState();
                AnimationClip curClip = curState.clip as AnimationClip;
                if (stateChanged)
                {
                    curState.loopStartTime = animTime;
                }

                curState.normalizedTime = Math.Clamp((animTime - curState.loopStartTime) / curState.Length, 0f, 1f);
                if(!curState.completed)
                {
                    if(curState.normalizedTime == 1f)
                    {
                        if (curState.IsLooping)
                        {
                            float excess = (animTime - curState.loopStartTime) / curState.Length - 1f;
                            curState.loopStartTime = animTime;
                            curState.normalizedTime = excess;
                        }
                        else
                        {
                            curState.completed = true;
                        }
                    }

                    curClip.Sample(curState.normalizedTime, skeleton.bones);
                }
            }
            );
        }
    }
}