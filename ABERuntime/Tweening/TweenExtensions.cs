using System;
using System.Numerics;
using System.Threading.Tasks;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.ECS;
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

        private static async Task<Tweener> GetTweenerAsync(Transform transform)
        {
            if (transform.entity.Has<Tweener>())
                return transform.entity.Get<Tweener>();

            Tweener tweener = new Tweener();

            bool gotAccess = false;

            try
            {
                await EntityManager.frameSemaphore.WaitAsync();
                gotAccess = true;
                transform.entity.Add<Tweener>(tweener);
            }
            finally
            {
                if(gotAccess)
                    EntityManager.frameSemaphore.Release();
            }

            return tweener;
        }

        public static async Task<Tween> TweenPosition(this AsyncEntity asyncEnt, Vector3 endPos, float duration)
        {
            Transform transform = asyncEnt.Get<Transform>();
            Tweener tweener = await GetTweenerAsync(transform);
            Vector3 startPos = transform.localPosition;

            var tween = new Tween((float time) =>
            {
                transform.localPosition = Vector3.Lerp(startPos, endPos, time);
            }, duration);
            tweener.SetTween(tween);
            tween.Start();
            return tween;
        }

        public static async Task<Tween> TweenScale(this AsyncEntity asyncEnt, Vector3 endScale, float duration)
        {
            Transform transform = asyncEnt.Get<Transform>();
            Tweener tweener = await GetTweenerAsync(transform);
            Vector3 startScale = transform.localScale;

            var tween = new Tween((float time) =>
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, time);
            }, duration);
            tweener.SetTween(tween);
            tween.Start();
            return tween;
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


        public static Tween TweenRotation(this Transform transform, Vector3 endRot, float duration)
        {
            Tweener tweener = GetTweener(transform);
            Vector3 startRot = transform.localEulerAngles;
            startRot = new Vector3(startRot.X % MathF.PI, startRot.Y % MathF.PI, startRot.Z % MathF.PI);

            var tween = new Tween((float time) =>
            {
                transform.localEulerAngles = Vector3.Lerp(startRot, endRot, time);
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

        public static Tween TweenJump(this Transform transform, Vector3 endPos, float jumpPower, float duration)
        {
            Tweener tweener = GetTweener(transform);
            Vector3 startPos = transform.localPosition;

            var tween = new Tween((float t) =>
            {
                float y = (float)(-4 * jumpPower * t * (t - 1));
                Vector3 position = Vector3.Lerp(startPos, endPos, t);
                position.Y += y;

                transform.localPosition = position;
            }, duration);
            tweener.SetTween(tween);
            tween.Start();
            return tween;
        }


        public static Tween TweenFloat(this PipelineMaterial material, Transform transform, string propName, float startValue, float endValue, float duration)
        {
            Tweener tweener = GetTweener(transform);

            var tween = new Tween((float time) =>
            {
                material.SetFloat(propName, Lerp(startValue, endValue, time));
            }, duration);
            tweener.SetTween(tween);
            tween.Start();
            return tween;
        }

        public static Tween TweenFloat(this float floatVal, Transform transform, float endValue, float duration, Action<float> setter)
        {
            Tweener tweener = GetTweener(transform);
            float startVal = floatVal;

            var tween = new Tween((float time) =>
            {
                setter?.Invoke(Lerp(startVal, endValue, time));
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

        public static float Lerp(float start, float end, float amount)
        {
            return start + (end - start) * amount;
        }
    }
}

