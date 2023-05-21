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

        public static ResourceLayout sharedPipelineLayout;
        public static ResourceLayout sharedTextureLayout;
        //public static ResourceLayout fullScreenTextureLayout;

        public static TextureView defaultTexView;

        public static List<PipelineMaterial> pipelineMaterials = new List<PipelineMaterial>();
        //private static List<Pipeline> pipelines = new List<Pipeline>();
        internal static Dictionary<string, PipelineAsset> pipelineAssets = new Dictionary<string, PipelineAsset>();

        public static DeviceBuffer fullScreenVB;
        public static DeviceBuffer fullScreenIB;


        public static PipelineMaterial GetUberMaterial()
        {
            return pipelineMaterials[0];
        }

        public static PipelineMaterial GetUberAdditiveMaterial()
        {
            return pipelineMaterials[1];
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

        public static void LoadPipelines(GraphicsDevice gd, CommandList cl, Framebuffer mainRenderFB, Framebuffer compositeFB)
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

            // Shared pipeline vertex layout
            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Scale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tint", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("ZRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("uvStart", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("uvScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
            vertexLayout.InstanceStepRate = 1;
            sharedVertexLayout = vertexLayout;



            // Full screen pipeline

            // Shaders
            ShaderDescription vertexShader = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(FullScreenQuadVertex),
                "main");

            ShaderDescription fragmentShader = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FullScreenQuadFragment),
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

            fullScreenVB = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)verts.Length * sizeof(float), BufferUsage.VertexBuffer));
            gd.UpdateBuffer(fullScreenVB, 0, verts);

            fullScreenIB = gd.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)s_quadIndices.Length * sizeof(float), BufferUsage.IndexBuffer));
            gd.UpdateBuffer(fullScreenIB, 0, s_quadIndices);

            // Composite Pipeline

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
                   shaders),
               new ResourceLayout[] { sharedTextureLayout },
               compositeFB.OutputDescription);
            CompositePipeline = gd.ResourceFactory.CreateGraphicsPipeline(ref compositePD);

            var uberAlpha = new UberPipelineAsset(mainRenderFB);
            var uberAdditive = new UberPipelineAdditive(mainRenderFB);
            var waterAsset = new WaterPipelineAsset(mainRenderFB);

            //pipelineMaterials.Add(CreateNewPipeline(UberPipelineAsset, mainRenderFB, true));
            //pipelineMaterials.Add(CreateNewPipeline(WaterPipelineAsset, mainRenderFB, false));

            //CreateSpritePipeline();

            // Dissolve Tex
            //var texData = StaticResourceCache.GetImage(Game.AssetPath + "Sprites/noise_tex.jpeg", false);
            //var tex = StaticResourceCache.GetTexture2D(_gd, _gd.ResourceFactory, texData);

            //var view = StaticResourceCache.GetTextureView(_gd.ResourceFactory, tex);
            //dissolveNoiseTexSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            //    PipelineManager.SpriteLayouts.Item3,
            //    view,
            //    _gd.LinearSampler));

            // Shader Debugs

            //ShaderDescription vertTest = new ShaderDescription(
            //    ShaderStages.Vertex,
            //    Encoding.UTF8.GetBytes(Shaders.DebugUberVertex),
            //    "main");

            //ShaderDescription fragTest = new ShaderDescription(
            //    ShaderStages.Fragment,
            //    Encoding.UTF8.GetBytes(Shaders.DebugUberFragment),
            //    "main");

            //SpecializationConstant[] specializations =
            //{

            //};
            //VertexFragmentCompilationResult result = SpirvCompilation.CompileVertexFragment(
            //    vertTest.ShaderBytes,
            //    fragTest.ShaderBytes,
            //    CrossCompileTarget.MSL,
            //    new CrossCompileOptions(false, false, specializations));

            //File.WriteAllText(@"/Users/akderebur/Documents/vert" + 1 + ".txt", result.VertexShader);
            //File.WriteAllText(@"/Users/akderebur/Documents/frag" + 1 + ".txt", result.FragmentShader);
        }

        public static void RecreatePipelines(Framebuffer mainRenderFB, bool newScene)
        {
            if (newScene)
            {
                pipelineMaterials = new List<PipelineMaterial>();
                pipelineAssets = new Dictionary<string, PipelineAsset>();

                var uberAlpha = new UberPipelineAsset(mainRenderFB);
                var uberAdditive = new UberPipelineAdditive(mainRenderFB);
                var waterAsset = new WaterPipelineAsset(mainRenderFB);
            }
            else
            {
                foreach (var pipeline in pipelineAssets)
                    pipeline.Value.UpdateFramebuffer(mainRenderFB);

                //foreach (var mat in pipelineMaterials)
                //{
                //    if (mat.pipelineAsset.GetType() == typeof(UberPipelineAsset))
                //        mat.pipelineAsset = pipe;
                //}
            }
        }

        public static void DisposeResources()
        {
            //SpritePipeline.Dispose();
            //SpriteLayouts.Item1.Dispose();
            //SpriteLayouts.Item2.Dispose();
            //SpriteLayouts.Item3.Dispose();

            sharedPipelineLayout.Dispose();
            sharedTextureLayout.Dispose();
            defaultTexView.Dispose();
            pointSamplerClamp.Dispose();

            FullScreenPipeline.Dispose();
        }

        public static Pipeline GetOrCreateEditorSpritePipeline(Framebuffer buffer)
        {
            if (EditorSpritePipeline != null)
                return EditorSpritePipeline;
            else
            {
                CreateEditorSpritePipeline(buffer);
                return EditorSpritePipeline;
            }
        }

        //static int test = 0;
        //private static PipelineMaterial CreateNewPipeline(string pipelineAsset, Framebuffer buffer, bool optimise)
        //{
        //    var rsFactory = gd.ResourceFactory;

        //    StringReader sr = new StringReader(pipelineAsset);

        //    List<ResourceLayout> resourceLayouts = new List<ResourceLayout> { sharedPipelineLayout, sharedTextureLayout };
        //    List<UniformElement> uniformElements = new List<UniformElement>();
        //    List<string> uniformElementNames = new List<string>();
        //    List<string> textureNames = new List<string>();

        //    string vertexShaderSrc = "", fragmentShaderSrc = "";
        //    string lastLine = "";

        //    int sectionIndex = -1;
        //    int openBracketCount = 0;
        //    while (true)
        //    {
        //        string line = sr.ReadLine();
        //        if (line != null)
        //        {
        //            line = line.Trim();
        //            if (line.Contains("{"))
        //                openBracketCount++;
        //            else if (line.Contains("}"))
        //                openBracketCount--;

        //            if (line.Equals("{") && openBracketCount == 1)
        //            {
        //                if (lastLine.Equals("Properties"))
        //                    sectionIndex = 0;
        //                else if (lastLine.Equals("Vertex"))
        //                    sectionIndex = 1;
        //                else if (lastLine.Equals("Fragment"))
        //                    sectionIndex = 2;
        //                else
        //                    sectionIndex = -1;
        //                continue;
        //            }
        //            else if (line.Equals("}") && openBracketCount == 0)
        //            {
        //                sectionIndex = -1;
        //                continue;
        //            }
        //            else
        //            {
        //                if(sectionIndex > -1)
        //                {
        //                    if(sectionIndex == 0)
        //                    {
        //                        string[] split = line.Split(':');

        //                        string name = split[0];
        //                        string type = split[1];

        //                        switch (type)
        //                        {
        //                            case "float":
        //                                uniformElements.Add(UniformElement.Float1);
        //                                break;
        //                            case "vec2":
        //                                uniformElements.Add(UniformElement.Float2);
        //                                break;
        //                            case "vec3":
        //                                uniformElements.Add(UniformElement.Float3);
        //                                break;
        //                            case "vec4":
        //                                uniformElements.Add(UniformElement.Float4);
        //                                break;
        //                            case "mat4":
        //                                uniformElements.Add(UniformElement.Matrix4x4);
        //                                break;
        //                            case "texture2d":
        //                                textureNames.Add(name);
        //                                break;
        //                            default:
        //                                continue;
        //                        }

        //                        uniformElementNames.Add(name);
        //                    }
        //                    else if(sectionIndex == 1)
        //                    {
        //                        vertexShaderSrc += line + System.Environment.NewLine;
        //                    }
        //                    else if(sectionIndex == 2)
        //                    {
        //                        fragmentShaderSrc += line + System.Environment.NewLine;
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            break;
        //        }

        //        lastLine = line;
        //    }

        //    // Shader propery Uniforms
        //    var shaderPropUniform = rsFactory.CreateResourceLayout(
        //       new ResourceLayoutDescription(
        //           new ResourceLayoutElementDescription("SpriteProps", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
        //    resourceLayouts.Add(shaderPropUniform);

        //    // Texture Uniforms
        //    ResourceLayout texUniform = null;

        //    if (textureNames.Count > 0)
        //    {
        //        ResourceLayoutElementDescription[] layoutElements = new ResourceLayoutElementDescription[textureNames.Count * 2];
        //        int index = 0;
        //        foreach (var textureName in textureNames)
        //        {
        //            layoutElements[index] = new ResourceLayoutElementDescription(textureName, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
        //            index++;
        //            layoutElements[index] = new ResourceLayoutElementDescription(textureName + "Sampler", ResourceKind.Sampler, ShaderStages.Fragment);
        //            index++;
        //        }
        //        texUniform = gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(layoutElements));
        //        resourceLayouts.Add(texUniform);
        //    }

        //    // Shaders
        //    ShaderDescription vertexShader = new ShaderDescription(
        //        ShaderStages.Vertex,
        //        Encoding.UTF8.GetBytes(vertexShaderSrc),
        //        "main");

        //    ShaderDescription fragmentShader = new ShaderDescription(
        //        ShaderStages.Fragment,
        //        Encoding.UTF8.GetBytes(fragmentShaderSrc),
        //        "main");

        //    //SpecializationConstant[] specializations =
        //    //{

        //    //};
        //    //VertexFragmentCompilationResult result = SpirvCompilation.CompileVertexFragment(
        //    //    vertexShader.ShaderBytes,
        //    //    fragmentShader.ShaderBytes,
        //    //    CrossCompileTarget.MSL,
        //    //    new CrossCompileOptions(false, false, specializations));

        //    //File.WriteAllText(@"/Users/akderebur/Documents/vert" + test + ".txt", result.VertexShader);
        //    //File.WriteAllText(@"/Users/akderebur/Documents/frag" + test + ".txt", result.FragmentShader);
        //    //test++;

        //    Shader[] shaders;
        //    if(optimise)
        //        shaders = rsFactory.CreateFromSpirv(vertexShader, fragmentShader);
        //    else
        //    {
        //        shaders = CompileShaderSet(vertexShader, fragmentShader);
                
        //    }
      
        //    // Pipeline
        //    GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
        //    pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;

        //    //pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
        //    //    depthTestEnabled: true,
        //    //    depthWriteEnabled: true,
        //    //    comparisonKind: ComparisonKind.LessEqual);
        //    pipelineDescription.DepthStencilState = DepthStencilStateDescription.Disabled;

        //    pipelineDescription.RasterizerState = new RasterizerStateDescription(
        //        cullMode: FaceCullMode.None,
        //        fillMode: PolygonFillMode.Solid,
        //        frontFace: FrontFace.Clockwise,
        //        depthClipEnabled: true,
        //        scissorTestEnabled: false);

        //    pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
        //    //pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
        //    pipelineDescription.ResourceLayouts = resourceLayouts.ToArray();

        //    pipelineDescription.ShaderSet = new ShaderSetDescription(new VertexLayoutDescription[] { sharedVertexLayout }, shaders);
        //    pipelineDescription.Outputs = buffer.OutputDescription;

        //    var pipeline = rsFactory.CreateGraphicsPipeline(pipelineDescription);
        //    pipelines.Add(pipeline);

        //    //SpriteLayouts = new Tuple<ResourceLayout, ResourceLayout>(sharedPipelineResource, texLayout);

        //    PipelineMaterial pipelineMaterial = new PipelineMaterial(pipelines.Count - 1, shaderPropUniform, texUniform);

        //    // Shader Props Array
        //    uint vertBufferSize = 0;
        //    List<ShaderProp> shaderVals = new List<ShaderProp>();
        //    foreach (var uniformElement in uniformElements)
        //    {
        //        ShaderProp prop = new ShaderProp();
        //        prop.Offset = (int)vertBufferSize;

        //        switch (uniformElement)
        //        {
        //            case UniformElement.Float1:
        //                prop.SizeInBytes = 4;
        //                vertBufferSize += 4;
        //                prop.SetValue(1.5f);
        //                break;
        //            case UniformElement.Float2:
        //                prop.SizeInBytes = 8;
        //                vertBufferSize += 8;
        //                prop.SetValue(Vector2.One);
        //                break;
        //            case UniformElement.Float3:
        //                prop.SizeInBytes = 12;
        //                vertBufferSize += 12;
        //                prop.SetValue(Vector3.One);
        //                break;
        //            case UniformElement.Float4:
        //                prop.SizeInBytes = 16;
        //                vertBufferSize += 16;
        //                prop.SetValue(Vector4.One);
        //                break;
        //            //case UniformElement.Matrix4x4:
        //            //    prop.SizeInBytes = 64;
        //            //    vertBufferSize += 64;
        //            //    prop.SetValue(Matrix4x4.Identity);
        //            //    break;
        //            default:
        //                break;
        //        }

        //        shaderVals.Add(prop);
        //    }

        //    pipelineMaterial.SetShaderPropBuffer(uniformElementNames, shaderVals, vertBufferSize);
        //    pipelineMaterial.SetShaderTextureResources(textureNames);

        //    return pipelineMaterial;
        //}

        private static void CreateSpritePipeline()
        {
            var rsFactory = gd.ResourceFactory;

            // Draw data uniform
            var sharedPipelineResource = rsFactory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("PipelineData", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            //var drawLayout = rsFactory.CreateResourceLayout(
            //  new ResourceLayoutDescription(
            //      new ResourceLayoutElementDescription("DrawData", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            // Texture
            var texLayout = rsFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SpriteTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SpriteSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                   ));

            // Pipeline vertex layout
            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Scale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tint", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("ZRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("uvStart", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("uvScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
            vertexLayout.InstanceStepRate = 1;


            VertexLayoutDescription uberValues = new VertexLayoutDescription(
                new VertexElementDescription("OutlineOffset", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("OutlineColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("DissolveFade", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1));
            uberValues.InstanceStepRate = 1;


            // Shaders
            ShaderDescription vertexShader = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexUberBatch),
                "main");

            ShaderDescription fragmentShader = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentUberBatch),
                "main");

            var shaders = rsFactory.CreateFromSpirv(vertexShader, fragmentShader);

            // Pipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;

            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.None,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
            //pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ResourceLayouts = new[] { sharedPipelineResource, texLayout };

            pipelineDescription.ShaderSet = new ShaderSetDescription(new VertexLayoutDescription[] { vertexLayout }, shaders);
            pipelineDescription.Outputs = gd.MainSwapchain.Framebuffer.OutputDescription;

            SpritePipeline = rsFactory.CreateGraphicsPipeline(pipelineDescription);
            SpriteLayouts = new Tuple<ResourceLayout, ResourceLayout>(sharedPipelineResource, texLayout);
        }

        private static void CreateEditorSpritePipeline(Framebuffer buffer)
        {
            var rsFactory = gd.ResourceFactory;

            // Draw data uniform
            var pipelineLayout = rsFactory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("PipelineData", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            //var drawLayout = rsFactory.CreateResourceLayout(
            //  new ResourceLayoutDescription(
            //      new ResourceLayoutElementDescription("DrawData", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            // Texture
            var texLayout = rsFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SpriteTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SpriteSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                   ));

            // Pipeline vertex layout
            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Scale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tint", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("ZRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("uvStart", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("uvScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
            vertexLayout.InstanceStepRate = 1;

            // Shaders
            ShaderDescription vertexShader = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexCode),
                "main");

            ShaderDescription fragmentShader = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentCode),
                "main");

            var shaders = rsFactory.CreateFromSpirv(vertexShader, fragmentShader);

            // Pipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;

            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.None,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
            //pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ResourceLayouts = new[] { pipelineLayout, texLayout };

            pipelineDescription.ShaderSet = new ShaderSetDescription(new VertexLayoutDescription[] { vertexLayout }, shaders);
            pipelineDescription.Outputs = buffer.OutputDescription;

            EditorSpritePipeline = rsFactory.CreateGraphicsPipeline(pipelineDescription);
            SpriteLayouts = new Tuple<ResourceLayout, ResourceLayout>(pipelineLayout, texLayout);
        }

