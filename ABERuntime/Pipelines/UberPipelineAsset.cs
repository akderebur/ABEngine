using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipelineAsset : PipelineAsset
    {
        public UberPipelineAsset() : base()
        {
            base.ParseAsset(Shaders.UberPipelineAsset);
        }
    }
}
