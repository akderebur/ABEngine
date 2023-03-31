﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ABEngine.ABERuntime.Core.Math;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Rendering;
using Box2D.NetStandard.Common;
using Halak;
using Veldrid;
using Vortice.DXGI;

namespace ABEngine.ABERuntime.Components
{
    public enum SimulationSpace
    {
        Local,
        World
    }

    public class ParticleModule : JSerializable
    {
        public int maxParticles { get; set; }
        public float startLifetime { get; set; }
        public float spawnRate { get; set; }
        public float spawnRange { get; set; }
        public float speed { get; set; }
        public float startSize { get; set; }
        public BezierCurve lifetimeSize { get; set; }
        public ColorGradient lifetimeColor { get; set; }

        private Texture2D _particleTexture;
        public Texture2D particleTexture
        {
            get { return _particleTexture; }
            set
            {
                if (_particleTexture == value || _particleTexture == null)
                    return;

                if (particleBatch != null)
                {
                    Stop();
                    _particleTexture = value;
                    Play();
                }
                else
                    _particleTexture = value;
            }
        }

        public PipelineMaterial _particleMaterial;
        public PipelineMaterial particleMaterial
        {
            get { return _particleMaterial; }
            set
            {
                if (_particleMaterial == value || _particleMaterial == null)
                    return;

                if (particleBatch != null)
                {
                    Stop();
                    _particleMaterial = value;
                    Play();
                }
                else
                    _particleMaterial = value;
            }
        }


        public SimulationSpace simulationSpace;
        public bool isPlaying { get; private set; }

        LinkedList<Particle> particles = new LinkedList<Particle>();

        float spawnInterval = 1f;
        float accumulator = 0f;

        public Vector3 moveDir = Vector3.UnitY;
        float scale = 1f;

        Random rnd;

        SpriteBatch particleBatch;
        Transform moduleTrans;

        public ParticleModule()
        {
            spawnRate = 1;
            startSize = 1f;
            spawnRange = 1f;
            speed = 2f;
            startLifetime = 3f;
            maxParticles = 100;
            rnd = new Random();
            _particleTexture = AssetCache.GetDefaultTexture();
            _particleMaterial = GraphicsManager.GetUberMaterial();
            simulationSpace = SimulationSpace.Local;

            lifetimeSize = new BezierCurve(Vector2.UnitY, Vector2.One, new Vector2(0.25f, 1f), new Vector2(0.75f, 1f));
            lifetimeColor = new ColorGradient()
            {
                colorKeys = {
                    new ColorKey(0f, RgbaFloat.White.ToVector4().ToVector3()),
                    new ColorKey(1f, RgbaFloat.White.ToVector4().ToVector3())
                },
                alphaKeys = {
                    new AlphaKey(0f, 1f),
                    new AlphaKey(1f, 1f)
                }
            };
        }

        public void Init(Transform transform)
        {
            moduleTrans = transform;
            Play();
        }

        public void Play()
        {
            Sprite sprite = new Sprite(particleTexture);
            sprite.manualLifetime = true;
            sprite.SetMaterial(_particleMaterial, true);
            Transform trans = new Transform("EditorNotVisible");
            var entity = Game.GameWorld.CreateEntity("P" + particles.Count, Guid.NewGuid(), trans, sprite);
            trans.localPosition = new Vector3(0f, 0f, -10f);
            Game.spriteBatcher.AddSpriteToBatch(trans, sprite);
            particleBatch = Game.spriteBatcher.GetBatchFromSprite(trans, sprite);

            Particle particle = new Particle()
            {
                Transform = trans,
                Sprite = sprite,
                Lifetime = -10
            };
            particles.AddLast(particle);
            isPlaying = true;
        }

        public void Stop()
        {
            foreach (var particle in particles)
            {
                particle.Transform.entity.DestroyEntity();
            }
            particles.Clear();

            particleBatch.DeleteBatch();
            Game.spriteBatcher.DeleteBatch(particleBatch);

            accumulator = 0f;
            isPlaying = false;
        }

