using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Pipelines;
using Friflo.Engine.ECS;
using WGIL;
using Buffer = WGIL.Buffer;
using Transform = ABEngine.ABERuntime.Components.Transform;

namespace ABEngine.ABERuntime
{
    public class LightRenderSystem : RenderSystem
    {
        const int maxLightCount = 30;

        uint lightCount = 0;
        public static float GlobalLightIntensity = 1f;

        Buffer lightBuffer;
        List<LightInfo> lightInfos;

        // Rendering
        BindGroup textureSet;

        public LightRenderSystem()
        {
            lightInfos = new List<LightInfo>();
            lightBuffer = wgil.CreateBuffer(LightInfo.VertexSize * maxLightCount, BufferUsages.VERTEX | BufferUsages.COPY_DST).SetManualDispose(true);
        }

        public override void SetupResources(params TextureView[] sampledTextures)
        {
            if(base.pipelineAsset == null)
                base.pipelineAsset = new LightPipelineAsset();

            if (textureSet != null)
                textureSet.Dispose();

            var textureSetDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = Graphics.sharedLightTexLayout,
                Entries = new BindResource[]
                {
                    sampledTextures[0],
                    Graphics.linearSamplerWrap,
                    sampledTextures[1],
                    Graphics.linearSamplerWrap
                }
            };

            textureSet = wgil.CreateBindGroup(ref textureSetDesc).SetManualDispose(true);
        }

        protected override void StartScene()
        {
            if (pipelineAsset == null)
                pipelineAsset = new LightPipelineAsset();

            //renderLayerStep = lightLimit / (uint)GraphicsManager.renderLayers.Count;
            //layerLightCounts = new uint[GraphicsManager.renderLayers.Count];
        }

        internal void AddLayer()
        {
            if (!started)
                return;
        }

        //public bool LayerHasLights(int layerId)
        //{
        //    return layerLightCounts[layerId] > 0;
        //}

        public override void Update(float gameTime, float deltaTime)
        {
            if (!started)
                return;

            base.Update(gameTime, deltaTime);
            
            lightCount = 0;
            lightInfos.Clear();
            var queryLights = Game.GameWorld.Query<TRS, PointLight2D>();
            queryLights.ForEachEntity((ref TRS transform, ref PointLight2D light, Entity entity) => {
                Vector4 sizeIntVol = new Vector4(light.radius, light.radius, light.intensity, light.volume);
                lightInfos.Add(new LightInfo(transform.Position,
                    light.color,
                    sizeIntVol,
                    light.renderLayerIndex
                ));
                lightCount++;
            });
        }

        public override void Render(RenderPass pass)
        {
            if (Game.activeCamera.IsNull)
                return;

            // Light pass
            pass.SetPipeline(pipelineAsset.pipeline);
            pass.SetBindGroup(1, textureSet);

            // Light Infos
            List<LightInfo> lightList = lightInfos;

            // Light Buffer
            Buffer lightInfoBuffer = lightBuffer;

            LightInfo[] writemap = new LightInfo[lightList.Count + 1];

            // Global Light
            writemap[0] = new LightInfo(Game.activeCamera.LocalTransform.Position - Vector3.UnitZ,
                                                        Vector4.One,
                                                        new Vector4(30, 30, GlobalLightIntensity, 0),
                                                        maxLightCount,
                                                        1);
            for (int i = 0; i < lightList.Count; i++)
            {
                writemap[i + 1] = lightList[i];
            }

            wgil.WriteBuffer(lightInfoBuffer, writemap, 0, LightInfo.VertexSize * (lightList.Count + 1));

            pass.SetVertexBuffer(0, lightInfoBuffer);

            //cl.Draw(lightCount, 1, 0, 0);
            //cl.Draw(layerLightCounts[renderLayer], 1, renderLayerStep * (uint)renderLayer, 0);
            //cl.Draw(6, layerLightCounts[renderLayer], renderLayerStep * (uint)renderLayer, 0);
            pass.Draw(0, 6, 0, lightList.Count + 1);

            lightList.Clear();
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            if (resize)
            {
                textureSet.Dispose();
                textureSet = null;
            }
            else
            {
                // Reload
                base.pipelineAsset = null;
            }
        }

        internal override TextureView GetMainColorAttachent()
        {
            return Game.resourceContext.lightRenderView;
        }
    }
}

