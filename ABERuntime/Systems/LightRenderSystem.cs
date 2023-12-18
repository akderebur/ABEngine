using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Arch.Core;
using ABEngine.ABERuntime.Pipelines;
using WGIL;
using Buffer = WGIL.Buffer;

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
                BindGroupLayout = ((LightPipelineAsset)base.pipelineAsset).GetTexResourceLayout(),
                Entries = new BindResource[]
                {
                    sampledTextures[0],
                    GraphicsManager.linearSamplerWrap,
                    sampledTextures[1],
                    GraphicsManager.linearSamplerWrap
                }
            };

            textureSet = wgil.CreateBindGroup(ref textureSetDesc).SetManualDispose(true);
        }

        public override void SceneSetup()
        {
            //base.pipelineAsset = new LightPipelineAsset(lightRenderFB);
        }

        public override void Start()
        {
            base.Start();

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

            var query = new QueryDescription().WithAll<Transform, PointLight2D>();

            lightCount = 0;
            lightInfos.Clear();
            Game.GameWorld.Query(in query, (ref Transform lightTrans, ref PointLight2D light) =>
            {

                lightInfos.Add(new LightInfo(lightTrans.worldPosition,
                                                    light.color,
                                                    light.radius,
                                                    light.intensity,
                                                    light.volume,
                                                    light.renderLayerIndex
                                                    ));

                lightCount++;
            });
        }

        public override void Render(RenderPass pass)
        {
            if (Game.activeCam == null)
                return;

            // Light pass
            pipelineAsset.BindPipeline(pass);
            pass.SetBindGroup(1, textureSet);

            // Light Infos
            List<LightInfo> lightList = lightInfos;

            // Light Buffer
            Buffer lightInfoBuffer = lightBuffer;

            LightInfo[] writemap = new LightInfo[lightList.Count + 1];

            // Global Light
            writemap[0] = new LightInfo(Game.activeCam.worldPosition - Vector3.UnitZ,
                                                        Vector4.One,
                                                        30,
                                                        GlobalLightIntensity,
                                                        0f,
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

