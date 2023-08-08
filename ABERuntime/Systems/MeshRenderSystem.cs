using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Components;
using Arch.Core;
using Veldrid;

namespace ABEngine.ABERuntime
{
    public struct SharedMeshVertex
    {
        public Matrix4x4 transformMatrix;
    }

    public struct SharedMeshFragment
    {
        public PointLightInfo PointLights0;
        public PointLightInfo PointLights1;
        public PointLightInfo PointLights2;
        public PointLightInfo PointLights3;
        public Vector3 CamPos;
        public float _padding;
        public int NumActiveLights;
        public float _padding1;
        public float _padding2;
        public float _padding3;
    }

    public struct SharedMeshFragmentTest
    {
        public Vector4 lightPos;
        public Vector4 lightColor;
        public Vector4 camPos;
    }

    public class MeshRenderSystem : RenderSystem
    {
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, Mesh>();
        private readonly QueryDescription lightQuery = new QueryDescription().WithAll<Transform, PointLight>();

        DeviceBuffer vertexUniformBuffer;
        DeviceBuffer fragmentUniformBuffer;

        ResourceSet sharedVertexSet;
        ResourceSet sharedFragmentSet;

        SharedMeshVertex sharedVertexUniform;
        SharedMeshFragment sharedFragmentUniform;

        PointLightInfo[] pointLightInfos;

        public MeshRenderSystem(PipelineAsset asset) : base(asset) { }

        public override void Start()
        {
            base.Start();

            pointLightInfos = new PointLightInfo[4];

            vertexUniformBuffer = rf.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            sharedVertexSet = rf.CreateResourceSet(new ResourceSetDescription(GraphicsManager.sharedMeshUniform_VS, vertexUniformBuffer));

            fragmentUniformBuffer = rf.CreateBuffer(new BufferDescription(160, BufferUsage.UniformBuffer));
            sharedFragmentSet = rf.CreateResourceSet(new ResourceSetDescription(GraphicsManager.sharedMeshUniform_FS, fragmentUniformBuffer));

            sharedVertexUniform = new SharedMeshVertex();
            sharedFragmentUniform = new SharedMeshFragment();
        }

        public override void Update(float gameTime, float deltaTime)
        {
            // Fragment uniform update
            int lightC = 0;
            Game.GameWorld.Query(in lightQuery, (ref PointLight light, ref Transform transform) =>
            {
                pointLightInfos[lightC] = new PointLightInfo() { Color = light.color.ToVector3(), Position = transform.worldPosition };

                if (++lightC == 4)
                    return;
            });

            sharedFragmentUniform.PointLights0 = pointLightInfos[0];
            sharedFragmentUniform.PointLights1 = pointLightInfos[1];
            sharedFragmentUniform.PointLights2 = pointLightInfos[2];
            sharedFragmentUniform.PointLights3 = pointLightInfos[3];

            sharedFragmentUniform.CamPos = Game.activeCam.worldPosition;
            sharedFragmentUniform.NumActiveLights = lightC;

            gd.UpdateBuffer(fragmentUniformBuffer, 0, sharedFragmentUniform);
        }

        public override void Render()
        {
            // TODO Render layers
            // TODO Pipeline batching            

            Game.GameWorld.Query(in meshQuery, (ref Mesh mesh, ref Transform transform) =>
            {
                // Update vertex uniform
                sharedVertexUniform.transformMatrix = transform.worldMatrix;
                gd.UpdateBuffer(vertexUniformBuffer, 0, sharedVertexUniform);

                mesh.material.pipelineAsset.BindPipeline();

                cl.SetVertexBuffer(0, mesh.vertexBuffer);
                cl.SetIndexBuffer(mesh.indexBuffer, IndexFormat.UInt16);

                cl.SetGraphicsResourceSet(1, sharedVertexSet);


                // Material Resource Sets
                foreach (var setKV in mesh.material.bindableSets)
                {
                    cl.SetGraphicsResourceSet(setKV.Key, setKV.Value);
                }

                cl.SetGraphicsResourceSet(4, sharedFragmentSet);


                cl.DrawIndexed((uint)mesh.indices.Length);
            });
        }

        public override void CleanUp(bool reload, bool newScene)
        {
            sharedVertexSet.Dispose();
            sharedFragmentSet.Dispose();

            vertexUniformBuffer.Dispose();
            fragmentUniformBuffer.Dispose();
        }

    }
}

