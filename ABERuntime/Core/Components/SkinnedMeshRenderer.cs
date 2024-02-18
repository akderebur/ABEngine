using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Halak;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Components
{
    public class SkinnedMeshRenderer : JSerializable
    {
        public PipelineMaterial material { get; set; }
        public Mesh mesh { get; set; }

        internal Buffer vertexUniformBuffer;
        internal Buffer vertexSkinBuffer;
        internal BindGroup vertexTransformSet;

        public SkinnedMeshRenderer()
        {
            material = GraphicsManager.GetUber3D();
            SetupResources();
        }

        public SkinnedMeshRenderer(Mesh mesh) : this()
        {
            this.mesh = mesh;
        }

        public SkinnedMeshRenderer(Mesh mesh, PipelineMaterial material)
        {
            this.mesh = mesh;
            this.material = material;
            SetupResources();
        }

        void SetupResources()
        {
            vertexUniformBuffer = Game.wgil.CreateBuffer(128, BufferUsages.UNIFORM | BufferUsages.COPY_DST);
            vertexSkinBuffer = Game.wgil.CreateBuffer(128 * 64 + 4, BufferUsages.UNIFORM | BufferUsages.COPY_DST);

            var vertexTransSkinDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = GraphicsManager.sharedSkinnedMeshUniform_VS,
                Entries = new BindResource[]
                {
                    vertexUniformBuffer,
                    vertexSkinBuffer
                }
            };
            vertexTransformSet = Game.wgil.CreateBindGroup(ref vertexTransSkinDesc);
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Mesh", AssetCache.GetAssetSceneIndex(this.mesh.fPathHash));
            jObj.Put("Material", AssetCache.GetAssetSceneIndex(this.material.fPathHash));

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);

            int meshSceneIndex = data["Mesh"];
            int matSceneIndex = data["Material"];

            var mesh = AssetCache.GetAssetFromSceneIndex(meshSceneIndex) as Mesh;
            if (mesh == null)
                mesh = Rendering.CubeModel.GetCubeMesh();
            var material = AssetCache.GetAssetFromSceneIndex(matSceneIndex) as PipelineMaterial;
            if (material == null)
                material = GraphicsManager.GetUber3D();

            this.mesh = mesh;
            this.material = material;
        }

        public void SetReferences()
        {

        }

        public JSerializable GetCopy()
        {
            MeshRenderer copyMR = new MeshRenderer()
            {
                material = this.material,
                mesh = this.mesh
            };

            return copyMR;
        }
    }
}

