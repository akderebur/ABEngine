using System;
using System.Numerics;
using System.Collections.Generic;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Components
{
	public class SpriteAnimation
	{
        internal Sprite sprite;
        internal AnimationState state;

        public SortedSet<int> spriteIds { get; set; }

        public bool isPlaying { get; set; }

        public SpriteAnimation(Sprite sprite)
		{
            this.sprite = sprite;
            spriteIds = new SortedSet<int>();
            spriteIds.Add(0);
            RecreateState();           
            isPlaying = true;
        }

        public SpriteAnimation(Sprite sprite, SpriteClip clip)
        {
            this.sprite = sprite;
            state = new AnimationState(clip);
            isPlaying = true;
        }

        public SpriteAnimation(Sprite sprite, List<Vector2> poses)
        {
            this.sprite = sprite;
            state = new AnimationState(AssetCache.CreateSpriteClip(sprite.texture, poses));
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
                poses.Add(sprite.texture[spriteId]);

            state = new AnimationState(AssetCache.CreateSpriteClip(sprite.texture, poses));
        }

        public void SetLooping(bool isLooping)
        {
            if (state != null)
                state.looping = isLooping;
        }
    }
}