private const string WaterPipelineAsset = @"
Properties
{
	Padding2:vec4
    NoiseTex:texture2d
    ScreenTex:texture2d
}
Vertex
{
    #version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Scale;
    layout(location = 2) in vec4 Tint;
    layout(location = 3) in float ZRotation;
    layout(location = 4) in vec2 uvStart;
    layout(location = 5) in vec2 uvScale;

    layout(location = 0) out vec2 fsin_TexCoords;
    layout(location = 1) out vec4 fsin_Tint;
    layout(location = 2) out vec2 fsin_UnitUV;
    layout(location = 3) out vec2 fsin_UVScale;

    //layout(constant_id = 0) const bool InvertedY = false;


    //   B____C
    //   |   /| 
    //   |  / |
    //   | /  |
    //   |/___|
    //  A     D

    const vec4 Quads[6]= vec4[6](
        vec4(-0.5, -1, 0, 1),
        vec4(-0.5, 0.0, 0, 0),
        vec4(0.5, 0.0, 1, 0),
        vec4(-0.5, -1.0, 0, 1),
        vec4(0.5, 0.0, 1, 0),
        vec4(0.5, -1.0, 1, 1)
    );

    vec2 rotate(vec2 v, float a)
    {
        float s = sin(a);
        float c = cos(a);
        mat2 m = mat2(c, -s, s, c);
        return m * v;
    }

    void main()
    {
        vec4 unit_quad = Quads[gl_VertexIndex];
        vec2 unit_pos = unit_quad.xy;
        vec2 uv_pos = unit_quad.zw;

        //vec2 srcPos = src.xy;
        vec2 pos = unit_pos * Scale.xy;
        pos = rotate(pos, ZRotation);
        pos += Position.xy;

        gl_Position = VP * vec4(pos, 0, 1);

        vec2 uv_sample = uv_pos * uvScale + uvStart;
        //uv_sample = vec2(1, 1) + FlipScale * uv_pos;
    
        fsin_TexCoords = uv_sample ;
        fsin_Tint = Tint;
        fsin_UnitUV = uv_pos;
        fsin_UVScale = uvScale;
    }
}
Fragment
{
    #version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
    layout (set = 1, binding = 1) uniform sampler SpriteSampler;

    layout (set = 2, binding = 0) uniform SpriteProps
    {
        vec4 Padding2;
    };

    layout (set = 3, binding = 0) uniform texture2D NoiseTex;
    layout (set = 3, binding = 1) uniform sampler NoiseTexSampler;

    layout (set = 3, binding = 2) uniform texture2D ScreenTex;
    layout (set = 3, binding = 3) uniform sampler ScreenTexSampler;

    //const float fade = 0.5;

    layout(location = 0) in vec2 fsin_TexCoords;
    layout(location = 1) in vec4 fsin_Tint;
    layout(location = 2) in vec2 fsin_UnitUV;
    layout(location = 3) in vec2 fsin_UVScale;

    layout(location = 0) out vec4 outputColor;

    // Water properties

    const float reflectionOffset = 0.65; // allows player to control reflection position
    const float reflectionBlur = 0.0; // works only if projec's driver is set to GLES3, more information here https://docs.godotengine.org/ru/stable/tutorials/shading/screen-reading_shaders.html
    const float calculatedOffset = 0.0; // this is controlled by script, it takes into account camera position and water object position, that way reflection stays in the same place when camera is moving
    const float calculatedAspect = 1.0; // is controlled by script, ensures that noise is not affected by object scale

    const vec2 distortionScale = vec2(1, 1);
    const vec2 distortionSpeed = vec2(0.02, 0.03);
    const vec2 distortionStrength = vec2(0.4, 0.5);

    const float waveSmoothing = 0.02;

    const float mainWaveSpeed = 1.5;
    const float mainWaveFrequency = 20.0;
    const float mainWaveAmplitude = 0.005;

    const float secondWaveSpeed = 2.5;
    const float secondWaveFrequency = 30.0;
    const float secondWaveAmplitude = 0.015;

    const float thirdWaveSpeed = 3.5;
    const float thirdWaveFrequency = 40.0;
    const float thirdWaveAmplitude = 0.01;

    const float squashing = 1.5;

    const vec4 shorelineColor  = vec4(1.0, 1.0, 1.0, 1.0);
    const float shorelineSize = 0.01;
    const float foamSize  = 0.25;
    const float foamStrength = 0.025;
    const float foamSpeed = 1.0;
    const vec2 foamScale = vec2(1.0, 1.0);

    void main()
    {
        vec4 dummyColor = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);
        vec4 noiseSample = texture(sampler2D(NoiseTex, NoiseTexSampler), fsin_TexCoords * foamScale + Time * foamSpeed);

        vec2 fragPos = gl_FragCoord.xy;
        vec2 uv = fragPos / Resolution;
	    uv.y = 1. - uv.y; // turning screen uvs upside down
	    uv.y *= squashing;
	    uv.y -= calculatedOffset;
	    uv.y += reflectionOffset;
	
	    vec2 noiseTextureUV = fsin_TexCoords * distortionScale; 
	    noiseTextureUV.y *= 1;
	    noiseTextureUV += Time * distortionSpeed; // scroll noise over time

        vec2 noiseDistort = texture(sampler2D(NoiseTex, NoiseTexSampler), noiseTextureUV).rg;
	
	    vec2 waterDistortion = noiseDistort;
	    waterDistortion.rg *= distortionStrength.xy;
	    waterDistortion.xy = smoothstep(0.0, 5., waterDistortion.xy); 
	    uv += waterDistortion;

        vec4 screenSample = texture(sampler2D(ScreenTex, ScreenTexSampler), uv);
	
	    vec4 color = screenSample;
        //color = mix(color, dummyColor, 0.1f);
        //color.a = max(dummyColor.a, 1);
	    //vec4 color = vec4(0.,0.,0.7,1.);
        //vec4 noiseColor = texture(sampler2D(NoiseTex, NoiseTexSampler), fsin_TexCoords);
        //vec4 color = mix(dummyColor, noiseColor, 0.1f);
        //vec4 color = dummyColor;

    
	    //adding the wave amplitude at the end to offset it enough so it doesn't go outside the sprite's bounds
	    float distFromTop = mainWaveAmplitude * sin(fsin_TexCoords.x * mainWaveFrequency + Time * mainWaveSpeed) + mainWaveAmplitude
	 			    + secondWaveAmplitude * sin(fsin_TexCoords.x * secondWaveFrequency + Time * secondWaveSpeed) + secondWaveAmplitude
				    + thirdWaveAmplitude * cos(fsin_TexCoords.x * thirdWaveFrequency - Time * thirdWaveSpeed) + thirdWaveAmplitude;

	    float waveArea = fsin_TexCoords.y - distFromTop;
	
	    waveArea = smoothstep(0., 1. * waveSmoothing, waveArea);
	
	    color.a *= waveArea;

	    float shorelineBottom = fsin_TexCoords.y - distFromTop - shorelineSize;
	    shorelineBottom = smoothstep(0., 1. * waveSmoothing,  shorelineBottom);
	
	    float shoreline = waveArea - shorelineBottom;
	    color.rgb += shoreline * shorelineColor.rgb;
	
	    //this approach allows smoother blendign between shoreline and foam
	    /*
	    float shorelineTest1 = UV.y - distFromTop;
	    shorelineTest1 = smoothstep(0.0, shorelineTest1, shorelineSize);
	    color.rgb += shorelineTest1 * shorelineColor.rgb;
	    */
	
	    vec4 foamNoise = noiseSample;
        //foamNoise.a = dummyColor.a;
	    foamNoise.r = smoothstep(0.0, foamNoise.r, foamStrength); 
	
	    float shorelineFoam = fsin_TexCoords.y - distFromTop;
	    shorelineFoam = smoothstep(0.0, shorelineFoam, foamSize);
	
	    shorelineFoam *= foamNoise.r;
        color.rgb = mix(color.rgb, vec3(0.0, 0.4, 0.9), 0.3);
	    color.rgb += shorelineFoam * shorelineColor.rgb;
	
	    outputColor = color;
    }
}
"
;

        private const string UberPipelineAsset = @"
