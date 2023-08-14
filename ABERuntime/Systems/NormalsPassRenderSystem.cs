using System;
using System.Linq;
using ABEngine.ABERuntime.Core.Components;
using Arch.Core;
using Veldrid;

namespace ABEngine.ABERuntime
{
	public class NormalsPassRenderSystem : RenderSystem
	{
        private readonly QueryDescription meshQuery = new QueryDescription().WithAll<Transform, Mesh>();

        SharedMeshVertex sharedVertexUniform;

        public NormalsPassRenderSystem(PipelineAsset asset) : base(asset) { }

        public override void Start()
        {
            base.Start();
            sharedVertexUniform = new SharedMeshVertex();
        }

        public override void Render()
        {
            pipelineAsset.BindPipeline();
            cl.SetGraphicsResourceSet(0, Game.pipelineSet);

            Game.GameWorld.Query(in meshQuery, (ref Mesh mesh, ref Transform transform) =>
            {
                if (mesh.material.isLateRender)
                    return;

                // Update vertex uniform
                sharedVertexUniform.transformMatrix = transform.worldMatrix;
                gd.UpdateBuffer(mesh.vertexUniformBuffer, 0, sharedVertexUniform);

                cl.SetVertexBuffer(0, mesh.vertexBuffer);
                cl.SetIndexBuffer(mesh.indexBuffer, IndexFormat.UInt16);

                cl.SetGraphicsResourceSet(1, mesh.vertexTransformSet);

                // Material Resource Sets
                if (mesh.material.bindableSets.Count > 0)
                {
                    var entry = mesh.material.bindableSets.ElementAt(0);
                    cl.SetGraphicsResourceSet(entry.Key, entry.Value);
                }

                cl.DrawIndexed((uint)mesh.indices.Length);
            });
        }
    }
}

