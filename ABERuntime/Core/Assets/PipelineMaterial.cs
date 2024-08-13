using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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

        private BindGroupLayout _propLayout;
        private BindGroupLayout _texLayout;
        public PipelineAsset pipelineAsset;

        internal List<ShaderProp> shaderProps;
        internal List<Texture2D> textures;

        public uint shaderPropBufferSize;

        private BindResource[] _texResources;
        private Buffer _propBuffer;

        public Dictionary<int, BindGroup> bindableSets = new();
        private BindGroup _propSet;
        private BindGroup _textureSet;
        public bool isLateRender { get; private set; }
        public int renderOrder { get; private set; }

        private byte[] _shaderPropData;

        internal event Action<PipelineAsset> OnPipelineChanged;

        public PipelineMaterial(PipelineAsset pipelineAsset, BindGroupLayout propLayout, BindGroupLayout texLayout)
        {
            this.pipelineAsset = pipelineAsset;
            this.instanceID = Graphics.GetPipelineMaterialCount();
            this._propLayout = propLayout;
            this._texLayout = texLayout;
            name = pipelineAsset.name + "_" + instanceID;

            Graphics.AddPipelineMaterial(this);
            this.renderOrder = (int)pipelineAsset.renderOrder;
            //Console.WriteLine(this.instanceID);
        }

        public void SetRenderOrder(int renderOrder)
        {
            this.renderOrder = renderOrder;
            OnPipelineChanged?.Invoke(this.pipelineAsset);
        }

        public void SetRenderOrder(RenderOrder renderOrder)
        {
            this.renderOrder = (int)renderOrder;
            OnPipelineChanged?.Invoke(this.pipelineAsset);
        }

        internal void SetShaderPropBuffer(List<ShaderProp> shaderProps, uint bufferSize)
        {
            this.shaderProps = shaderProps;

            if (_propLayout == null)
                return;

            this.shaderPropBufferSize = (uint)(MathF.Ceiling(bufferSize / 16f) * 16);
            this._shaderPropData = new byte[this.shaderPropBufferSize];

            unsafe
            {
                fixed (byte* dataPtr = _shaderPropData)
                {
                    byte* tempPtr = dataPtr;
                    foreach (var prop in shaderProps)
                    {
                        tempPtr = dataPtr + prop.Offset;
                        Unsafe.CopyBlock(tempPtr, prop.Bytes, prop.SizeInBytes);
                    }
                }
            }

            _propBuffer = Game.wgil.CreateBuffer((int)this.shaderPropBufferSize, BufferUsages.UNIFORM | BufferUsages.COPY_DST);

            BindGroupDescriptor propSetDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = this._propLayout,
                Entries = new[]
                {
                    _propBuffer
                }
            };

            _propSet = Game.wgil.CreateBindGroup(ref propSetDesc);
            Game.wgil.WriteBuffer(_propBuffer, this._shaderPropData, 0, this._shaderPropData.Length);
            bindableSets.Add(2, _propSet);
        }

        internal void SetShaderTextureResources(List<string> textureNames)
        {
            textures = new List<Texture2D>();
            Texture2D defTex = Assets.GetDefaultTexture();
            foreach (var texName in textureNames) // Invalid Textures
                textures.Add(null);

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
                        resources[index] = defTex.GetView();
                        textures[index] = defTex;
                    }

                    index++;
                    if (textureName.Equals("DepthTex"))
                        resources[index] = Graphics.pointSamplerClamp;
                    else
                        resources[index] = Graphics.linearSamplerWrap;
                    index++;
                }

                _texResources = resources;
                if(_textureSet != null)
                    _textureSet.Dispose();

                var textureSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = _texLayout,
                    Entries = _texResources
                };

                _textureSet = Game.wgil.CreateBindGroup(ref textureSetDesc);

                if(_propLayout != null)
                    bindableSets.Add(3, _textureSet);
                else
                    bindableSets.Add(2, _textureSet);
            }
        }

        public void SetTexture(string textureName, Texture2D tex2d)
        {
            int texNameInd = pipelineAsset.GetTextureID(textureName);
            if(texNameInd > -1)
            {
                int texInd = texNameInd * 2;
                _texResources[texInd] = tex2d.GetView();
                _texResources[texInd + 1] = tex2d.textureSampler;
                textures[texNameInd] = tex2d;
                if(_textureSet != null)
                    _textureSet.Dispose();

                var textureSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = _texLayout,
                    Entries = _texResources
                };

                _textureSet = Game.wgil.CreateBindGroup(ref textureSetDesc);

                if(_propLayout != null)
                    bindableSets[3] = _textureSet;
                else
                    bindableSets[2] = _textureSet;
            }
        }

        internal TextureView GetRawTextureView(string textureName)
        {
            int texNameInd = pipelineAsset.GetTextureID(textureName);
            if (texNameInd > -1)
            {
                int texInd = texNameInd * 2;
                return _texResources[texInd] as TextureView;
            }

            return Assets.GetDefaultTexture().GetView();
        }

        public PipelineMaterial GetCopy()
        {
            var matCopy = new PipelineMaterial(this.pipelineAsset, this._propLayout, this._texLayout);
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
            // Cache old data
            var oldProps = shaderProps;
            var oldTextures = textures;
            var oldPropNames = pipelineAsset.GetPropNames();
            var oldTexNames = pipelineAsset.GetTextureNames();

            _propBuffer?.Dispose();
            foreach (var resourceSet in bindableSets.Values)
                resourceSet.Dispose();
            bindableSets.Clear();

            var refMat = pipeline.refMaterial;

            this.pipelineAsset = pipeline;
            this._propLayout = refMat._propLayout;
            this._texLayout = refMat._texLayout;

            this.SetShaderPropBuffer(refMat.shaderProps.ToList(), refMat.shaderPropBufferSize);
            this.SetShaderTextureResources(pipeline.GetTextureNames());

            // Try setting old data
            for (int i = 0; i < oldPropNames.Count; i++)
            {
                this.SetVector4(oldPropNames[i], oldProps[i].Float4);
            }

            for (int i = 0; i < oldTexNames.Count; i++)
            {
                Texture2D tex = oldTextures[i];
                if(tex != null)
                    this.SetTexture(oldTexNames[i], tex);
            }

            OnPipelineChanged?.Invoke(pipeline);
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
                        _texResources[index] = Game.resourceContext.mainPPView;
                    else if (textureName.Equals("DepthTex"))
                        _texResources[index] = Game.normalsRenderSystem.GetDepthAttachment();
                    else if(textureName.Equals("CamNormalTex"))
                        _texResources[index] = Game.normalsRenderSystem.GetMainColorAttachent();

                    index++;
                    index++;
                }

                if (_textureSet != null)
                    _textureSet.Dispose();

                var textureSetDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = _texLayout,
                    Entries = _texResources
                };

                _textureSet = Game.wgil.CreateBindGroup(ref textureSetDesc);

                if (_propLayout != null)
                    bindableSets[3] = _textureSet;
                else
                    bindableSets[2] = _textureSet;
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
            fixed (byte* dataPtr = _shaderPropData)
            {
                byte* tempPtr = dataPtr;
                tempPtr = dataPtr + offset;
                Unsafe.CopyBlock(tempPtr, data, size);
            }

            Game.wgil.WriteBuffer(_propBuffer, this._shaderPropData);
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
