using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Numerics;
using System.Xml.Linq;
using ABEngine.ABERuntime.Core.Assets;
using Box2D.NetStandard.Common;
using Halak;
using Veldrid;

namespace ABEngine.ABERuntime 
{
	public class Texture2D : Asset
	{
		public Sampler textureSampler { get; set; }
        public Texture texture { get; set; }
        //public string folderPath { get; set; }
        //public string imagePath { get; set; }
        public Vector2 imageSize { get; set; }
        public Vector2 spriteSize { get; set; }

        public bool isSpriteSheet { get; set; }
        public int rowCount { get; set; }
        public int colCount { get; set; }

        public int Length
        {
            get
            {
                return rowCount * colCount;
            }
        }

        public int textureID;
        static int texInitC = 0;


        internal Texture2D(uint hash, Texture texture, Sampler sampler, Vector2 spriteSize)
		{
            textureID = texInitC++;
            this.texture = texture;
            imageSize = new Vector2(texture.Width, texture.Height);

            fPathHash = hash;
			this.textureSampler = sampler;
            this.spriteSize = spriteSize;
            if (spriteSize != Vector2.Zero)
            {
                isSpriteSheet = true;

                this.colCount = (int)(imageSize.X / spriteSize.X);
                this.rowCount = (int)(imageSize.Y / spriteSize.Y);
            }
		}

        internal void RetileTexture(Vector2 spriteSize)
        {
            this.spriteSize = spriteSize;
            if (spriteSize != Vector2.Zero)
            {
                isSpriteSheet = true;

                this.colCount = (int)(imageSize.X / spriteSize.X);
                this.rowCount = (int)(imageSize.Y / spriteSize.Y);
            }
        }

        // Indexer for sprite sheets
        public Vector2 this[int spriteID]
        {
            get
            {
                if (spriteID >= Length || spriteID < 0)
                    return Vector2.Zero;

                int curCol = spriteID % colCount;
                int curRow = spriteID / colCount;

                float xPos = spriteSize.X * curCol;
                float yPos = spriteSize.Y * curRow;

                return new Vector2(xPos, yPos);
            }
        }


        public Vector2 this[int row, int column]
        {
            get
            {
                float xPos = spriteSize.X * column;
                float yPos = spriteSize.Y * row;

                if (xPos > imageSize.X || yPos > imageSize.Y)
                    return Vector2.Zero;

                return new Vector2(xPos, yPos);
            }
        }

        public List<Vector2> Slice(int start, int length)
        {
            List<Vector2> frames = new List<Vector2>();

            for (int i = start; i < start + length; i++)
            {
                int curCol = i % colCount;
                int curRow = i / colCount;

                float xPos = spriteSize.X * curCol;
                float yPos = spriteSize.Y * curRow;

                frames.Add(new Vector2(xPos, yPos));
            }


            return frames;
        }

        internal override JValue SerializeAsset()
        {
            JsonObjectBuilder assetEnt = new JsonObjectBuilder(200);
            assetEnt.Put("TypeID", 0);
            assetEnt.Put("FileHash", (long)fPathHash);
            assetEnt.Put("Sampler", textureSampler.Name);
            assetEnt.Put("SpriteSizeX", spriteSize.X);
            assetEnt.Put("SpriteSizeY", spriteSize.Y);
            return assetEnt.Build();
        }
    }
}

