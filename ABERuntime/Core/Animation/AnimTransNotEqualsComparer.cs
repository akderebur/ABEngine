using System;
using ABEngine.ABERuntime.Animation;

namespace ABEngine.ABERuntime.Core.Animation
{
    public class AnimTransNotEqualsComparer : AnimTransComparer
    {
        public override bool CompareResult(float value, float target)
        {
            return value != target;
        }
    }
}

