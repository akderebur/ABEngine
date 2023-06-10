using System;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Animation.StateMatch;

namespace ABEngine.ABERuntime
{
    public class StateAnimatorSystem : BaseSystem
    {
        public override void Update(float gameTime, float deltaTime)
        {
            var query = Game.GameWorld.CreateQuery().Has<StateMatchAnimator>().Has<Sprite>();
            query.Foreach((ref StateMatchAnimator anim, ref Sprite sprite) =>
            {
                bool stateChanged = anim.CheckStates();
                anim.CheckTriggers(deltaTime);

                AnimationMatch curMatch = anim.GetCurrentAnimMatch();
                AnimationState curState = curMatch.animationState;
                SpriteClip curClip = curState.clip;
                if (stateChanged)
                {
                    anim.AnimationStarted(curMatch);
                    curState.loopStartTime = gameTime;
                    curState.lastFrameTime = 0f;
                    curState.curFrame = -1;
                }

                curState.normalizedTime = (gameTime - curState.loopStartTime) / curState.length;
                if(curState.normalizedTime >= 1f && !curState.looping && curState.loopStartTime != 0)
                {
                    curState.completed = true;
                    anim.AnimationComplete(curMatch);
                }

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
                            if (!curState.completed)
                                anim.AnimationComplete(curMatch);

                            curState.completed = true;
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
