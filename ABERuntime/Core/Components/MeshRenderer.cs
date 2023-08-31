using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Halak;
using Veldrid;

namespace ABEngine.ABERuntime.Components
{
	public class MeshRenderer : JSerializable
	{
        public PipelineMaterial material { get; set; }
        public Mesh mesh { get; set; }

        internal DeviceBuffer vertexUniformBuffer;
        internal ResourceSet vertexTransformSet;

        public MeshRenderer()
		{
            material = GraphicsManager.GetUber3D();
            SetupResources();
        }

        public MeshRenderer(Mesh mesh) : this()
        {
            this.mesh = mesh;
        }

        public MeshRenderer(Mesh mesh, PipelineMaterial material)
        {
            this.mesh = mesh;
            this.material = material;
            SetupResources();
        }

        void SetupResources()
        {
            vertexUniformBuffer = GraphicsManager.rf.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            vertexTransformSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(GraphicsManager.sharedMeshUniform_VS, vertexUniformBuffer));
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

