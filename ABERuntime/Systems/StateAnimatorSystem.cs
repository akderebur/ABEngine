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
        public override void Start()
        {
            var query = new QueryDescription().WithAll<StateMatchAnimator, Sprite>();
            Game.GameWorld.Query(in query, (ref StateMatchAnimator anim, ref Transform transform, ref Sprite sprite) =>
            {
                foreach (SpriteClip clip in anim.GetAllClips())
                {
                    var batch = Game.spriteBatchSystem.GetBatchFromSprite(transform, sprite, clip.texture2D, "");
                    if (batch == null)
                        batch = Game.spriteBatchSystem.CreateSpriteBatch(transform, sprite, clip.texture2D, "");

                    batch.SetAutoDestroy(false);
                }
            });
        }

        public override void Update(float gameTime, float deltaTime)
        {
            var query = new QueryDescription().WithAll<StateMatchAnimator, Sprite>();
            Game.GameWorld.Query(in query, (ref StateMatchAnimator anim, ref Sprite sprite) =>
            {
                bool stateChanged = anim.CheckStates();
                anim.CheckTriggers(deltaTime);

                AnimationMatch curMatch = anim.GetCurrentAnimMatch();
                AnimationState curState = curMatch.animationState;
                SpriteClip curClip = curState.clip as SpriteClip;
                if (stateChanged)
                {
                    sprite.SetTexture(curClip.texture2D);

                    anim.AnimationStarted(curMatch);
                    curState.loopStartTime = gameTime;
                    curState.lastFrameTime = 0f;
                    curState.curFrame = -1;
                }

                curState.normalizedTime = (gameTime - curState.loopStartTime) / curState.Length;
                if (curState.normalizedTime >= 1f && !curState.IsLooping && curState.loopStartTime != 0)
                {
                    curState.completed = true;
                    anim.AnimationComplete(curMatch);
                }

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
                            if (!curState.completed)
                                anim.AnimationComplete(curMatch);

                            curState.completed = true;
                            curState.curFrame = curClip.FrameCount - 1;
                        }
                    }
                    curState.lastFrameTime = gameTime;

                    sprite.SetUVPosScale(curClip.uvPoses[curState.curFrame], curClip.uvScales[curState.curFrame]);
                }
            });
        }
    }
}
