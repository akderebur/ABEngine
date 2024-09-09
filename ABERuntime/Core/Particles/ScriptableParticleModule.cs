using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Rendering;

namespace ABEngine.ABERuntime.Components
{
    public class ScriptableParticleModule
    {
        public int maxParticles { get; set; }

        private Texture2D _particleTexture;
        public Texture2D particleTexture
        {
            get => _particleTexture;
            set
            {
                if (_particleTexture == value || _particleTexture == null)
                    return;

                if (_particleBatch != null)
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
            get => _particleMaterial;
            set
            {
                if (_particleMaterial == value || _particleMaterial == null)
                    return;

                if (_particleBatch != null)
                {
                    Stop();
                    _particleMaterial = value;
                    Play();
                }
                else
                    _particleMaterial = value;
            }
        }

        public TileMode tileMode { get; set; }
        public float minStripDistance { get; set; }
        public float stripXTiling { get; set; }

        public SimulationSpace simulationSpace { get; set; }
        public bool isPlaying { get; private set; }
        protected float spawnRate { get; set; }

        float _spawnAcc;
        LinkedList<ScriptableParticle> _particles = new();
        RenderBatch _particleBatch;
        Action<float> _updateRoutine;

        string _batchGuid;
        protected Transform moduleTrans;
        public Vector3 worldPosition => moduleTrans.worldPosition;

        float _accumulator;
        float _scale = 1f;
        int _particleCount;
        
        int _spawnC;
        PTime _pTime;
        
        public ScriptableParticleModule()
        {
            maxParticles = 100;
            _particleTexture = Assets.GetDefaultTexture();
            _particleMaterial = Graphics.GetParticleMaterial();
            _batchGuid = Guid.NewGuid().ToString();

            minStripDistance = 0.1f;
            stripXTiling = 1f;
        }


        internal float GetDelta()
        {
            return _pTime.delta;
        }

        internal float GetTime()
        {
            return _pTime.moduleTime;
        }

        internal LinkedList<ScriptableParticle> GetParticles()
        {
            return _particles;
        }

        public void Init(Transform transform)
        {
            moduleTrans = transform;
            Play();
        }

        public void Play()
        {
            bool isStrip = _particleMaterial.pipelineAsset.HasFeature(MaterialFeature.ParticleStrip);
            if (isStrip)
            {
                _particleBatch = new StripParticleBatch(this, 0, 0);

                if(tileMode == TileMode.Stretch)
                    _updateRoutine = UpdateStripStretch;
                else
                    _updateRoutine = UpdateStripRepeat;
            }
            else
            {
                _particleBatch = new ParticleBatch(this, 0, 0);
                _updateRoutine = UpdateSingle;
            }
            _particleBatch.key = "PM_Batch_" + Guid.NewGuid();
            _particleBatch.active = true;

            //Game.spriteBatchSystem.AddGenericBatch(_particleBatch);

            isPlaying = true;
            _pTime.moduleTime = 0f;
            ModuleStart();
            SpawnInternal();
        }

        protected virtual void ModuleStart()
        {
            
        }

        private void SpawnInternal()
        {
            _spawnC = 0;
            Spawn();
        }

        protected virtual void Spawn()
        {

        }

        public void Stop()
        {
            _particles.Clear();

            _particleBatch.DeleteBatch();
            //Game.spriteBatchSystem.DeleteBatch(_particleBatch);

            _accumulator = 0f;
            isPlaying = false;
        }

        public virtual void Update(float deltaTime, Transform moduleTrans)
        {
            if (!isPlaying)
                return;
            if(spawnRate > 0)
            {
                _spawnAcc += deltaTime;
                if(_spawnAcc >= 1f/ spawnRate)
                {
                    _spawnAcc = 0;
                    SpawnInternal();
                }
            }

            this.moduleTrans = moduleTrans;
            _scale = moduleTrans.worldScale.X + 0.00001f;
            float scaledDelta = deltaTime * _scale;

            _pTime.moduleTime += deltaTime;
            _pTime.gameTime = Game.Time;
            _pTime.delta = deltaTime;
            _pTime.scaledDelta = scaledDelta;

            _updateRoutine(deltaTime);
        }

        void UpdateSingle(float deltaTime)
        {
            int instanceCount = 0;
            var curNode = _particles.First;
            ParticleVertex[] vertexData = ((ParticleBatch)_particleBatch).GetParticleVertices();

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
                    _particles.Remove(particle);
                    _particles.AddFirst(particle);
                }
                else
                {
                    UpdateParticle(particle, _pTime);
                    vertexData[instanceCount] = new ParticleVertex(particle.position,
                                                                   particle.size,
                                                                   particle.tintColor,
                                                                   Vector2.Zero, Vector2.One);

                    instanceCount++;
                }

                curNode = nextNode;
            }

