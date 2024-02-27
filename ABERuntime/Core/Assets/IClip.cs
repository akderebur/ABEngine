using System;
namespace ABEngine.ABERuntime.Core.Assets
{
	public interface IClip
	{
		public float SampleRate { get; }
		public float SampleFreq { get; }
        public float ClipLength { get; }
		public int FrameCount { get;  }

		public string ClipAssetPath { get; }
	}
}

