using System;
using System.Numerics;
using System.Collections.Generic;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Components
{
	public class SpriteAnimation
	{
        internal Texture2D texture;
        internal AnimationState state;

        public SortedSet<int> spriteIds { get; set; }

        public bool isPlaying { get; set; }

        public SpriteAnimation(Texture2D texture)
		{
            this.texture = texture;
            spriteIds = new SortedSet<int>();
            spriteIds.Add(0);
            RecreateState();           
            isPlaying = true;
        }
        
        public SpriteAnimation(Texture2D texture, SpriteClip clip)
        {
            this.texture = texture;
            state = new AnimationState(clip);
            isPlaying = true;
        }

        public SpriteAnimation(Sprite sprite, SpriteClip clip)
        {
            this.texture = sprite.texture;
            state = new AnimationState(clip);
            isPlaying = true;
        }

        public SpriteAnimation(Sprite sprite, List<Vector2> poses)
        {
            this.texture = sprite.texture;
            state = new AnimationState(AssetCache.CreateSpriteClip(texture, poses));
            isPlaying = true;
        }

        public void Play()
        {
            isPlaying = true;
            state.curFrame = 0;
            state.lastFrameTime = Game.Time;
            state.loopStartTime = Game.Time;
            state.normalizedTime = 0f;
        }

        public void Stop()
        {
            isPlaying = false;
        }
        
        public void Resume()
        {
            isPlaying = true;
        }
        
        internal void Refresh()
        {
            spriteIds = new SortedSet<int>();
            spriteIds.Add(0);
            RecreateState();
        }

        public int AddSpriteID(int id)
        {
            bool res = spriteIds.Add(id);
            if (res)
            {
                RecreateState();
                return id;
            }

            return -1;
        }

        public int RemoveSpriteID(int id)
        {
            bool res = spriteIds.Remove(id);
            if (res)
            {
                if (spriteIds.Count == 0)
                {
                    spriteIds.Add(0);
                    RecreateState();
                    return -2;
                }

                RecreateState();
                return id;
            }

            return -1;
        }

        private void RecreateState()
        {
            List<Vector2> poses = new List<Vector2>();
            foreach (var spriteId in spriteIds)
                poses.Add(texture[spriteId]);

            state = new AnimationState(AssetCache.CreateSpriteClip(texture, poses));
        }

        public void SetLooping(bool isLooping)
        {
            if (state != null)
                state.IsLooping = isLooping;
        }
    }
}

