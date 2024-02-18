using System;
namespace ABEngine.ABERuntime.Core.Assets
{
	public interface IClip
	{
		public float SampleRate { get; set; }
		public float SampleFreq { get; set; }
        public float ClipLength { get; set; }
		public int FrameCount { get; set; }

		public string ClipAssetPath { get; set; }
	}
}