Properties
{
	DissolveFade:float
    TwistFade:float
    Dummy:float
	OutlineThickness:float
	OutlineColor:vec4
}
Vertex
{
    #version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Scale;
    layout(location = 2) in vec4 Tint;
    layout(location = 3) in float ZRotation;
    layout(location = 4) in vec2 uvStart;
    layout(location = 5) in vec2 uvScale;

    layout(location = 0) out vec2 fsin_TexCoords;
    layout(location = 1) out vec4 fsin_Tint;
    layout(location = 2) out vec2 fsin_UnitUV;
    layout(location = 3) out vec2 fsin_UVScale;

    //layout(constant_id = 0) const bool InvertedY = false;


    //   B____C
    //   |   /| 
    //   |  / |
    //   | /  |
    //   |/___|
    //  A     D

    const vec4 Quads[6]= vec4[6](
        vec4(-0.5, -0.5, 0, 1),
        vec4(-0.5, 0.5, 0, 0),
        vec4(0.5, 0.5, 1, 0),
        vec4(-0.5, -0.5, 0, 1),
        vec4(0.5, 0.5, 1, 0),
        vec4(0.5, -0.5, 1, 1)
    );

    vec2 rotate(vec2 v, float a)
    {
        float s = sin(a);
        float c = cos(a);
        mat2 m = mat2(c, -s, s, c);
        return m * v;
    }

    void main()
    {
        vec4 unit_quad = Quads[gl_VertexIndex];
        vec2 unit_pos = unit_quad.xy;
        vec2 uv_pos = unit_quad.zw;

        //vec2 srcPos = src.xy;
        vec2 pos = unit_pos * Scale.xy;
        pos = rotate(pos, ZRotation);
        pos += Position.xy;

        gl_Position = VP * vec4(pos, 0, 1);

        vec2 uv_sample = uv_pos * uvScale + uvStart;
        //uv_sample = vec2(1, 1) + FlipScale * uv_pos;
    
        fsin_TexCoords = uv_sample ;
        fsin_Tint = Tint;
        fsin_UnitUV = uv_pos;
        fsin_UVScale = uvScale;
    }
}
Fragment
{
    #version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
    layout (set = 1, binding = 1) uniform sampler SpriteSampler;

    layout (set = 2, binding = 0) uniform SpriteProps
    {
        float DissolveFade;
        float TwistFade;
        float Dummy;
        float OutlineThickness;
        vec4 OutlineColor;
    };


    //const float fade = 0.5;

    layout(location = 0) in vec2 fsin_TexCoords;
    layout(location = 1) in vec4 fsin_Tint;
    layout(location = 2) in vec2 fsin_UnitUV;
    layout(location = 3) in vec2 fsin_UVScale;

    layout(location = 0) out vec4 outputColor;

    // Glitch

    
    float rand1(vec2 co, float random_seed)
    {
        return fract(sin(dot(co.xy * random_seed, vec2(12.,85.5))) * 120.01);
    }


    float rand2(vec2 co, float random_seed)
    {
        float r1 = fract(sin(dot(co.xy * random_seed ,vec2(12.9898, 78.233))) * 43758.5453);
        return fract(sin(dot(vec2(r1 + co.xy * 1.562) ,vec2(12.9898, 78.233))) * 43758.5453);
    }


    vec4 glitch(float glitch_size, float glitch_amount, float _speed, float value)
    {
        vec2 uv = fsin_TexCoords;
	    float seed = floor(value * _speed * 10.0) / 10.0;
	    vec2 blockS = floor(uv * vec2 (24., 19.) * glitch_size) * 4.0;
	    vec2  blockL = floor(uv * vec2 (38., 14.) * glitch_size) * 4.0;

	    float line_noise = pow(rand2(blockS, seed), 3.0) * glitch_amount * pow(rand2(blockL, seed), 3.0);
	    vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), uv + vec2 (line_noise * 0.02 * rand1(vec2(2.0), seed), 0) * fsin_UVScale);
	    return color;
    }

    vec4 blend_color(vec4 txt, vec4 color, float value)
    {
	    vec3 tint = vec3(dot(txt.rgb, vec3(.22, .7, .07)));
	    tint.rgb *= color.rgb;
	    txt.rgb = mix(txt.rgb, tint.rgb, value);
	    return txt;
    }
    

    // Dissolve
    float rand(vec2 coord) {
	    return fract(sin(dot(coord, vec2(12.9898, 78.233))) * 43758.5453);
    }

    float perlin_noise(vec2 coord) {
	    vec2 i = floor(coord);
	    vec2 f = fract(coord);
	
	    float t_l = rand(i) * 6.283;
	    float t_r = rand(i + vec2(1, 0)) * 6.283;
	    float b_l = rand(i + vec2(0, 1)) * 6.283;
	    float b_r = rand(i + vec2(1)) * 6.283;
	
	    vec2 t_l_vec = vec2(-sin(t_l), cos(t_l));
	    vec2 t_r_vec = vec2(-sin(t_r), cos(t_r));
	    vec2 b_l_vec = vec2(-sin(b_l), cos(b_l));
	    vec2 b_r_vec = vec2(-sin(b_r), cos(b_r));
	
	    float t_l_dot = dot(t_l_vec, f);
	    float t_r_dot = dot(t_r_vec, f - vec2(1, 0));
	    float b_l_dot = dot(b_l_vec, f - vec2(0, 1));
	    float b_r_dot = dot(b_r_vec, f - vec2(1));
	
	    vec2 cubic = f * f * (3.0 - 2.0 * f);
	
	    float top_mix = mix(t_l_dot, t_r_dot, cubic.x);
	    float bot_mix = mix(b_l_dot, b_r_dot, cubic.x);
	    float whole_mix = mix(top_mix, bot_mix, cubic.y);
	
	    return whole_mix + 0.5;
    }

    const vec2 OFFSETS[8] = {
	    vec2(-1, -1), vec2(-1, 0), vec2(-1, 1), vec2(0, -1), vec2(0, 1), 
	    vec2(1, -1), vec2(1, 0), vec2(1, 1)
    };

    void main()
    {
        vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);

        vec2 coord = fsin_UnitUV * 10;
	    float value = perlin_noise(coord) + VP[0][0];

        vec2 size = fsin_UVScale * OutlineThickness;
	    float outline = 0.0;
	
	    for (int i = 0; i < OFFSETS.length(); i++) {
		    outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + size * OFFSETS[i]).a;
	    }
	    outline = min(outline, 1.0);

       //float glitchVal = 20.0 + Time * 1.0;
	    //color = glitch(0.0, 10.0, 1.0, glitchVal);

	    color = mix(color, OutlineColor, outline - color.a);
        color.a *= floor(DissolveFade + min(1, value));
        //outputColor = color;



	    //vec4 tint = blend_color(color, vec4(1, 1, 1, 1), 0.0);
        color.rgb *= 0.5f;
        outputColor = color;
    }
}
"
;

        private const string VertexCode = @"
