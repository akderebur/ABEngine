using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Rendering;
using Arch.Core.Extensions;
using Box2D.NetStandard.Common;

namespace ABEngine.ABERuntime.Components
{
    public class ScriptableParticleModule
    {
        public int maxParticles { get; set; }

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

        private PipelineMaterial _particleMaterial;
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
        protected float spawnRate { get; set; }

        float spawnAcc = 0;

        LinkedList<ScriptableParticle> particles = new LinkedList<ScriptableParticle>();

        SpriteBatch particleBatch;
        string batchGuid;
        protected Transform moduleTrans;

        float accumulator = 0f;
        float scale = 1f;
        int particleCount;

        public ScriptableParticleModule()
        {
            maxParticles = 100;
            _particleTexture = AssetCache.GetDefaultTexture();
            _particleMaterial = GraphicsManager.GetUberMaterial();
            batchGuid = Guid.NewGuid().ToString();
        }

        PTime pTime;

        internal float GetDelta()
        {
            return pTime.delta;
        }

        internal float GetTime()
        {
            return pTime.moduleTime;
        }

        public void Init(Transform transform)
        {
            moduleTrans = transform;
            Play();
        }

        public void Play()
        {
            Sprite sprite = new Sprite(particleTexture);
            sprite.manualBatching = true;
            sprite.SetMaterial(_particleMaterial, true);
            Transform trans = new Transform("EditorNotVisible");
            var entity = Game.GameWorld.Create("P" + particleCount, Guid.NewGuid(), trans, sprite);
            trans.localPosition = new Vector3(0f, 0f, -10f);
            Game.spriteBatchSystem.AddSpriteToBatch(trans, sprite, batchGuid);
            particleBatch = Game.spriteBatchSystem.GetBatchFromSprite(trans, sprite, batchGuid);
            particleBatch.isDynamicSort = true;
            isPlaying = true;
            entity.Get<Transform>().enabled = false;

            pTime.moduleTime = 0f;
            SpawnInternal();
        }

        int spawnC = 0;
        private void SpawnInternal()
        {
            spawnC = 0;
            Spawn();
            if (spawnC > 0)
                particleBatch.InitBatch();
        }

        protected virtual void Spawn()
        {

        }

        public void Stop()
        {
            foreach (var particle in particles)
            {
                particle.transform.entity.DestroyEntity();
            }
            particles.Clear();

            particleBatch.DeleteBatch();
            Game.spriteBatchSystem.DeleteBatch(particleBatch);

            accumulator = 0f;
            isPlaying = false;
        }

        public virtual void Update(float deltaTime, Transform moduleTrans)
        {
            if(spawnRate > 0)
            {
                spawnAcc += deltaTime;
                if(spawnAcc >= 1f/ spawnRate)
                {
                    spawnAcc = 0;
                    SpawnInternal();
                }
            }

            this.moduleTrans = moduleTrans;
            scale = moduleTrans.worldScale.X + 0.00001f;
            float scaledDelta = deltaTime * scale;

            pTime.moduleTime += deltaTime;
            pTime.gameTime = Game.Time;
            pTime.delta = deltaTime;
            pTime.scaledDelta = scaledDelta;

            var curNode = particles.First;
            while (curNode != null)
            {
                ScriptableParticle particle = curNode.Value;
                var nextNode = curNode.Next;
                if (particle.lifetime == -10)
                {
                    curNode = nextNode;
                    continue;
                }

                particle.age += deltaTime;
                if (particle.age >= particle.lifetime)
                {
                    particle.lifetime = -10;
                    particles.Remove(particle);
                    particles.AddFirst(particle);

                    particle.transform.parent = null;
                    particle.transform.enabled = false;
                }
                else
                {
                    UpdateParticle(particle, pTime);
                }

                curNode = nextNode;
            }
        }

        protected virtual void UpdateParticle(ScriptableParticle particle, PTime pTime)
        {
        }

        protected virtual T SpawnParticle<T>() where T : ScriptableParticle, new()
        {
            ScriptableParticle reusePart = null;
            foreach (var particle in particles)
            {
                if (particle.lifetime > 0)
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
            if (reusePart != null)
            {
                Transform trans = reusePart.transform;
                trans.enabled = true;

                trans.localPosition = moduleTrans.worldPosition;
                trans.localScale = moduleTrans.worldScale;

                if (simulationSpace == SimulationSpace.Local)
                    trans.parent = moduleTrans;

                reusePart.age = 0;
                reusePart.Init();
                return reusePart as T;
            }
            else if (particleCount <= maxParticles)
            {
                Sprite sprite = new Sprite(particleTexture);
                sprite.manualBatching = true;
                sprite.SetMaterial(_particleMaterial, false);

                Transform trans = new Transform("EditorNotVisible");
                var entity = Game.GameWorld.Create("P" + particleCount, Guid.NewGuid(), trans, sprite);

                T newParticle = new T();
                newParticle.module = this;
                newParticle.transform = trans;
                newParticle.sprite = sprite;
                newParticle.Init();

                particles.AddLast(newParticle);
                particleCount++;

                particleBatch.AddSpriteEntity(trans, sprite);

                trans.localPosition = moduleTrans.worldPosition;
                trans.localScale = moduleTrans.worldScale;
                if (simulationSpace == SimulationSpace.Local)
                    trans.parent = moduleTrans;

                spawnC++;

                return newParticle;
            }

            return null;
        }
    }

