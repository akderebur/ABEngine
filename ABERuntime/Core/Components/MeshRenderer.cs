using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Systems;
using Friflo.Engine.ECS;
using Halak;

namespace ABEngine.ABERuntime.Components
{
	public struct MeshRenderer : JSerializable, IRenderer, IComponent
    {
        public BVHGroup bvhGroup;
        public MeshBatch batch;
        
        private PipelineMaterial _material;
        private Mesh _mesh;

        public PipelineMaterial Material
        {
            get => _material;
            set
            {
                if (_material == value || value == null)
                    return;
            }
        }
        
        public Mesh Mesh
        {
            get => _mesh;
            set
            {
                if (_mesh == value || value == null)
                    return;

            }
        }
        
        public MeshRenderer()
        {
            batch = null;
            bvhGroup = BVHSystem.GetDefaultBVHGroup();
            _mesh = CubeModel.GetCubeMesh();  
            _material = Graphics.GetUber3D();
        }

        public MeshRenderer(Mesh mesh) : this()
        {
            _mesh = mesh;
        }

        public MeshRenderer(Mesh mesh, PipelineMaterial material)
        {
            batch = null;
            bvhGroup = BVHSystem.GetDefaultBVHGroup();
            _mesh = mesh;
            _material = material;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Mesh", Assets.GetAssetSceneIndex(this.Mesh.fPathHash));
            jObj.Put("Material", Assets.GetAssetSceneIndex(this.Material.fPathHash));

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);

            int meshSceneIndex = data["Mesh"];
            int matSceneIndex = data["Material"];

            var mesh = Assets.GetAssetFromSceneIndex(meshSceneIndex) as Mesh;
            if (mesh == null)
                mesh = Rendering.CubeModel.GetCubeMesh();
            var material = Assets.GetAssetFromSceneIndex(matSceneIndex) as PipelineMaterial;
            if (material == null)
                material = Graphics.GetUber3D();

            this.Mesh = mesh;
            this.Material = material;
        }

        public void SetReferences()
        {
            
        }

        public JSerializable GetCopy()
        {
            MeshRenderer copyMR = new MeshRenderer()
            {
                Material = this.Material,
                Mesh = this.Mesh
            };

            return copyMR;
        }
    }
}