#version 450

layout (set = 0, binding = 1) uniform PipelineData
{
    mat4 VP;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Scale;
layout(location = 2) in vec4 Tint;
layout(location = 3) in float ZRotation;
layout(location = 4) in vec2 uvStart;
layout(location = 5) in vec2 uvScale;

layout(location = 0) out vec2 fsin_TexCoords;
layout(location = 1) out vec4 fsin_Tint;
layout(location = 2) out vec2 fsin_UnitUV;

layout(constant_id = 0) const bool InvertedY = false;


//   B____C
//   |   /| 
//   |  / |
//   | /  |
//   |/___|
//  A     D

const vec4 Quads[6]= vec4[6](
    vec4(-0.5, -0.5, 0, 1),
    vec4(-0.5, 0.5, 0, 0),
    vec4(0.5, 0.5, 1, 0),
    vec4(-0.5, -0.5, 0, 1),
    vec4(0.5, 0.5, 1, 0),
    vec4(0.5, -0.5, 1, 1)
);

vec2 rotate(vec2 v, float a)
{
    float s = sin(a);
    float c = cos(a);
    mat2 m = mat2(c, -s, s, c);
    return m * v;
}

void main()
{
    vec4 unit_quad = Quads[gl_VertexIndex];
    vec2 unit_pos = unit_quad.xy;
    vec2 uv_pos = unit_quad.zw;

    //vec2 srcPos = src.xy;
    vec2 pos = unit_pos * Scale.xy;
    pos = rotate(pos, ZRotation);
    pos += Position.xy;

    gl_Position = VP * vec4(pos, 0, 1);

    vec2 uv_sample = uv_pos * uvScale + uvStart;
    //uv_sample = vec2(1, 1) + FlipScale * uv_pos;
    
    fsin_TexCoords = uv_sample ;
    fsin_Tint = Tint;
    fsin_UnitUV = uv_pos;

}";


        private const string VertexUberBatch = @"

#version 450

layout (set = 0, binding = 1) uniform PipelineData
{
    mat4 VP;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Scale;
layout(location = 2) in vec4 Tint;
layout(location = 3) in float ZRotation;
layout(location = 4) in vec2 uvStart;
layout(location = 5) in vec2 uvScale;

layout(location = 6) in float OutlineOffset;
layout(location = 7) in vec4 OutlineColor;
layout(location = 8) in float DissolveFade;

layout(location = 0) out vec2 fsin_TexCoords;
layout(location = 1) out vec4 fsin_Tint;
layout(location = 2) out vec2 fsin_UnitUV;
layout(location = 3) out float fsin_OutlineOffset;
layout(location = 4) out vec4 fsin_OutlineColor;
layout(location = 5) out float fsin_DissolveFade;


layout(constant_id = 0) const bool InvertedY = false;


//   B____C
//   |   /| 
//   |  / |
//   | /  |
//   |/___|
//  A     D

const vec4 Quads[6]= vec4[6](
    vec4(-0.5, -0.5, 0, 1),
    vec4(-0.5, 0.5, 0, 0),
    vec4(0.5, 0.5, 1, 0),
    vec4(-0.5, -0.5, 0, 1),
    vec4(0.5, 0.5, 1, 0),
    vec4(0.5, -0.5, 1, 1)
);

vec2 rotate(vec2 v, float a)
{
    float s = sin(a);
    float c = cos(a);
    mat2 m = mat2(c, -s, s, c);
    return m * v;
}

void main()
{
    vec4 unit_quad = Quads[gl_VertexIndex];
    vec2 unit_pos = unit_quad.xy;
    vec2 uv_pos = unit_quad.zw;

    //vec2 srcPos = src.xy;
    vec2 pos = unit_pos * Scale.xy;
    pos = rotate(pos, ZRotation);
    pos += Position.xy;

    gl_Position = VP * vec4(pos, 0, 1);

    vec2 uv_sample = uv_pos * uvScale + uvStart;
    //uv_sample = vec2(1, 1) + FlipScale * uv_pos;
    
    fsin_TexCoords = uv_sample ;
    fsin_Tint = Tint;
    fsin_UnitUV = uv_pos;
    fsin_OutlineOffset = OutlineOffset;
    fsin_OutlineColor = OutlineColor;
    fsin_DissolveFade = DissolveFade;
}";

        private const string FragmentUberBatch = @"
#version 450


layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
layout (set = 1, binding = 1) uniform sampler SpriteSampler;

const float fade = 0.5;

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 1) in vec4 fsin_Tint;
layout(location = 2) in vec2 fsin_UnitUV;
layout(location = 3) in float fsin_OutlineOffset;
layout(location = 4) in vec4 fsin_OutlineColor;
layout(location = 5) in float fsin_DissolveFade;

layout(location = 0) out vec4 outputColor;

float rand(vec2 coord) {
	return fract(sin(dot(coord, vec2(12.9898, 78.233))) * 43758.5453);
}

float perlin_noise(vec2 coord) {
	vec2 i = floor(coord);
	vec2 f = fract(coord);
	
	float t_l = rand(i) * 6.283;
	float t_r = rand(i + vec2(1, 0)) * 6.283;
	float b_l = rand(i + vec2(0, 1)) * 6.283;
	float b_r = rand(i + vec2(1)) * 6.283;
	
	vec2 t_l_vec = vec2(-sin(t_l), cos(t_l));
	vec2 t_r_vec = vec2(-sin(t_r), cos(t_r));
	vec2 b_l_vec = vec2(-sin(b_l), cos(b_l));
	vec2 b_r_vec = vec2(-sin(b_r), cos(b_r));
	
	float t_l_dot = dot(t_l_vec, f);
	float t_r_dot = dot(t_r_vec, f - vec2(1, 0));
	float b_l_dot = dot(b_l_vec, f - vec2(0, 1));
	float b_r_dot = dot(b_r_vec, f - vec2(1));
	
	vec2 cubic = f * f * (3.0 - 2.0 * f);
	
	float top_mix = mix(t_l_dot, t_r_dot, cubic.x);
	float bot_mix = mix(b_l_dot, b_r_dot, cubic.x);
	float whole_mix = mix(top_mix, bot_mix, cubic.y);
	
	return whole_mix + 0.5;
}

void main()
{
    vec4 main_texture = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);

    vec2 coord = fsin_UnitUV * 10;
	float value = perlin_noise(coord);
    main_texture.a *= floor(fsin_DissolveFade + min(1, value));
	outputColor = main_texture;
    
}";

        private const string FragmentCode = @"