    public static class SPMExtensions
    {

        // Helpers
        public static float AgeOverLifetime(this ScriptableParticle particle)
        {
            return particle.age / particle.lifetime;
        }

        public static void SetPosition(this ScriptableParticle particle, Vector3 position)
        {
            particle.transform.localPosition = position;
        }

        public static void SetColor(this ScriptableParticle particle, Vector4 color)
        {
            particle.sprite.tintColor = color;
        }

        public static void SetColorOverTime(this ScriptableParticle particle, Vector4 startColor, Vector4 endColor, float time01)
        {
            particle.sprite.tintColor = Vector4.Lerp(startColor, endColor, time01);
        }

        public static void SetSize(this ScriptableParticle particle, float size)
        {
            particle.transform.localScale = Vector3.One * size;
        }

        public static void SetSize(this ScriptableParticle particle, Vector3 size)
        {
            particle.transform.localScale = size;
        }

        public static void SetForce(this ScriptableParticle particle, Vector3 force)
        {
            // Apply the force as acceleration to the particle's velocity
            Vector3 acceleration = force / 1f; // Mass
            particle.velocity += acceleration * particle.module.GetDelta();


            Vector3 targetVelocity = force.Normalize() * 5; // assuming a maxSpeed property
            particle.velocity = Vector3.Lerp(particle.velocity, targetVelocity, 1f * particle.module.GetDelta());

            // Update the position based on the new velocity
            particle.transform.localPosition += particle.velocity * particle.module.GetDelta();


            //particle.velocity += force * particle.module.GetDelta();
            //particle.transform.localPosition += particle.velocity * particle.module.GetDelta();
            particle.velocity *= 0.99f;
        }

        public static void SetAttractor(this ScriptableParticle particle, Vector3 target, float force)
        {
            Vector3 dir = target - particle.transform.worldPosition;
            particle.SetForce(dir * force);
        }

        public static void CurlNoise(this ScriptableParticle particle, FastNoiseLite noise, Vector3 position, float scale, float timeScale)
        {
            float time = particle.module.GetTime() % 2f;
            particle.velocity += GlnCurlFast(noise,
                position * 100 + Vector3.One * time * timeScale) * particle.module.GetDelta() * scale;
            particle.transform.localPosition += particle.velocity * particle.module.GetDelta();
            particle.velocity *= 0.99f;
        }

        private static Vector3 CurlGenNoise(FastNoiseLite noise, Vector3 x)
        {
            float s = noise.GetNoise(x.X, x.Y, x.Z);
            float s1 = noise.GetNoise(x.Y - 19.1f, x.Z + 33.4f, x.X + 47.2f);
            float s2 = noise.GetNoise(x.Z + 74.2f, x.X - 124.5f, x.Y + 99.4f);
            Vector3 c = new Vector3(s, s1, s2);
            return c;
        }

        private static Vector3 GlnCurlFast(FastNoiseLite noise, Vector3 p)
        {
            const float e = 0.1f;
            Vector3 dx = new Vector3(e, 0.0f, 0.0f);
            Vector3 dy = new Vector3(0.0f, e, 0.0f);
            Vector3 dz = new Vector3(0.0f, 0.0f, e);

            Vector3 p_x0 = CurlGenNoise(noise, p - dx);
            Vector3 p_x1 = CurlGenNoise(noise, p + dx);
            Vector3 p_y0 = CurlGenNoise(noise, p - dy);
            Vector3 p_y1 = CurlGenNoise(noise, p + dy);
            Vector3 p_z0 = CurlGenNoise(noise, p - dz);
            Vector3 p_z1 = CurlGenNoise(noise, p + dz);

            float x = p_y1.Z - p_y0.Z - p_z1.Y + p_z0.Y;
            float y = p_z1.X - p_z0.X - p_x1.Z + p_x0.Z;
            float z = p_x1.Y - p_x0.Y - p_y1.X + p_y0.X;

            const float divisor = 1.0f / (2.0f * e);
            return Vector3.Normalize(new Vector3(x, y, z) * divisor);
        }
    }

    public abstract class ScriptableParticle
    {
        public int particleID;
        public ScriptableParticleModule module;
        public Transform transform;
        public Sprite sprite;
        public float lifetime;
        public float age;
        public Vector3 velocity;

        public abstract void Init();
    }

    public struct PTime
    {
        public float moduleTime;
        public float gameTime;
        public float delta;
        public float scaledDelta;
    }
}