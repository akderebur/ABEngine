using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
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

        protected bool shaderOptimised;
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

        public static void ParseAsset(string pipelineAsset, PipelineAsset dest)
        {
            var rsFactory = GraphicsManager.rf;

            StringReader sr = new StringReader(pipelineAsset);

            List<UniformElement> uniformElements = new List<UniformElement>();
            List<string> uniformElementNames = new List<string>();
            List<string> textureNames = new List<string>();

            List<VertexElementDescription> vertexElements = new List<VertexElementDescription>();
            uint instanceStepRate = 0;

            string vertexShaderSrc = "", fragmentShaderSrc = "";
            string lastLine = "";

            int sectionIndex = -1;
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
                        if (lastLine.Equals("Properties"))
                            sectionIndex = 0;
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

                                if (line.StartsWith("@"))
                                {
                                    // Settings
                                    if (line.Contains("StepMode:"))
                                        instanceStepRate = 1;
                                }
                                else
                                {
                                    // Shader property
                                    string[] split = line.Split(':');

                                    string name = split[0];
                                    string type = split[1];

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
            dest.vertexLayout = new VertexLayoutDescription(vertexElements.ToArray());
            dest.vertexLayout.InstanceStepRate = instanceStepRate;

            // Shader propery Uniforms
            var shaderPropUniform = rsFactory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("ShaderProps", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));
            dest.resourceLayouts.Add(shaderPropUniform);

            // Texture Uniforms
            ResourceLayout texUniform = null;

            if (textureNames.Count > 0)
            {
                ResourceLayoutElementDescription[] layoutElements = new ResourceLayoutElementDescription[textureNames.Count * 2];
                int index = 0;
                foreach (var textureName in textureNames)
                {
                    dest.textureNames.Add(textureName, index / 2);
                    layoutElements[index] = new ResourceLayoutElementDescription(textureName, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
                    index++;
                    layoutElements[index] = new ResourceLayoutElementDescription(textureName + "Sampler", ResourceKind.Sampler, ShaderStages.Fragment);
                    index++;
                }
                texUniform = rsFactory.CreateResourceLayout(new ResourceLayoutDescription(layoutElements));
                dest.resourceLayouts.Add(texUniform);
            }

            // Shaders
            ShaderDescription vertexShader = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(vertexShaderSrc),
                "main");

            ShaderDescription fragmentShader = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragmentShaderSrc),
                "main");

            //if (dest.defaultMatName.Equals("ToonWater"))
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


            if (dest.shaderOptimised)
                dest.shaders = rsFactory.CreateFromSpirv(vertexShader, fragmentShader);
            else
            {
                dest.shaders = CompileShaderSet(vertexShader, fragmentShader);
            }

            dest.refMaterial = new PipelineMaterial(dest.defaultMatName.ToHash32(), dest, shaderPropUniform, texUniform);
            dest.refMaterial.name = dest.defaultMatName;

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

                dest.propNames.Add(uniformElementNames[shaderVals.Count], shaderVals.Count);
                shaderVals.Add(prop);
            }

            dest.refMaterial.SetShaderPropBuffer(shaderVals, vertBufferSize);
            dest.refMaterial.SetShaderTextureResources(textureNames);

            GraphicsManager.AddPipelineAsset(dest);
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

