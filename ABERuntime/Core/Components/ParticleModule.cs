using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Core.MathA;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Rendering;
using Halak;

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


        public SimulationSpace simulationSpace { get; set; }
        public bool isPlaying { get; private set; } 
        
        LinkedList<Particle> _particles = new();
        Random _rnd;

        float _spawnInterval = 1f;
        float _accumulator = 0f;
        float _scale = 1f;
        Vector3 _moveDir = Vector3.UnitY;

        SpriteBatch _particleBatch;
        string _batchGuid;
        Transform _moduleTrans;
        int _particleCount;

        public ParticleModule()
        {
            spawnRate = new FloatRange(1f);
            startSize = new FloatRange(1f);
            spawnRange = 1f;
            speed = new FloatRange(2f);
            startLifetime = new FloatRange(2f);
            maxParticles = 100;
            _rnd = new Random();
            _particleTexture = Assets.GetDefaultTexture();
            _particleMaterial = Graphics.GetUberMaterial();
            simulationSpace = SimulationSpace.Local;

            lifetimeSize = new BezierCurve(Vector2.UnitY, Vector2.One, new Vector2(0.25f, 1f), new Vector2(0.75f, 1f));
            lifetimeColor = new ColorGradient()
            {
                colorKeys = {
                    new ColorKey(0f, Vector3.One),
                    new ColorKey(1f, Vector3.One)
                },
                alphaKeys = {
                    new AlphaKey(0f, 1f),
                    new AlphaKey(1f, 1f)
                }
            };
            _batchGuid = Guid.NewGuid().ToString();
        }

        public void Init(Transform transform)
        {
            _moduleTrans = transform;
            Play();
        }

        public void Play()
        {
            Sprite sprite = new Sprite(particleTexture);
            sprite.manualBatching = true;
            sprite.SetMaterial(_particleMaterial, true);
            Transform trans = new Transform("EditorNotVisible");
            var entity = Game.GameWorld.Create("P" + _particleCount, Guid.NewGuid(), trans, sprite);
            trans.localPosition = new Vector3(0f, 0f, -10f);
            Game.spriteBatchSystem.AddSpriteToBatch(trans, sprite, _batchGuid);
            _particleBatch = Game.spriteBatchSystem.GetBatchFromSprite(trans, sprite, _batchGuid);
            _particleBatch.isDynamicSort = true;

            Particle particle = new Particle()
            {
                transform = trans,
                sprite = sprite,
                lifetime = -10
            };
            _particles.AddLast(particle);
            _particleCount++;
            isPlaying = true;

            float rate = Math.Clamp(spawnRate.NextValue(), 0.5f, 10000f);
            _spawnInterval = 1f / rate;
        }

        public void Stop()
        {
            foreach (var particle in _particles)
            {
                particle.transform.entity.DestroyEntity();
            }
            _particles.Clear();

            _particleBatch.DeleteBatch();
            Game.spriteBatchSystem.DeleteBatch(_particleBatch);

            _accumulator = 0f;
            isPlaying = false;
        }

        public void Update(float deltaTime, Transform moduleTrans)
        {
            if (!isPlaying)
                return;

            if (deltaTime > 1 / 10f)
                deltaTime = 1 / 10f;


            this._moduleTrans = moduleTrans;
            _scale = moduleTrans.worldScale.X + 0.00001f;
            float scaledDelta = deltaTime * _scale;
            float moveDelta = simulationSpace == SimulationSpace.Local ? deltaTime : scaledDelta;

            Matrix4x4 rotMat = Matrix4x4.CreateFromQuaternion(moduleTrans.worldRotation);
            Vector3 rotatedMoveDir = _moveDir;

            if (simulationSpace == SimulationSpace.World)
            {
                rotatedMoveDir = Vector3.Transform(_moveDir, rotMat);
                rotatedMoveDir = Vector3.Normalize(rotatedMoveDir);
                rotatedMoveDir.Z = 0f;
            }

            var curNode = _particles.First;
            while (curNode != null)
            {
                Particle particle = curNode.Value;
                var nextNode = curNode.Next;
                if (particle.lifetime == -10)
                {
                    curNode = nextNode;
                    continue;
                }

                particle.lifetime -= scaledDelta;
                float normLT = Math.Clamp(1f - particle.lifetime / (particle.startLifetime), 0, 1);
                Vector3 newPos = particle.transform.localPosition + rotatedMoveDir * particle.speed * moveDelta;
                newPos.Z = -normLT;
                particle.transform.localPosition = newPos;


                Vector2 sizeCurvePoint = lifetimeSize.Evaluate(normLT);
                if (simulationSpace == SimulationSpace.Local)
                    particle.transform.localScale = particle.startSize * new Vector3(sizeCurvePoint.Y, sizeCurvePoint.Y, 1f);
                else
                    particle.transform.localScale = moduleTrans.worldScale * particle.startSize * new Vector3(sizeCurvePoint.Y, sizeCurvePoint.Y, 1f);

                particle.sprite.tintColor = lifetimeColor.Evaluate(normLT);
                if (particle.lifetime <= 0)
                {
                    particle.lifetime = -10;
                    _particles.Remove(particle);
                    _particles.AddFirst(particle);

                    particle.transform.parent = null;
                    particle.transform.enabled = false;
                    //Vector3 pos = particle.Transform.worldPosition;
                    //pos.Z = -1001;
                    //particle.Transform.localPosition = pos;
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

            _accumulator += deltaTime;
            while (_accumulator >= _spawnInterval && spawnC < spawnLimit)
            {
                _accumulator -= _spawnInterval;
                intervalPassed = true;

                Particle reusePart = null;
                foreach (var particle in _particles)
                {
                    if (particle.lifetime > 0)
                        break;
                    else
                    {
                        reusePart = particle;
                        _particles.Remove(reusePart);
                        _particles.AddLast(reusePart);
                        break;
                    }
                }


                // Reuse or create particle
                Vector3 spawnVec = Vector3.Transform(Vector3.UnitX, rotMat);
                if (reusePart != null)
                {
                    reusePart.lifetime = startLifetime.NextValue() * _scale;
                    reusePart.startLifetime = reusePart.lifetime;
                    reusePart.speed = speed.NextValue();
                    reusePart.startSize = startSize.NextValue();

                    float spawnMid = spawnRange * _scale / 2f;

                    Transform trans = reusePart.transform;
                    trans.enabled = true;

                    Vector2 startSizePoint = lifetimeSize.Evaluate(0f);
                    trans.localPosition = moduleTrans.worldPosition + spawnVec * _rnd.NextFloat(-spawnMid, spawnMid);
                    trans.localScale = moduleTrans.worldScale * reusePart.startSize * new Vector3(startSizePoint.Y, startSizePoint.Y, 1f);
                    //trans.localRotation = moduleTrans.worldRotation;
                    if(simulationSpace == SimulationSpace.Local)
                        trans.parent = moduleTrans;
                    reusePart.sprite.tintColor = lifetimeColor.Evaluate(0f);
                }
                else if (_particleCount <= maxParticles)
                {
                    Sprite sprite = new Sprite(particleTexture);
                    sprite.manualBatching = true;
                    sprite.SetMaterial(_particleMaterial, false);


                    //sprite.sharedMaterial.SetFloat("EnableOutline", 1f);
                    //sprite.sharedMaterial.SetFloat("OutlineThickness", 0.01f);
                    //sprite.sharedMaterial.SetVector4("OutlineColor", Veldrid.RgbaFloat.Blue.ToVector4());

                    Transform trans = new Transform("EditorNotVisible");
                    var entity = Game.GameWorld.Create("P" + _particleCount, Guid.NewGuid(), trans, sprite);

                    float lifetime = startLifetime.NextValue() * _scale;
                    Particle particle = new Particle()
                    {
                        transform = trans,
                        sprite = sprite,
                        lifetime = lifetime,
                        startLifetime = lifetime,
                        speed = speed.NextValue(),
                        startSize = startSize.NextValue()
                    };
                    _particles.AddLast(particle);
                    _particleCount++;

                    _particleBatch.AddSpriteEntity(trans, sprite);

                    float spawnMid = spawnRange * _scale / 2f;

                    Vector2 startSizePoint = lifetimeSize.Evaluate(0f);
                    trans.localPosition = moduleTrans.worldPosition + spawnVec * _rnd.NextFloat(-spawnMid, spawnMid);
                    trans.localScale = moduleTrans.worldScale * particle.startSize * new Vector3(startSizePoint.Y, startSizePoint.Y, 1f);
                    //trans.localRotation = moduleTrans.worldRotation;
                    if (simulationSpace == SimulationSpace.Local)
                        trans.parent = moduleTrans;

                    sprite.tintColor = lifetimeColor.Evaluate(0f);

                    spawnC++;
                }
            }

            if (spawnC > 0)
            {
                _particleBatch.InitBatch();
            }

            if (intervalPassed)
            {
                float rate = Math.Clamp(spawnRate.NextValue(), 0.5f, 10000f);
                _spawnInterval = 1f / rate;
            }
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Texture", Assets.GetAssetSceneIndex(this._particleTexture.fPathHash));
            jObj.Put("Material", Assets.GetAssetSceneIndex(this._particleMaterial.fPathHash));
            jObj.Put("MaxParticles", maxParticles);
            jObj.Put("SpawnRange", spawnRange);
            jObj.Put("SpawnRate", ABComponent.Serialize(spawnRate));
            jObj.Put("StartLifetime", ABComponent.Serialize(startLifetime));
            jObj.Put("Speed", ABComponent.Serialize(speed));
            jObj.Put("StartSize", ABComponent.Serialize(startSize));
            jObj.Put("SimulationSpace", (int)simulationSpace);
            jObj.Put("MoveDir", _moveDir);

            jObj.Put("LifetimeSize", ABComponent.Serialize(lifetimeSize));
            jObj.Put("LifetimeColor", lifetimeColor.Serialize()); ;


            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);

            int texSceneIndex = data["Texture"];
            int matSceneIndex = data["Material"];

            var tex2d = Assets.GetAssetFromSceneIndex(texSceneIndex) as Texture2D;
            if (tex2d == null)
                tex2d = Assets.GetDefaultTexture();
            var material = Assets.GetAssetFromSceneIndex(matSceneIndex) as PipelineMaterial;

            this._particleTexture = tex2d;
            this._particleMaterial = material;

            maxParticles = data["MaxParticles"];
            spawnRange = data["SpawnRange"];
            spawnRate = ABComponent.Deserialize(data["SpawnRate"].ToString(), typeof(FloatRange)) as FloatRange;
            startLifetime = ABComponent.Deserialize(data["StartLifetime"].ToString(), typeof(FloatRange)) as FloatRange;
            speed = ABComponent.Deserialize(data["Speed"].ToString(), typeof(FloatRange)) as FloatRange;
            startSize = ABComponent.Deserialize(data["StartSize"].ToString(), typeof(FloatRange)) as FloatRange;
            int simSpaceInd = data["SimulationSpace"];
            simulationSpace = (SimulationSpace)simSpaceInd;
            _moveDir = data["MoveDir"];

            lifetimeSize = ABComponent.Deserialize(data["LifetimeSize"].ToString(), typeof(BezierCurve)) as BezierCurve;

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
                spawnRate = ABComponent.GetCopy(this.spawnRate) as FloatRange,
                startLifetime = ABComponent.GetCopy(this.startLifetime) as FloatRange,
                speed = ABComponent.GetCopy(this.speed) as FloatRange,
                startSize = ABComponent.GetCopy(this.startSize) as FloatRange,
                lifetimeSize = ABComponent.GetCopy(this.lifetimeSize) as BezierCurve,
                lifetimeColor = (ColorGradient)this.lifetimeColor.GetCopy(),

                _particleTexture = this._particleTexture,
                _particleMaterial = this._particleMaterial,

                simulationSpace = this.simulationSpace,
                _moveDir = this._moveDir
            };

            return pm;
        }
    }

    class Particle
    {
        public float lifetime;
        public Transform transform;
        public Sprite sprite;

        public float startLifetime;
        public float startSize;
        public float speed;
    }
}

