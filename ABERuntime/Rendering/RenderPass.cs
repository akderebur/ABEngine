using System;
using System.Collections.Generic;
using System.Numerics;
using WGIL;

namespace ABEngine.ABERuntime.Rendering
{
	public class RenderPassDefunct
	{
		//private List<Action<int>> workOrder;
		//private List<WGIL.Color> clearColors;
		//private bool clearDepth;
		//private float depthClearVal;

		//internal RenderPass()
		//{

		//}

		//public RenderPass(in RenderPassDescriptor passDesc)
		//{
		//	framebufferFetch = passDesc.framebufferFetch;
  //          workOrder = passDesc.workOrder;
		//	if(workOrder == null)
  //              workOrder = new List<Action<int>>();

		//	clearColors = passDesc.clearColors;
		//	if (clearColors == null)
		//		clearColors = new List<RgbaFloat>();


		//	var framebuffer = framebufferFetch();
		//	if (framebuffer.DepthTarget != null)
		//	{
		//		clearDepth = true;
  //              depthClearVal = passDesc.depthClearValue;
		//	}
		//}

		//public virtual void Render()
		//{
		//	// Setup framebuffer
		//	var framebuffer = framebufferFetch();
		//	GraphicsManager.cl.SetFramebuffer(framebufferFetch());
		//	GraphicsManager.cl.SetFullViewports();

		//	for (int i = 0; i < clearColors.Count; i++)
		//		GraphicsManager.cl.ClearColorTarget((uint)i, clearColors[i]);

		//	if (clearDepth)
		//		GraphicsManager.cl.ClearDepthStencil(depthClearVal);

		//	foreach (var renderWork in workOrder)
		//	{
  //              for (int i = 0; i < GraphicsManager.renderLayers.Count; i++)
  //              {
		//			renderWork(i);
  //              }
  //          }
		//}
    }

	//public struct RenderPassDescriptor
	//{
	//	public Func<Framebuffer> framebufferFetch;
	//	public List<Action<int>> workOrder;
	//	public List<RgbaFloat> clearColors;
	//	public float depthClearValue;
	//}
}

