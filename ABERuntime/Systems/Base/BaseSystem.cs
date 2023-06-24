using System;
using ABEngine.ABERuntime.Physics;
using Arch.Core;

namespace ABEngine.ABERuntime
{
    public class BaseSystem
    {
        public bool started;
        public bool dontDestroyOnLoad { get; private set; }

        public BaseSystem()
        {
        }

        public BaseSystem(bool dontDestroyOnLoad)
        {
            this.dontDestroyOnLoad = dontDestroyOnLoad;
        }

        public virtual void Awake()
        {
        }

        public virtual void Start()
        {
            started = true;
        }

        public virtual void Update(float gameTime, float deltaTime)
        {

        }


        public virtual void FixedUpdate(float gameTime, float fixedDeltaTime)
        {

        }

        public virtual void OnCollision(CollisionData collisionData)
        {

        }

        protected internal virtual void OnEntityCreated(in Entity entity)
        {

        }

        protected internal virtual void OnEntityDestroyed(in Entity entity)
        {

        }

        public virtual void CleanUp(bool reload, bool newScene)
        {

        }
    }
}
