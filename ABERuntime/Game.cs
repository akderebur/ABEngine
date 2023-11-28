﻿using System;
using System.Diagnostics;
using WGIL;
using Buffer = WGIL.Buffer;
using Halak;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ABEngine.ABERuntime.Pipelines;
using ABEngine.ABERuntime.Debug;
using ABEngine.ABERuntime.Physics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Animation.StateMatch;
using Arch.Core.Utils;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Extensions.Internal;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Core.Assets;
using WGIL.IO;

namespace ABEngine.ABERuntime
{
    internal enum GameMode
    {
        Runtime,
        Editor
    }

    public class Game
    {
        internal static WGILContext wgil;

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
        public static Vector2 pixelSize;
        public static Vector2 virtualSize;
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
        protected Tweening.TweenSystem tweenSystem;
        //protected private ColliderDebugSystem colDebugSystem;
        protected private ParticleModuleSystem particleSystem;

        // Render Systems
        public static NormalsPassRenderSystem normalsRenderSystem;
        protected MeshRenderSystem meshRenderSystem;
        public static SpriteBatchSystem spriteBatchSystem;
        //internal static MSAAResolveSystem msaaResolveSystem;
        public static LightRenderSystem lightRenderSystem;

        public List<RenderSystem> internalRenders;

        // Framebuffer
        protected BindGroup mainPPQuadRSSet;
        protected BindGroup finalQuadRSSet;

        public static PipelineData pipelineData;

        public static Buffer pipelineBuffer;
        public static BindGroup pipelineSet;

        protected private  static bool reload = false;
        protected private  static bool newScene = false;
        protected private bool resize = false;

        internal static bool debug = false;

        internal static float zoomFactor = 1f;

        internal static Game Instance;
        internal static ResourceContext resourceContext;

        // Render Passes
        RenderPass normalsPass, mainPass, mainPPPass, lightPass, fsPass; //debugPass

        public Game(bool debug, List<Type> userTypes)
        {
            Instance = this;
            resourceContext = new ResourceContext();

            UserTypes = userTypes;
            userSystems = new List<BaseSystem>();
            userSystemTypes = new List<Type>();
            AppPath = System.IO.Directory.GetCurrentDirectory() + "/";
            AssetPath = AppPath + "Assets/";
            Game.debug = debug;
            gameMode = GameMode.Runtime;
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

        void NormalsPassWork(RenderPass pass)
        {
            // First pass setup
            if (Game.activeCam != null)
            {
                var camEnt = Game.activeCam.entity;
                if (camEnt != Entity.Null)
                {
                    Vector3 forward = Vector3.Transform(-Vector3.UnitZ, Game.activeCam.worldRotation);
                    Vector3 cameraPosition = Game.activeCam.worldPosition;
                    Vector3 targetPosition = cameraPosition + forward;
                    Vector3 up = Vector3.Transform(Vector3.UnitY, Game.activeCam.worldRotation);

                    Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, targetPosition, up);

                    Game.pipelineData.View = view;
                    wgil.WriteBuffer(pipelineBuffer, pipelineData);
                }
            }

            normalsRenderSystem.Render(pass);
        }

        void MainPassWork(RenderPass pass)
        {
            meshRenderSystem.Render(pass);
            for (int i = 0; i < GraphicsManager.renderLayers.Count; i++)
            {
                spriteBatchSystem.Render(pass, i);

                // Dpeth Clear
                //pass.SetPipeline(GraphicsManager.DepthClearPipeline);
                //pass.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
                //pass.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.Uint16);
                //pass.DrawIndexed(6);
            }

        }

        void MainPPWork(RenderPass pass)
        {
            resourceContext.CopyScreenTexture(pass);

            //pass.SetPipeline(GraphicsManager.FullScreenPipeline);
            //pass.SetBindGroup(0, mainPPQuadRSSet);
            //pass.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
            //pass.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.Uint16);
            //pass.DrawIndexed(6);

            spriteBatchSystem.RenderPP(pass, GraphicsManager.renderLayers.Count - 1);
        }

