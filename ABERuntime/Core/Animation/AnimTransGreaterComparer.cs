using System;

namespace ABEngine.ABERuntime.Animation
{
    public class AnimTransGreaterComparer : AnimTransComparer
    {
        public override bool CompareResult(float value, float target)
        {
            return value > target;
        }
    }
}
