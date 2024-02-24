using System;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Arch.Core;

namespace ABEngine.ABERuntime
{
    public class MeshAnimatorSystem : BaseSystem
    {
        private readonly QueryDescription animQuery = new QueryDescription().WithAll<Animator>().WithNone<Sprite>();

        public override void Update(float gameTime, float deltaTime)
        {
            Game.GameWorld.Query(in animQuery, (ref Animator anim, ref Sprite sprite, ref Transform transform) =>
            {
                if (!transform.enabled)
                    return;

                anim.Time += deltaTime;
                float animTime = anim.Time;

                bool stateChanged = anim.CheckTransitions();
                anim.CheckTriggers(deltaTime);

                AnimationState curState = anim.GetCurrentState();
                SpriteClip curClip = curState.clip as SpriteClip;
                if (stateChanged)
                {
                    curState.loopStartTime = animTime;
                    curState.lastFrameTime = 0f;
                    curState.curFrame = -1;
                }

                curState.normalizedTime = (animTime - curState.loopStartTime) / curState.Length;
                if ((animTime - curState.SampleFreq) > curState.lastFrameTime)
                {
                    curState.curFrame++;
                    if (curState.curFrame >= curClip.FrameCount)
                    {
                        curState.normalizedTime = 1f;

                        if (curState.IsLooping)
                        {
                            curState.curFrame = 0;
                            curState.loopStartTime = animTime;
                        }
                        else
                        {
                            curState.curFrame = curClip.FrameCount - 1;
                        }
                    }
                    curState.lastFrameTime = animTime;

                    sprite.SetUVPosScale(curClip.uvPoses[curState.curFrame], curClip.uvScales[curState.curFrame]);
                }
            }
            );
        }
    }
}