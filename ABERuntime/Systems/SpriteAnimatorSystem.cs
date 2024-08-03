using System;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Arch.Core;

namespace ABEngine.ABERuntime
{
    public class SpriteAnimatorSystem : BaseSystem
    {       
        public override void Update(float gameTime, float deltaTime)
        {
            var query = new QueryDescription().WithAll<Animator, Sprite>();

            Game.GameWorld.Query(in query ,(ref Animator anim, ref Sprite sprite, ref Transform transform) =>
            {
                if (!transform.enabled)
                    return;

                anim.time += deltaTime;
                float animTime = anim.time;

                bool frameChanged = false;
                bool stateChanged = anim.CheckTransitions();
                anim.CheckTriggers(deltaTime);

                AnimationState curState = anim.GetCurrentState();
                SpriteClip curClip = curState.clip as SpriteClip;
                if (stateChanged)
                {
                    curState.loopStartTime = animTime;
                    curState.lastFrameTime = animTime;
                    curState.curFrame = 0;
                    frameChanged = true;
                }

                curState.normalizedTime = (animTime - curState.loopStartTime) / curState.length;

                float frameTime = curState.lastFrameTime + curState.sampleFreq;
                while(frameTime <= animTime)
                {
                    curState.curFrame++;
                    frameTime += curState.sampleFreq;
                    frameChanged = true;
                }
                frameTime -= curState.sampleFreq;

                if(frameChanged)
                {
                    if (curState.curFrame >= curClip.frameCount)
                    {
                        curState.normalizedTime = 1f;

                        if (curState.isLooping)
                        {
                            curState.curFrame = 0;
                            curState.loopStartTime = animTime;
                        }
                        else
                        {
                            curState.curFrame = curClip.frameCount - 1;
                        }
                    }
                    curState.lastFrameTime = frameTime;

                    sprite.SetUVPosScale(curClip.uvPoses[curState.curFrame], curClip.uvScales[curState.curFrame]);
                }
            });
        }
    }
}
