using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Arch.Core.Extensions;

namespace ABEngine.ABERuntime.Tweening
{
	public static class TweenExtensions
	{
        private static Tweener GetTweener(Transform transform)
        {
            if (transform.entity.Has<Tweener>())
                return transform.entity.Get<Tweener>();

            Tweener tweener = new Tweener();
            transform.entity.Add<Tweener>(tweener);

            return tweener;
        }

		public static Tween TweenPosition(this Transform transform, Vector3 endPos, float duration)
		{
			Tweener tweener = GetTweener(transform);
			Vector3 startPos = transform.localPosition;

			var tween = new Tween((float time) =>
			{
				transform.localPosition = Vector3.Lerp(startPos, endPos, time);
			}, duration);
			tweener.SetTween(tween);
			tween.Start();
			return tween;
		}

        public static Tween TweenScale(this Transform transform, Vector3 endScale, float duration)
        {
            Tweener tweener = GetTweener(transform);
            Vector3 startScale = transform.localScale;

            var tween = new Tween((float time) =>
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, time);
            }, duration);
            tweener.SetTween(tween);
            tween.Start();
            return tween;
        }

        public static Tween TweenSpriteColor(this Sprite sprite, Vector4 endColor, float duration)
        {
            Tweener tweener = GetTweener(sprite.transform);
            Vector4 startColor = sprite.tintColor;

            var tween = new Tween((float time) =>
            {
                sprite.tintColor = Vector4.Lerp(startColor, endColor, time);
            }, duration);
            tweener.SetTween(tween);
            tween.Start();
            return tween;
        }

        public static void KillTweens(this Transform transform)
		{
			if (transform.entity.Has<Tweener>())
				transform.entity.Remove<Tweener>();
		}
    }
}

