using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Rendering;
using Arch.Core;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime
{
    public struct MeshMatrixData
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

    public struct DrawData
    {
        public int matrixStartID;
    }

    struct RenderEntry
    {
        public int keyIndex;
        public List<(IRenderer, Transform)> renderList;
    }

    class MaterialGroup
    {
        public Dictionary<Mesh, MeshGroup> meshGroups;
        public List<Mesh> keyList;

        public MaterialGroup()
        {
            meshGroups = new Dictionary<Mesh, MeshGroup>();
            keyList = new List<Mesh>();
        }

        public void AddMesh(Transform transform, IRenderer mr)
        {
            if(meshGroups.TryGetValue(mr.mesh, out MeshGroup mg))
            {
                mg.AddMesh(transform, mr);
            }
            else
            {
                mg = new MeshGroup();
                mg.AddMesh(transform, mr);
                meshGroups.Add(mr.mesh, mg);
                keyList.Add(mr.mesh);
            }
        }

        public Transform RemoveMesh(IRenderer mr)
        {
            if(meshGroups.TryGetValue(mr.mesh, out MeshGroup mg))
            {
                Transform mrTrans = mg.RemoveMesh(mr);
                if (mg.renderCount == 0)
                {
                    meshGroups.Remove(mr.mesh);
                    keyList.Remove(mr.mesh);
                }
                return mrTrans;
            }

            return null;
        }
    }

    class MeshGroup
    {
        public List<MeshMatrixData> staticRenders;
        public List<Transform> dynamicRenders;
        public List<Transform[]> skinnedRenders;
        public int renderCount = 0;
        public int skinnedCount = 0;
        public int noSkinCount = 0;
        public int lastMatrixStart = -1;

        Dictionary<IRenderer, (Transform transform, int listId)> mrLookup;

        public MeshGroup()
        {
            mrLookup = new();
            staticRenders = new List<MeshMatrixData>();
            dynamicRenders = new List<Transform>();
            skinnedRenders = new List<Transform[]>();
        }

        public void AddMesh(Transform transform, IRenderer mr)
        {
            if (mrLookup.ContainsKey(mr))
                return;

            int listIndex = 0;
            if (transform.isStatic)
            {
                MeshMatrixData matrixData = new MeshMatrixData();
                matrixData.transformMatrix = transform.worldMatrix;
                Matrix4x4 MV = transform.worldMatrix;
                Matrix4x4 MVInv;
                Matrix4x4.Invert(MV, out MVInv);
                matrixData.normalMatrix = Matrix4x4.Transpose(MVInv);

                listIndex = staticRenders.Count;
                staticRenders.Add(matrixData);
            }
            else if(mr is MeshRenderer)
            {
                listIndex = dynamicRenders.Count;
                dynamicRenders.Add(transform);
            }
            else
            {
                listIndex = skinnedRenders.Count;
                skinnedRenders.Add(((SkinnedMeshRenderer)mr).bones);
            }

            mrLookup.Add(mr, (transform, listIndex));

            noSkinCount = staticRenders.Count + dynamicRenders.Count;
            skinnedCount = skinnedRenders.Count;
            renderCount++;
        }

        public Transform RemoveMesh(IRenderer mr)
        {
            Transform transform = null;
            if(mrLookup.TryGetValue(mr, out var transformData))
            {
                transform = transformData.transform;
                if (transformData.transform.isStatic)
                    staticRenders.RemoveAt(transformData.listId);
                else if(mr is MeshRenderer)
                    dynamicRenders.RemoveAt(transformData.listId);
                else
                    skinnedRenders.RemoveAt(transformData.listId);

                noSkinCount = staticRenders.Count + dynamicRenders.Count;
                skinnedCount = skinnedRenders.Count;
                renderCount--;
            }

            if (transform != null)
                mrLookup.Remove(mr);

            return transform;
        }
    }

    public class MeshRenderSystem : RenderSystem
    {
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();
        private readonly QueryDescription pointLightQuery = new QueryDescription().WithAll<Transform, PointLight>();
        private readonly QueryDescription directionalLightQuery = new QueryDescription().WithAll<Transform, DirectionalLight>();

        Buffer fragmentUniformBuffer;

        BindGroup sharedFrameSet;

        internal BindGroup drawDataset;

        SharedMeshFragment sharedFragmentUniform;

        LightInfo3D[] lightInfos;

        internal Buffer matrixStorageBuffer;
        internal Buffer drawDataBuffer;
        internal const int maxMeshCount = 100000;
        internal int bufferStep = 0;

        public override void SetupResources(params TextureView[] sampledTextures)
        {
            lightInfos = new LightInfo3D[4];

            if (fragmentUniformBuffer == null)
            {
                fragmentUniformBuffer = wgil.CreateBuffer(160, BufferUsages.UNIFORM | BufferUsages.COPY_DST).SetManualDispose(true);
                matrixStorageBuffer = wgil.CreateBuffer(64 * 2 * maxMeshCount, BufferUsages.STORAGE | BufferUsages.COPY_DST).SetManualDispose(true);

                var sharedFrameData = new BindGroupDescriptor()
                {
                    BindGroupLayout = GraphicsManager.sharedMeshFrameData,
                    Entries = new BindResource[]
                    {
                        Game.pipelineBuffer,
                        fragmentUniformBuffer,
                        matrixStorageBuffer
                    }
                };

                sharedFrameSet = wgil.CreateBindGroup(ref sharedFrameData).SetManualDispose(true);

                bufferStep = (int)wgil.GetMinUniformOffset();
                drawDataBuffer = wgil.CreateBuffer(bufferStep * maxMeshCount, BufferUsages.UNIFORM | BufferUsages.COPY_DST).SetManualDispose(true);
                drawDataBuffer.DynamicEntrySize = 4;

                var drawSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = GraphicsManager.sharedMeshUniform_VS,
                    Entries = new BindResource[]
                    {
                        drawDataBuffer
                    }
                };
                drawDataset = Game.wgil.CreateBindGroup(ref drawSetDesc).SetManualDispose(true);
            }

            sharedFragmentUniform = new SharedMeshFragment();
        }

        protected override void StartScene()
        {
            Game.GameWorld.Query(in meshQuery, (ref MeshRenderer mr, ref Transform transform) =>
            {
                AddMesh(transform, mr);
            });
        }

        public Vector3 RotateByQuaternion(Vector3 vec, Quaternion rotation)
        {
            Quaternion vecQuat = new Quaternion(vec.X, vec.Y, vec.Z, 0);
            Quaternion conjugate = Quaternion.Conjugate(rotation);
            Quaternion rotatedQuat = rotation * vecQuat * conjugate;
            return new Vector3(rotatedQuat.X, rotatedQuat.Y, rotatedQuat.Z);
        }


        internal Dictionary<PipelineMaterial, MaterialGroup> opaqueDict = new();
        internal List<PipelineMaterial> opaqueKeys = new();
        internal DrawData[] groupDrawDatas = new DrawData[100];

        SortedDictionary<int, List<(MeshRenderer, Transform)>> opaqueRenderOrder = new SortedDictionary<int, List<(MeshRenderer, Transform)>>();
        internal SortedDictionary<int, List<(MeshRenderer, Transform)>> transparentRenderOrder = new SortedDictionary<int, List<(MeshRenderer, Transform)>>();
        internal SortedDictionary<int, List<(MeshRenderer, Transform)>> lateRenderOrder = new SortedDictionary<int, List<(MeshRenderer, Transform)>>();

        public void AddMesh(Transform transform, MeshRenderer mr)
        {
            if (!base.started)
                return;

            if (mr.material.pipelineAsset.renderType == RenderType.Transparent)
                return;

            var key = mr.material;
            if (opaqueDict.TryGetValue(key, out MaterialGroup mg))
            {
                mg.AddMesh(transform, mr);
            }
            else
            {
                mg = new MaterialGroup();
                mg.AddMesh(transform, mr);
                opaqueDict.Add(key, mg);
                opaqueKeys.Add(key);
            }
        }

        public Transform RemoveMesh(MeshRenderer mr)
        {
            if (opaqueDict.TryGetValue(mr.material, out MaterialGroup mg))
            {
                Transform mrTrans = mg.RemoveMesh(mr);
                if (mg.keyList.Count == 0)
                {
                    opaqueDict.Remove(mr.material);
                    opaqueKeys.Remove(mr.material);
                }
                return mrTrans;
            }

            return null;
        }

        public override void Update(float gameTime, float deltaTime)
        {
            if (Game.activeCamTrans == null)
                return;

            //opaqueRenderOrder.Clear();
            //transparentRenderOrder.Clear();
            //lateRenderOrder.Clear();
            //opaqueDict.Clear();
            //opaqueKeys.Clear();

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

            sharedFragmentUniform.CamPos = Game.activeCamTrans.worldPosition;
            sharedFragmentUniform.NumDirectionalLights = dirLightC;
            sharedFragmentUniform.NumPointLights = pointLightC;

            wgil.WriteBuffer(fragmentUniformBuffer, sharedFragmentUniform);
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

            // Bind all matrices
            pass.SetBindGroup(0, sharedFrameSet);

            int groupID = 0;
            for (int matGroupID = 0; matGroupID < opaqueKeys.Count; matGroupID++)
            {
                var material = Game.meshRenderSystem.opaqueKeys[matGroupID];
                var matGroup = Game.meshRenderSystem.opaqueDict[material];

                pass.SetPipeline(material.pipelineAsset.pipeline);

                foreach (var setKV in material.bindableSets)
                {
                    pass.SetBindGroup(setKV.Key, setKV.Value);
                }

                foreach (var mesh in matGroup.keyList)
                {
                    var meshGroup = matGroup.meshGroups[mesh];

                    pass.SetBindGroup(1, (uint)(bufferStep * groupID), drawDataset);

                    pass.SetVertexBuffer(0, mesh.vertexBuffer);
                    pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

                    pass.DrawIndexed(mesh.Indices.Length, meshGroup.noSkinCount);

                    groupID++;
                }
            }


            //for (int i = 0; i < opaqueKeys.Count; i++)
            //{
            //    var key = opaqueKeys[i];
            //    wgil.WriteBuffer(drawDataBuffer, groupDrawDatas[i], 0, 4);

            //    PipelineMaterial material = key.material;
            //    pass.SetPipeline(material.pipelineAsset.pipeline);

                

            //}


            //foreach (var renderPair in opaqueRenderOrder)
            //{
            //    foreach (var render in renderPair.Value)
            //    {
            //        MeshRenderer mr = render.Item1;
            //        Mesh mesh = mr.mesh;

            //        int renderID = mr.renderID;
            //        // Opaque transform buffers updated in depth pre-pass


            //        if (!set)
            //        {
            //            pass.SetPipeline(mr.material.pipelineAsset.pipeline);

            //            pass.SetVertexBuffer(0, mesh.vertexBuffer);
            //            pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

            //            set = true;
            //        }

            //        pass.SetBindGroup(1, (uint)(bufferStep * renderID), transformSet);

            //        // Material Resource Sets
            //        foreach (var setKV in mr.material.bindableSets)
            //        {
            //            pass.SetBindGroup(setKV.Key, setKV.Value);
            //        }

            //        pass.DrawIndexed(mesh.Indices.Length);
            //    }
            //}


            //foreach (var renderPair in transparentRenderOrder)
            //{
            //    foreach (var render in renderPair.Value)
            //    {
            //        MeshRenderer mr = render.Item1;
            //        Transform transform = render.Item2;
            //        Mesh mesh = mr.mesh;

            //        int renderID = mr.renderID;
            //        // Update transform buffer

            //        sharedVertexUniform.transformMatrix = transform.worldMatrix;
            //        Matrix4x4 MV = transform.worldMatrix;
            //        Matrix4x4 MVInv;
            //        Matrix4x4.Invert(MV, out MVInv);
            //        sharedVertexUniform.normalMatrix = Matrix4x4.Transpose(MVInv);
            //        //wgil.WriteBuffer(mr.vertexUniformBuffer, sharedVertexUniform);
                

            //        pass.SetPipeline(mr.material.pipelineAsset.pipeline);

            //        pass.SetVertexBuffer(0, mesh.vertexBuffer);
            //        pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

            //        pass.SetBindGroup(1, (uint)(bufferStep * renderID), transformSet);

            //        // Material Resource Sets
            //        foreach (var setKV in mr.material.bindableSets)
            //        {
            //            pass.SetBindGroup(setKV.Key, setKV.Value);
            //        }

            //        pass.DrawIndexed(mesh.Indices.Length);
            //    }
            //}
        }

        public void RenderPP(RenderPass pass)
        {
            //foreach (var renderPair in lateRenderOrder)
            //{
            //    foreach (var render in renderPair.Value)
            //    {
            //        MeshRenderer mr = render.Item1;
            //        Transform transform = render.Item2;
            //        Mesh mesh = mr.mesh;

            //        // Update vertex uniform
            //        sharedVertexUniform.transformMatrix = transform.worldMatrix;
            //        Matrix4x4 MV = transform.worldMatrix;
            //        Matrix4x4 MVInv;
            //        Matrix4x4.Invert(MV, out MVInv);
            //        sharedVertexUniform.normalMatrix = Matrix4x4.Transpose(MVInv);

            //        //wgil.WriteBuffer(mr.vertexUniformBuffer, sharedVertexUniform);

            //        mr.material.pipelineAsset.BindPipeline(pass, 1, sharedFragmentSet);

            //        pass.SetVertexBuffer(0, mesh.vertexBuffer);
            //        pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);

            //        //pass.SetBindGroup(1, mr.vertexTransformSet);

            //        // Material Resource Sets
            //        foreach (var setKV in mr.material.bindableSets)
            //        {
            //            pass.SetBindGroup(setKV.Key, setKV.Value);
            //        }

            //        pass.DrawIndexed(mesh.Indices.Length);
            //    }
            //}
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            opaqueRenderOrder.Clear();
            transparentRenderOrder.Clear();
            lateRenderOrder.Clear();

            if (newScene)
            {
                opaqueKeys.Clear();
                opaqueDict.Clear();
            }

            if (!reload)
            {
                sharedFrameSet.Dispose();
                fragmentUniformBuffer.Dispose();

                drawDataBuffer.Dispose();
                drawDataset.Dispose();

                matrixStorageBuffer.Dispose();
            }
        }

    }
}

