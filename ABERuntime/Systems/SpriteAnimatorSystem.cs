﻿using System;
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

                anim.Time += deltaTime;
                float animTime = anim.Time;

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

                curState.normalizedTime = (animTime - curState.loopStartTime) / curState.Length;

                float frameTime = curState.lastFrameTime + curState.SampleFreq;
                while(frameTime <= animTime)
                {
                    curState.curFrame++;
                    frameTime += curState.SampleFreq;
                    frameChanged = true;
                }
                frameTime -= curState.SampleFreq;

                if(frameChanged)
                {
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
                    curState.lastFrameTime = frameTime;

                    sprite.SetUVPosScale(curClip.uvPoses[curState.curFrame], curClip.uvScales[curState.curFrame]);
                }
            });
        }
    }
}