#version 450


layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
layout (set = 1, binding = 1) uniform sampler SpriteSampler;

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 1) in vec4 fsin_Tint;

layout(location = 0) out vec4 outputColor;

void main()
{
    outputColor = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords) * fsin_Tint;
}";

        private const string OutlineFragment = @"
#version 450


layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
layout (set = 1, binding = 1) uniform sampler SpriteSampler;

const float offset = 1.0 / 700.0;
const vec4 b_color = vec4(0.0, 0.0, 1.0, 1.0);

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 1) in vec4 fsin_Tint;

layout(location = 0) out vec4 outputColor;

void main()
{
    vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);
	
	float alpha = -4.0 * color.a;
	alpha += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(offset, 0.0)).a;
	alpha += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(-offset, 0.0)).a;
	alpha += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2( 0.0, offset)).a;
	alpha += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2( 0.0,-offset)).a;

    //vec4 final_color = mix(color, b_color, clamp(alpha, 0.0, 1.0));
    //outputColor = vec4(final_color.rgb, clamp(abs(alpha) + color.a, 0.0, 1.0));

    vec4 final_color;
	if (color.a < 1.0 && alpha > 0.0) {
		final_color = b_color;
	} else {
		final_color = color;
	}

    outputColor = final_color;
}";

        private const string OutlineFragment2 = @"
