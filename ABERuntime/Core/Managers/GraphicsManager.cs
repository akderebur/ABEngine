using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ABEngine.ABERuntime.Pipelines;
using SharpGen.Runtime;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

namespace ABEngine.ABERuntime
{

    enum UniformElement
    {
        Float1,
        Float2,
        Float3,
        Float4,
        Matrix4x4
    }

    public static class GraphicsManager
    {
        // Settings
        public static TextureSampleCount msaaSampleCount { get; set; }

        static bool _render2DOnly;
        public static bool render2DOnly
        {
            get { return _render2DOnly; }
            set
            {
                _render2DOnly = value;
                Game.Instance.Toggle3D(!value);
            }
        }

        public static GraphicsDevice gd;
        public static CommandList cl;
        public static DisposeCollectorResourceFactory rf;

        public static Pipeline SpritePipeline;
        public static Pipeline EditorSpritePipeline;
        public static Pipeline FullScreenPipeline;
        public static Pipeline CompositePipeline;

        public static List<Sampler> AllSamplers;

        public static Sampler pointSamplerClamp;
        public static Sampler linearSamplerWrap;
        public static Sampler linearSampleClamp;

        public static Tuple<ResourceLayout, ResourceLayout> SpriteLayouts;

        public static VertexLayoutDescription sharedVertexLayout;
        public static VertexLayoutDescription sharedMeshVertexLayout;

        public static ResourceLayout sharedPipelineLayout;
        public static ResourceLayout sharedTextureLayout;
        public static ResourceLayout sharedSpriteNormalLayout;
        public static ResourceLayout sharedMeshUniform_VS;
        public static ResourceLayout sharedMeshUniform_FS;

        public static TextureView defaultTexView;

        public static List<PipelineMaterial> pipelineMaterials = new List<PipelineMaterial>();
        //private static List<Pipeline> pipelines = new List<Pipeline>();
        internal static Dictionary<string, PipelineAsset> pipelineAssets = new Dictionary<string, PipelineAsset>();

        public static DeviceBuffer fullScreenVB;
        public static DeviceBuffer fullScreenIB;

        static PipelineMaterial GetFirstMatByName(string name)
        {
            return pipelineMaterials.FirstOrDefault(pm => pm.name.Equals(name));
        }


        public static PipelineMaterial GetUberMaterial()
        {
            return GetFirstMatByName("UberStandard");
        }

        public static PipelineMaterial GetUberAdditiveMaterial()
        {
            return GetFirstMatByName("UberAdditive");
        }

        public static PipelineMaterial GetUber3D()
        {
            return GetFirstMatByName("Uber3D");
        }

        public static int GetPipelineCount()
        {
            return pipelineAssets.Count;
        }

        public static int GetPipelineMaterialCount()
        {
            return pipelineMaterials.Count;
        }

        public static PipelineAsset GetPipelineAssetByName(string name)
        {
            return pipelineAssets[name];
        }

        internal static void AddPipelineAsset(PipelineAsset pipelineAsset)
        {
            pipelineAssets.Add(pipelineAsset.ToString(), pipelineAsset);
        }

        internal static void AddPipelineMaterial(PipelineMaterial pipelineMaterial)
        {
            pipelineMaterials.Add(pipelineMaterial);
        }

        static string layersPath;
        internal static List<string> renderLayers;

        internal static void InitSettings()
        {
            layersPath = Game.AssetPath + "RenderLayers.abconfig";
            //if (!File.Exists(layersPath))
            //{
            //    File.WriteAllText(layersPath, "Default");
            //}

            renderLayers = new List<string>();
            renderLayers.Add("Default");
            //foreach (var line in File.ReadAllLines(layersPath))
            //{
            //    string layerName = line.Trim();
            //    if (!renderLayers.Contains(layerName))
            //        renderLayers.Add(layerName);
            //}
        }

        public static void AddRenderLayer(string layerName)
        {
            layerName = layerName.Trim();
            if (!renderLayers.Contains(layerName))
            {
                renderLayers.Add(layerName);
                Game.GetLightRenderer().AddLayer();

                //if (!File.Exists(layersPath))
                //{
                //    File.WriteAllText(layersPath, "Default");
                //}

                //using (StreamWriter w = File.AppendText(layersPath))
                //{
                //    w.Write("\n" + layerName);
                //}
            }
        }

