using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Core.Assets;
using Halak;

namespace ABEngine.ABERuntime.Animation
{
    public class AnimationState : JSerializable
    {
        public string name { get; set; }
        public IClip clip { get; set; }
        internal List<AnimationTransition> transitions { get; set; }
        public Guid stateUID { get; set; }

        // Playback
        private float _length;
        public float Length { get { return _length; } }

        private float _speed;
        public float Speed { get { return _speed; } set { SampleRate = clip.SampleRate * value; _speed = value; } }

        private float _sampleRate;
        public float SampleRate { get { return _sampleRate; } set { SampleFreq = 1f / value; _length = SampleFreq * clip.FrameCount; _sampleRate = value; } }
        public float SampleFreq { get; set; }

        public bool IsLooping { get; set; }

        // Clip Instance
        internal float loopStartTime { get; set; }
        public float normalizedTime { get; internal set; }
        internal float unclampedNormTime { get; set; }

        public int curFrame { get; set; }
        public float lastFrameTime { get; set; }

        internal bool completed { get; set; }

        internal float transitionTime { get; set; }
        internal float transitionDur { get; set; }

        public AnimationState(IClip clip, bool looping)
        {
            this.clip = clip;
            //this.name = clip.name;
            transitions = new List<AnimationTransition>();
            stateUID = Guid.NewGuid();

            curFrame = -1;
            lastFrameTime = Game.Time;

            Speed = 1;
            this.IsLooping = looping;
            this.transitionTime = 2f;
            this.transitionDur = 1f;
        }

        public AnimationState(IClip clip) : this(clip, false)
        {
          
        }

        public AnimationState()
        {
            transitions = new List<AnimationTransition>();
            stateUID = Guid.NewGuid();

            curFrame = -1;
            lastFrameTime = Game.Time;
        }

        public bool GetNextState(ref AnimationState animatorState, ref List<string> transParams)
        {
            foreach (var transition in transitions)
            {
                AnimationState nextState = transition.GetNextState();
                if(nextState != this) // Changing states
                {
                    transParams = transition.transParamKeys;
                    animatorState = nextState;
                    if (transition.transitionTime <= 0)
                    {
                        nextState.transitionTime = 2f;
                        nextState.transitionDur = 1f;
                    }
                    else
                    {
                        nextState.transitionTime = 0;
                        nextState.transitionDur = transition.transitionTime;
                    }
                    return true;
                }
            }

            return false;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            jObj.Put("type", GetType().ToString());
            jObj.Put("UID", stateUID.ToString());
            jObj.Put("Name", name);
            //jObj.Put("Clip", clip.clipAssetPath);
            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            stateUID = Guid.Parse(data["UID"]);
            name = data["Name"];
            //clip = AssetCache.CreateSpriteClip(data["Clip"]);
            Speed = 1f;
            IsLooping = true;
            //SetClipAsset(data["Clip"]);
        }

        //public void SetClipAsset(string clipAssetPath)
        //{
        //    clip = new SpriteClip(clipAssetPath);
        //    _length = clip.clipLength;
        //}

        public int GetTransitionIndex(AnimationTransition trans)
        {
            return transitions.IndexOf(trans);
        }

        public void SetReferences()
        {
        }

        public JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }
    }
}
