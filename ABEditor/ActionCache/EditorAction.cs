using System;

namespace ABEngine.ABEditor
{
    public abstract class EditorAction
    {
        public abstract void Do();
        public abstract void Undo();
    }
}

