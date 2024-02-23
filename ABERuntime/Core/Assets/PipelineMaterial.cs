using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Halak;
using WGIL;
using Buffer = WGIL.Buffer;
using ABEngine.ABERuntime.Rendering;

namespace ABEngine.ABERuntime.Core.Assets
{
    public class PipelineMaterial : Asset
    {
        public int instanceID;

        private BindGroupLayout propLayout;
        private BindGroupLayout texLayout;
        public PipelineAsset pipelineAsset;

        internal List<ShaderProp> shaderProps;
        internal List<uint> texHashes;

        public uint shaderPropBufferSize;

        private BindResource[] texResources;
        private Buffer propBuffer;

        public Dictionary<int, BindGroup> bindableSets = new Dictionary<int, BindGroup>();
        private BindGroup propSet;
        private BindGroup textureSet;
        public bool isLateRender = false;
        public int renderOrder { get; private set; }

        private byte[] shaderPropData;

        internal event Action<PipelineAsset> onPipelineChanged;

        public PipelineMaterial(uint hash, PipelineAsset pipelineAsset, BindGroupLayout propLayout, BindGroupLayout texLayout)
        {
            this.pipelineAsset = pipelineAsset;
            this.instanceID = GraphicsManager.GetPipelineMaterialCount();
            this.propLayout = propLayout;
            this.texLayout = texLayout;
            name = hash + "_" + instanceID;
            fPathHash = hash;

            GraphicsManager.AddPipelineMaterial(this);
            this.renderOrder = (int)pipelineAsset.renderOrder;
            //Console.WriteLine(this.instanceID);
        }

        public void SetRenderOrder(int renderOrder)
        {
            this.renderOrder = renderOrder;
            onPipelineChanged?.Invoke(this.pipelineAsset);
        }

        public void SetRenderOrder(RenderOrder renderOrder)
        {
            this.renderOrder = (int)renderOrder;
            onPipelineChanged?.Invoke(this.pipelineAsset);
        }

        internal void SetShaderPropBuffer(List<ShaderProp> shaderProps, uint bufferSize)
        {
            this.shaderProps = shaderProps;

            if (propLayout == null)
                return;

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

            propBuffer = Game.wgil.CreateBuffer((int)this.shaderPropBufferSize, BufferUsages.UNIFORM | BufferUsages.COPY_DST);

            BindGroupDescriptor propSetDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = this.propLayout,
                Entries = new[]
                {
                    propBuffer   
                }
            };

            propSet = Game.wgil.CreateBindGroup(ref propSetDesc);
            Game.wgil.WriteBuffer(propBuffer, this.shaderPropData, 0, this.shaderPropData.Length);
            bindableSets.Add(2, propSet);
        }

        internal void SetShaderTextureResources(List<string> textureNames)
        {
            texHashes = new List<uint>();
            foreach (var texName in textureNames) // Invalid Textures
                texHashes.Add(0);

            if (textureNames.Count > 0)
            {
                BindResource[] resources = new BindResource[textureNames.Count * 2];
                int index = 0;
                foreach (var textureName in textureNames)
                {
                    if (textureName.Equals("ScreenTex"))
                    {
                        isLateRender = true;
                        resources[index] = Game.resourceContext.mainPPView;
                    }
                    else if(textureName.Equals("DepthTex"))
                    {
                        isLateRender = true;
                        resources[index] = Game.normalsRenderSystem.GetDepthAttachment();
                    }
                    else if (textureName.Equals("CamNormalTex"))
                    {
                        isLateRender = true;
                        resources[index] = Game.normalsRenderSystem.GetMainColorAttachent();
                    }
                    else
                    {
                        resources[index] = AssetCache.GetDefaultTexture().GetView();
                    }

                    index++;
                    if (textureName.Equals("DepthTex"))
                        resources[index] = GraphicsManager.pointSamplerClamp;
                    else
                        resources[index] = GraphicsManager.linearSamplerWrap;
                    index++;
                }

                texResources = resources;
                if(textureSet != null)
                    textureSet.Dispose();

                var textureSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = texLayout,
                    Entries = texResources
                };

                textureSet = Game.wgil.CreateBindGroup(ref textureSetDesc);

                if(propLayout != null)
                    bindableSets.Add(3, textureSet);
                else
                    bindableSets.Add(2, textureSet);
            }
        }

        public void SetTexture(string textureName, Texture2D tex2d)
        {
            int texNameInd = pipelineAsset.GetTextureID(textureName);
            if(texNameInd > -1)
            {
                int texInd = texNameInd * 2;
                texResources[texInd] = tex2d.GetView();
                texResources[texInd + 1] = tex2d.textureSampler;
                texHashes[texNameInd] = tex2d.fPathHash;
                if(textureSet != null)
                    textureSet.Dispose();

                var textureSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = texLayout,
                    Entries = texResources
                };

                textureSet = Game.wgil.CreateBindGroup(ref textureSetDesc);

                if(propLayout != null)
                    bindableSets[3] = textureSet;
                else
                    bindableSets[2] = textureSet;
            }
        }

        internal TextureView GetRawTextureView(string textureName)
        {
            int texNameInd = pipelineAsset.GetTextureID(textureName);
            if (texNameInd > -1)
            {
                int texInd = texNameInd * 2;
                return texResources[texInd] as TextureView;
            }

            return AssetCache.GetDefaultTexture().GetView();
        }

        public PipelineMaterial GetCopy()
        {
            var matCopy = new PipelineMaterial(0, this.pipelineAsset, this.propLayout, this.texLayout);
            matCopy.SetShaderPropBuffer(this.shaderProps.ToList(), this.shaderPropBufferSize);
            matCopy.SetShaderTextureResources(this.pipelineAsset.GetTextureNames());
            matCopy.renderOrder = this.renderOrder;

            return matCopy;
        }

        internal PipelineMaterial GetCopy(uint hash)
        {
            var mat = GetCopy();
            mat.fPathHash = hash;
            return mat;
        }

        public void ChangePipeline(PipelineAsset pipeline)
        {
            //bool 
            //if((int)this.pipelineAsset.renderOrder == renderOrder)

            propBuffer?.Dispose();
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
                        texResources[index] = Game.resourceContext.mainPPView;
                    else if (textureName.Equals("DepthTex"))
                        texResources[index] = Game.normalsRenderSystem.GetDepthAttachment();
                    else if(textureName.Equals("CamNormalTex"))
                        texResources[index] = Game.normalsRenderSystem.GetMainColorAttachent();

                    index++;
                    index++;
                }

                if (textureSet != null)
                    textureSet.Dispose();

                var textureSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = texLayout,
                    Entries = texResources
                };

                textureSet = Game.wgil.CreateBindGroup(ref textureSetDesc);

                if (propLayout != null)
                    bindableSets[3] = textureSet;
                else
                    bindableSets[2] = textureSet;
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

            Game.wgil.WriteBuffer(propBuffer, this.shaderPropData);
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
