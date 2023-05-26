using System;
using System.Collections.Generic;
using Halak;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime.Animation
{
    public class AnimationState : JSerializable
    {
        public string name { get; set; }
        public SpriteClip clip { get; set; }
        internal List<AnimationTransition> transitions { get; set; }
        public Guid stateUID { get; set; }

        // Playback
        private float _length;
        public float length { get { return _length; } }

        private float _speed;
        public float speed { get { return _speed; } set { sampleRate = clip.sampleRate * value; _speed = value; } }

        private float _sampleRate;
        public float sampleRate { get { return _sampleRate; } set { sampleFreq = 1f / value; _length = sampleFreq * clip.frameCount; _sampleRate = value; } }
        public float sampleFreq { get; set; }

        public bool looping { get; set; }

        // Clip Instance
        internal float loopStartTime { get; set; }
        public float normalizedTime { get; internal set; }

        public int curFrame { get; set; }
        public float lastFrameTime { get; set; }

        public AnimationState(SpriteClip clip)
        {
            this.clip = clip;
            this.name = clip.name;
            transitions = new List<AnimationTransition>();
            stateUID = Guid.NewGuid();

            curFrame = -1;
            lastFrameTime = Game.Time;

            speed = 1;
            looping = true;
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
            jObj.Put("Clip", clip.clipAssetPath);
            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            stateUID = Guid.Parse(data["UID"]);
            name = data["Name"];
            clip = AssetCache.CreateSpriteClip(data["Clip"]);
            speed = 1f;
            looping = true;
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
