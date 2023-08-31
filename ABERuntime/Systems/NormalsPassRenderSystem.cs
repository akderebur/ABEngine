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

        Texture cameraNormalTexture;
        Texture normalsDepthTexture;
        Framebuffer normalsRenderFB;


        public override void SetupResources(params Texture[] samplesTextures)
        {
            Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;
            cameraNormalTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
               mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
               PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));


            normalsDepthTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                        mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                        PixelFormat.R16_UNorm, TextureUsage.DepthStencil | TextureUsage.Sampled, TextureSampleCount.Count1));

            normalsRenderFB = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(normalsDepthTexture, cameraNormalTexture));

            if (base.pipelineAsset != null)
                base.pipelineAsset.UpdateFramebuffer(normalsRenderFB);
        }

        public override void Start()
        {
            base.Start();
            sharedVertexUniform = new SharedMeshVertex();
        }

        public override void SceneSetup()
        {
            if(!GraphicsManager.render2DOnly)
                base.pipelineAsset = new NormalsPipeline(normalsRenderFB);
        }

        public override void Render(int renderLayer)
        {
            if(renderLayer == 0)
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
            return cameraNormalTexture;
        }

        internal override Texture GetDepthAttachment()
        {
            return normalsDepthTexture;
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            if(resize)
            {
                cameraNormalTexture.Dispose();
                normalsDepthTexture.Dispose();
                normalsRenderFB.Dispose();
            }
        }
    }
}

