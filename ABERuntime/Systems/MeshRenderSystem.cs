using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Rendering;
using Arch.Core;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime
{
    public struct SharedMeshVertex
    {
        public Matrix4x4 transformMatrix;
        public Matrix4x4 normalMatrix;
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

    public class MeshRenderSystem : RenderSystem
    {
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();
        private readonly QueryDescription pointLightQuery = new QueryDescription().WithAll<Transform, PointLight>();
        private readonly QueryDescription directionalLightQuery = new QueryDescription().WithAll<Transform, DirectionalLight>();

        Buffer fragmentUniformBuffer;

        BindGroup sharedFragmentSet;

        SharedMeshVertex sharedVertexUniform;
        SharedMeshFragment sharedFragmentUniform;

        LightInfo3D[] lightInfos;

        public override void SetupResources(params TextureView[] sampledTextures)
        {
            lightInfos = new LightInfo3D[4];

            if (fragmentUniformBuffer == null)
            {
                fragmentUniformBuffer = wgil.CreateBuffer(160, BufferUsages.UNIFORM | BufferUsages.COPY_DST).SetManualDispose(true);

                var sharedFragmentDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = GraphicsManager.sharedPipelineLightLayout,
                    Entries = new BindResource[]
                    {
                        Game.pipelineBuffer,
                        fragmentUniformBuffer
                    }
                };

                sharedFragmentSet = wgil.CreateBindGroup(ref sharedFragmentDesc).SetManualDispose(true);
            }

            sharedVertexUniform = new SharedMeshVertex();
            sharedFragmentUniform = new SharedMeshFragment();
        }

        public Vector3 RotateByQuaternion(Vector3 vec, Quaternion rotation)
        {
            Quaternion vecQuat = new Quaternion(vec.X, vec.Y, vec.Z, 0);
            Quaternion conjugate = Quaternion.Conjugate(rotation);
            Quaternion rotatedQuat = rotation * vecQuat * conjugate;
            return new Vector3(rotatedQuat.X, rotatedQuat.Y, rotatedQuat.Z);
        }


        //List<(MeshRenderer, Transform)> renderOrder = new List<(MeshRenderer, Transform)>();
        //List<(MeshRenderer, Transform)> lateRenderOrder = new List<(MeshRenderer, Transform)>();

        SortedDictionary<int, List<(MeshRenderer, Transform)>> renderOrder = new SortedDictionary<int, List<(MeshRenderer, Transform)>>();
        SortedDictionary<int, List<(MeshRenderer, Transform)>> lateRenderOrder = new SortedDictionary<int, List<(MeshRenderer, Transform)>>();

        public override void Update(float gameTime, float deltaTime)
        {
            if (Game.activeCam == null)
                return;

            renderOrder.Clear();
            lateRenderOrder.Clear();

            // Fragment uniform update
            int dirLightC = 0;
            int pointLightC = 0;
            int lightC = 0;
            Game.GameWorld.Query(in directionalLightQuery, (ref DirectionalLight light, ref Transform transform) =>
            {
                if (lightC >= 4)
                    return;

                Vector3 dir = RotateByQuaternion(-Vector3.UnitZ, transform.worldRotation);
                light.direction = dir;

                lightInfos[lightC++] = new LightInfo3D() { Color = light.color.ToVector3(), Position = dir, Intensity = light.Intensity };
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

            wgil.WriteBuffer(fragmentUniformBuffer, sharedFragmentUniform);

            // Mesh render order
            Game.GameWorld.Query(in meshQuery, (ref MeshRenderer mr, ref Transform transform) =>
            {
                SortedDictionary<int, List<(MeshRenderer, Transform)>> dict = renderOrder;
                if (mr.material.renderOrder >= (int)RenderOrder.PostProcess)
                    dict = lateRenderOrder;

                if (dict.TryGetValue(mr.material.renderOrder, out var pairList))
                    pairList.Add((mr, transform));
                else
                    dict.Add(mr.material.renderOrder, new List<(MeshRenderer, Transform)>() { (mr, transform) });
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

        public override void Render(RenderPass pass, int renderLayer)
        {
            if (renderLayer == 0)
                Render(pass);
        }

        //public void LateRender(int renderLayer)
        //{
        //    if (renderLayer == 0 && lateRenderOrder.Count > 0)
        //        LateRender();
        //}

        public override void Render(RenderPass pass)
        {
            // TODO Render layers
            // TODO Pipeline batching

            foreach (var renderPair in renderOrder)
            {
                foreach (var render in renderPair.Value)
                {
                    MeshRenderer mr = render.Item1;
                    Transform transform = render.Item2;
                    Mesh mesh = mr.mesh;

                    // Update vertex uniform
                    sharedVertexUniform.transformMatrix = transform.worldMatrix;
                    Matrix4x4 MV = transform.worldMatrix;
                    Matrix4x4 MVInv;
                    Matrix4x4.Invert(MV, out MVInv);
                    sharedVertexUniform.normalMatrix = Matrix4x4.Transpose(MVInv);

                    wgil.WriteBuffer(mr.vertexUniformBuffer, sharedVertexUniform);

                    mr.material.pipelineAsset.BindPipeline(pass, 1, sharedFragmentSet);

                    pass.SetVertexBuffer(0, mesh.vertexBuffer);
                    pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                    pass.SetBindGroup(1, mr.vertexTransformSet);

                    // Material Resource Sets
                    foreach (var setKV in mr.material.bindableSets)
                    {
                        pass.SetBindGroup(setKV.Key, setKV.Value);
                    }

                    pass.DrawIndexed(mesh.indices.Length);
                }
            }
        }

        public void RenderPP(RenderPass pass)
        {
            foreach (var renderPair in lateRenderOrder)
            {
                foreach (var render in renderPair.Value)
                {
                    MeshRenderer mr = render.Item1;
                    Transform transform = render.Item2;
                    Mesh mesh = mr.mesh;

                    // Update vertex uniform
                    sharedVertexUniform.transformMatrix = transform.worldMatrix;
                    Matrix4x4 MV = transform.worldMatrix;
                    Matrix4x4 MVInv;
                    Matrix4x4.Invert(MV, out MVInv);
                    sharedVertexUniform.normalMatrix = Matrix4x4.Transpose(MVInv);

                    wgil.WriteBuffer(mr.vertexUniformBuffer, sharedVertexUniform);

                    mr.material.pipelineAsset.BindPipeline(pass, 1, sharedFragmentSet);

                    pass.SetVertexBuffer(0, mesh.vertexBuffer);
                    pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                    pass.SetBindGroup(1, mr.vertexTransformSet);

                    // Material Resource Sets
                    foreach (var setKV in mr.material.bindableSets)
                    {
                        pass.SetBindGroup(setKV.Key, setKV.Value);
                    }

                    pass.DrawIndexed(mesh.indices.Length);
                }
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

