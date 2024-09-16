using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Rendering;
using Friflo.Engine.ECS;
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
        public int boneStartID;
        public int meshBoneCount;
    }

    class MaterialGroup
    {
        public PipelineMaterial material;
        public Dictionary<Mesh, MeshBatch> meshBatches;
        
        public MaterialGroup(PipelineMaterial material)
        {
            this.material = material;
            meshBatches = new();
        }

        public void AddMesh(ref MeshRenderer mr)
        {
            if (!meshBatches.TryGetValue(mr.mesh, out MeshBatch batch))
            {
                batch = new MeshBatch(mr.mesh, mr.material);
                meshBatches.Add(mr.mesh, batch);
            }
                
            mr.batch = batch;
            batch.AddMesh();
        }
    }

    public class MeshRenderSystem : RenderSystem
    {
        /*private readonly QueryDescription pointLightQuery = new QueryDescription().WithAll<Transform, PointLight>();
        private readonly QueryDescription directionalLightQuery = new QueryDescription().WithAll<Transform, DirectionalLight>();*/

        Buffer fragmentUniformBuffer;

        BindGroup sharedFrameSet;
        internal BindGroup drawDataset;

        SharedMeshFragment sharedFragmentUniform;

        LightInfo3D[] lightInfos;

        internal Buffer matrixStorageBuffer;
        internal Buffer boneStorageBuffer;
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
                boneStorageBuffer = wgil.CreateBuffer(64 * 2000, BufferUsages.STORAGE | BufferUsages.COPY_DST).SetManualDispose(true);

                var sharedFrameData = new BindGroupDescriptor()
                {
                    BindGroupLayout = Graphics.sharedMeshFrameData,
                    Entries = new BindResource[]
                    {
                        Game.pipelineBuffer,
                        fragmentUniformBuffer,
                        matrixStorageBuffer,
                        boneStorageBuffer
                    }
                };

                sharedFrameSet = wgil.CreateBindGroup(ref sharedFrameData).SetManualDispose(true);

                bufferStep = (int)wgil.GetMinUniformOffset();
                drawDataBuffer = wgil.CreateBuffer(bufferStep * 100, BufferUsages.UNIFORM | BufferUsages.COPY_DST).SetManualDispose(true);
                drawDataBuffer.DynamicEntrySize = 12;

                var drawSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = Graphics.sharedMeshUniform_VS,
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
            meshRenderQuery.ForEachEntity((ref MeshRenderer mr, ref WorldTransform Transform, Entity entity) => {
                if (!opaqueRenders.TryGetValue(mr.material, out MaterialGroup group))
                {
                    group = new MaterialGroup(mr.material);
                    opaqueRenders.Add(mr.material, group);
                }
                        
                group.AddMesh(ref mr);
            });
            
            /*QueryDescription mrQuery = new QueryDescription().WithAll<Transform, MeshRenderer>();
            QueryDescription smrQuery = new QueryDescription().WithAll<Transform, SkinnedMeshRenderer>();

            Game.GameWorld.Query(in mrQuery, (ref MeshRenderer mr, ref Transform transform) =>
            {
                AddMesh(transform, mr);
            });

            Game.GameWorld.Query(in smrQuery, (ref SkinnedMeshRenderer mr, ref Transform transform) =>
            {
                AddMesh(transform, mr);
            });*/
        }
        
        internal DrawData[] groupDrawDatas = new DrawData[100];
        internal SortedDictionary<PipelineMaterial, MaterialGroup> opaqueRenders = new(new MaterialKeyComparer());
        
        private readonly ArchetypeQuery<MeshRenderer, WorldTransform> meshRenderQuery = Game.GameWorld.Query<MeshRenderer, WorldTransform>();
        private readonly ArchetypeQuery<DirectionalLight, WorldTransform> lightQuery = Game.GameWorld.Query<DirectionalLight, WorldTransform>();
        
        public override void Update(float gameTime, float deltaTime)
        {
            // Meshes
            foreach (var (meshRenderers, transforms, entities) in meshRenderQuery.Chunks)
            {
                for (int n = 0; n < entities.Length; n++)
                {
                    ref MeshRenderer mr = ref meshRenderers[n];
                    if (mr.batch == null)
                    {
                        if (!opaqueRenders.TryGetValue(mr.material, out MaterialGroup group))
                        {
                            group = new MaterialGroup(mr.material);
                            opaqueRenders.Add(mr.material, group);
                        }
                        
                        group.AddMesh(ref mr);
                    }
                    
                    if (mr.bvhGroup.IsCulled)
                    {
                        continue;
                    }
 
                    ref WorldTransform transform = ref transforms[n];
                    mr.batch.UpdateMesh(transform.matrix);
                }
            }

            int start = 0;
            foreach (var matGroup in opaqueRenders.Values)
            {
                foreach (var batch in matGroup.meshBatches.Values)
                {
                    int renderCount = batch.UpdateBatch(start);
                    start += renderCount;
                }
            }
            
            /*foreach (var batch in opaqueBatches)
            {
                int renderCount = batch.UpdateBatch(start);
                groupDrawDatas[batch.batchID].matrixStartID = start;
                start += renderCount;
            }*/
            
            // Fragment
            if (Game.activeCamera.IsNull)
                return;

            // Light uniform update
            int dirLightC = 0;
            int pointLightC = 0;
            int lightC = 0;
            lightQuery.ForEachEntity((ref DirectionalLight light, ref WorldTransform transform, Entity entity) =>
            {
                transform.Position = Vector3.Zero;
                light.direction = Vector3.TransformNormal(-Vector3.UnitZ, transform.matrix);
                lightInfos[lightC++] = new LightInfo3D { Color = light.color.ToVector3(), Position = light.direction, Intensity = light.intensity };
                dirLightC++;
            });
            
            /*
            Game.GameWorld.Query(in pointLightQuery, (ref PointLight light, ref Transform transform) =>
            {
                if (lightC >= 4)
                    return;

                lightInfos[lightC++] = new LightInfo3D() { Color = light.color.ToVector3(), Position = transform.worldPosition };
                pointLightC++;
            });*/

            sharedFragmentUniform.Light0 = lightInfos[0];
            sharedFragmentUniform.Light1 = lightInfos[1];
            sharedFragmentUniform.Light2 = lightInfos[2];
            sharedFragmentUniform.Light3 = lightInfos[3];

            sharedFragmentUniform.CamPos = Game.activeCamera.WorldTransform.Position;
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
            
            foreach (var matGroup in opaqueRenders.Values)
            {
                if (matGroup.meshBatches.Count == 0)
                    continue;

                PipelineMaterial material = matGroup.material;
                foreach (var setKV in material.bindableSets)
                {
                    pass.SetBindGroup(setKV.Key, setKV.Value);
                }
                
                foreach (var pipelineAsset in material.pipelinePasses)
                {
                    pass.SetPipeline(pipelineAsset.pipeline);
                    
                    foreach (var batch in matGroup.meshBatches.Values)
                    {
                        pass.SetBindGroup(1, (uint)(bufferStep * batch.batchID), drawDataset);
                        batch.Render(pass);
                    }
                }
            }
        }

        public void RenderPP(RenderPass pass)
        {
            
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
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
    
    class MaterialKeyComparer : IComparer<PipelineMaterial>
    {
        public int Compare(PipelineMaterial x, PipelineMaterial y)
        {
            return x.renderOrder.CompareTo(y.renderOrder);
        }
    }
}