        void LightPassWork(RenderPass pass)
        {
            lightRenderSystem.Render(pass);
        }

        void FinalPassWork(RenderPass pass)
        {
            pass.SetPipeline(GraphicsManager.FullScreenPipeline);
            pass.SetBindGroup(0, finalQuadRSSet);
            pass.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
            pass.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.Uint16);
            pass.DrawIndexed(6);

            //UIRender();
        }

        //void DebugPassWork(RenderPass pass)
        //{
        //    colDebugSystem.Render(pass);
        //}

        protected private void CreateInternalRenders()
        { 
            normalsRenderSystem = new NormalsPassRenderSystem();
            meshRenderSystem = new MeshRenderSystem();
            spriteBatchSystem = new SpriteBatchSystem();
            //msaaResolveSystem = new MSAAResolveSystem();
            lightRenderSystem = new LightRenderSystem();


            // Create Passes
            var normalsPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                IsDepthClear = true,
                ClearColor = new WGIL.Color(0, 0, 0, 0),
                DepthValue = 1f,
                DepthAttachment = resourceContext.normalsDepthView,
                ColorAttachments = new TextureViewSet()
                {
                    TextureViews = new[]
                    {
                        resourceContext.cameraNormalView
                    }
                }
            };

            normalsPass = wgil.CreateRenderPass(ref normalsPassDesc);
            normalsPass.JoinRenderQueue(NormalsPassWork);

