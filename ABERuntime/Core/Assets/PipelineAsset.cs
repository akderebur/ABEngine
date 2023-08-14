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
        protected Framebuffer framebuffer;

        internal bool clearColor = true;
        internal bool clearDepth = true;

        // Description
        protected Shader[] shaders;
        protected List<ResourceLayout> resourceLayouts;

        Dictionary<string, int> propNames;
        Dictionary<string, int> textureNames;
        internal PipelineMaterial refMaterial;

        protected bool shaderOptimised;
        public int pipelineID;

        static int pipelineCount = 0;

        public PipelineAsset(Framebuffer fb, bool clearColor, bool clearDepth)
        {
            gd = GraphicsManager.gd;
            cl = GraphicsManager.cl;
            rf = GraphicsManager.rf;

            this.clearColor = clearColor;
            this.clearDepth = clearDepth;

            framebuffer = fb;

            pipelineID = pipelineCount;
            pipelineCount++;

            resourceLayouts = new List<ResourceLayout>();
            propNames = new Dictionary<string, int>();
            textureNames = new Dictionary<string, int>();
            defaultMatName = "NoName";
        }

        internal void UpdateFramebuffer(Framebuffer fb)
        {
            this.framebuffer = fb;
        }

        public virtual void BindPipeline()
        {
            cl.SetFramebuffer(framebuffer);
            cl.SetFullViewports();
            cl.SetPipeline(pipeline);

            if(clearColor)
                cl.ClearColorTarget(0, new RgbaFloat(0f,0,0,0));
            if (clearDepth)
                cl.ClearDepthStencil(1f);
        }

        public static void ParseAsset(string pipelineAsset, PipelineAsset dest)
        {
            var rsFactory = GraphicsManager.rf;

            StringReader sr = new StringReader(pipelineAsset);

            List<UniformElement> uniformElements = new List<UniformElement>();
            List<string> uniformElementNames = new List<string>();
            List<string> textureNames = new List<string>();

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
                            else if (sectionIndex == 1)
                            {
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

            // Shader propery Uniforms
            var shaderPropUniform = rsFactory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("ShaderProps", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
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

            //if (dest.defaultMatName.Equals("Uber3D"))
            //{
            //    SpecializationConstant[] specializations =
            //    {

            //    };
            //    VertexFragmentCompilationResult result = SpirvCompilation.CompileVertexFragment(
            //        vertexShader.ShaderBytes,
            //        fragmentShader.ShaderBytes,
            //        CrossCompileTarget.MSL,
            //        new CrossCompileOptions(false, false, specializations));

            //    File.WriteAllText(@"/Users/akderebur/Documents/vert.txt", result.VertexShader);
            //    File.WriteAllText(@"/Users/akderebur/Documents/frag.txt", result.FragmentShader);
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

