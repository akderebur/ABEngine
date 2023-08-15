using System;
using System.Linq;
using ABEngine.ABERuntime.Core.Components;
using ABEngine.ABERuntime.Pipelines;
using Arch.Core;
using Veldrid;

namespace ABEngine.ABERuntime
{
	public class NormalsPassRenderSystem : RenderSystem
	{
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, Mesh>();

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
                        PixelFormat.R16_UNorm, TextureUsage.DepthStencil, TextureSampleCount.Count1));

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

            Game.GameWorld.Query(in meshQuery, (ref Mesh mesh, ref Transform transform) =>
            {
                if (mesh.material.isLateRender)
                    return;

                // Update vertex uniform
                sharedVertexUniform.transformMatrix = transform.worldMatrix;
                gd.UpdateBuffer(mesh.vertexUniformBuffer, 0, sharedVertexUniform);

                cl.SetVertexBuffer(0, mesh.vertexBuffer);
                cl.SetIndexBuffer(mesh.indexBuffer, IndexFormat.UInt16);

                cl.SetGraphicsResourceSet(1, mesh.vertexTransformSet);

                // Material Resource Sets
                if (mesh.material.bindableSets.Count > 0)
                {
                    var entry = mesh.material.bindableSets.ElementAt(0);
                    cl.SetGraphicsResourceSet(entry.Key, entry.Value);
                }

                cl.DrawIndexed((uint)mesh.indices.Length);
            });
        }

        internal override Texture GetMainColorAttachent()
        {
            return cameraNormalTexture;
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

