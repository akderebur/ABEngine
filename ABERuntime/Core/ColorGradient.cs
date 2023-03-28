using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace ABEngine.ABERuntime
{
	public class ColorGradient
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

    public class ColorKey
    {
        public float Time { get; set; }
        public Vector3 Color { get; set; }

        public ColorKey(float time, Vector3 color)
        {
            Time = time;
            Color = color;
        }
    }

    public class AlphaKey
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

