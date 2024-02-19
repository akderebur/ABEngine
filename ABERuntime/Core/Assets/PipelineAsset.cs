using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ABEngine.ABERuntime.Pipelines;
using ABEngine.ABERuntime.Rendering;
using WGIL;
using WGIL.IO;

namespace ABEngine.ABERuntime.Core.Assets
{
    public abstract class PipelineAsset
    {
        public string name;
        protected string defaultMatName;

        public RenderPipeline pipeline;

        // Description
        protected string[] shaders = new string[2];
        protected List<BindGroupLayout> resourceLayouts;
        protected VertexLayout vertexLayout;
        protected VertexLayout instanceLayout;

        Dictionary<string, int> propNames;
        Dictionary<string, int> textureNames;
        internal PipelineMaterial refMaterial;

        public int pipelineID;

        static int pipelineCount = 0;

        public RenderOrder renderOrder { get; protected set; }
        public RenderType renderType { get; protected set; }

        Dictionary<string, VariantPipelineAsset> pipelineVariants;

        public PipelineAsset()
        {
            pipelineID = pipelineCount;
            pipelineCount++;

            resourceLayouts = new List<BindGroupLayout>();
            propNames = new Dictionary<string, int>();
            textureNames = new Dictionary<string, int>();
            defaultMatName = "NoName";

            renderOrder = RenderOrder.Opaque;
            renderType = RenderType.Opaque;
        }

        public virtual void BindPipeline(RenderPass pass)
        {
            pass.SetPipeline(pipeline);
            pass.SetBindGroup(0, Game.pipelineSet);
        }

        