#version 450


layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
layout (set = 1, binding = 1) uniform sampler SpriteSampler;

const float offset = 1.0 / 500.0;
const vec4 line_color = vec4(1.0, 192 / 255.0, 203 / 255.0, 1.0);

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 1) in vec4 fsin_Tint;

layout(location = 0) out vec4 outputColor;

void main()
{
    vec2 size = vec2(offset, offset);

	float outline = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(-size.x, 0)).a;
	outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(0, size.y)).a;
	outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(size.x, 0)).a;
	outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(0, -size.y)).a;
	outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(-size.x, size.y)).a;
	outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(size.x, size.y)).a;
	outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(-size.x, -size.y)).a;
	outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + vec2(size.x, -size.y)).a;
	outline = min(outline, 1.0);
	
	vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);
	outputColor = mix(color, line_color, outline - color.a);
}";

        private const string DissolveFragment = @"
#version 450


layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
layout (set = 1, binding = 1) uniform sampler SpriteSampler;
layout (set = 2, binding = 0) uniform texture2D DissolveTex; 
layout (set = 2, binding = 1) uniform sampler DissolveSampler;

const float noiseTiling = 1;
const float fade = 0.5;
const float edgeThickness = 0.01;
const vec4 edgeColor = vec4(0,0,0.7,1);

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 1) in vec4 fsin_Tint;
layout(location = 2) in vec2 fsin_UnitUV;

