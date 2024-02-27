using System;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
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
        DrawData drawData;
        BindGroup normalsFrameSet;
        MeshMatrixData[] matrixDataArray = new MeshMatrixData[MeshRenderSystem.maxMeshCount];
        Matrix4x4[] boneDataArray = new Matrix4x4[2000];

        new NormalsPipeline pipelineAsset;

        public override void SetupResources(params TextureView[] samplesTextures)
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
                    BindGroupLayout = GraphicsManager.normalsFrameData,
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
            int matrixStart = 0;
            int groupID = 0;
            for (int matGroupID = 0; matGroupID < Game.meshRenderSystem.opaqueKeys.Count; matGroupID++)
            {
                var material = Game.meshRenderSystem.opaqueKeys[matGroupID];
                var matGroup = Game.meshRenderSystem.opaqueDict[material];

                foreach (var meshKey in matGroup.keyList)
                {
                    var meshGroup = matGroup.meshGroups[meshKey];
                    int renderC = meshGroup.renderCount;

                    drawData.matrixStartID = matrixStart;
                    Game.meshRenderSystem.groupDrawDatas[groupID] = drawData;
                    wgil.WriteBuffer(Game.meshRenderSystem.drawDataBuffer,
                                     Game.meshRenderSystem.groupDrawDatas[groupID],
                                     Game.meshRenderSystem.bufferStep * groupID,
                                     12);

                    pass.SetBindGroup(1, (uint)(Game.meshRenderSystem.bufferStep * groupID), Game.meshRenderSystem.drawDataset);

                    Mesh mesh = meshKey;
                    pass.SetVertexBuffer(0, mesh.vertexBuffer);
                    pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                    int locID = 0;
                    int skipCount = 0;

                    if (meshGroup.lastMatrixStart != matrixStart)
                    {
                        // Include static matrices to write too
                        foreach (MeshMatrixData matrixData in meshGroup.staticRenders)
                        {
                            matrixDataArray[locID++] = matrixData;
                        }
                    }
                    else
                    {
                        // Write dynamics only
                        skipCount += meshGroup.staticRenders.Count;
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

                    if (locID > 0)
                        wgil.WriteBuffer(Game.meshRenderSystem.matrixStorageBuffer, matrixDataArray, matrixStart * 128 + skipCount * 128, locID * 128);
                    pass.DrawIndexed(mesh.Indices.Length, renderC);

                    matrixStart += renderC;
                    groupID++;
                }
            }

            // Skinned
            pass.SetPipeline(pipelineAsset.skinPipeline.pipeline);

            matrixStart = 0;
            for (int matGroupID = 0; matGroupID < Game.meshRenderSystem.opaqueKeys.Count; matGroupID++)
            {
                var material = Game.meshRenderSystem.opaqueKeys[matGroupID];
                var matGroup = Game.meshRenderSystem.opaqueDict[material];

                foreach (var meshKey in matGroup.skinnedKeyList)
                {
                    Mesh mesh = meshKey;

                    var meshGroup = matGroup.skinnedMeshGroups[mesh];
                    int renderC = meshGroup.renderCount;
                    int boneCount = mesh.invBindMatrices.Length;

                    drawData.boneStartID = matrixStart;
                    drawData.meshBoneCount = boneCount;
                    Game.meshRenderSystem.groupDrawDatas[groupID] = drawData;
                    wgil.WriteBuffer(Game.meshRenderSystem.drawDataBuffer,
                                     Game.meshRenderSystem.groupDrawDatas[groupID],
                                     Game.meshRenderSystem.bufferStep * groupID,
                                     12);

                    pass.SetBindGroup(1, (uint)(Game.meshRenderSystem.bufferStep * groupID), Game.meshRenderSystem.drawDataset);

                    pass.SetVertexBuffer(0, mesh.vertexBuffer);
                    pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                    int locID = 0;
                    foreach (Transform[] bones in meshGroup.renders)
                    {
                        // Update bone matrices
                        for (int b = 0; b < bones.Length; b++)
                        {
                            boneDataArray[locID++] = mesh.invBindMatrices[b] * bones[b].worldMatrix;
                        }
                    }

                    if (locID > 0)
                        wgil.WriteBuffer(Game.meshRenderSystem.boneStorageBuffer, boneDataArray, matrixStart * 64, locID * 64);
                    pass.DrawIndexed(mesh.Indices.Length, renderC);

                    matrixStart += renderC * boneCount;
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
            return Game.resourceContext.mainDepthView;
        }

    }
}

