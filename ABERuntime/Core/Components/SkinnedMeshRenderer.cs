using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Rendering;
using Friflo.Engine.ECS;
using Halak;

namespace ABEngine.ABERuntime.Components
{
    public struct SkinnedMeshRenderer : JSerializable, IRenderer, IComponent
    {
        public PipelineMaterial Material { get; set; }
        public Mesh Mesh { get; set; }

        public Entity[] Bones = null;

        public SkinnedMeshRenderer()
        {
            Mesh = CubeModel.GetCubeMesh();
            Material = Graphics.GetUber3D();
        }

        public SkinnedMeshRenderer(Mesh mesh) : this()
        {
            this.Mesh = mesh;
        }

        public SkinnedMeshRenderer(Mesh mesh, PipelineMaterial material)
        {
            this.Mesh = mesh;

            bool skinSupport = material.pipelineAsset.HasFeature(MaterialFeature.Skinning);
            if(!skinSupport)
            {
                int skinDefineIndex = material.pipelineAsset.GetDefineIndex("HAS_SKIN");
                if (skinDefineIndex > -1)
                {
                    int defineHash = material.pipelineAsset.defineHash;
                    defineHash |= (1 << skinDefineIndex);
                    var skinVariant = material.pipelineAsset.GetPipelineVariant(defineHash);
                    if (skinVariant != null)
                        material.ChangePipeline(skinVariant);
                }
            }
            this.Material = material;
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

