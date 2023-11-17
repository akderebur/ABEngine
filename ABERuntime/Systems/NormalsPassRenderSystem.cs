using System;
using System.Linq;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Pipelines;
using Arch.Core;
using WGIL;

namespace ABEngine.ABERuntime
{
	public class NormalsPassRenderSystem : RenderSystem
	{
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();

        SharedMeshVertex sharedVertexUniform;

        public override void SetupResources(params TextureView[] samplesTextures)
        {
           
        }

        public override void Start()
        {
            base.Start();
            sharedVertexUniform = new SharedMeshVertex();
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

            Game.GameWorld.Query(in meshQuery, (ref MeshRenderer mr, ref Transform transform) =>
            {
                Mesh mesh = mr.mesh;

                if (mesh == null || mr.material.isLateRender)
                    return;

                // Update vertex uniform
                sharedVertexUniform.transformMatrix = transform.worldMatrix;
                wgil.WriteBuffer(mr.vertexUniformBuffer, sharedVertexUniform);

                pass.SetVertexBuffer(0, mesh.vertexBuffer);
                pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                pass.SetBindGroup(1, mr.vertexTransformSet);

                // Material Resource Sets
                if (mr.material.bindableSets.Count > 0)
                {
                    var entry = mr.material.bindableSets.ElementAt(0);
                    pass.SetBindGroup(entry.Key, entry.Value);
                }

                pass.DrawIndexed(mesh.indices.Length);
            });
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