            var mainPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                IsDepthClear = true,
                ClearColor = new WGIL.Color(0, 0, 0, 0),
                DepthValue = 1,
                DepthAttachment = resourceContext.mainDepthView,
                ColorAttachments = new TextureViewSet()
                {
                    TextureViews = new[]
                    {
                        resourceContext.mainRenderView,
                        resourceContext.spriteNormalsView
                    }
                }
            };

            mainPass = wgil.CreateRenderPass(ref mainPassDesc);
            mainPass.JoinRenderQueue(MainPassWork);

            // Main PostProcess

            var mainPPDesc = new RenderPassDescriptor()
            {
                IsColorClear = false,
                IsDepthClear = false,
                ClearColor = new WGIL.Color(0, 0, 0, 0),
                DepthAttachment = resourceContext.mainDepthView,
                ColorAttachments = new TextureViewSet()
                {
                    TextureViews = new[]
                    {
                        resourceContext.mainRenderView,
                        resourceContext.spriteNormalsView
                    }
                }
            };

            //mainPPPass = wgil.CreateRenderPass(ref mainPPDesc);
            //mainPPPass.JoinRenderQueue(MainPPWork);

            var lightPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                ClearColor = new WGIL.Color(0f, 0f, 0f, 0f),
                ColorAttachments = new TextureViewSet()
                {
                    TextureViews = new[]
                    {
                        resourceContext.lightRenderView
                    }
                }
            };

            lightPass = wgil.CreateRenderPass(ref lightPassDesc);
            lightPass.JoinRenderQueue(LightPassWork);

            var fsPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                ClearColor = new WGIL.Color(0f, 0f, 0f, 1f),
                IsRenderSwapchain = true
            };
            fsPass = wgil.CreateRenderPass(ref fsPassDesc);
            fsPass.JoinRenderQueue(FinalPassWork);

            wgil.AddRenderPass(normalsPass);
            wgil.AddRenderPass(mainPass);
            //wgil.AddRenderPass(mainPPPass);
            wgil.AddRenderPass(lightPass);
            wgil.AddRenderPass(fsPass);

            internalRenders = new List<RenderSystem>()
            {
                normalsRenderSystem,
                meshRenderSystem,
                spriteBatchSystem,
                //msaaResolveSystem,
                lightRenderSystem
            };

            SetupRenderResources();
        }  

        protected private void SetupRenderResources()
        {
            meshRenderSystem.SetupResources();
            //msaaResolveSystem.SetupResources(resourceContext.mainRenderTexture, resourceContext.spriteNormalsTexture, resourceContext.mainDepthTexture);
            lightRenderSystem.SetupResources(resourceContext.mainRenderView, resourceContext.spriteNormalsView);
        }

        private void CheckResize()
        {
            return;
            if (resize)
            {
                resize = false;

                // Resize render targets
                finalQuadRSSet.Dispose();
                foreach (var render in internalRenders)
                    render.CleanUp(true, false, true);

                resourceContext.RecreateFrameResources((uint)pixelSize.X, (uint)pixelSize.Y);

                // Update pass attachments
                TextureViewSet newSet = new TextureViewSet();

                normalsPass.UpdateDepthAttachment(resourceContext.normalsDepthView);
                newSet.TextureViews = new[] { resourceContext.cameraNormalView };
                normalsPass.UpdateColorAttachments(ref newSet);

                mainPass.UpdateDepthAttachment(resourceContext.mainDepthView);
                newSet.TextureViews = new[] { resourceContext.mainRenderView, resourceContext.spriteNormalsView };
                mainPass.UpdateColorAttachments(ref newSet);

                mainPPPass.UpdateDepthAttachment(resourceContext.mainDepthView);
                mainPPPass.UpdateColorAttachments(ref newSet);

                newSet.TextureViews = new[] { resourceContext.lightRenderView };
                lightPass.UpdateColorAttachments(ref newSet);

                var finalQuadDesc = new BindGroupDescriptor()
                {
                    BindGroupLayout = GraphicsManager.sharedTextureLayout,
                    Entries = new BindResource[]
                    {
                        resourceContext.lightRenderView,
                        GraphicsManager.linearSampleClamp
                    }
                };

                finalQuadRSSet = wgil.CreateBindGroup(ref finalQuadDesc).SetManualDispose(true);

                SetupRenderResources();

                pipelineData = new PipelineData()
                {
                    Projection = Matrix4x4.Identity,
                    View = Matrix4x4.Identity,
                    PixelSize = pixelSize,
                    Time = 0,
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
        }

        void CheckNewScene()
        {
            if(newScene)
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

                //if (debug)
                //    colDebugSystem.CleanUp(true, true);

                // Clean Resources
                AssetCache.DisposeResources();
                wgil.DisposeResources(false);

                GraphicsManager.ResetPipelines();

                foreach (var render in internalRenders)
                {
                    render.CleanUp(true, true, false);
                    render.SceneSetup();
                }

                // Reset Camera
                Game.activeCam = null;
                TriggerCamCheck();

                //LineDbgPipelineAsset lineDbgPipelineAsset = new LineDbgPipelineAsset();

                // Systems
                camMoveSystem = new CameraMovementSystem();
                b2dInitSystem = new B2DInitSystem();
                spriteAnimatorSystem = new SpriteAnimatorSystem();
                stateAnimatorSystem = new StateAnimatorSystem();
                spriteAnimSystem = new SpriteAnimSystem();
                rbMoveSystem = new RigidbodyMoveSystem();
                tweenSystem = new Tweening.TweenSystem();
                particleSystem = new ParticleModuleSystem();
                spriteBatchSystem = new SpriteBatchSystem();
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
                if (!GraphicsManager.render2DOnly)
                {
                    normalsRenderSystem.Start();
                    meshRenderSystem.Start();
                }
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
        }

        private protected virtual void MainLoop(float newTime, float elapsed)
        {
            Time = newTime;
            pipelineData.Time = Time;

            EntityManager.CheckEntityChanges();

            if (Input.GetKeyDown(Key.KeyR))
            {
                reload = true;
                newScene = true;
            }

            if (reload)
            {
                reload = false;
                CheckResize();
                CheckNewScene();
            }

            MainFixedUpdate(newTime, elapsed);
            interpolation = accumulator / TimeStep;
            MainUpdate(newTime, elapsed, interpolation);
            foreach (var rendExt in renderExtensions)
            {
                rendExt.Update(newTime, elapsed);
            }
        }

        float accumulator;
        float interpolation;
        protected virtual void Init(string windowName)
        {
            // ECS and Physics Worlds
            CreateWorlds();

            // Init
            PhysicsManager.ResetPhysics();
            GraphicsManager.InitSettings();

            // WGIL 
            SetupGraphics(windowName);
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
                        RefreshProjection(canvas);
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
            if (!GraphicsManager.render2DOnly)
            {
                normalsRenderSystem.Update(newTime, elapsed);
                meshRenderSystem.Update(newTime, elapsed);
            }
            lightRenderSystem.Update(newTime, elapsed);
            //if(debug)
            //    colDebugSystem.Update(newTime, elapsed);
        }

      
        private protected void MainRender()
        {
          

        }

        void LateRender()
        {
            //spriteBatcher.LateRender();
        }

        internal static void RefreshProjection(Canvas canvas)
        {
            if (canvas == null || Game.canvas != canvas || Game.activeCam == null)
                return;

            Camera camera = Game.activeCam.entity.Get<Camera>();
            if (camera.cameraProjection == CameraProjection.Orthographic)
            {
                Vector2 extents = canvas.canvasSize / 2f / 100f;
                projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(-extents.X, extents.X, -extents.Y, extents.Y, -1000f, 1000f);
            }
            else
                projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, canvas.canvasSize.X / canvas.canvasSize.Y, 0.1f, 1000f);

            //projectionMatrix = CreatePerspective(MathF.PI / 4f, canvas.canvasSize.X / canvas.canvasSize.Y, 1000f, 0.1f);

            Game.pipelineData.Projection = Game.projectionMatrix;
            onCanvasResize?.Invoke();
        }

        protected void SetupGraphics(string windowName)
        {
            wgil = new WGILContext();
            wgil.OnStart += SetupComplete;
            wgil.OnUpdate += MainLoop;
            wgil.OnResize += Window_Resized;

            // Window and Graphics
            _ = wgil.Start(windowName, new WindowInfo()
            {
                Width = 800,
                Height = 600,
                CSSFullSize = true
            });

            //wgil.DisposeResources(true);
        }

        private void Window_Resized()
        {
            // Physical Size
            uint pw = wgil.GetWidth();
            uint ph = wgil.GetHeight();
            pixelSize = new Vector2(pw, ph);

            uint w = wgil.GetWidthLogical();
            uint h = wgil.GetHeightLogical();
            virtualSize = new Vector2(w, h);
            canvas.UpdateScreenSize(virtualSize);
            onWindowResize?.Invoke();
            wgil.logicalSize = virtualSize;

            resize = true;
            reload = true;
        }

        private void Window_Closing()
        {
            wgil.Stop();
        }

        private protected virtual void SetupComplete()
        {
            uint pw = wgil.GetWidth();
            uint ph = wgil.GetHeight();

            uint w = wgil.GetWidthLogical();
            uint h = wgil.GetHeightLogical();

            pixelSize = new Vector2(pw, ph);
            virtualSize = new Vector2(w, h);
            canvas = new Canvas(w, h);
            canvas.isDynamicSize = false;
            canvas.referenceSize = new Vector2(1280f, 720f);
            wgil.logicalSize = virtualSize;

            CreateRenderResources((uint)pw, (uint)ph);

            CreateInternalRenders();

            //if(debug)
            //{
            //    // Debug Pass
            //    var debugPassDesc = new RenderPassDescriptor()
            //    {
            //        IsColorClear = false,
            //        IsRenderSwapchain = true
            //    };
            //    debugPass = wgil.CreateRenderPass(ref debugPassDesc);
            //    debugPass.JoinRenderQueue(DebugPassWork);
            //    wgil.AddRenderPass(debugPass);
            //}

            foreach (var render in internalRenders)
                render.SceneSetup();

            AssetCache.InitAssetCache();

            EntityManager.Init();

            // Systems
            // Shared
            camMoveSystem = new CameraMovementSystem();
            b2dInitSystem = new B2DInitSystem();
            spriteAnimatorSystem = new SpriteAnimatorSystem();
            stateAnimatorSystem = new StateAnimatorSystem();
            spriteAnimSystem = new SpriteAnimSystem();
            rbMoveSystem = new RigidbodyMoveSystem();
            renderExtensions = new List<RenderSystem>();
            tweenSystem = new Tweening.TweenSystem();
            particleSystem = new ParticleModuleSystem();

            //if(debug)
            //{
            //    LineDbgPipelineAsset lineDbgPipelineAsset = new LineDbgPipelineAsset();
            //    colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);
            //}


            // Once in game lifetime
            Game_Init();

            Scene_Init();

            // User systems _ Reflection
            Scene_RegisterSystems();
            SubscribeSystems();

            Scene_Setup();
            onSceneLoad?.Invoke();

            RefreshProjection(Game.canvas);

            // Start Events
            b2dInitSystem.Start();
            rbMoveSystem.ResetSmoothStates();
            foreach (var system in userSystems)
            {
                system.Start();
            }

            //spriteRenderer.Start();
            spriteBatchSystem.Start();
            if (!GraphicsManager.render2DOnly)
            {
                normalsRenderSystem.Start();
                meshRenderSystem.Start();
            }
            spriteAnimatorSystem.Start();
            stateAnimatorSystem.Start();
            spriteAnimSystem.Start();
            camMoveSystem.Start();
            lightRenderSystem.Start();
            particleSystem.Start();
            //if (debug)
            //    colDebugSystem.Start();


            foreach (var rendExt in renderExtensions)
            {
                rendExt.Start();
            }
        }

        private protected void CreateRenderResources(uint pixelWidth, uint pixelHeight)
        {
            resourceContext.RecreateFrameResources(pixelWidth, pixelHeight);
            GraphicsManager.LoadPipelines();

            var finalQuadDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = GraphicsManager.sharedTextureLayout,
                Entries = new BindResource[]
                {
                    resourceContext.lightRenderView,
                    GraphicsManager.linearSampleClamp
                }
            };

            finalQuadRSSet = wgil.CreateBindGroup(ref finalQuadDesc).SetManualDispose(true);

            var ppQuadDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = GraphicsManager.sharedTextureLayout,
                Entries = new BindResource[]
                {
                    resourceContext.mainRenderView,
                    GraphicsManager.linearSampleClamp
                }
            };

            mainPPQuadRSSet = wgil.CreateBindGroup(ref ppQuadDesc).SetManualDispose(true);

            pipelineBuffer = wgil.CreateBuffer(144, BufferUsages.UNIFORM | BufferUsages.COPY_DST).SetManualDispose(true);

            var pipelineSetDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = GraphicsManager.sharedPipelineLayout,
                Entries = new BindResource[]
                {
                    pipelineBuffer
                }
            };
            pipelineSet = wgil.CreateBindGroup(ref pipelineSetDesc).SetManualDispose(true);

            pipelineData = new PipelineData()
            {
                Projection = Matrix4x4.Identity,
                View = Matrix4x4.Identity,
                PixelSize = pixelSize,
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

            GameWorld.SubscribeComponentAdded((in Entity entity, ref CircleCollider newCC) =>
            {
                if (!newCC.sizeSet && entity.Has<Sprite>())
                {
                    var spriteSize = entity.Get<Sprite>().size;
                    newCC.radius = spriteSize.X;
                    newCC.sizeSet = true;
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
            //});CreateOrthographicOffCenter


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
            scene.Put("Version", 0.1f);

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

            float sceneVersion = scene["Version"];
            SceneManager.sceneVersion = sceneVersion;


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
                    canvas.UpdateScreenSize(virtualSize);
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

  
        private void FinalRender()
        {
          
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
