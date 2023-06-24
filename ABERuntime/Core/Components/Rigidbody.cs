using System;
using System.Collections.Generic;
using System.Numerics;
using Box2D.NetStandard.Dynamics.Bodies;
using Halak;
using ABEngine.ABERuntime.Physics;
using Arch.Core;

namespace ABEngine.ABERuntime.Components
{
    public class Rigidbody : JSerializable
    {
        public Transform transform { get; private set; }
        public BodyType bodyType;
        public float mass { get; set; }
        public float density { get; set; }
        public float friction { get; set; }
        public float linearDamping { get; set; }
        public RBInterpolationType interpolationType { get; set; }
        public bool isTrigger { get; set; }
        public CollisionLayer collisionLayer { get; set; }

        public bool enabled { get; set; }

        public Body b2dBody { get; internal set; }
        public bool isColliding { get { return _colliders.Count > 0; } }
        public List<Rigidbody> colliders { get { return _colliders; } }

        private List<Rigidbody> _colliders;

        // Events
        public event Action<Rigidbody, Rigidbody> onCollisionEnter;
        public event Action<Rigidbody, Rigidbody> onCollisionExit;

        public Vector2 previousPosition;
        public Vector2 smoothedPosition;

        public Vector2 start;
        public Vector2 target;
        public Vector2 current;

        internal bool destroyed;

        public Rigidbody()
        {
            _colliders = new List<Rigidbody>();
            bodyType = BodyType.Static;
            mass = 1f;
            density = 1f;
            interpolationType = RBInterpolationType.None;
            collisionLayer = PhysicsManager.GetDefaultCollisionLayer();
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("type", GetType().ToString());
            jObj.Put("BodyType", (int)bodyType);
            jObj.Put("Mass", mass);
            jObj.Put("Density", density);
            jObj.Put("Friction", friction);
            jObj.Put("LinearDamp", linearDamping);

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            bodyType = (BodyType)((int)data["BodyType"]);
            mass = data["Mass"];
            density = data["Density"];
            friction = data["Friction"];
            linearDamping = data["LinearDamp"];
        }


        public void SetReferences()
        {
        }

        internal void CollisionEnter(Rigidbody collider)
        {
            if (!_colliders.Contains(collider))
            {
                _colliders.Add(collider);
                onCollisionEnter?.Invoke(this, collider);
            }
        }

        internal void CollisionExit(Rigidbody collider)
        {
            if (_colliders.Contains(collider))
            {
                _colliders.Remove(collider);

                onCollisionExit?.Invoke(this, collider);
            }
        }

        internal void SetEntity(Transform transform)
        {
            this.transform = transform;
        }

        public void SetPosition(Vector3 position)
        {
            b2dBody?.SetTransform(position.ToB2DVector(), b2dBody.GetAngle());
        }

        internal void SetBodyEnabled(bool enabled)
        {
            if (b2dBody == null)
                return;

            b2dBody.SetEnabled(enabled);
        }

        internal void Destroy()
        {
            onCollisionEnter = null;
            onCollisionExit = null;
            colliders.Clear();

            PhysicsManager.DestroyBody(this);
        }

        public JSerializable GetCopy()
        {
            Rigidbody rb = new Rigidbody()
            {
                friction = this.friction,
                density = this.density,
                linearDamping = this.linearDamping,
                mass = this.mass,
                _colliders = new List<Rigidbody>(),
                interpolationType = this.interpolationType,
                bodyType = this.bodyType,
                isTrigger = this.isTrigger,
                collisionLayer = this.collisionLayer
            };

            return rb;
        }
    }

    public enum RBInterpolationType
    {
        None,
        Interpolate
    }
}
