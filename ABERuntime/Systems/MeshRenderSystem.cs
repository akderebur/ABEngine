using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Components;
using ABEngine.ABERuntime.Pipelines;
using Arch.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Vortice.DXGI;

namespace ABEngine.ABERuntime
{
    public struct SharedMeshVertex
    {
        public Matrix4x4 transformMatrix;
    }

    public struct LightInfo3D
    {
        public Vector3 Position;
        public float Range;
        public Vector3 Color;
        public float Intensity;
    }

    public struct SharedMeshFragment
    {
        public LightInfo3D Light0;
        public LightInfo3D Light1;
        public LightInfo3D Light2;
        public LightInfo3D Light3;
        public Vector3 CamPos;
        public float _padding;
        public int NumDirectionalLights;
        public int NumPointLights;
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
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();
        private readonly QueryDescription pointLightQuery = new QueryDescription().WithAll<Transform, PointLight>();
        private readonly QueryDescription directionalLightQuery = new QueryDescription().WithAll<Transform, DirectionalLight>();

        DeviceBuffer fragmentUniformBuffer;

        ResourceSet sharedFragmentSet;

        SharedMeshVertex sharedVertexUniform;
        SharedMeshFragment sharedFragmentUniform;

        LightInfo3D[] lightInfos;

        public override void SetupResources(params Texture[] sampledTextures)
        {
            lightInfos = new LightInfo3D[4];

            if (fragmentUniformBuffer == null)
            {
                fragmentUniformBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(160, BufferUsage.UniformBuffer));
                sharedFragmentSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(GraphicsManager.sharedMeshUniform_FS, fragmentUniformBuffer));
            }

            sharedVertexUniform = new SharedMeshVertex();
            sharedFragmentUniform = new SharedMeshFragment();
        }

        List<(MeshRenderer, Transform)> renderOrder = new List<(MeshRenderer, Transform)>();
        public override void Update(float gameTime, float deltaTime)
        {
            if (Game.activeCam == null)
                return;

            renderOrder.Clear();

            // Fragment uniform update
            int dirLightC = 0;
            int pointLightC = 0;
            int lightC = 0;
            Game.GameWorld.Query(in directionalLightQuery, (ref DirectionalLight light, ref Transform transform) =>
            {
                if (lightC >= 4)
                    return;

                lightInfos[lightC++] = new LightInfo3D() { Color = light.color.ToVector3(), Position = light.direction, Intensity = light.Intensity };
                dirLightC++;
            });

            Game.GameWorld.Query(in pointLightQuery, (ref PointLight light, ref Transform transform) =>
            {
                if (lightC >= 4)
                    return;

                lightInfos[lightC++] = new LightInfo3D() { Color = light.color.ToVector3(), Position = transform.worldPosition };
                pointLightC++;
            });

            sharedFragmentUniform.Light0 = lightInfos[0];
            sharedFragmentUniform.Light1 = lightInfos[1];
            sharedFragmentUniform.Light2 = lightInfos[2];
            sharedFragmentUniform.Light3 = lightInfos[3];

            sharedFragmentUniform.CamPos = Game.activeCam.worldPosition;
            sharedFragmentUniform.NumDirectionalLights = dirLightC;
            sharedFragmentUniform.NumPointLights = pointLightC;

            gd.UpdateBuffer(fragmentUniformBuffer, 0, sharedFragmentUniform);

            // Mesh render order
            Game.GameWorld.Query(in meshQuery, (ref MeshRenderer mr, ref Transform transform) =>
            {
                if(mr.material.isLateRender)
                    renderOrder.Add((mr, transform));
                else
                    renderOrder.Insert(0, (mr, transform));
            });
        }

        float LinearEyeDepth(float z)
        {
            float far = 1000f;
            float near = 0.1f;
            float paramZ = (1 - far / near) / far;
            float paramW = far / near / far;
            return 1.0f / (paramZ * z + paramW);
        }

        public override void Render(int renderLayer)
        {
            if (renderLayer == 0)
                Render();
        }

        public override void Render()
        {
            // TODO Render layers
            // TODO Pipeline batching

            int ind = 0;
            foreach (var render in renderOrder)
            {
                MeshRenderer mr = render.Item1;
                Transform transform = render.Item2;
                Mesh mesh = mr.mesh;

                if (mr.material.isLateRender)
                {
                    cl.End();
                    gd.SubmitCommands(cl);
                    gd.WaitForIdle();
                    cl.Begin();
                }

                // Update vertex uniform
                sharedVertexUniform.transformMatrix = transform.worldMatrix;
                gd.UpdateBuffer(mesh.vertexUniformBuffer, 0, sharedVertexUniform);

                mr.material.pipelineAsset.BindPipeline();

                cl.SetVertexBuffer(0, mesh.vertexBuffer);
                cl.SetIndexBuffer(mesh.indexBuffer, IndexFormat.UInt16);

                cl.SetGraphicsResourceSet(0, Game.pipelineSet);
                cl.SetGraphicsResourceSet(1, mesh.vertexTransformSet);


                // Material Resource Sets
                foreach (var setKV in mr.material.bindableSets)
                {
                    cl.SetGraphicsResourceSet(setKV.Key, setKV.Value);
                }

                cl.SetGraphicsResourceSet(4, sharedFragmentSet);


                cl.DrawIndexed((uint)mesh.indices.Length);

                //cl.End();
                //gd.SubmitCommands(cl);
                //gd.WaitForIdle();
                //cl.Begin();

                //if (ind == 0)
                //{
                //    cl.End();
                //    gd.SubmitCommands(cl);
                //    gd.WaitForIdle();

                //    // Copy depth
                //    cl.Begin();
                //    cl.CopyTexture(Game.mainDepthTexture, Game.DepthTexture);
                //    cl.End();
                //    gd.SubmitCommands(cl);
                //    gd.WaitForIdle();

                //    MappedResourceView<ushort> map = gd.Map<ushort>(Game.DepthTexture, MapMode.Read);
                //    ushort[] data = new ushort[Game.DepthTexture.Width * Game.DepthTexture.Height];
                //    for (int i = 0; i < data.Length; i++)
                //    {
                //        data[i] = map[i];
                //    }

                //    gd.Unmap(Game.DepthTexture);

                //    Image<L8> image = new Image<L8>((int)Game.DepthTexture.Width, (int)Game.DepthTexture.Height);
                //    for (int y = 0; y < Game.DepthTexture.Height; y++)
                //    {
                //        for (int x = 0; x < Game.DepthTexture.Width; x++)
                //        {
                //            // Normalize the 16-bit depth data to 8-bit for visualization

                //            float sample = data[y * Game.DepthTexture.Width + x] / 65535.0f;
                //            byte pixelValue = (byte)(LinearEyeDepth(sample) * 255.0);
                //            image[x, y] = new L8(pixelValue);
                //        }
                //    }
                //    image.Save("depth_output.png");

                //    cl.Begin();
                //}

                //ind++;
            }
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            renderOrder.Clear();
            if (!reload)
            {
                sharedFragmentSet.Dispose();
                fragmentUniformBuffer.Dispose();
            }
        }

    }
}

