using System;
using System.Numerics;
using System.Text;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ImGuiNET;
using SixLabors.ImageSharp.ColorSpaces;
using Veldrid;
using Veldrid.SPIRV;
using Vulkan;

namespace ABEngine.ABEUI
{
	struct SliderInfo
	{
        public const uint BufferSize = 32;

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
                UpdateGraphics();
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
        private Framebuffer _sliderFB;
        private Pipeline _sliderPipeline;

        private DeviceBuffer _infoBuffer;

        private ResourceSet _texSet;
        private ResourceSet _sliderInfoSet;
        private CommandList _sliderCL;

        // Static shared
        static Shader[] _shaders;
        static DeviceBuffer sliderVB;
        static DeviceBuffer sliderIB;
        static ResourceLayout sliderInfoLayout;


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


        private void UpdateGraphics()
        {
            GraphicsManager.gd.UpdateBuffer(_infoBuffer, 0, _sliderInfo);

            _sliderCL.Begin();

            _sliderCL.SetFramebuffer(_sliderFB);
            _sliderCL.SetFullViewports();
            _sliderCL.ClearColorTarget(0, RgbaFloat.Black);
            _sliderCL.SetPipeline(_sliderPipeline);

            _sliderCL.SetGraphicsResourceSet(0, _sliderInfoSet);
            _sliderCL.SetGraphicsResourceSet(1, _texSet);

            _sliderCL.SetVertexBuffer(0, sliderVB);
            _sliderCL.SetIndexBuffer(sliderIB, IndexFormat.UInt16);
            _sliderCL.DrawIndexed(6, 1, 0, 0, 0);

            _sliderCL.End();
            GraphicsManager.gd.SubmitCommands(_sliderCL);
        }


        private void LoadGraphics()
        {
            // Framebuffer
            sliderTexture = GraphicsManager.rf.CreateTexture(TextureDescription.Texture2D(
               (uint)texture2d.imageSize.X, (uint)texture2d.imageSize.Y, 1, 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));
            _sliderFB = GraphicsManager.rf.CreateFramebuffer(new FramebufferDescription(null, sliderTexture));

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
                  _shaders),
              new ResourceLayout[] { sliderInfoLayout, GraphicsManager.sharedTextureLayout },
              _sliderFB.OutputDescription);
            _sliderPipeline = GraphicsManager.rf.CreateGraphicsPipeline(ref pd);


            _texSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(
                   GraphicsManager.sharedTextureLayout,
                   texture2d.texture, GraphicsManager.linearSampleClamp
                   ));

            _sliderCL = GraphicsManager.rf.CreateCommandList();

            _sliderInfo = new SliderInfo
            {
                percentage = percentage,
                slideDir = slideDir,
                color = Vector4.One,
                dummy = 0f
            };

            _infoBuffer = GraphicsManager.rf.CreateBuffer(new BufferDescription(SliderInfo.BufferSize, BufferUsage.UniformBuffer));
            _sliderInfoSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(sliderInfoLayout, _infoBuffer));

            GraphicsManager.gd.UpdateBuffer(_infoBuffer, 0, _sliderInfo);

            imgPtr = UIRenderer.Instance.GetImGuiTextureBinding(sliderTexture);
        }

		internal static void InitSliderAssets()
		{
            // Shaders
            ShaderDescription vertexShader = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(ShadersUI.SliderVertex),
                "main");

            ShaderDescription fragmentShader = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(ShadersUI.SliderFragment),
                "main");

            _shaders = GraphicsManager.gd.ResourceFactory.CreateFromSpirv(vertexShader, fragmentShader);

            float[] verts = new float[]
             {
                        -1, 1, 0, 0,
                        1, 1, 1, 0,
                        1, -1, 1, 1,
                        -1, -1, 0, 1
             };
            ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };

            sliderVB = GraphicsManager.gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)verts.Length * sizeof(float), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            GraphicsManager.gd.UpdateBuffer(sliderVB, 0, verts);

            sliderIB = GraphicsManager.gd.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)s_quadIndices.Length * sizeof(float), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            GraphicsManager.gd.UpdateBuffer(sliderIB, 0, s_quadIndices);

            sliderInfoLayout = GraphicsManager.gd.ResourceFactory.CreateResourceLayout(
              new ResourceLayoutDescription(
                  new ResourceLayoutElementDescription("SliderInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

        }

        internal static void DisposeResources()
        {
            foreach (var shader in _shaders)
            {
                shader.Dispose();
            }

            sliderVB.Dispose();
            sliderIB.Dispose();
            sliderInfoLayout.Dispose();
        }
	}
}