layout(location = 0) out vec4 outputColor;

float rand(vec2 coord) {
	return fract(sin(dot(coord, vec2(12.9898, 78.233))) * 43758.5453);
}

float perlin_noise(vec2 coord) {
	vec2 i = floor(coord);
	vec2 f = fract(coord);
	
	float t_l = rand(i) * 6.283;
	float t_r = rand(i + vec2(1, 0)) * 6.283;
	float b_l = rand(i + vec2(0, 1)) * 6.283;
	float b_r = rand(i + vec2(1)) * 6.283;
	
	vec2 t_l_vec = vec2(-sin(t_l), cos(t_l));
	vec2 t_r_vec = vec2(-sin(t_r), cos(t_r));
	vec2 b_l_vec = vec2(-sin(b_l), cos(b_l));
	vec2 b_r_vec = vec2(-sin(b_r), cos(b_r));
	
	float t_l_dot = dot(t_l_vec, f);
	float t_r_dot = dot(t_r_vec, f - vec2(1, 0));
	float b_l_dot = dot(b_l_vec, f - vec2(0, 1));
	float b_r_dot = dot(b_r_vec, f - vec2(1));
	
	vec2 cubic = f * f * (3.0 - 2.0 * f);
	
	float top_mix = mix(t_l_dot, t_r_dot, cubic.x);
	float bot_mix = mix(b_l_dot, b_r_dot, cubic.x);
	float whole_mix = mix(top_mix, bot_mix, cubic.y);
	
	return whole_mix + 0.5;
}

void main()
{
    vec4 main_texture = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);
    //vec4 noise_texture = texture(sampler2D(DissolveTex, DissolveSampler), fsin_UnitUV);
    //main_texture.a *= floor(fade + min(1, noise_texture.x));

    vec2 coord = fsin_UnitUV * 10;
	float value = perlin_noise(coord);
    main_texture.a *= floor(fade + min(1, value));
	outputColor = main_texture;
    
}";


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

layout(set = 0, binding = 0) uniform texture2D SpriteTex;
layout(set = 0, binding = 1) uniform sampler SpriteSampler;

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 0) out vec4 OutputColor;

void main()
{
    vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);
    OutputColor = color;
}
";
       
    }
}


