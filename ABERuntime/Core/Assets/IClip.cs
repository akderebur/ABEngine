using System;
namespace ABEngine.ABERuntime.Core.Assets
{
	public interface IClip
	{
		public float sampleRate { get; }
		public float sampleFreq { get; }
        public float clipLength { get; }
		public int frameCount { get;  }
	}
}

