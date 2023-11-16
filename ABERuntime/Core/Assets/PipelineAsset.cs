using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace ABEngine.ABERuntime
{
    public abstract class PipelineAsset
    {
        protected string defaultMatName;
        protected GraphicsDevice gd;
        protected CommandList cl;
        protected ResourceFactory rf;

        protected Pipeline pipeline;

        // Description
        protected Shader[] shaders;
        protected List<ResourceLayout> resourceLayouts;
        protected VertexLayoutDescription vertexLayout;

        Dictionary<string, int> propNames;
        Dictionary<string, int> textureNames;
        internal PipelineMaterial refMaterial;

        public int pipelineID;

        static int pipelineCount = 0;

        public PipelineAsset()
        {
            gd = GraphicsManager.gd;
            cl = GraphicsManager.cl;
            rf = GraphicsManager.rf;

            pipelineID = pipelineCount;
            pipelineCount++;

            resourceLayouts = new List<ResourceLayout>();
            propNames = new Dictionary<string, int>();
            textureNames = new Dictionary<string, int>();
            defaultMatName = "NoName";
        }

        public virtual void BindPipeline()
        {
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, Game.pipelineSet);
        }

        private static Type ShaderToNetType(string shaderType) => shaderType switch
        {
            "float" => typeof(float),
            "vec2" => typeof(Vector2),
            "vec3" => typeof(Vector3),
            "vec4" => typeof(Vector4),
            _ => null
        };

        private static VertexElementFormat ShaderToVertexElement(string shaderType) => shaderType switch
        {
            "float" => VertexElementFormat.Float1,
            "vec2" => VertexElementFormat.Float2,
            "vec3" => VertexElementFormat.Float3,
            "vec4" => VertexElementFormat.Float4,
            _ => VertexElementFormat.Float1
        };

        protected void ParseAsset(string pipelineAsset, bool readDescriptor = true, bool shaderOptimised = false)
        {
            var rsFactory = GraphicsManager.rf;

            StringReader sr = new StringReader(pipelineAsset);

            List<UniformElement> uniformElements = new List<UniformElement>();
            List<string> uniformElementNames = new List<string>();
            List<string> textureNames = new List<string>();

            List<VertexElementDescription> vertexElements = new List<VertexElementDescription>();
            uint instanceStepRate = 0;
            bool pipeline3d = false;

            // Set 0 - Shared pipeline data
            if(readDescriptor)
                resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);

            // Descriptor Defaults
            BlendStateDescription blendDesc = new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.OverrideBlend, BlendAttachmentDescription.OverrideBlend);
            DepthStencilStateDescription depthDesc = DepthStencilStateDescription.DepthOnlyLessEqual;
            RasterizerStateDescription rasterizerDesc = RasterizerStateDescription.Default;
            PrimitiveTopology topology = PrimitiveTopology.TriangleList;

            string vertexShaderSrc = "", fragmentShaderSrc = "";
            string lastLine = "";

            int sectionIndex = -2;
            int openBracketCount = 0;
            while (true)
            {
                string line = sr.ReadLine();
                if (line != null)
                {
                    line = line.Trim();
                    if (line.Contains("{"))
                        openBracketCount++;
                    else if (line.Contains("}"))
                        openBracketCount--;

                    if (line.Equals("{") && openBracketCount == 1)
                    {
                        if (sectionIndex == -2)
                        {
                            if(defaultMatName.Equals("NoName"))
                                defaultMatName = lastLine;
                            sectionIndex = 0;
                        }
                        else if (lastLine.Equals("Vertex"))
                            sectionIndex = 1;
                        else if (lastLine.Equals("Fragment"))
                            sectionIndex = 2;
                        else
                            sectionIndex = -1;
                        continue;
                    }
                    else if (line.Equals("}") && openBracketCount == 0)
                    {
                        sectionIndex = -1;
                        continue;
                    }
                    else
                    {
                        if (sectionIndex > -1)
                        {
                            if (sectionIndex == 0)
                            {
                                if (string.IsNullOrEmpty(line))
                                    continue;

                                string[] split = line.Split(':');

                                if (line.StartsWith("@") && readDescriptor)
                                {
                                    // Pipeline Descriptor
                                    string descName = split[0].Trim();
                                    string value = split[1].Trim();

                                    switch (descName)
                                    {
                                        case "@Pipeline":
                                            // Set 1 - Reserved for pipeline specific data
                                            if (value.Equals("3D"))
                                            {
                                                resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
                                                pipeline3d = true;
                                            }
                                            else
                                                resourceLayouts.Add(GraphicsManager.sharedSpriteNormalLayout);
                                            break;
                                        case "@StepMode":
                                            if (value.Equals("Instance"))
                                                instanceStepRate = 1;
                                            break;
                                        case "@Blend":
                                            if (value.Equals("Alpha"))
                                                blendDesc = new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.AlphaBlend, BlendAttachmentDescription.AlphaBlend);
                                            else if (value.Equals("Additive"))
                                                blendDesc = new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.AdditiveBlend, BlendAttachmentDescription.AlphaBlend);
                                            break;
                                        case "@Depth":
                                            if (value.Equals("GE"))
                                                depthDesc = DepthStencilStateDescription.DepthOnlyGreaterEqual;
                                            break;
                                        case "@Cull":
                                            if (value.Equals("None"))
                                                rasterizerDesc.CullMode = FaceCullMode.None;
                                            else if (value.Equals("Front"))
                                                rasterizerDesc.CullMode = FaceCullMode.Front;
                                            break;
                                        case "@FrontFace":
                                            if (value.Equals("CCW"))
                                                rasterizerDesc.FrontFace = FrontFace.CounterClockwise;
                                            break;
                                        case "@Topology":
                                            if (value.Equals("PointList"))
                                                topology = PrimitiveTopology.PointList;
                                            else if (value.Equals("LineList"))
                                                topology = PrimitiveTopology.LineList;
                                            else if (value.Equals("LineStrip"))
                                                topology = PrimitiveTopology.LineStrip;
                                            else if (value.Equals("TriangleStrio"))
                                                topology = PrimitiveTopology.TriangleStrip;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                else
                                {
                                    // Shader property
                                    string name = split[0].Trim();
                                    string type = split[1].Trim();

                                    switch (type)
                                    {
                                        case "float":
                                            uniformElements.Add(UniformElement.Float1);
                                            uniformElementNames.Add(name);
                                            break;
                                        case "vec2":
                                            uniformElements.Add(UniformElement.Float2);
                                            uniformElementNames.Add(name);
                                            break;
                                        case "vec3":
                                            uniformElements.Add(UniformElement.Float3);
                                            uniformElementNames.Add(name);
                                            break;
                                        case "vec4":
                                            uniformElements.Add(UniformElement.Float4);
                                            uniformElementNames.Add(name);
                                            break;
                                        case "mat4":
                                            uniformElements.Add(UniformElement.Matrix4x4);
                                            uniformElementNames.Add(name);
                                            break;
                                        case "texture2d":
                                            textureNames.Add(name);
                                            break;
                                        default:
                                            continue;
                                    }
                                }
                            }
                            else if (sectionIndex == 1)
                            {
                                // Vertex shader

                                // Vertex element
                                if(line.Contains("layout") && line.Contains("location") && line.Contains("in "))
                                {
                                    string[] split = line.Split("in ");
                                    string typeAndName = split[1];

                                    string type = "";
                                    string name = "";

                                    string tmp = "";
                                    bool typeSet = false;

                                    foreach (char item in typeAndName)
                                    {
                                        if(item == ' ')
                                        {
                                            if(!typeSet)
                                                type = tmp;
                                            typeSet = true;
                                            tmp = "";
                                            continue;
                                        }
                                        else if(item == ';')
                                        {
                                            name = tmp;
                                            tmp = "";
                                            break;
                                        }

                                        tmp += item;
                                    }

                                    VertexElementDescription vertexElement = new VertexElementDescription();
                                    vertexElement.Name = name;
                                    vertexElement.Semantic = VertexElementSemantic.TextureCoordinate;
                                    vertexElement.Format = ShaderToVertexElement(type);
                                    vertexElements.Add(vertexElement);
                                }
                        
                                vertexShaderSrc += line + System.Environment.NewLine;
                            }
                            else if (sectionIndex == 2)
                            {
                                fragmentShaderSrc += line + System.Environment.NewLine;
                            }
                        }
                    }
                }
                else
                {
                    break;
                }

                lastLine = line;
            }

            // Vertex layout
            vertexLayout = new VertexLayoutDescription(vertexElements.ToArray());
            vertexLayout.InstanceStepRate = instanceStepRate;

            // Shader propery Uniforms
            var shaderPropUniform = rsFactory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("ShaderProps", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));
            resourceLayouts.Add(shaderPropUniform);

            // Texture Uniforms
            ResourceLayout texUniform = null;

            if (textureNames.Count > 0)
            {
                ResourceLayoutElementDescription[] layoutElements = new ResourceLayoutElementDescription[textureNames.Count * 2];
                int index = 0;
                foreach (var textureName in textureNames)
                {
                    this.textureNames.Add(textureName, index / 2);
                    layoutElements[index] = new ResourceLayoutElementDescription(textureName, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
                    index++;
                    layoutElements[index] = new ResourceLayoutElementDescription(textureName + "Sampler", ResourceKind.Sampler, ShaderStages.Fragment);
                    index++;
                }
                texUniform = rsFactory.CreateResourceLayout(new ResourceLayoutDescription(layoutElements));
                resourceLayouts.Add(texUniform);
            }

            if(readDescriptor && pipeline3d)
                resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

            // Shaders
            ShaderDescription vertexShader = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(vertexShaderSrc),
                "main");

            ShaderDescription fragmentShader = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragmentShaderSrc),
                "main");

            //if (defaultMatName.Equals("ToonWater"))
            //{
            //    SpecializationConstant[] specializations =
            //    {

            //    };

            //    var fragDebug = SpirvCompilation.CompileGlslToSpirv(
            //     Encoding.UTF8.GetString(fragmentShader.ShaderBytes), "FS", ShaderStages.Fragment, new GlslCompileOptions(debug: true));

            //    VertexFragmentCompilationResult result = SpirvCompilation.CompileVertexFragment(
            //        vertexShader.ShaderBytes,
            //        fragDebug.SpirvBytes,
            //        CrossCompileTarget.MSL,
            //        new CrossCompileOptions(false, false, specializations));

            //    File.WriteAllText(@"/Users/akderebur/Documents/vert_dum.txt", result.VertexShader);
            //    File.WriteAllText(@"/Users/akderebur/Documents/frag_dum.txt", result.FragmentShader);
            //}


            if (shaderOptimised)
                shaders = rsFactory.CreateFromSpirv(vertexShader, fragmentShader);
            else
            {
                shaders = CompileShaderSet(vertexShader, fragmentShader);
            }

            refMaterial = new PipelineMaterial(defaultMatName.ToHash32(), this, shaderPropUniform, texUniform);
            refMaterial.name = defaultMatName;

            // Shader Props Array
            uint vertBufferSize = 0;
            List<ShaderProp> shaderVals = new List<ShaderProp>();
            foreach (var uniformElement in uniformElements)
            {
                ShaderProp prop = new ShaderProp();
                prop.Offset = (int)vertBufferSize;

                switch (uniformElement)
                {
                    case UniformElement.Float1:
                        prop.SizeInBytes = 4;
                        vertBufferSize += 4;
                        prop.SetValue(0f);
                        break;
                    case UniformElement.Float2:
                        prop.SizeInBytes = 8;
                        vertBufferSize += 8;
                        prop.SetValue(Vector2.One);
                        break;
                    case UniformElement.Float3:
                        prop.SizeInBytes = 12;
                        vertBufferSize += 12;
                        prop.SetValue(Vector3.One);
                        break;
                    case UniformElement.Float4:
                        prop.SizeInBytes = 16;
                        vertBufferSize += 16;
                        prop.SetValue(Vector4.One);
                        break;
                    default:
                        break;
                }

                propNames.Add(uniformElementNames[shaderVals.Count], shaderVals.Count);
                shaderVals.Add(prop);
            }

            refMaterial.SetShaderPropBuffer(shaderVals, vertBufferSize);
            refMaterial.SetShaderTextureResources(textureNames);

            if (readDescriptor)
            {
                // Create pipeline
                GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription(
                  blendDesc,
                  depthDesc,
                  rasterizerDesc,
                  topology,
                  new ShaderSetDescription(
                      new[]
                      {
                        vertexLayout
                      },
                      shaders),
                  resourceLayouts.ToArray(),
                  Game.resourceContext.mainRenderFB.OutputDescription);
                pipeline = GraphicsManager.rf.CreateGraphicsPipeline(ref pipelineDescription);
            }

            GraphicsManager.AddPipelineAsset(this);
        }

        private static Shader[] CompileShaderSet(ShaderDescription vertexDescription, ShaderDescription pixelDescription)
        {
            var vertexSpirvCompilation = SpirvCompilation.CompileGlslToSpirv(
              Encoding.UTF8.GetString(vertexDescription.ShaderBytes), "VS", ShaderStages.Vertex, new GlslCompileOptions(debug: true));
            vertexDescription.ShaderBytes = vertexSpirvCompilation.SpirvBytes;

            var pixelSpirvCompilation = SpirvCompilation.CompileGlslToSpirv(
                Encoding.UTF8.GetString(pixelDescription.ShaderBytes), "FS", ShaderStages.Fragment, new GlslCompileOptions(debug: true));
            pixelDescription.ShaderBytes = pixelSpirvCompilation.SpirvBytes;

            return GraphicsManager.rf.CreateFromSpirv(vertexDescription, pixelDescription);
        }

        public int GetPropID(string propName)
        {
            if (propNames.TryGetValue(propName, out int id))
                return id;

            return -1;
        }

        public int GetTextureID(string texName)
        {
            if (textureNames.TryGetValue(texName, out int id))
                return id;

            return -1;
        }

        public List<string> GetTextureNames()
        {
            return textureNames.Keys.ToList();
        }

        public List<string> GetPropNames()
        {
            return propNames.Keys.ToList();
        }

        internal List<ResourceLayout> GetResourceLayouts()
        {
            return resourceLayouts;
        }

        public PipelineMaterial GetDefaultMaterial()
        {
            return refMaterial;
        }
    }
}

