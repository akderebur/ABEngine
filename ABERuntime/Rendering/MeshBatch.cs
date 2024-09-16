using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime.Rendering;

public class MeshBatch
{
    private PipelineMaterial material;
    private WGILContext _wgil;

    private Mesh mesh;
    private MeshMatrixData[] matrices = null;
    private MeshMatrixData currentMatrixData;
    private DrawData drawData;
    
    public int batchID = 0;
    
    private int meshCount = 0;
    private int instanceCount = 0;
    private int renderCount = 0;
    private int transformCount = 0;

    internal static int batchCounter = 0;
    
    public MeshBatch(Mesh mesh, PipelineMaterial pipelineMaterial)
    {
        _wgil = Game.wgil;
        this.mesh = mesh;
        material = pipelineMaterial;
        matrices = new MeshMatrixData[10];

        batchID = batchCounter;
        batchCounter++;
    }
    
    internal void AddMesh()
    {
        meshCount++;
        if (matrices.Length < meshCount)
        {
            Array.Resize(ref matrices, (int)MathF.Floor(meshCount * 1.5f));
        }
    }
    
    internal void UpdateMesh(in Matrix4x4 worldMatrix)
    {
        currentMatrixData.transformMatrix = worldMatrix;
        Matrix4x4 MVInv;
        Matrix4x4.Invert(worldMatrix, out MVInv);
        currentMatrixData.normalMatrix = Matrix4x4.Transpose(MVInv);
        matrices[instanceCount++] = currentMatrixData;
        transformCount++;
    }

    internal void CullMesh()
    {
        transformCount++;
    }

    internal int UpdateBatch(int start)
    {
        int returnCount = instanceCount;
        
        // Write to GPU buffer
        // Draw data
        drawData.matrixStartID = start;
        _wgil.WriteBuffer(Game.meshRenderSystem.drawDataBuffer,
            drawData,
            Game.meshRenderSystem.bufferStep * batchID,
            12);

        // Matrices
        _wgil.WriteBuffer(Game.meshRenderSystem.matrixStorageBuffer, matrices, start * 128, instanceCount * 128);
        
        renderCount = instanceCount;
        instanceCount = 0;

        if (transformCount < meshCount)
            meshCount = transformCount;
        transformCount = 0;
        return returnCount;
    }
    
    internal void Render(RenderPass pass)
    {
        pass.SetVertexBuffer(0, mesh.vertexBuffer);
        pass.SetIndexBuffer(mesh.indexBuffer, IndexFormat.Uint16);
        pass.DrawIndexed(mesh.indices.Length, renderCount);
    }
}