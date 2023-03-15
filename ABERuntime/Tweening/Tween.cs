using System;
namespace ABEngine.ABERuntime.Tweening
{
	public enum LoopType
	{
		Reset,
		PingPong
	}

	public class Tween
	{
		//public bool start { get; set; }
		public float duration { get; set; }
		public LoopType loopType { get; set; }

        private Func<float, float> _ease;

        public bool isRunning = false;
		public Tweener tweener;

		private float startTime;
        private float endTime;
		private int loopCount;
		private int loopLeft;
		public float progress;

        //private float time;
		private Action<float> tweenFunc;
		private Action onComplete;

		private float scale = 1f;
		private float bias = 0f;

		public Tween(Action<float> tweenDelegate, float duration)
		{
			this.tweenFunc = tweenDelegate;
			this.duration = duration;
			_ease = Ease.Linear;

			if (duration <= 0f)
				duration = 0.01f;
		}

		public Tween Start()
		{
			isRunning = true;
			startTime = Game.Time;
			return this;
		}

        public Tween SetLoops(int loopCount)
        {
            this.loopCount = loopCount;
			this.loopLeft = loopCount;
            return this;
        }

        public Tween SetLoops(int loopCount, LoopType loopType)
        {
            this.loopCount = loopCount;
            this.loopLeft = loopCount;
			this.loopType = loopType;
            return this;
        }

        public Tween SetEase(Func<float, float> ease)
        {
            _ease = ease;
            return this;
        }


        public Tween OnComplete(Action onComplete)
        {
			this.onComplete = onComplete;
            return this;
        }


        internal void SetTweener(Tweener tweener)
		{
			this.tweener = tweener;
		}

		internal void Pause(bool isPaused)
		{
			if(isPaused)
			{
				isRunning = false;
			}
			else
			{
                startTime = Game.Time - progress * duration;
                isRunning = true;
			}
		}

        public void UpdateTween(float newTime)
		{
			if (!isRunning)
				return;
           
            progress = (newTime - startTime) / duration;
			tweenFunc(Math.Clamp(_ease(progress * scale + bias), 0f, 1f)); // Tween step

            if (progress >= 1f)
			{
				loopLeft--;
				if(loopCount == -1 || loopLeft >= 1) // Has loops left, restart
				{
					startTime = newTime;
					switch (loopType)
					{
						case LoopType.PingPong: // Invert scale/bias: Normal 1 / 0, Inverse -1 / 1
							scale *= -1f; 
							bias = MathF.Abs(bias - 1f);
							break;
						default:
							break;
					}
				}
				else
				{	// End and remove tween
					tweener.RemoveTween(this);
					isRunning = false;

					onComplete?.Invoke();
				}
			}

		}

	}
}

