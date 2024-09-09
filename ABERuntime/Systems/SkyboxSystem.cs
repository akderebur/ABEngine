using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Pipelines;
using ABEngine.ABERuntime.Rendering;
using WGIL;

namespace ABEngine.ABERuntime.Systems;

public class SkyboxSystem : RenderSystem
{
   // private readonly QueryDescription skyQuery = new QueryDescription().WithAll<Skybox>();
    
    private Buffer _uniformBuffer;
    private BindGroup _skyboxEngineGroup;
    
    private Skybox _skybox;
    private Transform _skyboxTransform;
    private PipelineMaterial _skyMaterial;
    private Mesh _cubeMesh;
    
    protected override void StartScene()
    {
        /*Game.GameWorld.Query(in skyQuery, (ref Skybox skybox, ref Transform skyboxTrans) =>
        {
            _skybox = skybox;
            _skyboxTransform = skyboxTrans;
        });*/

        if (_skybox == null) return;

        _cubeMesh = CubeModel.GetCubeMesh();
        _skyMaterial = Graphics.GetSkyboxMaterial();
        _uniformBuffer = Game.wgil.CreateBuffer(64, BufferUsages.UNIFORM | BufferUsages.COPY_DST);
        var bindDesc = new BindGroupDescriptor()
        {
            BindGroupLayout = SkyboxPipeline.SkyboxBindLayout,
            Entries = new BindResource[]
            {
                _uniformBuffer,
                _skybox.cubemap.GetView(),
                _skybox.cubemap.textureSampler
            }
        };
        _skyboxEngineGroup = Game.wgil.CreateBindGroup(ref bindDesc);
    }

    public override void Update(float gameTime, float deltaTime)
    {
        /*_skyboxTransform.localPosition = Game.activeCamTrans.worldPosition;
        Game.wgil.WriteBuffer(_uniformBuffer, _skyboxTransform.worldMatrix);*/
    }

    public override void Render(RenderPass pass)
    {
        /*if (Game.activeCamera.cameraProjection == CameraProjection.Perspective)
        {
            _skyMaterial.pipelineAsset.BindPipeline(pass);
            pass.SetBindGroup(1, _skyboxEngineGroup);
            pass.SetVertexBuffer(0, _cubeMesh.vertexBuffer);
            pass.SetIndexBuffer(_cubeMesh.indexBuffer, IndexFormat.Uint16);
            pass.DrawIndexed(CubeModel.IndexCount);
        }*/
    }
}