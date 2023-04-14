using System;
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
        public FloatRange startLifetime { get; set; }
        public FloatRange spawnRate { get; set; }
        public float spawnRange { get; set; }
        public FloatRange speed { get; set; }
        public FloatRange startSize { get; set; }
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
        string batchGuid;
        Transform moduleTrans;

        public ParticleModule()
        {
            spawnRate = new FloatRange(1f);
            startSize = new FloatRange(1f);
            spawnRange = 1f;
            speed = new FloatRange(2f);
            startLifetime = new FloatRange(2f);
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
            batchGuid = Guid.NewGuid().ToString();
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
            var entity = Game.GameWorld.CreateEntity("P" + particles.Count, Guid.NewGuid(), trans, sprite);
            trans.localPosition = new Vector3(0f, 0f, -10f);
            Game.spriteBatchSystem.AddSpriteToBatch(trans, sprite, batchGuid);
            particleBatch = Game.spriteBatchSystem.GetBatchFromSprite(trans, sprite, batchGuid);

            Particle particle = new Particle()
            {
                Transform = trans,
                Sprite = sprite,
                Lifetime = -10
            };
            particles.AddLast(particle);
            isPlaying = true;

            float rate = Math.Clamp(spawnRate.NextValue(), 0.5f, 10000f);
            spawnInterval = 1f / rate;
        }

        public void Stop()
        {
            foreach (var particle in particles)
            {
                particle.Transform.entity.DestroyEntity();
            }
            particles.Clear();

            particleBatch.DeleteBatch();
            Game.spriteBatchSystem.DeleteBatch(particleBatch);

            accumulator = 0f;
            isPlaying = false;
        }

        public void Update(float deltaTime, Transform moduleTrans)
        {
            if (!isPlaying)
                return;

            this.moduleTrans = moduleTrans;
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
                float normLT = Math.Clamp(1f - particle.Lifetime / (particle.StartLifetime), 0, 1);
                Vector3 newPos = particle.Transform.localPosition + rotatedMoveDir * particle.Speed * moveDelta;
                newPos.Z = -normLT;
                particle.Transform.localPosition = newPos;


                Vector2 sizeCurvePoint = lifetimeSize.Evaluate(normLT);
                if (simulationSpace == SimulationSpace.Local)
                    particle.Transform.localScale = particle.StartSize * new Vector3(sizeCurvePoint.Y, sizeCurvePoint.Y, 1f);
                else
                    particle.Transform.localScale = moduleTrans.worldScale * particle.StartSize * new Vector3(sizeCurvePoint.Y, sizeCurvePoint.Y, 1f);

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
            bool intervalPassed = false;

            accumulator += deltaTime;
            while (accumulator >= spawnInterval && spawnC < spawnLimit)
            {
                accumulator -= spawnInterval;
                intervalPassed = true;

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
                    reusePart.Lifetime = startLifetime.NextValue() * scale;
                    reusePart.StartLifetime = reusePart.Lifetime;
                    reusePart.Speed = speed.NextValue();
                    reusePart.StartSize = startSize.NextValue();

                    float spawnMid = spawnRange * scale / 2f;

                    Transform trans = reusePart.Transform;

                    Vector2 startSizePoint = lifetimeSize.Evaluate(0f);
                    trans.localPosition = moduleTrans.worldPosition + spawnVec * rnd.NextFloat(-spawnMid, spawnMid);
                    trans.localScale = moduleTrans.worldScale * reusePart.StartSize * new Vector3(startSizePoint.Y, startSizePoint.Y, 1f);
                    //trans.localRotation = moduleTrans.worldRotation;
                    if(simulationSpace == SimulationSpace.Local)
                        trans.parent = moduleTrans;
                    reusePart.Sprite.tintColor = lifetimeColor.Evaluate(0f);
                }
                else if (particles.Count <= maxParticles)
                {
                    Sprite sprite = new Sprite(particleTexture);
                    sprite.manualBatching = true;
                    sprite.SetMaterial(_particleMaterial, false);


                    //sprite.sharedMaterial.SetFloat("EnableOutline", 1f);
                    //sprite.sharedMaterial.SetFloat("OutlineThickness", 0.01f);
                    //sprite.sharedMaterial.SetVector4("OutlineColor", Veldrid.RgbaFloat.Blue.ToVector4());

                    Transform trans = new Transform("EditorNotVisible");
                    var entity = Game.GameWorld.CreateEntity("P" + particles.Count, Guid.NewGuid(), trans, sprite);

                    float lifetime = startLifetime.NextValue() * scale;
                    Particle particle = new Particle()
                    {
                        Transform = trans,
                        Sprite = sprite,
                        Lifetime = lifetime,
                        StartLifetime = lifetime,
                        Speed = speed.NextValue(),
                        StartSize = startSize.NextValue()
                    };
                    particles.AddLast(particle);

                    particleBatch.AddSpriteEntity(trans, sprite);

                    float spawnMid = spawnRange * scale / 2f;

                    Vector2 startSizePoint = lifetimeSize.Evaluate(0f);
                    trans.localPosition = moduleTrans.worldPosition + spawnVec * rnd.NextFloat(-spawnMid, spawnMid);
                    trans.localScale = moduleTrans.worldScale * particle.StartSize * new Vector3(startSizePoint.Y, startSizePoint.Y, 1f);
                    //trans.localRotation = moduleTrans.worldRotation;
                    if (simulationSpace == SimulationSpace.Local)
                        trans.parent = moduleTrans;

                    sprite.tintColor = lifetimeColor.Evaluate(0f);

                    spawnC++;
                }
            }

            if (spawnC > 0)
                particleBatch.InitBatch();

            if (intervalPassed)
            {
                float rate = Math.Clamp(spawnRate.NextValue(), 0.5f, 10000f);
                spawnInterval = 1f / rate;
            }
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Texture", AssetCache.GetAssetSceneIndex(this._particleTexture.fPathHash));
            jObj.Put("Material", AssetCache.GetAssetSceneIndex(this._particleMaterial.fPathHash));
            jObj.Put("MaxParticles", maxParticles);
            jObj.Put("SpawnRange", spawnRange);
            jObj.Put("SpawnRate", AutoSerializable.Serialize(spawnRate));
            jObj.Put("StartLifetime", AutoSerializable.Serialize(startLifetime));
            jObj.Put("Speed", AutoSerializable.Serialize(speed));
            jObj.Put("StartSize", AutoSerializable.Serialize(startSize));
            jObj.Put("SimulationSpace", (int)simulationSpace);
            jObj.Put("MoveDir", moveDir);

            jObj.Put("LifetimeSize", AutoSerializable.Serialize(lifetimeSize));
            jObj.Put("LifetimeColor", lifetimeColor.Serialize()); ;


            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);

            int texSceneIndex = data["Texture"];
            int matSceneIndex = data["Material"];

            var tex2d = AssetCache.GetAssetFromSceneIndex(texSceneIndex) as Texture2D;
            var material = AssetCache.GetAssetFromSceneIndex(matSceneIndex) as PipelineMaterial;

            this._particleTexture = tex2d;
            this._particleMaterial = material;

            maxParticles = data["MaxParticles"];
            spawnRange = data["SpawnRange"];
            spawnRate = AutoSerializable.Deserialize(data["SpawnRate"].ToString(), typeof(FloatRange)) as FloatRange;
            startLifetime = AutoSerializable.Deserialize(data["StartLifetime"].ToString(), typeof(FloatRange)) as FloatRange;
            speed = AutoSerializable.Deserialize(data["Speed"].ToString(), typeof(FloatRange)) as FloatRange;
            startSize = AutoSerializable.Deserialize(data["StartSize"].ToString(), typeof(FloatRange)) as FloatRange;
            int simSpaceInd = data["SimulationSpace"];
            simulationSpace = (SimulationSpace)simSpaceInd;
            moveDir = data["MoveDir"];

            lifetimeSize = AutoSerializable.Deserialize(data["LifetimeSize"].ToString(), typeof(BezierCurve)) as BezierCurve;

            ColorGradient colorGradient = new ColorGradient();
            colorGradient.Deserialize(data["LifetimeColor"].ToString());
            lifetimeColor = colorGradient;

            maxParticles = data["MaxParticles"];
        }

        public void SetReferences()
        {
            
        }

        public JSerializable GetCopy()
        {
            ParticleModule pm = new ParticleModule()
            {
                maxParticles = this.maxParticles,
                spawnRange = this.spawnRange,
                spawnRate = AutoSerializable.GetCopy(this.spawnRate) as FloatRange,
                startLifetime = AutoSerializable.GetCopy(this.startLifetime) as FloatRange,
                speed = AutoSerializable.GetCopy(this.speed) as FloatRange,
                startSize = AutoSerializable.GetCopy(this.startSize) as FloatRange,
                lifetimeSize = AutoSerializable.GetCopy(this.lifetimeSize) as BezierCurve,
                lifetimeColor = (ColorGradient)this.lifetimeColor.GetCopy(),

                _particleTexture = this._particleTexture,
                _particleMaterial = this._particleMaterial,

                simulationSpace = this.simulationSpace,
                moveDir = this.moveDir
            };

            return pm;
        }
    }

    class Particle
    {
        public float Lifetime;
        public Transform Transform;
        public Sprite Sprite;

        public float StartLifetime;
        public float StartSize;
        public float Speed;
    }
}

