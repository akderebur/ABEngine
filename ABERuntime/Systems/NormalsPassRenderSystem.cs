using System;
using System.Linq;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Pipelines;
using Arch.Core;
using Veldrid;

namespace ABEngine.ABERuntime
{
	public class NormalsPassRenderSystem : RenderSystem
	{
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();

        SharedMeshVertex sharedVertexUniform;

        public override void SetupResources(params Texture[] samplesTextures)
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

        public override void Render(int renderLayer)
        {
            if (renderLayer == 0)
                Render();
        }

        public override void Render()
        {
            pipelineAsset.BindPipeline();
            cl.SetGraphicsResourceSet(0, Game.pipelineSet);

            Game.GameWorld.Query(in meshQuery, (ref MeshRenderer mr, ref Transform transform) =>
            {
                Mesh mesh = mr.mesh;

                if (mesh == null || mr.material.isLateRender)
                    return;

                // Update vertex uniform
                sharedVertexUniform.transformMatrix = transform.worldMatrix;
                gd.UpdateBuffer(mr.vertexUniformBuffer, 0, sharedVertexUniform);

                cl.SetVertexBuffer(0, mesh.vertexBuffer);
                cl.SetIndexBuffer(mesh.indexBuffer, IndexFormat.UInt16);

                cl.SetGraphicsResourceSet(1, mr.vertexTransformSet);

                // Material Resource Sets
                if (mr.material.bindableSets.Count > 0)
                {
                    var entry = mr.material.bindableSets.ElementAt(0);
                    cl.SetGraphicsResourceSet(entry.Key, entry.Value);
                }

                cl.DrawIndexed((uint)mesh.indices.Length);
            });
        }

        internal override Texture GetMainColorAttachent()
        {
            return Game.resourceContext.cameraNormalTexture;
        }

        internal override Texture GetDepthAttachment()
        {
            return Game.resourceContext.normalsDepthTexture;
        }
    }
}

