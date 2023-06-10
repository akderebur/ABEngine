using System;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABERuntime
{
    public class SpriteAnimatorSystem : BaseSystem
    {
        public override void Start()
        {
            //var query = _world.CreateQuery().Has<Animator>();
            //foreach (var animEnt in query.GetEntities())
            //{
            //    var animator = animEnt.Get<Animator>();
            //    animator.animGraph = animator.animGraph;
            //}
        }

        public override void Update(float gameTime, float deltaTime)
        {
            var query = Game.GameWorld.CreateQuery().Has<Animator>().Has<Sprite>();
            query.Foreach((ref Animator anim, ref Sprite sprite) =>
            {
                bool stateChanged = anim.CheckTransitions();
                anim.CheckTriggers(deltaTime);

                AnimationState curState = anim.GetCurrentState();
                SpriteClip curClip = curState.clip;
                if (stateChanged)
                {
                    curState.loopStartTime = gameTime;
                    curState.lastFrameTime = 0f;
                    curState.curFrame = -1;
                }

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
