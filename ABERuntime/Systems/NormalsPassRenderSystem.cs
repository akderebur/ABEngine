using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Pipelines;
using WGIL;

namespace ABEngine.ABERuntime
{
	public class NormalsPassRenderSystem : RenderSystem
	{
        //private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();

        MeshMatrixData matrixData;
        DrawData drawData;
        BindGroup normalsFrameSet;
        MeshMatrixData[] matrixDataArray = new MeshMatrixData[MeshRenderSystem.maxMeshCount];
        Matrix4x4[] boneDataArray = new Matrix4x4[2000];

        new NormalsPipeline pipelineAsset;

        public override void SetupResources(params TextureView[] samplesTextures)
        {
           
        }

        protected override void StartScene()
        {
            matrixData = new MeshMatrixData();
            drawData = new DrawData();
        }

        protected override void ChangeScene()
        {
            pipelineAsset = new NormalsPipeline();

            if (normalsFrameSet == null)
            {
                var normalsFrameData = new BindGroupDescriptor()
                {
                    BindGroupLayout = Graphics.normalsFrameData,
                    Entries = new BindResource[]
                      {
                        Game.pipelineBuffer,
                        Game.meshRenderSystem.matrixStorageBuffer,
                        Game.meshRenderSystem.boneStorageBuffer
                      }
                };
                normalsFrameSet = wgil.CreateBindGroup(ref normalsFrameData).SetManualDispose(true);
            }
        }

        public override void Render(RenderPass pass, int renderLayer)
        {
            if (renderLayer == 0)
                Render(pass);
        }

        public override void Update(float gameTime, float deltaTime)
        {
            
        }

        public override void Render(RenderPass pass)
        {
            pass.SetPipeline(pipelineAsset.pipeline);
            pass.SetBindGroup(0, normalsFrameSet);

            foreach (var matGroup in Game.meshRenderSystem.opaqueRenders.Values)
            {
                foreach (var batch in matGroup.meshBatches.Values)
                {
                    pass.SetBindGroup(1, (uint)(Game.meshRenderSystem.bufferStep * batch.batchID), Game.meshRenderSystem.drawDataset);
                    batch.Render(pass);
                }
            }
        }

        internal override TextureView GetMainColorAttachent()
        {
            return Game.resourceContext.cameraNormalView;
        }

        internal override TextureView GetDepthAttachment()
        {
            return Game.resourceContext.mainDepthView;
        }

    }
}