        public void Update(float deltaTime, Transform moduleTrans)
        {
            if (!isPlaying)
                return;

            spawnInterval = 1f / spawnRate;
            scale = moduleTrans.worldScale.X + 0.00001f;
            float scaledDelta = deltaTime * scale;
            float moveDelta = simulationSpace == SimulationSpace.Local ? deltaTime : scaledDelta;

            Matrix4x4 rotMat = Matrix4x4.CreateFromQuaternion(moduleTrans.worldRotation);
            Vector3 rotatedMoveDir = moveDir;

            if (simulationSpace == SimulationSpace.World)
            {
                rotatedMoveDir = Vector3.Transform(moveDir, rotMat);
                rotatedMoveDir = Vector3.Normalize(rotatedMoveDir);
                rotatedMoveDir.Z = 0f;
            }

            var curNode = particles.First;
            while (curNode != null)
            {
                Particle particle = curNode.Value;
                var nextNode = curNode.Next;
                if (particle.Lifetime == -10)
                {
                    curNode = nextNode;
                    continue;
                }

                particle.Lifetime -= scaledDelta;
                float normLT = Math.Clamp(1f - particle.Lifetime / (startLifetime * scale), 0, 1);
                Vector3 newPos = particle.Transform.localPosition + rotatedMoveDir * speed * moveDelta;
                newPos.Z = -normLT;
                particle.Transform.localPosition = newPos;


                Vector2 sizeCurvePoint = lifetimeSize.Evaluate(normLT);
                if (simulationSpace == SimulationSpace.Local)
                    particle.Transform.localScale = startSize * new Vector3(sizeCurvePoint.Y, sizeCurvePoint.Y, 1f);
                else
                    particle.Transform.localScale = moduleTrans.worldScale * startSize * new Vector3(sizeCurvePoint.Y, sizeCurvePoint.Y, 1f);

                particle.Sprite.tintColor = lifetimeColor.Evaluate(normLT);
                if (particle.Lifetime <= 0)
                {
                    particle.Lifetime = -10;
                    particles.Remove(particle);
                    particles.AddFirst(particle);

                    particle.Transform.parent = null;
                    Vector3 pos = particle.Transform.worldPosition;
                    pos.Z = -100f;
                    particle.Transform.localPosition = pos;
                }

                curNode = nextNode;
            }

            //for (int i = 0; i < particles.Count; i++)
            //{
            //    Particle particle = particles[i];
            //    if (particle.Lifetime == -10)
            //        continue;

            //    particle.Lifetime -= scaledDelta;
            //    float normLT = Math.Clamp(1f - particle.Lifetime / (startLifetime * scale), 0, 1);
            //    Vector3 newPos = particle.Transform.localPosition + moveDir * speed * scaledDelta;
            //    newPos.Z = -normLT;
            //    particle.Transform.localPosition = newPos;

            //    particle.Transform.localScale = moduleTrans.worldScale * startSize * new Vector3(lifetimeSize.Evaluate(normLT), 1f);
            //    particle.Sprite.tintColor = lifetimeColor.Evaluate(normLT);
            //    if (particle.Lifetime <= 0)
            //    {
            //        particle.Lifetime = -10;
            //        particles.Remove(particle);
            //        particles.Insert(0, particle);
            //        Vector3 pos = particle.Transform.localPosition;
            //        pos.Z = -100f;
            //        particle.Transform.localPosition = pos;
            //    }
            //}

            //Console.WriteLine(curParticles);

            int spawnLimit = 10000;
            int spawnC = 0;

            accumulator += deltaTime;
            while (accumulator >= spawnInterval && spawnC < spawnLimit)
            {
                accumulator -= spawnInterval;

                Particle reusePart = null;
                foreach (var particle in particles)
                {
                    if (particle.Lifetime > 0)
                        break;
                    else
                    {
                        reusePart = particle;
                        particles.Remove(reusePart);
                        particles.AddLast(reusePart);
                        break;
                    }
                }


                // Reuse or create particle
                Vector3 spawnVec = Vector3.Transform(Vector3.UnitX, rotMat);
                if (reusePart != null)
                {
                    reusePart.Lifetime = startLifetime * scale;

                    float spawnMid = spawnRange * scale / 2f;

                    Transform trans = reusePart.Transform;

                    Vector2 startSizePoint = lifetimeSize.Evaluate(0f);
                    trans.localPosition = moduleTrans.worldPosition + spawnVec * rnd.NextFloat(-spawnMid, spawnMid);
                    trans.localScale = moduleTrans.worldScale * startSize * new Vector3(startSizePoint.Y, startSizePoint.Y, 1f);
                    //trans.localRotation = moduleTrans.worldRotation;
                    if(simulationSpace == SimulationSpace.Local)
                        trans.parent = moduleTrans;
                    reusePart.Sprite.tintColor = lifetimeColor.Evaluate(0f);
                }
                else if (particles.Count <= maxParticles)
                {
                    Sprite sprite = new Sprite(particleTexture);
                    sprite.manualLifetime = true;
                    sprite.SetMaterial(_particleMaterial, false);

                    //sprite.sharedMaterial.SetFloat("EnableOutline", 1f);
                    //sprite.sharedMaterial.SetFloat("OutlineThickness", 0.01f);
                    //sprite.sharedMaterial.SetVector4("OutlineColor", Veldrid.RgbaFloat.Blue.ToVector4());

                    Transform trans = new Transform("EditorNotVisible");
                    var entity = Game.GameWorld.CreateEntity("P" + particles.Count, Guid.NewGuid(), trans, sprite);

                    particleBatch.AddSpriteEntity(trans, sprite);

                    float spawnMid = spawnRange * scale / 2f;

                    Vector2 startSizePoint = lifetimeSize.Evaluate(0f);
                    trans.localPosition = moduleTrans.worldPosition + spawnVec * rnd.NextFloat(-spawnMid, spawnMid);
                    trans.localScale = moduleTrans.worldScale * startSize * new Vector3(startSizePoint.Y, startSizePoint.Y, 1f);
                    //trans.localRotation = moduleTrans.worldRotation;
                    if (simulationSpace == SimulationSpace.Local)
                        trans.parent = moduleTrans;

                    sprite.tintColor = lifetimeColor.Evaluate(0f);

                    spawnC++;

                    Particle particle = new Particle()
                    {
                        Transform = trans,
                        Sprite = sprite,
                        Lifetime = startLifetime * scale,
                    };
                    particles.AddLast(particle);
                }
            }

            if (spawnC > 0)
                particleBatch.InitBatch();
        }

        public JValue Serialize()
        {
            throw new NotImplementedException();
        }

        public void Deserialize(string json)
        {
            throw new NotImplementedException();
        }

        public void SetReferences()
        {
            throw new NotImplementedException();
        }

        public JSerializable GetCopy(ref Entity newEntity)
        {
            throw new NotImplementedException();
        }
    }

    class Particle
    {
        public float Lifetime;
        public Transform Transform;
        public Sprite Sprite;
    }
}
