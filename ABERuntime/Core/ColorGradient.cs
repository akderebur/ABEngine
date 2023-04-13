using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.ECS;
using Halak;
using Veldrid;

namespace ABEngine.ABERuntime
{
	public class ColorGradient : JSerializable
	{
        //public SortedDictionary<float, Vector3> colors = new SortedDictionary<float, Vector3>();
        //      public SortedDictionary<float, float> alphas = new SortedDictionary<float, float>();

        public List<ColorKey> colorKeys = new List<ColorKey>();
        public List<AlphaKey> alphaKeys = new List<AlphaKey>();

        public ColorGradient()
		{
		}

        public Vector4 Evaluate(float x)
        {
            return new Vector4(GetColor(x), GetAlpha(x));
            //return new Vector4(GetColor(x), GetAlpha(x));
        }

        public Vector3 GetColor(float normalizedLifetime)
        {
            if (colorKeys.Count < 1)
                return Vector3.One;

            if (normalizedLifetime <= colorKeys[0].Time)
            {
                return colorKeys[0].Color;
            }

            if (normalizedLifetime >= colorKeys[colorKeys.Count - 1].Time)
            {
                return colorKeys[colorKeys.Count - 1].Color;
            }

            ColorKey lowerKey = colorKeys[0];
            ColorKey upperKey = colorKeys[1];

            for (int i = 1; i < colorKeys.Count; i++)
            {
                if (colorKeys[i].Time >= normalizedLifetime)
                {
                    upperKey = colorKeys[i];
                    break;
                }
                lowerKey = colorKeys[i];
            }

            float t = (normalizedLifetime - lowerKey.Time) / (upperKey.Time - lowerKey.Time);
            return Vector3.Lerp(lowerKey.Color, upperKey.Color, t);
        }

        public float GetAlpha(float normalizedLifetime)
        {
            if (alphaKeys.Count < 1)
                return 1f;

            if (normalizedLifetime <= alphaKeys[0].Time)
            {
                return alphaKeys[0].Alpha;
            }

            if (normalizedLifetime >= alphaKeys[alphaKeys.Count - 1].Time)
            {
                return alphaKeys[alphaKeys.Count - 1].Alpha;
            }

            AlphaKey lowerKey = alphaKeys[0];
            AlphaKey upperKey = alphaKeys[1];

            for (int i = 1; i < alphaKeys.Count; i++)
            {
                if (alphaKeys[i].Time >= normalizedLifetime)
                {
                    upperKey = alphaKeys[i];
                    break;
                }
                lowerKey = alphaKeys[i];
            }

            float t = (normalizedLifetime - lowerKey.Time) / (upperKey.Time - lowerKey.Time);
            return upperKey.Alpha * t + lowerKey.Alpha * (1.0f - t);
        }

        public JValue Serialize()
        {
            throw new NotImplementedException();
        }

        public void Deserialize(string json)
        {
            throw new NotImplementedException();
        }

        public void SetReferences()
        {
            throw new NotImplementedException();
        }

        public JSerializable GetCopy()
        {
            List<ColorKey> colorCopy = new List<ColorKey>();
            List<AlphaKey> alphaCopy = new List<AlphaKey>();

            foreach (var colorKey in colorKeys)
                colorCopy.Add(new ColorKey(colorKey.Time, colorKey.Color));

            foreach (var alphaKey in alphaKeys)
                alphaCopy.Add(new AlphaKey(alphaKey.Time, alphaKey.Alpha));


            return new ColorGradient()
            {
                colorKeys = colorCopy,
                alphaKeys = alphaCopy
            };
        }

        //private Vector3 GetColor(float x)
        //{
        //    if (colors.TryGetValue(x, out Vector3 val))
        //        return val;

        //    if (x < colors.First().Key)
        //    {
        //        return colors.First().Value;
        //    }

        //    if (colors.Last().Key <= x)
        //    {
        //        return colors.Last().Value;
        //    }

        //    float prev = 0f;
        //    foreach (var key in colors.Keys)
        //    {
        //        if (key >= x)
        //        {
        //            float dif = key - prev;
        //            if (dif == 0)
        //                return colors[key];

        //            float alpha = (x - prev) / dif;
        //            return Vector3.Lerp(colors[prev], colors[key], alpha);
        //        }

        //        prev = key;
        //    }

        //    return Vector3.One;
        //}

        //private float GetAlpha(float x)
        //{
        //    if (alphas.TryGetValue(x, out float val))
        //        return val;

        //    if (x < alphas.First().Key)
        //    {
        //        return alphas.First().Value;
        //    }

        //    if (alphas.Last().Key <= x)
        //    {
        //        return alphas.Last().Value;
        //    }

        //    float prev = 0f;
        //    foreach (var key in alphas.Keys)
        //    {
        //        if (key >= x)
        //        {
        //            float dif = key - prev;
        //            if (dif == 0)
        //                return alphas[key];

        //            float alpha = (x - prev) / dif;
        //            return alphas[key] * alpha + alphas[prev] * (1.0f - alpha);
        //        }

        //        prev = key;
        //    }

        //    return 1.0f;
        //}
    }

    public class ColorKey : AutoSerializable
    {
        public float Time { get; set; }
        public Vector3 Color { get; set; }

        public ColorKey(float time, Vector3 color)
        {
            Time = time;
            Color = color;
        }
    }

    public class AlphaKey : AutoSerializable
    {
        public float Time { get; set; }
        public float Alpha { get; set; }

        public AlphaKey(float time, float color)
        {
            Time = time;
            Alpha = color;
        }
    }
}

