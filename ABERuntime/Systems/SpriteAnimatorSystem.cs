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

            Game.GameWorld.Query(in query ,(ref Animator anim, ref Sprite sprite) =>
            {
                bool stateChanged = anim.CheckTransitions();
                anim.CheckTriggers(deltaTime);

                AnimationState curState = anim.GetCurrentState();
                SpriteClip curClip = curState.clip as SpriteClip;
                if (stateChanged)
                {
                    curState.loopStartTime = gameTime;
                    curState.lastFrameTime = 0f;
                    curState.curFrame = -1;
                }

                curState.normalizedTime = (gameTime - curState.loopStartTime) / curState.Length;
                if ((gameTime - curState.SampleFreq) > curState.lastFrameTime)
                {
                    curState.curFrame++;
                    if (curState.curFrame >= curClip.FrameCount)
                    {
                        curState.normalizedTime = 1f;

                        if (curState.IsLooping)
                        {
                            curState.curFrame = 0;
                            curState.loopStartTime = gameTime;
                        }
                        else
                        {
                            curState.curFrame = curClip.FrameCount - 1;
                        }
                    }
                    curState.lastFrameTime = gameTime;

                    sprite.SetUVPosScale(curClip.uvPoses[curState.curFrame], curClip.uvScales[curState.curFrame]);
                }
            }
            );
        }
    }
}