            ((ParticleBatch)_particleBatch).SetParticleInstance(instanceCount);
        }

        void UpdateStripStretch(float deltaTime)
        {
            int instanceCount = 0;
            var curNode = _particles.First;
            StripVertex[] vertexData = ((StripParticleBatch)_particleBatch).GetParticleVertices();

            float totalDist = 0f;

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
                    _particles.Remove(particle);
                    _particles.AddFirst(particle);
                }
                else
                {
                    //float uvStep = (float)instanceCount / (particleCount - 1);
                    UpdateParticle(particle, _pTime);
                    instanceCount++;

                    if (instanceCount == 2)
                    {
                        ScriptableParticle prevPart = curNode.Previous.Value;
                        Vector3 point1 = prevPart.position;
                        Vector3 point2 = particle.position;

                        Vector3 dif = point2 - point1;

                        float dist = dif.Length();

                        if (dist > minStripDistance)
                        {
                            totalDist += dist;
                            Vector3 direction = Vector3.Normalize(dif);
                            Vector3 sideVector = Vector3.Cross(direction, Game.activeCamera.GetComponent<Camera>().forward);

                            vertexData[0] = new StripVertex(point1 - sideVector * (prevPart.size / 2f),
                                                            new Vector2(0, 1),
                                                            particle.tintColor);

                            vertexData[1] = new StripVertex(point1 + sideVector * (prevPart.size / 2f),
                                                            Vector2.Zero,
                                                            particle.tintColor);

                            vertexData[2] = new StripVertex(point2 - sideVector * (particle.size / 2f),
                                                            new Vector2(totalDist, 1),
                                                            particle.tintColor);

                            vertexData[3] = new StripVertex(point2 + sideVector * (particle.size / 2f),
                                                           new Vector2(totalDist, 0),
                                                           particle.tintColor);
                        }
                        else
                        {
                            instanceCount--;

                            particle.lifetime = -10;
                            _particles.Remove(particle);
                            _particles.AddFirst(particle);
                        }
                    }
                    else if (instanceCount > 2)
                    {
                        ScriptableParticle prevPart = curNode.Previous.Value;
                        Vector3 point1 = prevPart.position;
                        Vector3 point2 = particle.position;

                        Vector3 dif = point2 - point1;
                        float dist = dif.Length();

                        if (dist > minStripDistance)
                        {
                            totalDist += dist;

                            Vector3 direction = Vector3.Normalize(dif);
                            Vector3 sideVector = Vector3.Cross(direction, Game.activeCamera.GetComponent<Camera>().forward);
                            //Vector3 sideVector = new Vector3(-direction.Y, direction.X, 0);

                            int index = (instanceCount - 1) * 2;
                            vertexData[index] = new StripVertex(point2 - sideVector * (particle.size / 2f),
                                                        new Vector2(totalDist, 1),
                                                        particle.tintColor);

                            vertexData[index + 1] = new StripVertex(point2 + sideVector * (particle.size / 2f),
                                                      new Vector2(totalDist, 0),
                                                      particle.tintColor);
                        }
                        else
                        {

                            instanceCount--;

                            particle.lifetime = -10;
                            _particles.Remove(particle);
                            _particles.AddFirst(particle);
                        }

                    }
                }

