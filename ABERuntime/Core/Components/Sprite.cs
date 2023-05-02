using System.Numerics;
using Halak;
using ABEngine.ABERuntime.ECS;
using Veldrid;

namespace ABEngine.ABERuntime.Components
{
    public struct QuadVertex
    {
        public const uint VertexSize = 60;

        public Vector3 Position;
        public Vector3 Scale;
        public Vector4 Tint;
        public float ZRotation;
        public Vector2 UvStart;
        public Vector2 UvScale;

        public QuadVertex(Vector3 position, Vector3 scale) : this(position, scale, RgbaFloat.White.ToVector4(), 0f, Vector2.Zero, Vector2.One) { }
        public QuadVertex(Vector3 position, Vector3 scale, Vector4 tint, float zRotation, Vector2 uvStart, Vector2 uvScale)
        {
            Position = position;
            Scale = scale;
            Tint = tint;
            ZRotation = zRotation;
            UvStart = uvStart;
            UvScale = uvScale;
        }
    }

    public struct PipelineData
    {
        public Matrix4x4 VP;
        public Vector2 Resolution;
        public float Time;
        public float Padding;
    }

    public class Sprite : JSerializable
    {
        //private Vector2 defSize = new Vector2(100f, 100f);


        private bool _flipX;
        private bool _flipY;

        public bool flipX
        {
            get { return _flipX;  }
            set { _flipX = value; flipScale.X = value ? -1 : 1; }
        }
        public bool flipY
        {
            get { return _flipY; }
            set { _flipY = value; flipScale.Y = value ? -1 : 1;  }
        }

        public Vector4 tintColor = Vector4.One;
        public Vector2 size { get; set; }
        internal bool sizeSet = false;

        public Vector2 uvPos;
        public Vector2 uvScale = Vector2.One;
        public Vector3 flipScale = Vector3.One;

        internal bool manualBatching = false;

        private int _renderLayerIndex = 0;
        public int renderLayerIndex
        {
            get { return _renderLayerIndex; }
            set
            {
                if (value != _renderLayerIndex)
                {
                    int oldLayer = _renderLayerIndex;
                    _renderLayerIndex = value;

                    Game.spriteBatchSystem.UpdateSpriteBatch(this, oldLayer, texture, _material.instanceID);
                }
            }
        }

        bool isMatCopy = false;
        public PipelineMaterial _material;
        public PipelineMaterial material {
            get
            {
                if(!isMatCopy)
                {
                    int lastMatInsId = _material.instanceID;
                    _material = _material.GetCopy();
                    sharedMaterial = _material;
                    isMatCopy = true;

                    Game.spriteBatchSystem.UpdateSpriteBatch(this, renderLayerIndex, this.texture, lastMatInsId);
                }

                return _material;
            } set
            {
                int lastMatInsId = _material.instanceID;
                _material = value;
                sharedMaterial = value;
                Game.spriteBatchSystem.UpdateSpriteBatch(this, renderLayerIndex, this.texture, lastMatInsId);
            }
        }

        internal void SetMaterial(PipelineMaterial mat, bool updateBatch = true)
        {
            int lastMatInsId = _material.instanceID;
            _material = mat;
            sharedMaterial = mat;
            if(updateBatch)
                Game.spriteBatchSystem.UpdateSpriteBatch(this, renderLayerIndex, this.texture, lastMatInsId);
        }

        public PipelineMaterial sharedMaterial;
        public Transform transform;

        public Texture2D texture { get; private set; }

        public Sprite() : base()
        {
            sharedMaterial = GraphicsManager.GetUberMaterial();
            _material = sharedMaterial;
            this.texture = AssetCache.GetDefaultTexture();
        }

        public Sprite(Texture2D texture)
        {
            this.texture = texture;
            Resize(texture.imageSize);
            sharedMaterial = GraphicsManager.GetUberMaterial();
            _material = sharedMaterial;
        }


        public Sprite(Texture2D texture, Vector2 spriteSize) : base()
        {
            this.texture = texture;
            Resize(texture.spriteSize);
            this.uvScale = spriteSize / texture.imageSize;
            sizeSet = true;
            sharedMaterial = GraphicsManager.GetUberMaterial();
            _material = sharedMaterial;
        }


