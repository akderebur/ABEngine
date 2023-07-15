using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;
using Halak;
using Veldrid.ImageSharp;

namespace ABEngine.ABERuntime
{
    public class SpriteClip : Asset
    {
        public string imgPath { get; set; }
        public string clipAssetPath { get; set; }
        //public string name { get; set; }
        public Texture2D texture2D { get; set; }
        public List<Vector2> uvPoses = new List<Vector2>();
        public List<Vector2> uvScales = new List<Vector2>();

        private float _sampleRate;
        public float sampleRate { get { return _sampleRate; } set { sampleFreq = 1f / value; clipLength = sampleFreq * frameCount; _sampleRate = value; } }
        public float sampleFreq { get; set; }
        public float clipLength { get; set; }
        public int frameCount { get; set; }

        public float frameWidth { get; set; }
        public float frameHeight { get; set; }

        /// <summary>
        /// Load a sprite sheet animation from json description
        /// </summary>
        /// <param name="jsonPath">Path to the json file on disk</param>
        internal SpriteClip(string jsonAssetPath)
        {
            //string folder = Path.GetFileName(Path.GetDirectoryName(jsonPath));
            clipAssetPath = jsonAssetPath;
            name = Path.GetFileNameWithoutExtension(jsonAssetPath);
            string jsonPath = Game.AssetPath + jsonAssetPath;
            string folder = Path.GetDirectoryName(jsonAssetPath);
            string json = File.ReadAllText(jsonPath);

            JValue data = JValue.Parse(json);
            JValue meta = data["meta"];
            imgPath = folder + "/" + meta["image"];

            JValue size = meta["size"];
            float width = size["w"];
            float height = size["h"];

            JValue frameSize = meta["cutSize"];
            frameWidth = frameSize["w"];
            frameHeight = frameSize["h"];

            int frameC = 0;
            foreach (var frameDesc in data["frames"].Array())
            {
                var frame = frameDesc["frame"];

                float frX = frame["x"];
                float frY = frame["y"];
                float frWidth = frame["w"];
                float frHeight = frame["h"];


                uvPoses.Add(new Vector2(frX / width, frY / height));
                uvScales.Add(new Vector2(frWidth / width, frHeight / height));
                frameC++;
            }

            InitClipParams(frameC);
        }

        internal SpriteClip(int id, Texture2D tex2d, List<Vector2> framePoses)
        {
            clipAssetPath = id.ToString();
            name = clipAssetPath;
            this.texture2D = tex2d;
            //imgPath = tex2d.imagePath;

            frameWidth = tex2d.spriteSize.X;
            frameHeight = tex2d.spriteSize.Y;

            foreach (Vector2 framePos in framePoses)
                uvPoses.Add(framePos / tex2d.imageSize);

            Vector2 spriteSize = tex2d.spriteSize != Vector2.Zero ? tex2d.spriteSize : tex2d.imageSize;
            Vector2 uvScale = spriteSize / tex2d.imageSize;
            foreach (var item in uvPoses)
                uvScales.Add(uvScale);

            InitClipParams(uvPoses.Count);
        }

        void InitClipParams(int frameC)
        {
            //sampleFreq = 1f / sampleRate;

            frameCount = frameC;
            sampleRate = 10f;
            //curFrame = -1;
            //lastFrameTime = 0f;
        }

        internal override JValue SerializeAsset()
        {
            throw new NotImplementedException();
        }

        //public SpriteClip(string imgPath, float frameRate)
        //{
        //    // Todo Remove HardCode

        //    sampleRate = frameRate;
        //    //sampleFreq = 1f / sampleRate;

        //    frameCount = 25;
        //    curFrame = -1;
        //    lastFrameTime = 0f;

        //    this.imgPath = imgPath;

        //    //var texData = new ImageSharpTexture(Game.AppPath + imgPath, false);
        //    var texData = AssetCache.GetImage(Game.AssetPath + imgPath, false);
        //    float width = texData.Width;
        //    float height = texData.Height;
        //    float step = width / (float)frameCount;

        //    for (int i = 0; i < frameCount; i++)
        //    {
        //        uvPoses.Add(new Vector2(step * i / width, 0));
        //        uvScales.Add(new Vector2(step / width, 1f));
        //    }
        //}
    }
}
