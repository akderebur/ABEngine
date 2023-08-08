using System.Numerics;
using Halak;
using Veldrid;

namespace ABEngine.ABERuntime.Components
{
    public struct QuadVertex
    {
        public const uint VertexSize = 76;

        public Vector3 Position;
        public Vector2 Scale;
        public Vector3 WorldScale;
        public Vector4 Tint;
        public float ZRotation;
        public Vector2 UvStart;
        public Vector2 UvScale;
        public Vector2 Pivot;

        public QuadVertex(Vector3 position, Vector2 scale, Vector3 worldScale) : this(position, scale, worldScale, RgbaFloat.White.ToVector4(), 0f, Vector2.Zero, Vector2.One, Vector2.Zero) { }
        public QuadVertex(Vector3 position, Vector2 scale, Vector3 worldScale, Vector4 tint, float zRotation, Vector2 uvStart, Vector2 uvScale, Vector2 pivot)
        {
            Position = position;
            Scale = scale;
            WorldScale = worldScale;
            Tint = tint;
            ZRotation = zRotation;
            UvStart = uvStart;
            UvScale = uvScale;
            Pivot = pivot; 
        }
    }

    public struct PipelineData
    {
        public Matrix4x4 Projection;
        public Matrix4x4 View;
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

        [JSerialize]
        public Vector4 tintColor { get; set; }
        public Vector2 size { get; set; }
        internal bool sizeSet = false;

        public Vector2 uvPos;
        public Vector2 uvScale = Vector2.One;
        public Vector2 flipScale = Vector2.One;
        public Vector2 pivot;

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
            tintColor = Vector4.One;
            this.texture = AssetCache.GetDefaultTexture();
            Resize(texture.imageSize);
        }

        public Sprite(Texture2D texture)
        {
            this.texture = texture;
            Resize(texture.imageSize);
            sharedMaterial = GraphicsManager.GetUberMaterial();
            _material = sharedMaterial;
            tintColor = Vector4.One;
        }


        public Sprite(Texture2D texture, Vector2 spriteSize) : base()
        {
            this.texture = texture;
            Resize(texture.spriteSize);
            this.uvScale = spriteSize / texture.imageSize;
            sizeSet = true;
            sharedMaterial = GraphicsManager.GetUberMaterial();
            _material = sharedMaterial;
            tintColor = Vector4.One;
        }


        public Sprite(Texture2D texture, Vector2 spriteSize, Vector2 spritePos) : base()
        {
            this.texture = texture;
            Resize(texture.spriteSize);
            this.SetUVPosScale(spritePos / texture.imageSize, spriteSize / texture.imageSize);
            sizeSet = true;
            sharedMaterial = GraphicsManager.GetUberMaterial();
            _material = sharedMaterial;
            tintColor = Vector4.One;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Texture", AssetCache.GetAssetSceneIndex(this.texture.fPathHash));
            jObj.Put("Material", AssetCache.GetAssetSceneIndex(this.sharedMaterial.fPathHash));
            jObj.Put("RenderLayerIndex", renderLayerIndex);
            jObj.Put("FlipX", flipX);
            jObj.Put("FlipY", flipY);
            jObj.Put("UVPosX", uvPos.X);
            jObj.Put("UVPosY", uvPos.Y);
            jObj.Put("UVScaX", uvScale.X);
            jObj.Put("UVScaY", uvScale.Y);
            jObj.Put("Pivot", pivot);

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            flipX = data["FlipX"];
            flipY = data["FlipY"];
            _renderLayerIndex = data["RenderLayerIndex"];

            int texSceneIndex = data["Texture"];
            int matSceneIndex = data["Material"];

            var tex2d = AssetCache.GetAssetFromSceneIndex(texSceneIndex) as Texture2D;
            var material = AssetCache.GetAssetFromSceneIndex(matSceneIndex) as PipelineMaterial;

            if (tex2d != null)
            {
                SetTexture(tex2d);


                SetUVPosScale(new Vector2(data["UVPosX"], data["UVPosY"]),
                              new Vector2(data["UVScaX"], data["UVScaY"]));


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

            pivot = data["Pivot"];


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


        public Vector2 GetSize()
        {
            return size * flipScale;
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
                pivot = pivot
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
