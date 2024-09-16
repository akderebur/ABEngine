using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Systems;
using Friflo.Engine.ECS;
using Halak;

namespace ABEngine.ABERuntime.Components
{
    public struct QuadVertex
    {
        public const uint VertexSize = 52;

        public Vector2 Pivot;
        public Vector2 Scale;
        public Vector4 Tint;
        public Vector2 UvStart;
        public Vector2 UvScale;
        public int TransformID;

        //public QuadVertex(Vector3 position, Vector2 scale, Vector3 worldScale) : this(position, scale, worldScale, Vector4.One, 0f, Vector2.Zero, Vector2.One, Vector2.Zero) { }
        public QuadVertex(Vector2 pivot, Vector2 scale, Vector4 tint, Vector2 uvStart, Vector2 uvScale, int transformID)
        {
            Pivot = pivot; 
            Scale = scale;
            Tint = tint;
            UvStart = uvStart;
            UvScale = uvScale;
            TransformID = transformID;
        }
    }

    public struct Sprite : IComponent, JSerializable
    {
        private bool _flipX;
        private bool _flipY;

        public bool flipX
        {
            get => _flipX;
            set { _flipX = value; flipScale.X = value ? -1 : 1; }
        }
        public bool flipY
        {
            get => _flipY;
            set { _flipY = value; flipScale.Y = value ? -1 : 1;  }
        }

        public Vector4 tintColor { get; set; }
        public Vector2 size { get; set; }
        internal bool sizeSet = false;

        public Vector2 uvPos;
        public Vector2 uvScale = Vector2.One;
        public Vector2 flipScale = Vector2.One;
        public Vector2 pivot;

        // Batching
        internal bool manualBatching = false;
        private int _renderLayerIndex = 0;
        public int renderLayerIndex
        {
            get => _renderLayerIndex;
            set
            {
                if (value != _renderLayerIndex)
                {
                    int oldLayer = _renderLayerIndex;
                    _renderLayerIndex = value;
                    batch = null;
                }
            }
        }
        
        bool _isMatCopy = false;
        private PipelineMaterial _material;
        public PipelineMaterial material {
            get
            {
                if(!_isMatCopy)
                {
                    int lastMatInsId = _material.instanceID;
                    _material = _material.GetCopy();
                    sharedMaterial = _material;
                    _isMatCopy = true;
                    batch = null;
                }

                return _material;
            } set
            {
                int lastMatInsId = _material.instanceID;
                _material = value;
                sharedMaterial = value;
                batch = null;
            }
        }

        internal void SetMaterial(PipelineMaterial mat, bool updateBatch = true)
        {
            int lastMatInsId = _material.instanceID;
            _material = mat;
            sharedMaterial = mat;
            if (updateBatch)
                batch = null;
        }

        public PipelineMaterial sharedMaterial;

        public Texture2D texture { get; private set; }
        public Texture2D normalTexture { get; set; }
        internal SpriteBatch batch;
        public BVHGroup bvhGroup;

        public Sprite()
        {
            sharedMaterial = Graphics.GetUberMaterial();
            _material = sharedMaterial;
            tintColor = Vector4.One;
            this.texture = Assets.GetDefaultTexture();
            _flipX = false;
            _flipY = false;
            uvPos = default;
            pivot = default;
            size = default;
            normalTexture = Assets.GetDefaultTexture();
            batch = null;
            bvhGroup = BVHSystem.GetDefaultBVHGroup();
            
            Resize(texture.imageSize);
        }

        public Sprite(Texture2D texture) : this()
        {
            this.texture = texture;
            Resize(texture.imageSize);
            sharedMaterial = Graphics.GetUberMaterial();
            _material = sharedMaterial;
            tintColor = Vector4.One;
        }


        public Sprite(Texture2D texture, Vector2 spriteSize) : this()
        {
            this.texture = texture;
            Resize(texture.spriteSize);
            this.uvScale = spriteSize / texture.imageSize;
            sizeSet = true;
            sharedMaterial = Graphics.GetUberMaterial();
            _material = sharedMaterial;
            tintColor = Vector4.One;
        }


        public Sprite(Texture2D texture, Vector2 spriteSize, Vector2 spritePos) : this()
        {
            this.texture = texture;
            Resize(texture.spriteSize);
            this.SetUVPosScale(spritePos / texture.imageSize, spriteSize / texture.imageSize);
            sizeSet = true;
            sharedMaterial = Graphics.GetUberMaterial();
            _material = sharedMaterial;
            tintColor = Vector4.One;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Texture", Assets.GetAssetSceneIndex(this.texture.fPathHash));
            jObj.Put("Material", Assets.GetAssetSceneIndex(this.sharedMaterial.fPathHash));
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

            var tex2d = Assets.GetAssetFromSceneIndex(texSceneIndex) as Texture2D;
            var material = Assets.GetAssetFromSceneIndex(matSceneIndex) as PipelineMaterial;

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

            if (!manualBatching)
                batch = null;
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
        
        public JSerializable GetCopy()
        {
            Sprite copySprite = new Sprite()
            {
                // Material
                _isMatCopy = this._isMatCopy,
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
