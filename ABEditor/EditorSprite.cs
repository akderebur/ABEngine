using System;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using Veldrid;

namespace ABEngine.ABEditor
{
    public struct DrawData
    {
        public Matrix4x4 VP;
    }

    public class EditorSprite : Sprite
    {
        public Texture frameTex;
        public TextureView frameView;
        public Framebuffer spriteFB;

        private TextureView spriteTexView;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _drawBuffer;
        private ResourceSet _drawSet;
        private ResourceSet _texSet;
        private Pipeline _pipeline;

        bool disposed = false;

        DrawData drawData;

        Vector3 drawPos;

        public EditorSprite(Texture2D tex, ResourceFactory rs) : base(tex)
        {
            // Framebuffer
            frameTex = rs.CreateTexture(TextureDescription.Texture2D(
               100, 100, 1, 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));
            frameView = rs.CreateTextureView(frameTex);
            Texture offscreenDepth = rs.CreateTexture(TextureDescription.Texture2D(
                100, 100, 1, 1, PixelFormat.R16_UNorm, TextureUsage.DepthStencil));
            spriteFB = rs.CreateFramebuffer(new FramebufferDescription(offscreenDepth, frameTex));
            _pipeline = GraphicsManager.GetOrCreateEditorSpritePipeline(spriteFB);

            // Draw Set
            drawData = new DrawData()
            {
                VP = Matrix4x4.CreateOrthographicOffCenter(0, 1, 0, 1, 0, 1)
            };
            _drawBuffer = rs.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _drawSet = rs.CreateResourceSet(new ResourceSetDescription(GraphicsManager.SpriteLayouts.Item1, _drawBuffer));
            GraphicsManager.gd.UpdateBuffer(_drawBuffer, 0, drawData);

            // Texture Set
           // spriteTexView = base.GetTextureView();
            _texSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(
                GraphicsManager.SpriteLayouts.Item2,
                tex.texture,
                GraphicsManager.linearSampleClamp));

            // Vertex Buffer
            base.Resize(new Vector2(100, 100));
            Vector2 size = base.GetSize();
            drawPos = new Vector3(size.X / 2f, size.Y / 2f, 0f);

            QuadVertex quad = new QuadVertex(drawPos, size, Vector3.Zero);
            _vertexBuffer = GraphicsManager.rf.CreateBuffer(new BufferDescription(QuadVertex.VertexSize, BufferUsage.VertexBuffer));
            GraphicsManager.gd.UpdateBuffer(_vertexBuffer, 0, quad);
           
        }

        public void DrawEditor()
        {
            //ort.projection = Matrix4x4.CreateOrthographicOffCenter(0, 100, 0, 100, 0, 1);
            //ort.model = Matrix4x4.Identity;

            if (disposed)
                return;

            var newVert = new QuadVertex(drawPos,
                                            base.GetSize(),
                                            Vector3.Zero,
                                            RgbaFloat.White.ToVector4(),
                                            0f,
                                            base.uvPos,
                                            base.uvScale,
                                            base.pivot);
            GraphicsManager.gd.UpdateBuffer(_vertexBuffer, 0, newVert);


            GraphicsManager.cl.SetPipeline(_pipeline);
            GraphicsManager.cl.SetGraphicsResourceSet(0, _drawSet);
            GraphicsManager.cl.SetGraphicsResourceSet(1, _texSet);
            GraphicsManager.cl.SetVertexBuffer(0, _vertexBuffer);
            GraphicsManager.cl.Draw(6, 1, 0, 0);
        }

        public void DestroyResources()
        {
            frameTex.Dispose();
            frameView.Dispose();
            spriteFB.Dispose();
            _drawBuffer.Dispose();
            _drawSet.Dispose();
            _vertexBuffer.Dispose();

            disposed = true;
        }
    }
}
