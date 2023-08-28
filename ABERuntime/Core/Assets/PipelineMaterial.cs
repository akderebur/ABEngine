using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Veldrid;
using System.Runtime.CompilerServices;
using Halak;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime
{
    public class PipelineMaterial : Asset
    {
        public int instanceID;

        private ResourceLayout propLayout;
        private ResourceLayout texLayout;
        public PipelineAsset pipelineAsset;

        internal List<ShaderProp> shaderProps;
        internal List<uint> texHashes;

        public uint shaderPropBufferSize;

        private BindableResource[] texResources;
        private DeviceBuffer propBuffer;

        public Dictionary<uint, ResourceSet> bindableSets = new Dictionary<uint, ResourceSet>();
        private ResourceSet propSet;
        private ResourceSet textureSet;
        public bool isLateRender = false;

        private byte[] shaderPropData;

        internal event Action<PipelineAsset> onPipelineChanged;

        public PipelineMaterial(uint hash, PipelineAsset pipelineAsset, ResourceLayout propLayout, ResourceLayout texLayout)
        {
            this.pipelineAsset = pipelineAsset;
            this.instanceID = GraphicsManager.GetPipelineMaterialCount();
            this.propLayout = propLayout;
            this.texLayout = texLayout;
            name = hash + "_" + instanceID;
            fPathHash = hash;

            GraphicsManager.AddPipelineMaterial(this);
            //Console.WriteLine(this.instanceID);
        }

        internal void SetShaderPropBuffer(List<ShaderProp> shaderProps, uint bufferSize)
        {
            this.shaderProps = shaderProps;
            this.shaderPropBufferSize = (uint)(MathF.Ceiling(bufferSize / 16f) * 16);
            this.shaderPropData = new byte[this.shaderPropBufferSize];

            unsafe
            {
                fixed (byte* dataPtr = shaderPropData)
                {
                    byte* tempPtr = dataPtr;
                    foreach (var prop in shaderProps)
                    {
                        tempPtr = dataPtr + prop.Offset;
                        Unsafe.CopyBlock(tempPtr, prop.Bytes, prop.SizeInBytes);
                    }
                }
            }

            propBuffer = GraphicsManager.rf.CreateBuffer(new BufferDescription(this.shaderPropBufferSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            propSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(this.propLayout, propBuffer));

            GraphicsManager.gd.UpdateBuffer(propBuffer, 0, this.shaderPropData);

            bindableSets.Add(2, propSet);
        }

        internal void SetShaderTextureResources(List<string> textureNames)
        {
            texHashes = new List<uint>();
            foreach (var texName in textureNames) // Invalid Textures
                texHashes.Add(0);

            if (textureNames.Count > 0)
            {
                BindableResource[] resources = new BindableResource[textureNames.Count * 2];
                int index = 0;
                foreach (var textureName in textureNames)
                {
                    if (textureName.Equals("ScreenTex"))
                    {
                        isLateRender = true;
                        resources[index] = Game.compositeRenderTexture;
                    }
                    else if(textureName.Equals("DepthTex"))
                    {
                        isLateRender = true;
                        resources[index] = Game.msaaResolveSystem.GetDepthAttachment();
                    }
                    else if (textureName.Equals("CamNormalTex"))
                    {
                        isLateRender = true;
                        resources[index] = Game.normalsRenderSystem.GetMainColorAttachent();
                    }
                    else
                    {
                        resources[index] = AssetCache.GetDefaultTexture().texture;
                    }

                    index++;
                    resources[index] = GraphicsManager.linearSamplerWrap;
                    index++;
                }

                texResources = resources;
                if(textureSet != null)
                    textureSet.Dispose();
                textureSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(
                    this.texLayout,
                    texResources
                    ));
               
                bindableSets.Add(3, textureSet);
            }
            
        }

        public void SetTexture(string textureName, Texture2D tex2d)
        {
            int texNameInd = pipelineAsset.GetTextureID(textureName);
            if(texNameInd > -1)
            {
                int texInd = texNameInd * 2;
                texResources[texInd] = tex2d.texture;
                texHashes[texNameInd] = tex2d.fPathHash;
                if(textureSet != null)
                    textureSet.Dispose();
                textureSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(
                   this.texLayout,
                   texResources
                   ));

                bindableSets[3] = textureSet;
            }
        }

        internal Texture GetRawTexture(string textureName)
        {
            int texNameInd = pipelineAsset.GetTextureID(textureName);
            if (texNameInd > -1)
            {
                int texInd = texNameInd * 2;
                return texResources[texInd] as Texture;
            }

            return null;
        }

        public PipelineMaterial GetCopy()
        {
            var matCopy = new PipelineMaterial(0, this.pipelineAsset, this.propLayout, this.texLayout);
            matCopy.SetShaderPropBuffer(this.shaderProps.ToList(), this.shaderPropBufferSize);
            matCopy.SetShaderTextureResources(this.pipelineAsset.GetTextureNames());

            return matCopy;
        }

        public void ChangePipeline(PipelineAsset pipeline)
        {
            propBuffer.Dispose();
            foreach (var resourceSet in bindableSets.Values)
                resourceSet.Dispose();
            bindableSets.Clear();

            var refMat = pipeline.refMaterial;

            this.pipelineAsset = pipeline;
            this.propLayout = refMat.propLayout;
            this.texLayout = refMat.texLayout;

            this.SetShaderPropBuffer(refMat.shaderProps.ToList(), refMat.shaderPropBufferSize);
            this.SetShaderTextureResources(pipeline.GetTextureNames());

            onPipelineChanged?.Invoke(pipeline);
        }

        internal void UpdateSampledTextures()
        {
            if (isLateRender)
            {
                var textureNames = pipelineAsset.GetTextureNames();
                int index = 0;
                foreach (var textureName in textureNames)
                {
                    if (textureName.Equals("ScreenTex"))
                        texResources[index] = Game.compositeRenderTexture;
                    else if (textureName.Equals("DepthTex"))
                        texResources[index] = Game.msaaResolveSystem.GetDepthAttachment();
                    else if(textureName.Equals("CamNormalTex"))
                        texResources[index] = Game.normalsRenderSystem.GetMainColorAttachent();

                    index++;
                    index++;
                }

                if (textureSet != null)
                    textureSet.Dispose();
                textureSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(
                   this.texLayout,
                   texResources
                   ));

                bindableSets[3] = textureSet;
            }
        }

        public void SetFloat(string propName, float value)
        {
            SetVector4(propName, Vector4.UnitX * value);
        }

        public void SetVector2(string propName, Vector2 value)
        {
            SetVector4(propName, new Vector4(value, 0f, 0f));
        }

        public void SetVector3(string propName, Vector3 value)
        {
            SetVector4(propName, new Vector4(value, 0f));
        }

        public void SetVector4(string propName, Vector4 value)
        {
            int propInd = pipelineAsset.GetPropID(propName);
            if (propInd > -1)
            {
                ShaderProp prop = shaderProps[propInd];
                prop.SetValue(value);
                shaderProps[propInd] = prop;
                unsafe
                {
                    UpdatePropBuffer(prop.Offset, prop.Bytes, prop.SizeInBytes);
                }
            }
        }

        private unsafe void UpdatePropBuffer(int offset, byte* data, uint size)
        {
            fixed (byte* dataPtr = shaderPropData)
            {
                byte* tempPtr = dataPtr;
                tempPtr = dataPtr + offset;
                Unsafe.CopyBlock(tempPtr, data, size);
            }

            GraphicsManager.gd.UpdateBuffer(propBuffer, 0, this.shaderPropData);
        }

        internal override JValue SerializeAsset()
        {
            JsonObjectBuilder assetEnt = new JsonObjectBuilder(200);
            assetEnt.Put("TypeID", 1);
            assetEnt.Put("FileHash", (long)fPathHash);
            return assetEnt.Build();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ShaderProp
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        [FieldOffset(0)]
        public fixed byte Bytes[24];

        [FieldOffset(0)] public float Float1;
        [FieldOffset(0)] public Vector2 Float2;
        [FieldOffset(0)] public Vector3 Float3;
        [FieldOffset(0)] public Vector4 Float4;
        [FieldOffset(16)] public uint SizeInBytes;
        [FieldOffset(20)] public int Offset;

        public void SetValue(float floatVal)
        {
            Float1 = floatVal;
        }

        public void SetValue(Vector2 vec2)
        {
            Float2 = vec2;
        }

        public void SetValue(Vector3 vec3)
        {
            Float3 = vec3;
        }

        public void SetValue(Vector4 vec4)
        {
            Float4 = vec4;
        }
    }
}
