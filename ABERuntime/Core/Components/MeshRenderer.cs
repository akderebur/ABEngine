﻿using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Halak;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Components
{
	public class MeshRenderer : JSerializable
	{
        public PipelineMaterial material { get; set; }
        public Mesh mesh { get; set; }

        internal Buffer vertexUniformBuffer;
        internal BindGroup vertexTransformSet;

        public MeshRenderer()
		{
            material = GraphicsManager.GetUber3D();
            SetupResources();
        }

        public MeshRenderer(Mesh mesh) : this()
        {
            this.mesh = mesh;
        }

        public MeshRenderer(Mesh mesh, PipelineMaterial material)
        {
            this.mesh = mesh;
            this.material = material;
            SetupResources();
        }

        void SetupResources()
        {
            vertexUniformBuffer = Game.wgil.CreateBuffer(128, BufferUsages.UNIFORM | BufferUsages.COPY_DST);

            var vertexTransformDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = GraphicsManager.sharedMeshUniform_VS,
                Entries = new BindResource[]
                {
                    vertexUniformBuffer
                }
            };
            vertexTransformSet = Game.wgil.CreateBindGroup(ref vertexTransformDesc);
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Mesh", AssetCache.GetAssetSceneIndex(this.mesh.fPathHash));
            jObj.Put("Material", AssetCache.GetAssetSceneIndex(this.material.fPathHash));

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);

            int meshSceneIndex = data["Mesh"];
            int matSceneIndex = data["Material"];

            var mesh = AssetCache.GetAssetFromSceneIndex(meshSceneIndex) as Mesh;
            if (mesh == null)
                mesh = Rendering.CubeModel.GetCubeMesh();
            var material = AssetCache.GetAssetFromSceneIndex(matSceneIndex) as PipelineMaterial;
            if (material == null)
                material = GraphicsManager.GetUber3D();

            this.mesh = mesh;
            this.material = material;
        }

        public void SetReferences()
        {
            
        }

        public JSerializable GetCopy()
        {
            MeshRenderer copyMR = new MeshRenderer()
            {
                material = this.material,
                mesh = this.mesh
            };

            return copyMR;
        }
    }
}