        public static void LoadPipelines(GraphicsDevice gd, CommandList cl, Framebuffer compositeFB)
        {
            // Samplers
            AllSamplers = new List<Sampler>();
            var pointSampler = SamplerDescription.Point;
            pointSampler.AddressModeU = SamplerAddressMode.Clamp;
            pointSampler.AddressModeV = SamplerAddressMode.Clamp;

            pointSamplerClamp = gd.ResourceFactory.CreateSampler(pointSampler);
            pointSamplerClamp.Name = "PointClamp";


            var wrapLinear = SamplerDescription.Linear;
            wrapLinear.AddressModeU = SamplerAddressMode.Wrap;
            wrapLinear.AddressModeV = SamplerAddressMode.Wrap;

            linearSamplerWrap = gd.ResourceFactory.CreateSampler(wrapLinear);
            linearSamplerWrap.Name = "LinearWrap";

            var clampLinear = SamplerDescription.Linear;
            clampLinear.AddressModeU = SamplerAddressMode.Clamp;
            clampLinear.AddressModeV = SamplerAddressMode.Clamp;

            linearSampleClamp = gd.ResourceFactory.CreateSampler(clampLinear);
            linearSampleClamp.Name = "LinearClamp";

            AllSamplers.Add(linearSampleClamp);
            AllSamplers.Add(linearSamplerWrap);
            AllSamplers.Add(pointSamplerClamp);

            // Default Texture
            Texture defTex = gd.ResourceFactory.CreateTexture(
                            TextureDescription.Texture2D(100, 100, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled));
            defaultTexView = gd.ResourceFactory.CreateTextureView(defTex);

            // Shared Uniforms
            var sharedPipelineResource = gd.ResourceFactory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("PipelineData", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));
            sharedPipelineLayout = sharedPipelineResource;


            // Texture Layout
            var texLayout = gd.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SpriteTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SpriteSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));
            sharedTextureLayout = texLayout;

            // Texture Layout Normals
            var texLayoutNormal = gd.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SpriteTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SpriteSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("NormalTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("NormalSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));
            sharedSpriteNormalLayout = texLayoutNormal;

            // Shared vertex layouts
            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Scale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("WorldScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tint", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("ZRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("uvStart", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("uvScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Pivot", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
            vertexLayout.InstanceStepRate = 1;
            sharedVertexLayout = vertexLayout;

            VertexLayoutDescription vertexLayout3D = new VertexLayoutDescription(
              new VertexElementDescription("position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
              new VertexElementDescription("vertexNormal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
              new VertexElementDescription("texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
              new VertexElementDescription("tangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));
            sharedMeshVertexLayout = vertexLayout3D;

            // 3D Shared
            var meshVertexLayout = gd.ResourceFactory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("SharedMeshVertex", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            sharedMeshUniform_VS = meshVertexLayout;

            var meshFragmentLayout = gd.ResourceFactory.CreateResourceLayout(
              new ResourceLayoutDescription(
                  new ResourceLayoutElementDescription("SharedMeshFragment", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
            sharedMeshUniform_FS = meshFragmentLayout;

            // Full screen pipeline

            // Shaders
            ShaderDescription vertexShader = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(FullScreenQuadVertex),
                "main");

            ShaderDescription fragmentShader = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FullScreenQuadFragmentPP),
                "main");

            var shaders = gd.ResourceFactory.CreateFromSpirv(vertexShader, fragmentShader);

            //ResourceLayout resourceLayout = PipelineManager.gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            //    new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //    new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                new BlendStateDescription(
                    RgbaFloat.Black,
                    BlendAttachmentDescription.OverrideBlend),
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[]
                    {
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                    },
                    shaders),
                new ResourceLayout[] { sharedTextureLayout },
                gd.SwapchainFramebuffer.OutputDescription);
            FullScreenPipeline = gd.ResourceFactory.CreateGraphicsPipeline(ref pd);

            float[] verts = new float[]
               {
                        -1, 1, 0, 0,
                        1, 1, 1, 0,
                        1, -1, 1, 1,
                        -1, -1, 0, 1
               };
            ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };

            // Temp buffer
            CommandList tmpCl = gd.ResourceFactory.CreateCommandList();
            tmpCl.Begin();

            var stageBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)verts.Length * sizeof(float), BufferUsage.Staging));

            tmpCl.UpdateBuffer(stageBuffer, 0, verts);
            fullScreenVB = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)verts.Length * sizeof(float), BufferUsage.VertexBuffer));
            tmpCl.CopyBuffer(stageBuffer, 0, fullScreenVB, 0, sizeof(float) * (uint)verts.Length);

            tmpCl.UpdateBuffer(stageBuffer, 0, s_quadIndices);
            fullScreenIB = gd.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)s_quadIndices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            tmpCl.CopyBuffer(stageBuffer, 0, fullScreenIB, 0, (uint)s_quadIndices.Length * sizeof(ushort));

            tmpCl.End();
            gd.SubmitCommands(tmpCl);
            gd.WaitForIdle();

            tmpCl.Dispose();
            stageBuffer.Dispose();


            // Composite Pipeline
            ShaderDescription fragmentShaderComposite = new ShaderDescription(
               ShaderStages.Fragment,
               Encoding.UTF8.GetBytes(FullScreenQuadFragment),
               "main");

            var shadersComposite = gd.ResourceFactory.CreateFromSpirv(vertexShader, fragmentShaderComposite);

            GraphicsPipelineDescription compositePD = new GraphicsPipelineDescription(
               new BlendStateDescription(RgbaFloat.White, false,
               new BlendAttachmentDescription
               (
                   true,
                   BlendFactor.One,
                   BlendFactor.InverseSourceAlpha,
                   BlendFunction.Add,
                   BlendFactor.One,
                   BlendFactor.InverseSourceAlpha,
                   BlendFunction.Add
               )),
               DepthStencilStateDescription.Disabled,
               new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
               PrimitiveTopology.TriangleList,
               new ShaderSetDescription(
                   new[]
                   {
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                   },
                   shadersComposite),
               new ResourceLayout[] { sharedTextureLayout },
               compositeFB.OutputDescription);
            CompositePipeline = gd.ResourceFactory.CreateGraphicsPipeline(ref compositePD);

            // MSAA
            var maxSamples = gd.GetSampleCountLimit(PixelFormat.R8_G8_B8_A8_UNorm, false);
            if (msaaSampleCount > maxSamples)
                msaaSampleCount = maxSamples;
        }

        public static void ResetPipelines()
        {
            pipelineMaterials = new List<PipelineMaterial>();
            pipelineAssets = new Dictionary<string, PipelineAsset>();
        }

        public static void RefreshMaterials()
        {
            foreach (var pipeline in pipelineAssets)
                pipeline.Value.RefreshFrameBuffer();
            foreach (var material in pipelineMaterials)
                material.UpdateSampledTextures();
        }

        public static void DisposeResources()
        {
            //SpritePipeline.Dispose();
            //SpriteLayouts.Item1.Dispose();
            //SpriteLayouts.Item2.Dispose();
            //SpriteLayouts.Item3.Dispose();

            sharedPipelineLayout.Dispose();
            sharedTextureLayout.Dispose();

            sharedMeshUniform_VS.Dispose();
            sharedMeshUniform_FS.Dispose();

            defaultTexView.Dispose();
            pointSamplerClamp.Dispose();

            FullScreenPipeline.Dispose();
        }

        public static Pipeline GetOrCreateEditorSpritePipeline(Framebuffer buffer)
        {
            return EditorSpritePipeline;
        }


        private const string FullScreenQuadVertex = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoords;

