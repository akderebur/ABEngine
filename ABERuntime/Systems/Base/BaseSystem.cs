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

        internal void Start()
        {
            started = true;
            StartScene();
        }

        protected virtual void StartScene()
        {
        }

        internal void SceneChange()
        {
            started = false;
            ChangeScene();
        }

        protected virtual void ChangeScene()
        {
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

        public virtual void CleanUp(bool reload, bool newScene, bool resize)
        {

        }

        public void CleanUp(bool reload, bool newScene)
        {
            CleanUp(reload, newScene, false);
        }
    }
}
