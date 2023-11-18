using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Pipelines;
using WGIL;
using Buffer = WGIL.Buffer;

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
        //public static TextureSampleCount msaaSampleCount { get; set; }

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

        public static TextureFormat surfaceFormat;

        public static RenderPipeline SpritePipeline;
        public static RenderPipeline EditorSpritePipeline;
        public static RenderPipeline FullScreenPipeline;

        public static List<Sampler> AllSamplers;

        public static Sampler pointSamplerClamp;
        public static Sampler linearSamplerWrap;
        public static Sampler linearSampleClamp;

        public static Tuple<BindGroupLayout, BindGroupLayout> SpriteLayouts;

        public static VertexAttribute[] sharedVertexLayout;
        public static VertexAttribute[] sharedMeshVertexLayout;

        public static BindGroupLayout sharedPipelineLayout;
        public static BindGroupLayout sharedTextureLayout;
        public static BindGroupLayout sharedSpriteNormalLayout;
        public static BindGroupLayout sharedMeshUniform_VS;
        public static BindGroupLayout sharedMeshUniform_FS;

        public static TextureView defaultTexView;

        public static List<PipelineMaterial> pipelineMaterials = new List<PipelineMaterial>();
        //private static List<Pipeline> pipelines = new List<Pipeline>();
        internal static Dictionary<string, PipelineAsset> pipelineAssets = new Dictionary<string, PipelineAsset>();

        public static Buffer fullScreenVB;
        public static Buffer fullScreenIB;

        static PipelineMaterial GetFirstMatByName(string name)
        {
            var mat = pipelineMaterials.FirstOrDefault(pm => pm.name.Equals(name));

            if (mat == null)
            {
                return name switch
                {
                    "UberStandard" => new UberPipelineAsset().refMaterial,
                    "UberAdditive" => new UberPipelineAdditive().refMaterial,
                    "Uber3D" => new UberPipeline3D().refMaterial,
                    _ => null
                };
            }
            else
                return mat;
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

        public static void LoadPipelines()
        {
            WGILContext wgil = Game.wgil;

            surfaceFormat = wgil.GetSurfaceFormat();

            // Samplers
            AllSamplers = new List<Sampler>();
            pointSamplerClamp = wgil.CreateSampler(SamplerAddressMode.ClampToEdge, SamplerFilterMode.Nearest).SetManualDispose(true);
            pointSamplerClamp.Name = "PointClamp";

            linearSamplerWrap = wgil.CreateSampler(SamplerAddressMode.Repeat, SamplerFilterMode.Linear).SetManualDispose(true);
            linearSamplerWrap.Name = "LinearWrap";

            linearSampleClamp = wgil.CreateSampler(SamplerAddressMode.ClampToEdge, SamplerFilterMode.Linear).SetManualDispose(true);
            linearSampleClamp.Name = "LinearClamp";

            AllSamplers.Add(linearSampleClamp);
            AllSamplers.Add(linearSamplerWrap);
            AllSamplers.Add(pointSamplerClamp);

            // Default Texture
            Texture defTex = wgil.CreateTexture(100, 100, TextureFormat.Rgba8UnormSrgb, TextureUsages.TEXTURE_BINDING).SetManualDispose(true);
            defaultTexView = defTex.CreateView().SetManualDispose(true);

            // Shared Uniforms
            var sharedPipelineLayoutDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
                {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Buffer,
                        ShaderStages = ShaderStages.VERTEX | ShaderStages.FRAGMENT
                    }
                }
            };

            sharedPipelineLayout = wgil.CreateBindGroupLayout(ref sharedPipelineLayoutDesc).SetManualDispose(true);

            // Texture Layout
            var texLayoutDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
                {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Sampler,
                        ShaderStages = ShaderStages.FRAGMENT
                    }
                }
            };

            sharedTextureLayout = wgil.CreateBindGroupLayout(ref texLayoutDesc).SetManualDispose(true);

            // Texture Layout Normals
            var texLayoutNormalDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
               {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Sampler,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Sampler,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Buffer,
                        ShaderStages = ShaderStages.FRAGMENT
                    }
                }
            };

            sharedSpriteNormalLayout = wgil.CreateBindGroupLayout(ref texLayoutNormalDesc).SetManualDispose(true);

            // Shared vertex layouts
            sharedVertexLayout = WGILUtils.GetVertexLayout<QuadVertex>(out _);

            sharedMeshVertexLayout = WGILUtils.GetVertexLayout<VertexStandard>(out _);

            // 3D Shared
            var meshVertexDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
                {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Buffer,
                        ShaderStages = ShaderStages.VERTEX
                    }
                }
            };


            sharedMeshUniform_VS = wgil.CreateBindGroupLayout(ref meshVertexDesc).SetManualDispose(true);

            var meshFragmentDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
                {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Buffer,
                        ShaderStages = ShaderStages.FRAGMENT
                    }
                }
            };

            sharedMeshUniform_FS = wgil.CreateBindGroupLayout(ref meshFragmentDesc).SetManualDispose(true);

            // Full screen pipeline
            var fsPipelineDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[]
                {
                    BlendState.OverrideBlend
                },
                PrimitiveState = new PrimitiveState()
                {
                    Topology = PrimitiveTopology.TriangleList,
                    PolygonMode = PolygonMode.Fill,
                    CullFace = CullFace.None,
                    FrontFace = FrontFace.Cw
                },
                VertexAttributes = new VertexAttribute[]
                {
                    new VertexAttribute() { format = VertexFormat.Float32x2, location = 0, offset = 0 },
                    new VertexAttribute() { format = VertexFormat.Float32x2, location = 1, offset = 8 }
                },
                BindGroupLayouts = new BindGroupLayout[]
                {
                    sharedTextureLayout
                },
                AttachmentDescription = new AttachmentDescription()
                {
                    ColorFormats = new[] { surfaceFormat }
                }
            };

            FullScreenPipeline = wgil.CreateRenderPipeline(FullScreenQuadVertex, FullScreenQuadFragmentPP, ref fsPipelineDesc).SetManualDispose(true);

            float[] verts = new float[]
               {
                        -1, 1, 0, 0,
                        1, 1, 1, 0,
                        1, -1, 1, 1,
                        -1, -1, 0, 1
               };
            ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };

            fullScreenVB = wgil.CreateBuffer(verts.Length * sizeof(float), BufferUsages.VERTEX | BufferUsages.COPY_DST).SetManualDispose(true);
            wgil.WriteBuffer(fullScreenVB, verts);

            fullScreenIB = wgil.CreateBuffer(s_quadIndices.Length * sizeof(ushort), BufferUsages.INDEX | BufferUsages.COPY_DST).SetManualDispose(true);
            wgil.WriteBuffer(fullScreenIB, s_quadIndices);
        }

        public static void ResetPipelines()
        {
            pipelineMaterials = new List<PipelineMaterial>();
            pipelineAssets = new Dictionary<string, PipelineAsset>();
        }

        public static void RefreshMaterials()
        {
            foreach (var material in pipelineMaterials)
                material.UpdateSampledTextures();
        }

        public static void DisposeResources()
        {
            sharedPipelineLayout.Dispose();
            sharedTextureLayout.Dispose();

            sharedMeshUniform_VS.Dispose();
            sharedMeshUniform_FS.Dispose();

            defaultTexView.Dispose();
            pointSamplerClamp.Dispose();

            fullScreenIB.Dispose();
            fullScreenVB.Dispose();

            FullScreenPipeline.Dispose();

            foreach (var sampler in AllSamplers)
            {
                sampler.Dispose();
            }
        }

        public static RenderPipeline GetOrCreateEditorSpritePipeline()
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
    vec3 tonedColor = adjustSaturation(color.rgb, 1.1);
    tonedColor = adjustContrast(tonedColor, 1.0);
    OutputColor = vec4(tonedColor.rgb, color.a);
}
";

    }
}


