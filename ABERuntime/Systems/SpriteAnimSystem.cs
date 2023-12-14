using System;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Arch.Core;

namespace ABEngine.ABERuntime
{
    public class SpriteAnimSystem : BaseSystem
    {

        public override void Update(float gameTime, float deltaTime)
        {
            var query = new QueryDescription().WithAll<SpriteAnimation, Sprite>().WithNone<Animator>();
            Game.GameWorld.Query(in query, (ref SpriteAnimation anim, ref Sprite sprite) =>
            {
                AnimationState curState = anim.state;
                SpriteClip curClip = curState.clip;

                curState.normalizedTime = (gameTime - curState.loopStartTime) / curState.length;
                if ((gameTime - curState.sampleFreq) > curState.lastFrameTime)
                {
                    curState.curFrame++;
                    if (curState.curFrame >= curClip.frameCount)
                    {
                        curState.normalizedTime = 1f;

                        if (curState.looping)
                        {
                            curState.curFrame = 0;
                            curState.loopStartTime = gameTime;
                        }
                        else
                        {
                            curState.curFrame = curClip.frameCount - 1;
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
