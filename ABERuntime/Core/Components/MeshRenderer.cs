using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Halak;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Components
{
	public class MeshRenderer : JSerializable, IRenderer
	{
        private PipelineMaterial _material;
        private Mesh _mesh;

        public PipelineMaterial material
        {
            get { return _material; }
            set
            {
                if (_material == value || value == null)
                    return;

                Transform transform = null;
                if(_material != null)
                    transform = Game.meshRenderSystem.RemoveMesh(this);
                _material = value;
                if(transform != null)
                    Game.meshRenderSystem.AddMesh(transform, this);
            }
        }


        public Mesh mesh
        {
            get { return _mesh; }
            set
            {
                if (_mesh == value || value == null)
                    return;

                Transform transform = null;
                if (_mesh != null)
                    transform = Game.meshRenderSystem.RemoveMesh(this);
                _mesh = value;
                if (transform != null)
                    Game.meshRenderSystem.AddMesh(transform, this);
            }
        }

        internal int renderID { get; set; }

        public MeshRenderer()
		{
            material = GraphicsManager.GetUber3D();
        }

        public MeshRenderer(Mesh mesh) : this()
        {
            this.mesh = mesh;
        }

        public MeshRenderer(Mesh mesh, PipelineMaterial material)
        {
            this.mesh = mesh;
            this.material = material;
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

