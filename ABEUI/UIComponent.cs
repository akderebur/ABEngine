using System;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABEUI
{
	public class UIComponent : ABComponent 
    {
		public Transform transform { get; set; }
		public UIAnchor anchor { get; set; }
		private bool _enabled = true;
		public bool enabled
		{
			get { return _enabled; }
			set
			{
				_enabled = value;
				hovered = false;
				clicked = false;
			}
		}

		public bool hovered;
		public bool clicked;

		internal virtual void Render()
		{

		}
	}
}

