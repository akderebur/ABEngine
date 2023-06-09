using System;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
    public class BaseSystem
    {
        protected static World _world;
        protected Archetype archetype;

        public bool started;
        public bool dontDestroyOnLoad { get; private set; }

        public static void SetECSWorld(World world)
        {
            _world = world;
        }

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
