using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using WGIL;

namespace ABEngine.ABERuntime.Core.Assets
{
    public abstract class PipelineAsset
    {
        protected string defaultMatName;

        protected RenderPipeline pipeline;

        // Description
        protected string[] shaders = new string[2];
        protected List<BindGroupLayout> resourceLayouts;
        protected VertexAttribute[] vertexLayout;

        Dictionary<string, int> propNames;
        Dictionary<string, int> textureNames;
        internal PipelineMaterial refMaterial;

        public int pipelineID;

        static int pipelineCount = 0;

        public PipelineAsset()
        {
            pipelineID = pipelineCount;
            pipelineCount++;

            resourceLayouts = new List<BindGroupLayout>();
            propNames = new Dictionary<string, int>();
            textureNames = new Dictionary<string, int>();
            defaultMatName = "NoName";
        }

        public virtual void BindPipeline(RenderPass pass)
        {
            pass.SetPipeline(pipeline);
            pass.SetBindGroup(0, Game.pipelineSet);
        }

        private static Type ShaderToNetType(string shaderType) => shaderType switch
        {
            "float" => typeof(float),
            "vec2" => typeof(Vector2),
            "vec3" => typeof(Vector3),
            "vec4" => typeof(Vector4),
            _ => null
        };

        private static VertexFormat ShaderToVertexFormat(string shaderType) => shaderType switch
        {
            "float" => VertexFormat.Float32,
            "vec2" => VertexFormat.Float32x2,
            "vec3" => VertexFormat.Float32x3,
            "vec4" => VertexFormat.Float32x4,
            _ => VertexFormat.Float32
        };

        protected void ParseAsset(string pipelineAsset, bool readDescriptor = true, bool shaderOptimised = false)
        {
            var wgil = Game.wgil;

            StringReader sr = new StringReader(pipelineAsset);

            List<UniformElement> uniformElements = new List<UniformElement>();
            List<string> uniformElementNames = new List<string>();
            List<string> textureNames = new List<string>();

            List<VertexAttribute> vertexElements = new List<VertexAttribute>();
            bool pipeline3d = false;

            // Set 0 - Shared pipeline data
            if(readDescriptor)
                resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);

            // Descriptor Defaults
            VertexStepMode stepMode = VertexStepMode.Vertex;

            BlendState[] blendDesc = new[]
            {
                BlendState.OverrideBlend,
                BlendState.OverrideBlend
            };

            DepthStencilState depthDesc = new()
            {
                DepthTestEnabled = true,
                DepthWriteEnabled = true,
                DepthComparison = CompareFunction.LessEqual
            };

            PrimitiveState primitiveDesc = new PrimitiveState()
            {
                Topology = PrimitiveTopology.TriangleList,
                CullFace = CullFace.Back,
                FrontFace = FrontFace.Cw,
                PolygonMode = PolygonMode.Fill
            };

            // Vertex shader
            uint vertexOffset = 0;
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
                                                stepMode = VertexStepMode.Instance;
                                            break;
                                        case "@Blend":
                                            if (value.Equals("Alpha"))
                                                blendDesc = new BlendState[] { BlendState.AlphaBlend, BlendState.AlphaBlend };
                                            else if (value.Equals("Additive"))
                                                blendDesc = new BlendState[] { BlendState.AdditiveBlend, BlendState.AdditiveBlend };
                                            break;
                                        case "@Depth":
                                            if (value.Equals("GE"))
                                                depthDesc.DepthComparison = CompareFunction.GreaterEqual;
                                            break;
                                        case "@Cull":
                                            if (value.Equals("None"))
                                                primitiveDesc.CullFace = CullFace.None;
                                            else if (value.Equals("Front"))
                                                primitiveDesc.CullFace = CullFace.Front;
                                            break;
                                        case "@FrontFace":
                                            if (value.Equals("CCW"))
                                                primitiveDesc.FrontFace = FrontFace.Ccw;
                                            break;
                                        case "@Topology":
                                            if (value.Equals("PointList"))
                                                primitiveDesc.Topology = PrimitiveTopology.PointList;
                                            else if (value.Equals("LineList"))
                                                primitiveDesc.Topology = PrimitiveTopology.LineList;
                                            else if (value.Equals("LineStrip"))
                                                primitiveDesc.Topology = PrimitiveTopology.LineStrip;
                                            else if (value.Equals("TriangleStrio"))
                                                primitiveDesc.Topology = PrimitiveTopology.TriangleStrip;
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

                                    Type variableType = ShaderToNetType(type);
                                    VertexAttribute vertexElement = new VertexAttribute()
                                    {
                                        format = ShaderToVertexFormat(type),
                                        location = (uint)vertexElements.Count,
                                        offset = vertexOffset
                                    };

                                    vertexOffset += (uint)System.Runtime.InteropServices.Marshal.SizeOf(variableType);
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
            vertexLayout = vertexElements.ToArray();

            // Shader propery Uniforms
            var shaderPropLayoutDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
                {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Buffer,
                        ShaderStages = ShaderStages.VERTEX | ShaderStages.FRAGMENT,
                    }
                }
            };

            var shaderPropUniform = wgil.CreateBindGroupLayout(ref shaderPropLayoutDesc);
            resourceLayouts.Add(shaderPropUniform);

            // Texture Uniforms
            BindGroupLayout texUniform = null;

            if (textureNames.Count > 0)
            {
                BindGroupLayoutEntry[] layoutElements = new BindGroupLayoutEntry[textureNames.Count * 2];
                int index = 0;
                foreach (var textureName in textureNames)
                {
                    this.textureNames.Add(textureName, index / 2);
                    layoutElements[index] = new BindGroupLayoutEntry { BindingType = BindingType.Texture, ShaderStages = ShaderStages.FRAGMENT };
                    index++;
                    layoutElements[index] = new BindGroupLayoutEntry { BindingType = BindingType.Sampler, ShaderStages = ShaderStages.FRAGMENT };
                    index++;
                }

                var texLayoutDesc = new BindGroupLayoutDescriptor()
                {
                    Entries = layoutElements
                };

                texUniform = wgil.CreateBindGroupLayout(ref texLayoutDesc);
                resourceLayouts.Add(texUniform);
            }

            if(readDescriptor && pipeline3d)
                resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

            // Shaders
            shaders[0] = vertexShaderSrc;
            shaders[1] = fragmentShaderSrc;

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
                var pipelineDesc = new PipelineDescriptor()
                {
                    VertexStepMode = stepMode,
                    BlendStates = blendDesc,
                    DepthStencilState = depthDesc,
                    PrimitiveState = primitiveDesc,
                    VertexAttributes = vertexLayout,
                    BindGroupLayouts = resourceLayouts.ToArray(),
                    AttachmentDescription = new AttachmentDescription()
                    {
                        DepthFormat = TextureFormat.Depth32Float,
                        ColorFormats = new[] { Game.resourceContext.mainRenderView.Format, Game.resourceContext.spriteNormalsView.Format }
                    }
                };

                pipeline = wgil.CreateRenderPipeline(shaders[0], shaders[1], ref pipelineDesc);
            }

            GraphicsManager.AddPipelineAsset(this);
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

        internal List<BindGroupLayout> GetResourceLayouts()
        {
            return resourceLayouts;
        }

        public PipelineMaterial GetDefaultMaterial()
        {
            return refMaterial;
        }
    }
}