        public Sprite(Texture2D texture, Vector2 spriteSize, Vector2 spritePos) : base()
        {
            this.texture = texture;
            Resize(texture.spriteSize);
            this.SetUVPosScale(spritePos / texture.imageSize, spriteSize / texture.imageSize);
            sizeSet = true;
            sharedMaterial = GraphicsManager.GetUberMaterial();
            _material = sharedMaterial;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Texture", AssetCache.AddAssetDependency(this.texture.fPathHash));
            jObj.Put("Material", AssetCache.AddAssetDependency(this.sharedMaterial.fPathHash));
            jObj.Put("RenderLayerIndex", renderLayerIndex);
            jObj.Put("FlipX", flipX);
            jObj.Put("FlipY", flipY);
            jObj.Put("UVPosX", uvPos.X);
            jObj.Put("UVPosY", uvPos.Y);
            jObj.Put("UVScaX", uvScale.X);
            jObj.Put("UVScaY", uvScale.Y);

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            flipX = data["FlipX"];
            flipY = data["FlipY"];
            _renderLayerIndex = data["RenderLayerIndex"];

            uint texHash = (uint)(long)data["Texture"];
            uint matHash = (uint)(long)data["Material"];

            var tex2d = AssetCache.GetAssetFromHash(texHash) as Texture2D;
            var material = AssetCache.GetAssetFromHash(matHash) as PipelineMaterial;

            if (tex2d != null)
            {
                SetTexture(tex2d);

                for (int i = 0; i < tex2d.Length; i++)
                {
                    Vector2 samplePos = tex2d[i] / tex2d.imageSize;

                    if (samplePos.X >= uvPos.X && samplePos.Y >= uvPos.Y)
                    {
                        spriteID = i;
                        break;
                    }
                }
            }

            SetUVPosScale(new Vector2(data["UVPosX"], data["UVPosY"]),
                          new Vector2(data["UVScaX"], data["UVScaY"]));


            _material = material;
            sharedMaterial = material;
        }


        public void Resize(Vector2 size)
        {
            this.size = size.PixelToWorld();
            sizeSet = true;
        }

        public void SetTexture(Texture2D newTex)
        {
            if (this.texture == newTex)
                return;

            Texture2D oldTex = this.texture;
            this.texture = newTex;

            if (newTex.isSpriteSheet)
            {
                Resize(newTex.spriteSize);
                this.uvScale = newTex.spriteSize / newTex.imageSize;
            }
            else
            {
                Resize(newTex.imageSize);
                this.uvScale = Vector2.One;
            }

            this.uvPos = Vector2.Zero;

            this.sizeSet = true;

            if(!manualBatching)
                Game.spriteBatchSystem.UpdateSpriteBatch(this, renderLayerIndex, oldTex, _material.instanceID);
        }

        public void SetUVPosScale(Vector2 uvPos, Vector2 uvScale)
        {
            this.uvPos = uvPos;
            this.uvScale = uvScale;
        }

        public void SetUVPos(Vector2 uvPos)
        {
            this.uvPos = uvPos;
        }


        public Vector3 GetSize()
        {
            return new Vector3(size, 1f) * flipScale;
        }

        public void SetReferences()
        {
        }

        public void SetTransform(Transform transform)
        {
            this.transform = transform;
        }

        public JSerializable GetCopy()
        {
            Sprite copySprite = new Sprite()
            {
                // Material
                isMatCopy = this.isMatCopy,
                sharedMaterial = this.sharedMaterial,
                _material = this._material,

                // Sprite props
                size = this.size,
                sizeSet = this.sizeSet,
                flipX = this.flipX,
                flipY = this.flipY,
                _renderLayerIndex = this._renderLayerIndex,
                texture = this.texture,
                uvScale = this.uvScale,
                uvPos = this.uvPos,
                tintColor = this.tintColor,
            };

            return copySprite;
        }

        // Editor ease of use mainly
        // Texture change and Tilemaps
        int spriteID = 0;
        internal int GetSpriteID()
        {
            return spriteID;
        }

        internal void SetSpriteID(int spriteID)
        {
            if (spriteID < 0)
                return;

            Vector2 uvPos = texture[spriteID];
            this.uvPos = uvPos / texture.imageSize;
            this.uvScale = texture.spriteSize / texture.imageSize;
            this.spriteID = spriteID;
        }

        internal void SetUVIndent(float indent)
        {
            uvPos += new Vector2(indent, indent);
            uvScale -= new Vector2(indent, indent);
        }
    }
}
