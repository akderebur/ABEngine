using System;
using System.Diagnostics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Halak;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ABEngine.ABERuntime.Pipelines;
using Veldrid.Utilities;
using ABEngine.ABERuntime.Debug;
using ABEngine.ABERuntime.Physics;
using System.Runtime.Serialization;
using System.Reflection;
using System.Xml.Linq;
using ABEngine.ABERuntime.Tweening;
using ABEngine.ABERuntime.Components;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using ABEngine.ABERuntime.Core.Animation.StateMatch;
using Arch.Core.Utils;
using Arch.Core;
using Arch.Core.Extensions;
using System.Collections;
using Arch.Core.Extensions.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ABEngine.ABERuntime
{
    internal enum GameMode
    {
        Runtime,
        Editor
    }

    public class Game
    {
        protected private  GraphicsDevice gd;
        private DisposeCollectorResourceFactory rf;

        // Resources
        private protected CommandList _commandList;
        protected Sdl2Window window;

        // Worlds and Systems
        public static World GameWorld;
        public static Box2D.NetStandard.Dynamics.World.World B2DWorld;
        private List<Type> userSystemTypes;
        private protected List<BaseSystem> userSystems;
        protected List<RenderSystem> renderExtensions;

        internal static GameMode gameMode;

        // Global Vars
        public static string AppPath;
        public static string AssetPath;
        public static Transform activeCam;
        public static Canvas canvas;
        public static Vector2 screenSize;
        public static Matrix4x4 projectionMatrix;
        public static float Time;
        internal static List<Type> UserTypes;

        // Events
        public static event Action onWindowResize;
        public static event Action onSceneLoad;
        public static event Action onCanvasResize;


        // Flags
        protected static bool _checkCamUpdate;


        // BOX2D
        protected private const float TimeStep = 1.0f / 50.0f;
        const int MAX_STEPS = 5;

        const int VelocityIterations = 8;
        const int PositionIterations = 3;


        // Systems
        protected CameraMovementSystem camMoveSystem;
        internal static B2DInitSystem b2dInitSystem;
        protected SpriteAnimatorSystem spriteAnimatorSystem;
        protected StateAnimatorSystem stateAnimatorSystem;
        protected SpriteAnimSystem spriteAnimSystem;
        protected RigidbodyMoveSystem rbMoveSystem;
        public static SpriteBatchSystem spriteBatchSystem;
        protected Tweening.TweenSystem tweenSystem;
        protected private ColliderDebugSystem colDebugSystem;
        protected private ParticleModuleSystem particleSystem;

        // Render Systems
        public static NormalsPassRenderSystem normalsRenderSystem;
        public static MainRenderSystem mainRenderSystem;
        protected MeshRenderSystem meshRenderSystem;
        public static LightRenderSystem lightRenderSystem;

        public List<RenderSystem> internalRenders;

        // Framebuffer

        public static Texture compositeRenderTexture;
        protected Framebuffer compositeRenderFB;

        protected ResourceSet compositeRSSetLight;
        protected ResourceSet finalQuadRSSet;

        internal static OutputDescription mainFBOutDesc;

        public static PipelineData pipelineData;

        public static DeviceBuffer pipelineBuffer;
        public static ResourceSet pipelineSet;

        protected private  static bool reload = false;
        protected private  static bool newScene = false;
        protected private bool resize = false;

        internal static bool debug = false;

        internal static float zoomFactor = 1f;

        public Game(string windowName, bool debug, List<Type> userTypes)
        {
            UserTypes = userTypes;
            userSystems = new List<BaseSystem>();
            userSystemTypes = new List<Type>();
            AppPath = System.IO.Directory.GetCurrentDirectory() + "/";
            AssetPath = AppPath + "Assets/";
            Game.debug = debug;
            gameMode = GameMode.Runtime;

            Init(windowName);
        }

        internal static void ReloadGame(bool isNewScene)
        {
            reload = true;
            newScene = isNewScene;
        }

        protected virtual void Game_Init()
        {

        }

        protected virtual void Scene_Init()
        {

        }

        void CreateInternalRenders(bool newScene)
        {
            if (newScene)
            {
                normalsRenderSystem = new NormalsPassRenderSystem();
                mainRenderSystem = new MainRenderSystem();
                meshRenderSystem = new MeshRenderSystem();
                lightRenderSystem = new LightRenderSystem();

                internalRenders = new List<RenderSystem>()
                {
                    normalsRenderSystem,
                    mainRenderSystem,
                    meshRenderSystem,
                    lightRenderSystem
                };
            }

            normalsRenderSystem.SetupResources(newScene);
            mainRenderSystem.SetupResources(newScene);
            meshRenderSystem.SetupResources(newScene);
            lightRenderSystem.SetupResources(newScene, mainRenderSystem.GetMainColorAttachent());

            compositeRSSetLight = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                  GraphicsManager.sharedTextureLayout,
                  lightRenderSystem.GetMainColorAttachent(), gd.LinearSampler
                  ));
        }


        float accumulator;
        float interpolation;
        protected private virtual void Init(string windowName)
        {
            // ECS and Physics Worlds
            CreateWorlds();

            // Init
            PhysicsManager.ResetPhysics();
            GraphicsManager.InitSettings();

            // Veldrid 
            SetupGraphics(windowName);
            CreateInternalRenders(true);

            AssetCache.InitAssetCache();

            EntityManager.Init();

            LineDbgPipelineAsset lineDbgPipelineAsset = new LineDbgPipelineAsset(compositeRenderFB);

            // Systems
            // Shared
            camMoveSystem = new CameraMovementSystem();
            b2dInitSystem = new B2DInitSystem();
            spriteAnimatorSystem = new SpriteAnimatorSystem();
            stateAnimatorSystem = new StateAnimatorSystem();
            spriteAnimSystem = new SpriteAnimSystem();
            rbMoveSystem = new RigidbodyMoveSystem();
            spriteBatchSystem = new SpriteBatchSystem(null);
            renderExtensions = new List<RenderSystem>();
            tweenSystem = new Tweening.TweenSystem();
            //colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);
            particleSystem = new ParticleModuleSystem();


            // Once in game lifetime
            Game_Init();

            Scene_Init();

            // User systems _ Reflection
            Scene_RegisterSystems();
            SubscribeSystems();

            Scene_Setup();
            onSceneLoad?.Invoke();

            RefreshProjection(Game.canvas);

            InputSnapshot snapshot = window.PumpEvents();
            Input.UpdateFrameInput(snapshot);

            // Awake events
            //if (debug)
            //    colDebugSystem.Awake();

            // Start Events
            b2dInitSystem.Start();
            rbMoveSystem.ResetSmoothStates();
            foreach (var system in userSystems)
            {
                system.Start();
            }

            //spriteRenderer.Start();
            spriteBatchSystem.Start();
            meshRenderSystem.Start();
            spriteAnimatorSystem.Start();
            stateAnimatorSystem.Start();
            spriteAnimSystem.Start();
            camMoveSystem.Start();
            lightRenderSystem.Start();
            normalsRenderSystem.Start();
            particleSystem.Start();
            //if (debug)
            //    colDebugSystem.Start();
           

            foreach (var rendExt in renderExtensions)
            {
                rendExt.Start();
            }


            // Game Loop
            Stopwatch sw = Stopwatch.StartNew();

            float previousTime = (float)sw.Elapsed.TotalSeconds;
            float lastFps = previousTime;
            int nbFrames = 0;

            while (window.Exists)
            {
                float newTime = (float)sw.Elapsed.TotalSeconds;
                float elapsed = newTime - previousTime;
                Time = newTime;
                pipelineData.Time = Time;

                previousTime = newTime;
                snapshot = window.PumpEvents();
                Input.UpdateFrameInput(snapshot);

                nbFrames++;
                if (newTime - lastFps >= 1.0)
                {
                    float ms = 1000.0f / nbFrames;
                    Console.WriteLine(ms);
                    nbFrames = 0;
                    lastFps += 1.0f;
                }

                EntityManager.CheckEntityChanges();

                if (Input.GetKeyDown(Key.R))
                {
                    reload = true;
                    newScene = true;
                }

                if (reload)
                {
                    reload = false;

                    gd.WaitForIdle();

                    // Clean extensions
                    foreach (var rendExt in renderExtensions)
                    {
                        rendExt.CleanUp(true, newScene);
                    }

                    if (resize)
                    {
                        resize = false;

                        // Resize render targets

                        compositeRenderTexture.Dispose();
                        finalQuadRSSet.Dispose();
                        compositeRSSetLight.Dispose();

                        foreach (var render in internalRenders)
                            render.CleanUp(true, false, true);

                        Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;
                        compositeRenderTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                           mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                            mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled));

                        compositeRenderFB = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(null, compositeRenderTexture));


                        finalQuadRSSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                               GraphicsManager.sharedTextureLayout,
                               compositeRenderTexture, gd.LinearSampler
                               ));

                        CreateInternalRenders(false);

                        pipelineData = new PipelineData()
                        {
                            Projection = Matrix4x4.Identity,
                            View = Matrix4x4.Identity,
                            Resolution = screenSize,
                            Time = elapsed,
                            Padding = 0f
                        };

                        GraphicsManager.RefreshMaterials();

                        //lineDbgPipelineAsset = new LineDbgPipelineAsset(compositeRenderFB);

                        //if (debug)
                        //    colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);

                        lightRenderSystem.Start();
                        //if (debug)
                        //    colDebugSystem.Start();

                        RefreshProjection(Game.canvas);
                    }
                    else if(newScene)
                    {
                        newScene = false;
                        EntityManager.SetImmediateDestroy(true);
                        CoroutineManager.StopAllCoroutines();
                        PrefabManager.ClearScene();

                        // Recreate assets/worlds
                        World.Destroy(GameWorld);
                        CreateWorlds();
                        PhysicsManager.ResetPhysics();

                        EntityManager.SetImmediateDestroy(false);

                        // Clean systems
                        foreach (var system in userSystems)
                        {
                            system.CleanUp(true, newScene);
                        }

                        // Clean Resources
                        AssetCache.DisposeResources();
                        rf.DisposeCollector.DisposeAll();

                        GraphicsManager.ResetPipelines();

                        foreach (var render in internalRenders)
                            render.CleanUp(true, true, false);

                        //lineDbgPipelineAsset = new LineDbgPipelineAsset(compositeRenderFB);

                        // Systems
                        camMoveSystem = new CameraMovementSystem();
                        b2dInitSystem = new B2DInitSystem();
                        spriteAnimatorSystem = new SpriteAnimatorSystem();
                        stateAnimatorSystem = new StateAnimatorSystem();
                        spriteAnimSystem = new SpriteAnimSystem();
                        rbMoveSystem = new RigidbodyMoveSystem();
                        spriteBatchSystem = new SpriteBatchSystem(null);
                        tweenSystem = new Tweening.TweenSystem();
                        particleSystem = new ParticleModuleSystem();
                        //if (debug)
                        //    colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);

                        for (int i = renderExtensions.Count - 1; i >= 0; i--)
                        {
                            BaseSystem system = renderExtensions[i];
                            if (!system.dontDestroyOnLoad)
                            {
                                renderExtensions.RemoveAt(i);
                            }
                        }

                        for (int i = userSystems.Count - 1; i >= 0; i--)
                        {
                            BaseSystem system = userSystems[i];
                            if (!system.dontDestroyOnLoad)
                            {
                                userSystems.RemoveAt(i);
                                userSystemTypes.RemoveAt(i);
                            }
                        }

                        notifySystems.Clear();
                        notifyAnySystems.Clear();

                        AssetCache.ClearSceneCache();
                        EntityManager.frameSemaphore.Release();
                        EntityManager.Init();

                        Scene_Init();

                        // User systems _ Reflection
                        Scene_RegisterSystems();
                        SubscribeSystems();

                        Scene_Setup();
                        onSceneLoad?.Invoke();

                        //Start Events
                        b2dInitSystem.Start();
                        foreach (var system in userSystems)
                        {
                            system.Start();
                        }

                        spriteBatchSystem.Start();
                        meshRenderSystem.Start();
                        spriteAnimatorSystem.Start();
                        stateAnimatorSystem.Start();
                        spriteAnimSystem.Start();
                        camMoveSystem.Start();
                        lightRenderSystem.Start();
                        tweenSystem.Start();
                        particleSystem.Start();
                        //if (debug)
                        //    colDebugSystem.Start();
                    }


                    //snapshot = window.PumpEvents();
                    //Input.UpdateFrameInput(snapshot);

                    foreach (var rendExt in renderExtensions)
                    {
                        rendExt.Start();
                    }


                    continue;

                }

                MainFixedUpdate(newTime, elapsed);
                interpolation = accumulator / TimeStep;
                MainUpdate(newTime, elapsed, interpolation);
                foreach (var rendExt in renderExtensions)
                {
                    rendExt.Update(newTime, elapsed);
                }

                if(!window.Exists)
                {
                    break;
                }

                DrawBegin();
                MainRender3D();
                foreach (var rendExt in renderExtensions)
                {
                    rendExt.Render();
                }
                //DrawEnd();

                // Copy frame buffer
                //_commandList.Begin();
                //_commandList.CopyTexture(mainRenderTexture, ScreenTexture);
                //_commandList.End();
                //gd.SubmitCommands(_commandList);
                //gd.WaitForIdle();

                //DrawBeginNoClear();
                //lightRenderer.Render();
                //DrawEnd();

                //// Copy frame buffer
                //_commandList.Begin();
                //_commandList.CopyTexture(mainRenderTexture, ScreenTexture);
                //_commandList.End();
                //gd.SubmitCommands(_commandList);
                //gd.WaitForIdle();

                //LateDrawBegin();
                //LateRender();
                //foreach (var rendExt in renderExtensions)
                //{
                //    rendExt.LateRender();
                //}
                //LateDrawEnd();


                FinalRender();

                //next_game_tick += SKIP_TICKS;
                //float sleepTime  = next_game_tick - newTime;

                //if (sleepTime >= 0)
                //{
                //    Console.WriteLine("Sleep");
                //    System.Threading.Thread.Sleep(sleepTime.ToMilliseconds());
                //}
            }

            // Resource clean up
            CleanUp();

            SDL2.SDL.SDL_VideoQuit();
            SDL2.SDL.SDL_Quit();
        }



        private protected void MainFixedUpdate(float newTime, float elapsed)
        {
            // Fixed
            accumulator += elapsed;

            int steps = 0;
            while (TimeStep < accumulator && MAX_STEPS > steps)
            {
                if (steps == 0)
                {
                    PhysicsManager.PreFixedUpdate();
                    rbMoveSystem.PreFixedUpdate();
                }

                //rbMoveSystem.ResetSmoothStates();

                foreach (var system in userSystems)
                {
                    system.FixedUpdate(newTime, TimeStep);
                }

                B2DWorld?.Step(TimeStep, VelocityIterations, PositionIterations);

                steps++;

                rbMoveSystem.FixedUpdate(newTime, TimeStep);
                camMoveSystem.FixedUpdate(newTime, TimeStep);

                accumulator -= TimeStep;
            }
            PhysicsManager.PostFixedUpdate();
        }

        internal static Dictionary<BitSet, List<BaseSystem>> notifySystems;
        internal static Dictionary<BitSet, List<BaseSystem>> notifyAnySystems;
        internal static Dictionary<BitSet, List<BaseSystem>> collisionAnySystems;


        protected virtual void Scene_RegisterSystems() { }

        protected void RegisterSystem(BaseSystem system)
        {
            Type type = system.GetType();
            if (userSystemTypes.Contains(type)) // Duplicate system
                return;

            userSystemTypes.Add(type);
            userSystems.Add(system);
        }

        private void AddSystemFromAttribute(Type type, BaseSystem system, Type attributeType, Dictionary<BitSet, List<BaseSystem>> dict)
        {
            if (!typeof(Attribute).IsAssignableFrom(attributeType))
            {
                return;
            }

            object[] attributes = type.GetCustomAttributes(attributeType, false);
            if (attributes.Length > 0)
            {
                ComponentType[] subTypes = null;
                for (int i = 0; i < attributes.Length; i++)
                {
                    dynamic attribute = attributes[i];
                    subTypes = new ComponentType[attribute.ComponentTypes.Length];
                    for (int t = 0; t < attribute.ComponentTypes.Length; t++)
                    {
                        subTypes[t] = attribute.ComponentTypes[t];
                    }
                }

                var bitSet = subTypes.ToBitSet();
                BitSet foundBitSet = null;

                if (dict.Count > 0)
                {
                    foreach (var exBitSet in dict.Keys)
                    {
                        if (exBitSet.All(bitSet))
                        {
                            foundBitSet = exBitSet;
                            break;
                        }
                    }
                }

                if (foundBitSet != null)
                    dict[foundBitSet].Add(system);
                else
                    dict.Add(bitSet, new List<BaseSystem> { system });

            }
        }

        private protected void SubscribeSystems()
        {
            notifySystems = new Dictionary<BitSet, List<BaseSystem>>();
            notifyAnySystems = new Dictionary<BitSet, List<BaseSystem>>();
            collisionAnySystems = new Dictionary<BitSet, List<BaseSystem>>();

            for (int s = 0; s < userSystemTypes.Count; s++)
            {
                Type type = userSystemTypes[s];
                BaseSystem system = userSystems[s];

                // Subscribe entity creation
                AddSystemFromAttribute(type, system, typeof(SubscribeAttribute), notifySystems);

                // Subscribe collision
                AddSystemFromAttribute(type, system, typeof(CollisionEventAttribute), collisionAnySystems);

            }


            for (int s = 0; s < renderExtensions.Count; s++)
            {
                Type type = renderExtensions[s].GetType();
                BaseSystem system = renderExtensions[s];

                // Subscribe entity creation
                AddSystemFromAttribute(type, system, typeof(SubscribeAnyAttribute), notifyAnySystems);
            }


            // Render extensions - Subscribe Any
            //for (int s = 0; s < renderExtensions.Count; s++)
            //{
            //    BaseSystem system = renderExtensions[s];
            //    Type type = renderExtensions[s].GetType();

            //    object[] attributes = type.GetCustomAttributes(typeof(SubscribeAnyAttribute), false);
            //    if (attributes.Length > 0)
            //    {
            //        List<Type> subTypes = new List<Type>();
            //        for (int i = 0; i < attributes.Length; i++)
            //        {
            //            var attribute = (SubscribeAnyAttribute)attributes[i];
            //            for (int t = 0; t < attribute.ComponentTypes.Length; t++)
            //            {
            //                subTypes.Add(attribute.ComponentTypes[t]);
            //            }
            //        }

            //        TypeSignature typeSig = new TypeSignature(subTypes);
            //        if (notifyAnySystems.ContainsKey(typeSig))
            //            notifyAnySystems[typeSig].Add(system);
            //        else
            //            notifyAnySystems.Add(typeSig, new List<BaseSystem> { system });
            //    }
            //}
        }

        private protected void MainUpdate(float newTime, float elapsed, float interpolation)
        {
            // Active Cam Update
            if (_checkCamUpdate)
            {
                activeCam = null;
                var camQ = new QueryDescription().WithAll<Camera, Transform>();
                GameWorld.Query(in camQ, (in Entity camEnt) =>
                {
                    if (camEnt != Entity.Null)
                    {
                        activeCam = camEnt.Get<Transform>();
                        return;
                    }
                });
                _checkCamUpdate = false;
            }

            foreach (var system in userSystems)
            {
                system.Update(newTime, elapsed);
            }
            tweenSystem.Update(newTime, elapsed);
            spriteAnimatorSystem.Update(newTime, elapsed);
            stateAnimatorSystem.Update(newTime, elapsed);
            spriteAnimSystem.Update(newTime, elapsed);
            particleSystem.Update(newTime, elapsed);
            rbMoveSystem.Update(newTime, interpolation);
            camMoveSystem.Update(newTime, elapsed);
            spriteBatchSystem.Update(newTime, elapsed);
            meshRenderSystem.Update(newTime, elapsed);
            lightRenderSystem.Update(newTime, elapsed);
            normalsRenderSystem.Update(newTime, elapsed);
            //if(debug)
            //    colDebugSystem.Update(newTime, elapsed);
        }

        //private protected void MainRender()
        //{
        //    if (Game.activeCam == null)
        //        return;

        //    var camEnt = Game.activeCam.entity;
        //    if (camEnt == Entity.Null)
        //        return;

        //    Vector3 forward = Vector3.Transform(-Vector3.UnitZ, Game.activeCam.worldRotation);
        //    Vector3 cameraPosition = Game.activeCam.worldPosition;
        //    Vector3 targetPosition = cameraPosition + forward;
        //    Vector3 up = Vector3.Transform(Vector3.UnitY, Game.activeCam.worldRotation);

        //    Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, targetPosition, up);

        //    Game.pipelineData.View = view;
        //    gd.UpdateBuffer(Game.pipelineBuffer, 0, Game.pipelineData);

        //    for (int i = 0; i < GraphicsManager.renderLayers.Count; i++)
        //    {
        //        _commandList.SetFramebuffer(mainRenderFB);
        //        _commandList.SetFullViewports();
        //        _commandList.ClearColorTarget(0, new RgbaFloat(0f, 0f, 0f, 0f));
        //        _commandList.ClearDepthStencil(1f);

        //        spriteBatchSystem.Render(i);

        //        lightRenderSystem.Render(i);

        //        // Composition / No Clear - No depth
        //        _commandList.SetFramebuffer(compositeRenderFB);
        //        _commandList.SetFullViewports();
        //        _commandList.SetPipeline(GraphicsManager.CompositePipeline);

        //        _commandList.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
        //        _commandList.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.UInt16);

        //        _commandList.SetGraphicsResourceSet(0, compositeRSSetLight);
        //        _commandList.DrawIndexed(6, 1, 0, 0, 0);
        //    }

        //    if (debug)
        //    {
        //        colDebugSystem.Render();
        //    }
        //}

        private protected void MainRender3D()
        {
            if (Game.activeCam == null)
                return;

            var camEnt = Game.activeCam.entity;
            if (camEnt == Entity.Null)
                return;

            Vector3 forward = Vector3.Transform(-Vector3.UnitZ, Game.activeCam.worldRotation);
            Vector3 cameraPosition = Game.activeCam.worldPosition;
            Vector3 targetPosition = cameraPosition + forward;
            Vector3 up = Vector3.Transform(Vector3.UnitY, Game.activeCam.worldRotation);

            Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, targetPosition, up);

            Game.pipelineData.View = view;
            gd.UpdateBuffer(Game.pipelineBuffer, 0, Game.pipelineData);


            foreach (var render in internalRenders)
            {
                for (int i = 0; i < GraphicsManager.renderLayers.Count; i++)
                    render.Render(i);
            }

            // Composition / No Clear - No depth
            _commandList.SetFramebuffer(compositeRenderFB);
            _commandList.SetFullViewports();
            _commandList.SetPipeline(GraphicsManager.CompositePipeline);

            _commandList.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
            _commandList.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.UInt16);

            _commandList.SetGraphicsResourceSet(0, compositeRSSetLight);
            _commandList.DrawIndexed(6, 1, 0, 0, 0);
        }

        void LateRender()
        {
            //spriteBatcher.LateRender();
        }

        private static Matrix4x4 CreatePerspective(float fov, float aspectRatio, float near, float far)
        {
            if (fov <= 0.0f || fov >= MathF.PI)
                throw new ArgumentOutOfRangeException(nameof(fov));

            if (near <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(near));

            if (far <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(far));

            float yScale = 1.0f / MathF.Tan(fov * 0.5f);
            float xScale = yScale / aspectRatio;

            Matrix4x4 result;

            result.M11 = xScale;
            result.M12 = result.M13 = result.M14 = 0.0f;

            result.M22 = yScale;
            result.M21 = result.M23 = result.M24 = 0.0f;

            result.M31 = result.M32 = 0.0f;
            var negFarRange = float.IsPositiveInfinity(far) ? -1.0f : far / (near - far);
            result.M33 = negFarRange;
            result.M34 = -1.0f;

            result.M41 = result.M42 = result.M44 = 0.0f;
            result.M43 = near * negFarRange;

            return result;
        }


        internal static void RefreshProjection(Canvas canvas)
        {
            if (canvas == null || Game.canvas != canvas)
                return;

            //projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, canvas.canvasSize.X / 100f, 0, canvas.canvasSize.Y / 100f, -1000f, 1000f);
            //projectionMatrix = CreatePerspective(MathF.PI / 4f, canvas.canvasSize.X / canvas.canvasSize.Y, 1000f, 0.1f);
            projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, canvas.canvasSize.X / canvas.canvasSize.Y, 0.1f, 1000f);
            Game.pipelineData.Projection = Game.projectionMatrix;
            onCanvasResize?.Invoke();
        }

        protected void SetupGraphics(string windowName)
        {
            // Graphics
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = (int)(1280 * 1f),
                WindowHeight = (int)(720 * 1f),
                WindowTitle = windowName,
            };
            window = VeldridStartup.CreateWindow(ref windowCI);
            screenSize = new Vector2(window.Width, window.Height);
            canvas = new Canvas(window.Width, window.Height);
            canvas.isDynamicSize = false;
            canvas.referenceSize = new Vector2(1280f, 720f);

            
            window.Resized += () =>
            {
                //canvas.canvasSize = new Vector2(window.Width, window.Height);
                screenSize = new Vector2(window.Width, window.Height);
                canvas.UpdateScreenSize(screenSize);

                //pipelineData.Resolution = canvas.canvasSize;
                //gd.UpdateBuffer(pipelineBuffer,0, pipelineData);
                gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);

                onWindowResize?.Invoke();

                reload = true;
                resize = true;
            };

            GraphicsDeviceOptions options = new GraphicsDeviceOptions();
            //_window = new Sdl2Window("Snake", 50, 50, width, height, SDL_WindowFlags., false);
            options.SyncToVerticalBlank = true;
            options.ResourceBindingModel = ResourceBindingModel.Improved;

            //gd = VeldridStartup.CreateGraphicsDevice(window, options);

            var backend = VeldridStartup.GetPlatformDefaultBackend();


            gd = VeldridStartup.CreateGraphicsDevice(window, new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: null,
                syncToVerticalBlank: true,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true,
                swapchainSrgbFormat: false
            ), backend);

            
            rf = new DisposeCollectorResourceFactory(gd.ResourceFactory);
            _commandList = gd.ResourceFactory.CreateCommandList();

            // Graphics Manager
            GraphicsManager.gd = gd;
            GraphicsManager.rf = rf;
            GraphicsManager.cl = _commandList;

            CreateRenderResources();

        }

        void CreateRenderResources()
        {
            Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;

            compositeRenderTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
               mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled));
        
            compositeRenderFB = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(null, compositeRenderTexture));

            GraphicsManager.LoadPipelines(gd, _commandList, compositeRenderFB);

            finalQuadRSSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                   GraphicsManager.sharedTextureLayout,
                   compositeRenderTexture, gd.LinearSampler
                   ));

            pipelineBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(144, BufferUsage.UniformBuffer));
            pipelineSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(GraphicsManager.sharedPipelineLayout, pipelineBuffer));

            pipelineData = new PipelineData()
            {
                Projection = Matrix4x4.Identity,
                View = Matrix4x4.Identity,
                Resolution = screenSize,
                Time = 0f,
                Padding = 0f
            };
        }

        protected virtual void CreateWorlds()
        {
            // Box2D World
            Vector2 gravity = new Vector2(0f, -9.81f);
            B2DWorld = new Box2D.NetStandard.Dynamics.World.World(gravity);
            B2DWorld.SetContactListener(new B2DContactListener());

            // ECS World
            GameWorld = World.Create();
            GameWorld.SubscribeComponentAdded((in Entity entity, ref Transform transform) =>
            {
                transform.SetEntity(entity);
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Rigidbody rb) =>
            {
                if (rb.transform != null)
                    return;
                rb.SetEntity(entity.Get<Transform>());
                if (b2dInitSystem.started)
                    PhysicsManager.CreateBody(rb);
            });

            //GameWorld.SubscribeComponentSet((in Entity entity, ref Rigidbody rb) =>
            //{
            //    //if (rb.transform == null)
            //    //    return;
            //    rb.SetEntity(entity.Get<Transform>());
            //    if (b2dInitSystem.started)
            //        PhysicsManager.CreateBody(rb);
            //});

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Sprite sprite) =>
            {
                sprite.SetTransform(entity.Get<Transform>());
                if(!sprite.manualBatching)
                    Game.spriteBatchSystem.UpdateSpriteBatch(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Camera cam) => TriggerCamCheck());

            GameWorld.SubscribeComponentAdded((in Entity entity, ref AABB newBB) =>
            {
                if (newBB == null)
                    return;

                if (!newBB.sizeSet && entity.Has<Sprite>())
                {
                    var spriteSize = entity.Get<Sprite>().size;
                    newBB.size = spriteSize;
                    newBB.sizeSet = true;
                }
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref StateMatchAnimator animator) => animator.SetTransform(entity.Get<Transform>()));

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref Sprite sprite) =>
            {
                if (!sprite.manualBatching)
                    Game.spriteBatchSystem.RemoveSprite(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref ParticleModule pm) => pm.Stop());

            //GameWorld.SubscribeComponentRemoved((in Entity entity, ref Rigidbody rb) => rb.Destroy());

            //GameWorld.OnEnable((Entity entity, Sprite sprite) =>
            //{
            //    Game.spriteBatchSystem.UpdateSpriteBatch(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            //});


            //GameWorld.OnDisable((Entity entity, Sprite sprite) =>
            //{
            //    Game.spriteBatchSystem.RemoveSprite(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            //});

            //GameWorld.OnEnable((Entity entity, Rigidbody rb) =>
            //{
            //    rb.SetBodyEnabled(true);
            //});


            //GameWorld.OnDisable((Entity entity, Rigidbody rb) =>
            //{
            //    rb.SetBodyEnabled(false);
            //});

            //GameWorld.OnEnable((Entity entity, Tweener tweener) =>
            //{
            //    tweener.Pause(false);
            //});


            //GameWorld.OnDisable((Entity entity, Tweener tweener) =>
            //{
            //    tweener.Pause(true);
            //});

            PrefabManager.SceneInit();
        }

        internal static void TriggerCamCheck()
        {
            _checkCamUpdate = true;
        }

        internal static LightRenderSystem GetLightRenderer()
        {
            return lightRenderSystem;
        }

        protected void CleanUp()
        {
            gd.WaitForIdle();

            CoroutineManager.StopAllCoroutines();

            // Clean extensions
            foreach (var rendExt in renderExtensions)
            {
                rendExt.CleanUp(false, false);
            }

            // Clean systems
            foreach (var system in userSystems)
            {
                system.CleanUp(false, false);
            }


            // Clean up Veldrid resources
            rf.DisposeCollector.DisposeAll();
            GraphicsManager.DisposeResources();
            AssetCache.DisposeResources();
            _commandList.Dispose();
            gd.Dispose();

            Console.WriteLine("Clean");

            foreach (var item in internalRenders)
            {
                item.CleanUp(false, false, true);
            }

            compositeRenderTexture.Dispose();
            compositeRenderFB.Dispose();
        }

        void CleanRenderResources()
        {
            foreach (var item in internalRenders)
            {
                item.CleanUp(false, false, true);
            }

            compositeRenderTexture.Dispose();
            compositeRenderFB.Dispose();
        }

        protected void PhysicsUpdate()
        {
           

            // Instruct the world to perform a single step of simulation. It is
            // generally best to keep the time step and iterations fixed.

            B2DWorld?.Step(TimeStep, VelocityIterations, PositionIterations);
            B2DWorld.ClearForces();
        }

        public void AddRenderExtension(RenderSystem renderSystem)
        {
            renderExtensions.Add(renderSystem);
        }
   
        protected virtual void Scene_Setup()
        {

        }

        protected virtual string SaveScene()
        {
            AssetCache.ClearSerializeDependencies();

            JsonObjectBuilder scene = new JsonObjectBuilder(10000);
            scene.Put("SceneName", "Test");

            JsonArrayBuilder extensions = new JsonArrayBuilder(1000);
            foreach (var rendExt in renderExtensions)
            {
                JsonObjectBuilder extObj = new JsonObjectBuilder(200);
                extObj.Put("Type", rendExt.GetType().ToString());
                extensions.Push(extObj.Build());
            }
            scene.Put("Extensions", extensions.Build());

            // Scene Objects
            //scene.Put("Canvas", canvas.Serialize());

            var query = new QueryDescription().WithAll<Transform>();
            var entities = new List<Entity>();
            Game.GameWorld.GetEntities(query, entities);


            JsonArrayBuilder entArr = new JsonArrayBuilder(10000);
            foreach (var entity in entities)
            {
                if (entity.Get<Transform>().tag.StartsWith("Editor"))
                    continue;

                JsonObjectBuilder entObj = new JsonObjectBuilder(10000);
                entObj.Put("GUID", entity.Get<Guid>().ToString());
                entObj.Put("Name", entity.Get<string>());

                JsonArrayBuilder compArr = new JsonArrayBuilder(10000);
                var comps = entity.GetAllComponents();
                var types = entity.GetComponentTypes();

                // Serialize transform first
                int transIndex = Array.IndexOf(types, typeof(Transform));
                compArr.Push(((JSerializable)comps[transIndex]).Serialize());

                for (int i = 0; i < comps.Length; i++)
                {
                    if (types[i].Type == typeof(Transform))
                        continue;

                    if (typeof(JSerializable).IsAssignableFrom(types[i].Type))
                    {
                        compArr.Push(((JSerializable)comps[i]).Serialize());
                    }
                    else if (types[i].Type.IsSubclassOf(typeof(ABComponent)))
                    {
                        //ompArr.Push(((AutoSerializable)comps[i]).Serialize());
                        compArr.Push(ABComponent.Serialize((ABComponent)comps[i]));

                    }
                }

                entObj.Put("Components", compArr.Build());
                entArr.Push(entObj.Build());
            }

            scene.Put("Assets", AssetCache.SerializeAssets());
            scene.Put("Entities", entArr.Build());

            //Console.WriteLine(scene.Build().ToString());

            return scene.Build().ToString();
        }

        protected void LoadScene(string json)
        {
            JValue scene = JValue.Parse(json);

            // Assets
            var jAssets = scene["Assets"];
            AssetCache.ClearSerializeDependencies();
            AssetCache.DeserializeAssets(jAssets);

            //canvas.Deserialize(scene["Canvas"].ToString());
            //projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, canvas.canvasSize.X / 100f, 0, canvas.canvasSize.Y / 100f, 1, -1);

            foreach (var extObj in scene["Extensions"].Array())
            {
                string extTypeStr = extObj["Type"];
                RenderSystem rendExt = (RenderSystem)Activator.CreateInstance(Type.GetType(extTypeStr));
                renderExtensions.Add(rendExt);
            }


            //window.Title = window.Title + " - " + scene["SceneName"];
            foreach (var entity in scene["Entities"].Array())
            {
                string entName = entity["Name"];
                string guid = entity["GUID"];
                Entity newEnt = GameWorld.Create(entName, Guid.Parse(guid));
                bool isCanvasEnt = false;

                foreach (var component in entity["Components"].Array())
                {
                    Type type = Type.GetType(component["type"]);

                    if (type == null)
                        type = UserTypes.FirstOrDefault(t => t.ToString().Equals(component["type"]));

                    if (type == null)
                        continue;

                    if (type == typeof(Canvas))
                        isCanvasEnt = true;

                    if (typeof(JSerializable).IsAssignableFrom(type))
                    {
                        //var serializedComponent = (JSerializable)Activator.CreateInstance(type);
                        //serializedComponent.Deserialize(component.ToString());

                        //newEnt.Add(serializedComponent);
                        //newEnt.Set(type, serializedComponent);

                        var comp = DeserializeComponent(type, component.ToString());
                        newEnt.Add(comp);
                        //AddSerializedComponent(type, component.ToString(), newEnt);
                    }
                    else if (type.IsSubclassOf(typeof(ABComponent)))
                    {
                        var comp = ABComponent.Deserialize(component.ToString(), type);
                        newEnt.Add(comp);
                    }

                }

                if (isCanvasEnt)
                {
                    canvas = newEnt.Get<Canvas>();
                    canvas.UpdateScreenSize(screenSize);
                    Game.canvas.UpdateCanvasSize(canvas.canvasSize);
                }
            }

            var query = new QueryDescription().WithAll<Transform>();
            var entities = new List<Entity>();
            Game.GameWorld.GetEntities(query, entities);

            // Parenting

            foreach (var entity in entities)
            {
                Transform trans = entity.Get<Transform>();
                if (!string.IsNullOrEmpty(trans.parentGuidStr))
                {
                    Guid parGuid = Guid.Parse(trans.parentGuidStr);
                    trans.SetParent(entities.FirstOrDefault(e => e.Get<Guid>().Equals(parGuid)).Get<Transform>(), false);
                }
            }

            // References
            foreach (var entity in entities)
            {
                var comps = entity.GetAllComponents();
                var types = entity.GetComponentTypes();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (typeof(JSerializable).IsAssignableFrom(types[i].Type))
                    {
                        ((JSerializable)comps[i]).SetReferences();
                    }
                    else if (types[i].Type.IsSubclassOf(typeof(ABComponent)))
                    {
                        ABComponent.SetReferences(((ABComponent)comps[i]));
                    }
                }
            }

        }

        public object DeserializeComponent(Type type, string serializedComponent)
        {
            var method = GetType().GetMethod(nameof(DeserializeComponentGeneric)).MakeGenericMethod(type);
            return method.Invoke(this, new object[] { serializedComponent });
        }

        public T DeserializeComponentGeneric<T>(string serializedComponent) where T : JSerializable, new()
        {
            T component = new T();
            component.Deserialize(serializedComponent);
            return component;
        }

      

        protected virtual void DrawBegin()
        {
            _commandList.Begin();

            _commandList.SetFramebuffer(compositeRenderFB);
            _commandList.SetFullViewports();
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
        }


        private void FinalRender()
        {
            _commandList.SetFramebuffer(gd.MainSwapchain.Framebuffer);
            _commandList.SetFullViewports();
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.SetPipeline(GraphicsManager.FullScreenPipeline);

            _commandList.SetGraphicsResourceSet(0, finalQuadRSSet);
            _commandList.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
            _commandList.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.UInt16);
            _commandList.DrawIndexed(6, 1, 0, 0, 0);

            UIRender();

            _commandList.End();
            gd.SubmitCommands(_commandList);
            gd.WaitForIdle();
            gd.SwapBuffers();
        }

        private void UIRender()
        {
            foreach (var rendExt in renderExtensions)
            {
                rendExt.UIRender();
            }
        }

    }
}
