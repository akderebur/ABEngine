using System;
using ABEngine.ABERuntime;

namespace ABEngine.ABEUI
{
	public class UIComponent : ABComponent 
    {
		public Transform transform { get; set; }
		public UIAnchor anchor { get; set; }

		internal virtual void Render()
		{

		}
	}
}

