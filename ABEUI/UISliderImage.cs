using System;
using System.Numerics;
using System.Text;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ImGuiNET;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABEUI
{
	struct SliderInfo
	{
        public const int BufferSize = 32;

        public float percentage;
        public float dummy;
        public Vector2 slideDir;
        public Vector4 color;
	}

	public class UISliderImage : UIComponent
	{
        private float _percentage;
        public float percentage
        {
            get
            {
                return _percentage;
           }
            set
            {
                _percentage = value;
                _sliderInfo.percentage = value;
                isUpdateNeeded = true;
            }
        }

        public Vector2 size { get; set; }
        public Vector2 slideDir { get; set; }

        public Vector2 uvPos { get; set; }
        public Vector2 uvScale { get; set; }

        public IntPtr imgPtr;

        Texture2D texture2d;
        SliderInfo _sliderInfo;


        // GPU Resources
        private Texture sliderTexture;
        private RenderPass _pass;

        private Buffer _infoBuffer;

        private BindGroup _sliderGroup;
        private bool isUpdateNeeded;

        // Shared
        static BindGroupLayout sliderInfoLayout;
        static RenderPipeline sliderPipeline;

        public UISliderImage(Texture2D texture)
		{
			this.texture2d = texture;
            this.size = texture.imageSize;
            this.slideDir = Vector2.UnitX;
            LoadGraphics();
            percentage = 100;
            uvScale = Vector2.One;
        }


        public UISliderImage(Texture2D texture, Vector2 slideDir)
        {
            this.texture2d = texture;
            this.size = texture.imageSize;
            this.slideDir = slideDir;
            LoadGraphics();
            percentage = 100;
            uvScale = Vector2.One;
        }

        public UISliderImage(Texture2D texture, Vector2 slideDir, Vector2 size)
        {
            this.texture2d = texture;
            this.size = size;
            this.slideDir = Vector2.UnitX;
            LoadGraphics();
            percentage = 100;
            uvScale = Vector2.One;
        }


        internal override void Render()
        {
            UISliderImage uiSliderImage = this;
            Transform imgTrans = base.transform;

            Vector2 screenPos = imgTrans.worldPosition.ToImGuiVector2();
            Vector2 endPos = screenPos;
            if (base.anchor != null) // Calculate anchor pos
            {
                endPos = UIRenderer.Instance.CalculateEndPos(base.anchor, imgTrans.worldPosition);
            }

            ImGui.SetCursorPos(endPos);
            ImGui.Image(uiSliderImage.imgPtr, uiSliderImage.size * imgTrans.localScale.ToVector2() * UIRenderer.Instance.screenScale);
        }

        private void OnPassRender(RenderPass pass)
        {
            pass.SetPipeline(sliderPipeline);
            pass.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
            pass.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.Uint16);
            pass.SetBindGroup(0, _sliderGroup);
            pass.DrawIndexed(6);
        }

        internal override void WGILRender()
        {
            if(isUpdateNeeded)
            {
                _pass.BeginPass();
                isUpdateNeeded = false;
            }
        }

        private void LoadGraphics()
        {
            // Pass
            var wgil = UIRenderer.Instance.GetWGIL();

            sliderTexture = wgil.CreateTexture((uint)texture2d.imageSize.X, (uint)texture2d.imageSize.Y,
                                               TextureFormat.Rgba8UnormSrgb, TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING);
            var sliderView = sliderTexture.CreateView();
            var sliderPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                ClearColor = new WGIL.Color(0f, 0f, 0f, 0f),
                ColorAttachments = new TextureViewSet
                {
                    TextureViews = new TextureView[]
                    {
                        sliderView
                    }
                }
            };
            _pass = wgil.CreateRenderPass(ref sliderPassDesc);
            _pass.JoinRenderQueue(OnPassRender);

            _infoBuffer = wgil.CreateBuffer(SliderInfo.BufferSize, BufferUsages.UNIFORM | BufferUsages.COPY_DST);

            var sliderGroupDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = sliderInfoLayout,
                Entries = new BindResource[]
                {
                    _infoBuffer,
                    texture2d.GetView(),
                    GraphicsManager.pointSamplerClamp

                }
            };
            _sliderGroup = wgil.CreateBindGroup(ref sliderGroupDesc);
        
            _sliderInfo = new SliderInfo
            {
                percentage = percentage,
                slideDir = slideDir,
                color = Vector4.One,
                dummy = 0f
            };

            wgil.WriteBuffer(_infoBuffer, _sliderInfo);

            imgPtr = UIRenderer.Instance.GetImGuiTextureBinding(sliderView);
        }

        internal static void InitSliderAssets()
        {
            var wgil = UIRenderer.Instance.GetWGIL();

            // Shaders
            var sliderLayoutDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new BindGroupLayoutEntry[]
                {
                    new BindGroupLayoutEntry
                    {
                        ShaderStages = ShaderStages.FRAGMENT,
                        BindingType = BindingType.Buffer
                    },
                    new BindGroupLayoutEntry
                    {
                        ShaderStages = ShaderStages.FRAGMENT,
                        BindingType = BindingType.Texture
                    },
                    new BindGroupLayoutEntry
                    {
                        ShaderStages = ShaderStages.FRAGMENT,
                        BindingType = BindingType.Sampler
                    }
                }
            };

            sliderInfoLayout = wgil.CreateBindGroupLayout(ref sliderLayoutDesc);

            var sliderPipeDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[]
                {
                    BlendState.OverrideBlend
                },
                PrimitiveState = new PrimitiveState()
                {
                    PolygonMode = PolygonMode.Fill,
                    Topology = PrimitiveTopology.TriangleList,
                    FrontFace = FrontFace.Cw,
                    CullFace = CullFace.None
                },
                BindGroupLayouts = new BindGroupLayout[] { sliderInfoLayout },
                VertexLayouts = new[] { GraphicsManager.fullScreenVertexLayout },
                AttachmentDescription = new AttachmentDescription()
                {
                    ColorFormats = new TextureFormat[] { TextureFormat.Rgba8UnormSrgb }
                }
            };

            sliderPipeline = wgil.CreateRenderPipeline(ShadersUI.SliderVertex, ShadersUI.SliderFragment, ref sliderPipeDesc);
        }

        internal static void DisposeResources()
        {
            sliderPipeline.Dispose();
            sliderInfoLayout.Dispose();
        }
	}
}

