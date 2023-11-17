using System;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipeline3D : PipelineAsset
    {
        public UberPipeline3D() : base()
        {
            base.ParseAsset(Shaders3D.UberPipeline3DAsset);
        }
    }
}