        public virtual void BindPipeline(RenderPass pass, int bindC, params BindGroup[] bindGroups)
        {
            pass.SetPipeline(pipeline);

            int i = 0;
            while(i < bindC)
            {
                pass.SetBindGroup(i, bindGroups[i]);
                i++;
            }
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

        public VariantPipelineAsset GetPipelineVariant(string defineKey)
        {
            if (pipelineVariants != null && pipelineVariants.TryGetValue(defineKey, out VariantPipelineAsset variant))
            {
                if (!variant.IsBuilt)
                    variant.Build();
                return variant;
            }

            return null;
        }

        protected void ParseAsset(string pipelineAsset, bool readDescriptor = true)
        {
            Dictionary<string, bool> defines = new Dictionary<string, bool>();

            string source = pipelineAsset; 
            StringReader sr = new StringReader(source);
            StringBuilder sb = new StringBuilder();

            // Includes
            while (true)
            {
                string orgLine = sr.ReadLine();
                if (orgLine != null)
                {
                    string line = orgLine.Trim();
                    if (line.StartsWith("#include "))
                    {
                        string incParam = line.Replace("#include ", "").Trim();
                        int builtIncStart = incParam.IndexOf('<');
                        if (builtIncStart > -1)
                        {
                            // Built-in include
                            int builtIncEnd = incParam.IndexOf('>');
                            int length = builtIncEnd - builtIncStart - 1;
                            if (builtIncEnd > -1 && length > 0)
                            {
                                string incName = incParam.Substring(builtIncStart + 1, length);
                                sb.AppendLine(ShaderIncludes.GetShaderInclude(incName));
                            }
                        }
                        else
                        {
                            // Check user include
                        }
                    }
                    else
                        sb.AppendLine(orgLine);
                }
                else
                    break;
            }
            source = sb.ToString();

            // Defines
            sr = new StringReader(source);
            while (true)
            {
                string line = sr.ReadLine();
                if (line != null)
                {
                    line = line.Trim();
                    if(line.StartsWith("#ifdef "))
                    {
                        string defVar = line.Replace("#ifdef ", "");
                        if(!defines.ContainsKey(defVar))
                            defines.Add(defVar, false);
                    }
                }
                else
                    break;
            }

            int variantCount = (int)MathF.Pow(2, defines.Count);

            if (variantCount == 1)
                ParseAsset(source, readDescriptor, "");
            else
            {
                pipelineVariants = new Dictionary<string, VariantPipelineAsset>();

                // Create each variant
                List<string> keys = defines.Keys.ToList();
                for (int i = 0; i < variantCount; i++)
                {
                    sr = new StringReader(source);
                    sb = new StringBuilder();
                    Stack<string> defStack = new Stack<string>();
                    bool defineChain = true;

                    // Set defines
                    string defineKey = "";
                    string binary = Convert.ToString(i, 2).PadLeft(defines.Count, '0');
                    int index = binary.Length - 1;
                    foreach (var key in keys)
                    {
                        bool hasKey = binary[index] == '0' ? false : true;
                        defines[key] = hasKey;
                        index--;

                        if (hasKey)
                            defineKey += "*" + key;
                    }                        

                    while (true)
                    {
                        string orgLine = sr.ReadLine();
                        if (orgLine != null)
                        {
                            string line = orgLine.Trim();
                            if (line.StartsWith("#ifdef "))
                            {
                                string defVar = line.Replace("#ifdef ", "");
                                defStack.Push(defVar);

                                defineChain = true;
                                foreach (var item in defStack)
                                    defineChain &= defines[item];
                            }
                            else if (defStack.Count > 0)
                            {
                                if (line.StartsWith("#endif"))
                                {
                                    defStack.Pop();

                                    defineChain = true;
                                    foreach (var item in defStack)
                                        defineChain &= defines[item];
                                }
                                else if(defineChain)
                                {
                                    sb.AppendLine(orgLine);
                                }
                            }
                            else
                                sb.AppendLine(orgLine);
                        }
                        else
                            break;
                    }

                    // Default - No defines
                    if(i == 0)
                        ParseAsset(sb.ToString(), readDescriptor, defineKey);
                    else
                    {
                        // Variant
                        VariantPipelineAsset variant = new VariantPipelineAsset(sb.ToString(), readDescriptor, defineKey);
                        pipelineVariants.Add(defineKey, variant);
                    }
                }
            }
        }

        protected void ParseAsset(string pipelineAsset, bool readDescriptor, string defineKey)
        {
            var wgil = Game.wgil;

            StringReader sr = new StringReader(pipelineAsset);

            List<UniformElement> uniformElements = new List<UniformElement>();
            List<string> uniformElementNames = new List<string>();
            List<string> textureNames = new List<string>();

            List<VertexAttribute> vertexElements = new List<VertexAttribute>();
            List<VertexAttribute> instanceElements = new List<VertexAttribute>();

            List<VertexAttribute> curVertexAttrList = null;

            bool pipeline3d = false;

            bool useSkin = defineKey.Contains("HAS_SKIN");
            bool useInstance = defineKey.Contains("HAS_INSTANCE");


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
                                defaultMatName = lastLine + defineKey;
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
                                        case "@RenderType":
                                            if(value.Equals("Transparent"))
                                            {
                                                renderType = RenderType.Transparent;
                                                renderOrder = RenderOrder.Transparent;
                                            }
                                            break;
                                        case "@Pipeline":
                                            // Set 1 - Reserved for pipeline specific data
                                            if (value.Equals("3D"))
                                            {
                                                // Set 0 - Shared pipeline data

                                                resourceLayouts.Add(GraphicsManager.sharedMeshFrameData);
                                                if (useSkin)
                                                    resourceLayouts.Add(GraphicsManager.sharedSkinnedMeshUniform_VS);
                                                else
                                                    resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);

                                                pipeline3d = true;
                                            }
                                            else
                                            {
                                                resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
                                                resourceLayouts.Add(GraphicsManager.sharedSpriteNormalLayout);
                                            }
                                            break;
                                        case "@StepMode":
                                            VertexStepMode stepModeAttr;
                                            if (Enum.TryParse(value, true, out stepModeAttr))
                                                stepMode = VertexStepMode.Instance;
                                            break;
                                        case "@Blend":
                                            if (value.Equals("Alpha"))
                                                blendDesc = new BlendState[] { BlendState.AlphaBlend, BlendState.AlphaBlend };
                                            else if (value.Equals("Additive"))
                                                blendDesc = new BlendState[] { BlendState.AdditiveBlend, BlendState.AdditiveBlend };
                                            break;
                                        case "@Depth":
                                            CompareFunction compFuncAttr;
                                            if (Enum.TryParse(value, true, out compFuncAttr))
                                                depthDesc.DepthComparison = compFuncAttr;
                                            break;
                                        case "@DepthWrite":
                                            bool depthWriteAttr;
                                            if (bool.TryParse(value, out depthWriteAttr))
                                                depthDesc.DepthWriteEnabled = depthWriteAttr;
                                            break;
                                        case "@Cull":
                                            CullFace cullFaceAttr;
                                            if (Enum.TryParse(value, true, out cullFaceAttr))
                                                primitiveDesc.CullFace = cullFaceAttr;
                                            break;
                                        case "@FrontFace":
                                            if (value.Equals("CCW"))
                                                primitiveDesc.FrontFace = FrontFace.Ccw;
                                            break;
                                        case "@Topology":
                                            PrimitiveTopology topologyAttr;
                                            if (Enum.TryParse(value, true, out topologyAttr))
                                                primitiveDesc.Topology = topologyAttr;
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

                                    if (useInstance && name.ToLower().StartsWith("ins_"))
                                        curVertexAttrList = instanceElements;
                                    else
                                        curVertexAttrList = vertexElements;

                                    Type variableType = ShaderToNetType(type);
                                    VertexAttribute vertexElement = new VertexAttribute()
                                    {
                                        format = ShaderToVertexFormat(type),
                                        location = (uint)vertexElements.Count,
                                        offset = vertexOffset
                                    };

                                    vertexOffset += (uint)System.Runtime.InteropServices.Marshal.SizeOf(variableType);
                                    curVertexAttrList.Add(vertexElement);
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
            vertexLayout = new VertexLayout()
            {
                VertexStepMode = stepMode,
                VertexAttributes = vertexElements.ToArray()
            };

            VertexLayout[] vertexLayouts = null;
            if (useInstance)
            {
                instanceLayout = new VertexLayout()
                {
                    VertexStepMode = VertexStepMode.Instance,
                    VertexAttributes = instanceElements.ToArray()
                };
                vertexLayouts = new VertexLayout[] { vertexLayout, instanceLayout };
            }
            else
                vertexLayouts = new VertexLayout[] { vertexLayout };


            // Shader propery Uniforms
            BindGroupLayout shaderPropUniform = null;
            if (uniformElements.Count > 0)
            {
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
                shaderPropUniform = wgil.CreateBindGroupLayout(ref shaderPropLayoutDesc);
                resourceLayouts.Add(shaderPropUniform);
            }

            // Texture Uniforms
            BindGroupLayout texUniform = null;

            if (textureNames.Count > 0)
            {
                BindGroupLayoutEntry[] layoutElements = new BindGroupLayoutEntry[textureNames.Count * 2];
                int index = 0;
                foreach (var textureName in textureNames)
                {
                    this.textureNames.Add(textureName, index / 2);
                    if (textureName.Equals("DepthTex"))
                    {
                        layoutElements[index] = new BindGroupLayoutEntry { BindingType = BindingType.Texture, TextureSampleType = TextureSampleType.FloatNoFilter, ShaderStages = ShaderStages.FRAGMENT };
                        index++;
                        layoutElements[index] = new BindGroupLayoutEntry { BindingType = BindingType.Sampler, SamplerBindingType = SamplerBindingType.NonFiltering, ShaderStages = ShaderStages.FRAGMENT };
                        index++;
                    }
                    else
                    {
                        layoutElements[index] = new BindGroupLayoutEntry { BindingType = BindingType.Texture, ShaderStages = ShaderStages.FRAGMENT };
                        index++;
                        layoutElements[index] = new BindGroupLayoutEntry { BindingType = BindingType.Sampler, ShaderStages = ShaderStages.FRAGMENT };
                        index++;
                    }
                }

                var texLayoutDesc = new BindGroupLayoutDescriptor()
                {
                    Entries = layoutElements
                };

                texUniform = wgil.CreateBindGroupLayout(ref texLayoutDesc);
                resourceLayouts.Add(texUniform);
            }

            //if(readDescriptor && pipeline3d)
            //    resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

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
                    BlendStates = blendDesc,
                    DepthStencilState = depthDesc,
                    PrimitiveState = primitiveDesc,
                    VertexLayouts = vertexLayouts,
                    BindGroupLayouts = resourceLayouts.ToArray(),
                    AttachmentDescription = new AttachmentDescription()
                    {
                        DepthFormat = TextureFormat.Depth32Float,
                        ColorFormats = new[] { Game.resourceContext.mainRenderView.Format, Game.resourceContext.spriteNormalsView.Format }
                    }
                };

                pipeline = wgil.CreateRenderPipeline(shaders[0], shaders[1], ref pipelineDesc);
            }

            name = defaultMatName;
            GraphicsManager.AddPipelineAsset(defaultMatName, this);
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

        public bool HasProperties()
        {
            return propNames.Count > 0;
        }

        public bool HasTextures()
        {
            return textureNames.Count > 0;
        }
    }

    public class VariantPipelineAsset : PipelineAsset
    {
        public bool IsBuilt { get; set; }
        public string DefineKey { get; set; }

        private string pipelineSource;
        private bool readDescriptor;

        public VariantPipelineAsset(string assetContent, bool readDescriptor, string defineKey) : base()
        {
            this.pipelineSource = assetContent;
            this.readDescriptor = readDescriptor;
            this.DefineKey = defineKey;
        }

        public void Build()
        {
            base.ParseAsset(pipelineSource, readDescriptor, DefineKey);
            pipelineSource = null;
            IsBuilt = true;
        }
    }
}

