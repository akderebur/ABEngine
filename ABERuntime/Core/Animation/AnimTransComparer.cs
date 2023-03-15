using System;

namespace ABEngine.ABERuntime.Animation
{
    public abstract class AnimTransComparer
    {
        public abstract bool CompareResult(float value, float target);
    }
}