layout(location = 0) out vec2 fsin_0;

void main()
{
    fsin_0 = TexCoords;
    gl_Position = vec4(Position, 0, 1);
}
";

        private const string FullScreenQuadFragment = @"
#version 450

layout(set = 0, binding = 0) uniform texture2D SceneTex;
layout(set = 0, binding = 1) uniform sampler SceneSampler;

layout(location = 0) in vec2 fsTexCoord;
layout(location = 0) out vec4 OutputColor;


vec3 adjustSaturation(vec3 color, float saturationFactor) {
    float luminance = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
    return mix(vec3(luminance), color, saturationFactor);
}

vec3 adjustContrast(vec3 color, float contrastFactor) {
    return (color - vec3(0.5)) * contrastFactor + vec3(0.5);
}

void main()
{
  
    vec4 color = texture(sampler2D(SceneTex, SceneSampler), fsTexCoord);
    OutputColor = vec4(color.rgb, color.a);
}
";

        private const string FullScreenQuadFragmentPP = @"
#version 450

layout(set = 0, binding = 0) uniform texture2D SceneTex;
layout(set = 0, binding = 1) uniform sampler SceneSampler;

layout(location = 0) in vec2 fsTexCoord;
layout(location = 0) out vec4 OutputColor;


vec3 adjustSaturation(vec3 color, float saturationFactor) {
    float luminance = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
    return mix(vec3(luminance), color, saturationFactor);
}

vec3 adjustContrast(vec3 color, float contrastFactor) {
    return (color - vec3(0.5)) * contrastFactor + vec3(0.5);
}

void main()
{ 
    vec4 color = texture(sampler2D(SceneTex, SceneSampler), fsTexCoord);
    vec3 tonedColor = adjustSaturation(color.rgb, 1.2);
    tonedColor = adjustContrast(tonedColor, 1.15);
    OutputColor = vec4(tonedColor.rgb, color.a);
}
";

    }
}


