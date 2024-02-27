using System;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Animation.StateMatch;
using ABEngine.ABERuntime.Core.Assets;
using Arch.Core;

namespace ABEngine.ABERuntime
{
    public class StateAnimatorSystem : BaseSystem
    {
        private readonly QueryDescription query = new QueryDescription().WithAll<StateMatchAnimator, Sprite>();

        protected override void StartScene()
        {
            Game.GameWorld.Query(in query, (ref StateMatchAnimator anim, ref Transform transform, ref Sprite sprite) =>
            {
                foreach (SpriteClip clip in anim.GetAllClips())
                {
                    var batch = Game.spriteBatchSystem.GetBatchFromSprite(transform, sprite, clip.texture2D, "");
                    if (batch == null)
                        batch = Game.spriteBatchSystem.CreateSpriteBatch(transform, sprite, clip.texture2D, "");

                    batch.SetAutoDestroy(false);
                }

                anim.Init();
            });
        }

        public override void Update(float gameTime, float deltaTime)
        {
            Game.GameWorld.Query(in query, (ref StateMatchAnimator anim, ref Sprite sprite, ref Transform transform) =>
            {
                if (!transform.enabled)
                    return;

                anim.Time += deltaTime;
                float animTime = anim.Time;

                bool frameChanged = false;
                bool stateChanged = anim.CheckStates();
                anim.CheckTriggers(deltaTime);

                AnimationMatch curMatch = anim.GetCurrentAnimMatch();
                AnimationState curState = curMatch.animationState;
                SpriteClip curClip = curState.clip as SpriteClip;
                if (stateChanged)
                {
                    sprite.SetTexture(curClip.texture2D);

                    anim.AnimationStarted(curMatch);
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

                //if (!curState.completed && curState.normalizedTime >= 1f && !curState.IsLooping && curState.loopStartTime != 0)
                //{
                //    curState.completed = true;
                //    anim.AnimationComplete(curMatch);
                //}

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
                            if (!curState.completed)
                                anim.AnimationComplete(curMatch);

                            curState.completed = true;
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
