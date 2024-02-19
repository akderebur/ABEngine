using System;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Pipelines;
using ABEngine.ABERuntime.Rendering;
using Arch.Core;
using WGIL;
using WGIL.IO;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime
{
	public class NormalsPassRenderSystem : RenderSystem
	{
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();

        MeshMatrixData matrixData;
        BindGroup normalsFrameSet;

        public override void SetupResources(params TextureView[] samplesTextures)
        {
           
        }

        public override void Start()
        {
            base.Start();
            matrixData = new MeshMatrixData();
        }

        public override void SceneSetup()
        {
            if(!GraphicsManager.render2DOnly)
                base.pipelineAsset = new NormalsPipeline();

            if (normalsFrameSet == null)
            {
                var normalsFrameData = new BindGroupDescriptor()
                {
                    BindGroupLayout = GraphicsManager.normalsFrameData,
                    Entries = new BindResource[]
                      {
                        Game.pipelineBuffer,
                        Game.meshRenderSystem.matrixStorageBuffer
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

        public override void Render(RenderPass pass)
        {
            pass.SetPipeline(pipelineAsset.pipeline);
            pass.SetBindGroup(0, normalsFrameSet);

            int matrixStart = 0;
            int groupID = 0;
            for (int matGroupID = 0; matGroupID < Game.meshRenderSystem.opaqueKeys.Count; matGroupID++)
            {
                var material = Game.meshRenderSystem.opaqueKeys[matGroupID];
                var matGroup = Game.meshRenderSystem.opaqueDict[material];

                foreach (var meshKey in matGroup.keyList)
                {
                    var meshGroup = matGroup.meshGroups[meshKey];
                    int renderC = meshGroup.renderC;

                    Game.meshRenderSystem.groupDrawDatas[groupID].matrixStartID = matrixStart;
                    wgil.WriteBuffer(Game.meshRenderSystem.drawDataBuffer,
                                     Game.meshRenderSystem.groupDrawDatas[groupID],
                                     Game.meshRenderSystem.bufferStep * matGroupID,
                                     4);

                    pass.SetBindGroup(1, (uint)(Game.meshRenderSystem.bufferStep * groupID), Game.meshRenderSystem.drawDataset);

                    Mesh mesh = meshKey;
                    pass.SetVertexBuffer(0, mesh.vertexBuffer);
                    pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                    int locID = 0;
                    int skipCount = 0;

                    MeshMatrixData[] matrixDataArray = null;
                    if (meshGroup.lastMatrixStart != matrixStart)
                    {
                        // Include static matrices to write too
                        matrixDataArray = new MeshMatrixData[renderC];
                        foreach (MeshMatrixData matrixData in meshGroup.staticRenders)
                        {
                            matrixDataArray[locID++] = matrixData;
                        }
                    }
                    else
                    {
                        // Write dynamics only
                        skipCount += meshGroup.staticRenders.Count;
                        matrixDataArray = new MeshMatrixData[meshGroup.dynamicRenders.Count];
                    }
                    meshGroup.lastMatrixStart = matrixStart;

                    foreach (Transform transform in meshGroup.dynamicRenders)
                    {
                        // Update model matrices
                        matrixData.transformMatrix = transform.worldMatrix;
                        Matrix4x4 MV = transform.worldMatrix;
                        Matrix4x4 MVInv;
                        Matrix4x4.Invert(MV, out MVInv);
                        matrixData.normalMatrix = Matrix4x4.Transpose(MVInv);
                        matrixDataArray[locID++] = matrixData;
                    }

                    if(locID > 0)
                        wgil.WriteBuffer(Game.meshRenderSystem.matrixStorageBuffer, matrixDataArray, matrixStart * 128 + skipCount * 128, locID * 128);
                    pass.DrawIndexed(mesh.Indices.Length, renderC);

                    matrixStart += renderC;
                    groupID++;
                }
            }
        }

        internal override TextureView GetMainColorAttachent()
        {
            return Game.resourceContext.cameraNormalView;
        }

        internal override TextureView GetDepthAttachment()
        {
            return Game.resourceContext.normalsDepthView;
        }

    }
}

