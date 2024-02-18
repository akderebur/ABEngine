using System;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Pipelines;
using ABEngine.ABERuntime.Rendering;
using Arch.Core;
using WGIL;

namespace ABEngine.ABERuntime
{
	public class NormalsPassRenderSystem : RenderSystem
	{
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();

        SharedMeshVertex sharedVertexUniform;
        SharedMeshVertex[] meshBufferData;

        public override void SetupResources(params TextureView[] samplesTextures)
        {
           
        }

        public override void Start()
        {
            base.Start();
            sharedVertexUniform = new SharedMeshVertex();
            meshBufferData = new SharedMeshVertex[MeshRenderSystem.maxMeshCount];
        }

        public override void SceneSetup()
        {
            if(!GraphicsManager.render2DOnly)
                base.pipelineAsset = new NormalsPipeline();
        }

        public override void Render(RenderPass pass, int renderLayer)
        {
            if (renderLayer == 0)
                Render(pass);
        }

        public override void Render(RenderPass pass)
        {
            pipelineAsset.BindPipeline(pass);
            
            int renderID = 0;

            foreach (var renderPair in Game.meshRenderSystem.opaqueRenderOrder)
            {
                var renderList = renderPair.Value;

                foreach (var meshGr in renderList.GroupBy(r => r.Item1.mesh))
                {
                    Mesh mesh = meshGr.Key;
                    pass.SetVertexBuffer(0, mesh.vertexBuffer);
                    pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                    foreach (var render in meshGr)
                    {
                        MeshRenderer mr = render.Item1;
                        Transform transform = render.Item2;

                        // Update vertex uniform
                        sharedVertexUniform.transformMatrix = transform.worldMatrix;
                        Matrix4x4 MV = transform.worldMatrix;
                        Matrix4x4 MVInv;
                        Matrix4x4.Invert(MV, out MVInv);
                        sharedVertexUniform.normalMatrix = Matrix4x4.Transpose(MVInv);
                        meshBufferData[renderID] = sharedVertexUniform;
                        mr.renderID = renderID;

                        pass.SetBindGroup(1, (uint)(Game.meshRenderSystem.bufferStep * renderID), Game.meshRenderSystem.transformSet);

                        pass.DrawIndexed(mesh.Indices.Length);

                        renderID++;
                    }
                }
            }

            wgil.WriteBuffer(Game.meshRenderSystem.meshTransformBuffer, meshBufferData, 0, Game.meshRenderSystem.bufferStep * renderID);
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