                particle.stripIndex = instanceCount;
                curNode = nextNode;
            }

            ((StripParticleBatch)_particleBatch).SetParticleInstance(instanceCount, totalDist);
        }

        void UpdateStripRepeat(float deltaTime)
        {
            int instanceCount = 0;
            float uvStart = 0f;
            var curNode = _particles.First;
            StripVertex[] vertexData = ((StripParticleBatch)_particleBatch).GetParticleVertices();

            ScriptableParticle lastPart = null;

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
                    _particles.Remove(particle);
                    _particles.AddFirst(particle);
                }
                else
                {
                    //float uvStep = (float)instanceCount / (particleCount - 1);
                    UpdateParticle(particle, _pTime);
                    instanceCount++;

                    if(instanceCount == 1)
                    {
                        lastPart = particle;
                    }
                    else if (instanceCount == 2)
                    {
                        ScriptableParticle prevPart = lastPart;
                        Vector3 point1 = prevPart.position;
                        Vector3 point2 = particle.position;

                        Vector3 dif = point2 - point1;
                        Vector3 direction = Vector3.Normalize(dif);
                        Vector3 sideVector = Vector3.Cross(direction, Game.activeCamera.GetComponent<Camera>().forward);
                        float uvPortion = dif.Length() / stripXTiling;

                        if (dif.Length() > minStripDistance)
                        {
                            if (!prevPart.stripUVSet)
                            {
                                prevPart.uvX = 0;
                                prevPart.stripUVSet = true;
                            }

                            if (!particle.stripUVSet)
                            {
                                particle.uvX = prevPart.uvX + uvPortion;
                                particle.stripUVSet = true;
                            }

                            vertexData[0] = new StripVertex(point1 - sideVector * (prevPart.size / 2f),
                                                            new Vector2(prevPart.uvX, 1),
                                                            particle.tintColor);

                            vertexData[1] = new StripVertex(point1 + sideVector * (prevPart.size / 2f),
                                                            new Vector2(prevPart.uvX, 0),
                                                            particle.tintColor);

                            vertexData[2] = new StripVertex(point2 - sideVector * (particle.size / 2f),
                                                            new Vector2(particle.uvX, 1),
                                                            particle.tintColor);

                            vertexData[3] = new StripVertex(point2 + sideVector * (particle.size / 2f),
                                                           new Vector2(particle.uvX, 0),
                                                           particle.tintColor);

                            uvStart += uvPortion;
                            lastPart = particle;
                        }
                        else
                        {
                            instanceCount--;

                            particle.lifetime = -10;
                            _particles.Remove(particle);
                            _particles.AddFirst(particle);
                        }
                    }
                    else if(instanceCount > 2)
                    {
                        ScriptableParticle prevPart = lastPart;
                        Vector3 point1 = prevPart.position;
                        Vector3 point2 = particle.position;

                        Vector3 dif = point2 - point1;
                        Vector3 direction = Vector3.Normalize(dif);
                        Vector3 sideVector = Vector3.Cross(direction, Game.activeCamera.GetComponent<Camera>().forward);
                        //Vector3 sideVector = new Vector3(-direction.Y, direction.X, 0);

                        float uvPortion = dif.Length() / stripXTiling;


                        if (dif.Length() > minStripDistance)
                        {
                            if (!particle.stripUVSet)
                            {
                                particle.uvX = prevPart.uvX + uvPortion;
                                particle.stripUVSet = true;
                            }


                            int index = (instanceCount - 1) * 2;
                            vertexData[index] = new StripVertex(point2 - sideVector * (particle.size / 2f),
                                                        new Vector2(particle.uvX, 1),
                                                        particle.tintColor);

                            vertexData[index + 1] = new StripVertex(point2 + sideVector * (particle.size / 2f),
                                                      new Vector2(particle.uvX, 0),
                                                      particle.tintColor);

                            uvStart += uvPortion;
                            lastPart = particle;
                        }
                        else
                        {
                            instanceCount--;

                            particle.lifetime = -10;
                            _particles.Remove(particle);
                            _particles.AddFirst(particle);
                        }
                    }
                }

                curNode = nextNode;
            }

            ((StripParticleBatch)_particleBatch).SetParticleInstance(instanceCount);
        }

        protected virtual void UpdateParticle(ScriptableParticle particle, PTime pTime)
        {
        }

        protected virtual void SpawnUnused<T>() where T : ScriptableParticle, new()
        {
            T newParticle = new T();
            newParticle.module = this;
            newParticle.size = 1f;
            newParticle.tintColor = Vector4.One;
            newParticle.lifetime = -10;
            _particles.AddFirst(newParticle);
        }

        protected virtual T SpawnParticle<T>() where T : ScriptableParticle, new()
        {
            ScriptableParticle reusePart = null;
            foreach (var particle in _particles)
            {
                if (particle.lifetime > 0)
                    break;
                else
                {
                    reusePart = particle;
                    reusePart.stripUVSet = false;
                    _particles.Remove(reusePart);
                    _particles.AddLast(reusePart);
                    break;
                }
            }

            // Reuse or create particle
            if (reusePart != null)
            {
                reusePart.age = 0;
                reusePart.velocity = Vector3.Zero;
                reusePart.Init();
                return reusePart as T;
            }
            else if (_particleCount < maxParticles)
            {
                T newParticle = new T();
                newParticle.module = this;
                newParticle.size = 1f;
                newParticle.tintColor = Vector4.One;
                //newParticle.sprite = sprite;
                newParticle.Init();

                _particles.AddLast(newParticle);
                _particleCount++;

                _spawnC++;

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
            particle.position = position;
        }

        public static void SetColor(this ScriptableParticle particle, Vector4 color)
        {
            particle.tintColor = color;
        }

        public static void SetColorOverTime(this ScriptableParticle particle, Vector4 startColor, Vector4 endColor, float time01)
        {
            particle.tintColor = Vector4.Lerp(startColor, endColor, time01);
        }

        public static void SetColorOverTime(this ScriptableParticle particle, ColorGradient gradient, float time01)
        {
            particle.tintColor = gradient.Evaluate(time01);
        }

        public static void SetSize(this ScriptableParticle particle, float size)
        {
            particle.size = size;
        }

        public static void SetForce(this ScriptableParticle particle, Vector3 force)
        {
            // Apply the force as acceleration to the particle's velocity
            Vector3 acceleration = force / 1f; // Mass
            particle.velocity += acceleration;
            //particle.position += particle.velocity * particle.module.GetDelta();



            Vector3 targetVelocity = force.Normalize() * 5; // assuming a maxSpeed property
            particle.velocity = Vector3.Lerp(particle.velocity, targetVelocity, 1f * particle.module.GetDelta());

            // Update the position based on the new velocity
            particle.position += particle.velocity * particle.module.GetDelta();


            particle.velocity += force * particle.module.GetDelta();
            //particle.transform.localPosition += particle.velocity * particle.module.GetDelta();
            particle.velocity *= 0.99f;
        }

        public static void SetAttractor(this ScriptableParticle particle, Vector3 target, float force)
        {
            Vector3 dir = target - particle.position;
            particle.SetForce(dir * force);
        }

        public static void CurlNoise(this ScriptableParticle particle, FastNoiseLite noise, Vector3 position, float scale, float timeScale)
        {
            float time = particle.module.GetTime();
            particle.velocity += GlnCurlFast(noise,
                position * 100 + Vector3.One * time * timeScale) * particle.module.GetDelta() * scale;
            particle.position += particle.velocity * particle.module.GetDelta();
            particle.velocity *= 0.99f;
        }

        public static void CurlNoise(this ScriptableParticle particle, FastNoiseLite noise, Vector3 position, Vector3 scale, float timeScale)
        {
            float time = particle.module.GetTime();
            particle.velocity += GlnCurlFast(noise,
                position  + Vector3.One * time * timeScale) * particle.module.GetDelta() * scale;
            particle.position += particle.velocity * particle.module.GetDelta();
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
        public int stripIndex;
        public ScriptableParticleModule module;
        public Vector3 position;
        public float uvX;
        public float size;
        public Vector4 tintColor;
        public float lifetime;
        public float age;
        public Vector3 velocity;

        internal bool stripUVSet;

        public abstract void Init();
    }

    public struct PTime
    {
        public float moduleTime;
        public float gameTime;
        public float delta;
        public float scaledDelta;
    }

    public enum TileMode
    {
        Stretch,
        Repeat
    }
}