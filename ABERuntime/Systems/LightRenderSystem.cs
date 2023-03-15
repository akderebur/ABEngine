using System;
using Veldrid;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABERuntime
{
    public class LightRenderSystem : RenderSystem
    {
        const uint lightCountLayer = 10;

        //LightInfo[] lightInfos = new LightInfo[lightLimit];
        //DeviceBuffer lightInfoBuffer;
        ResourceSet textureSet;

        uint lightCount = 0;

        //uint renderLayerStep = lightLimit;

        //uint[] layerLightCounts;

        public static float GlobalLightIntensity = 1f;

        Dictionary<int, DeviceBuffer> layerLightBuffers;
        Dictionary<int, List<LightInfo>> layerLightInfos;

        //lightTestVB = _gd.ResourceFactory.CreateBuffer(new BufferDescription(LightInfo.VertexSize, BufferUsage.VertexBuffer));
        //_gd.UpdateBuffer(lightTestVB, 0, lightVertTest);

        public LightRenderSystem(PipelineAsset asset) : base(asset) { }

        public override void Start()
        {
            base.Start();
            layerLightBuffers = new Dictionary<int, DeviceBuffer>();
            layerLightInfos = new Dictionary<int, List<LightInfo>>();

            textureSet = rf.CreateResourceSet(new ResourceSetDescription(
                   GraphicsManager.sharedTextureLayout,
                   Game.mainRenderTexture, GraphicsManager.linearSamplerWrap
                   ));

            //renderLayerStep = lightLimit / (uint)GraphicsManager.renderLayers.Count;
            //layerLightCounts = new uint[GraphicsManager.renderLayers.Count];

            for (int i = 0; i < GraphicsManager.renderLayers.Count; i++)
            {
                DeviceBuffer lightInfoBuffer = rf.CreateBuffer(new BufferDescription(LightInfo.VertexSize * lightCountLayer, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
                layerLightBuffers.Add(i, lightInfoBuffer);
                layerLightInfos.Add(i, new List<LightInfo>());
            }
        }

        internal void AddLayer()
        {
            if (!started)
                return;

            DeviceBuffer lightInfoBuffer = rf.CreateBuffer(new BufferDescription(LightInfo.VertexSize * lightCountLayer, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            layerLightBuffers.Add(layerLightInfos.Count, lightInfoBuffer);
            layerLightInfos.Add(layerLightInfos.Count, new List<LightInfo>());
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


            var query = Game.GameWorld.CreateQuery().Has<Transform>().Has<PointLight2D>();

            lightCount = 0;
            foreach (var ent in query.GetEntities())
            {
                Transform lightTrans = ent.Get<Transform>();
                PointLight2D light = ent.Get<PointLight2D>();

                for (int i = 0; i <= light.renderLayerIndex; i++)
                {

                    layerLightInfos[i].Add(new LightInfo(lightTrans.worldPosition,
                                                        light.color,
                                                        light.radius,
                                                        light.intensity,
                                                        light.volume
                                                        ));
                }


                lightCount++;
            }
        }

        public override void Render(int renderLayer)
        {
            // Light pass
            pipelineAsset.BindPipeline();
            cl.SetGraphicsResourceSet(1, textureSet);

            // Light Infos
            List<LightInfo> lightList = layerLightInfos[renderLayer];

            // Light Buffer
            DeviceBuffer lightInfoBuffer = layerLightBuffers[renderLayer];
           
            MappedResourceView<LightInfo> writemap = gd.Map<LightInfo>(lightInfoBuffer, MapMode.Write);

            // Global Light
            writemap[0] = new LightInfo(Game.activeCam.worldPosition,
                                                        Vector4.One,
                                                        30,
                                                        GlobalLightIntensity,
                                                        200f
                                                        );
            for (int i = 0; i < lightList.Count; i++)
            {
                writemap[i + 1] = lightList[i];
            }
            gd.Unmap(lightInfoBuffer);

            cl.SetVertexBuffer(0, lightInfoBuffer);

            //cl.Draw(lightCount, 1, 0, 0);
            //cl.Draw(layerLightCounts[renderLayer], 1, renderLayerStep * (uint)renderLayer, 0);
            //cl.Draw(6, layerLightCounts[renderLayer], renderLayerStep * (uint)renderLayer, 0);
            cl.Draw(6, (uint)lightList.Count + 1, 0, 0);

            lightList.Clear();


        }
    }
}

