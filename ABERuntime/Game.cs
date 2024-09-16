using System;
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
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Systems;
using WGIL.IO;
using ABEngine.ABERuntime.Windowing;
using Friflo.Engine.ECS;
using static SDL2.SDL;
using Entities = ABEngine.ABERuntime.ECS.Entities;
using Transform = ABEngine.ABERuntime.Components.Transform;

namespace ABEngine.ABERuntime
{
    public class Game
    {
        internal static WGILContext wgil;

        // Resources
        protected Sdl2Window window;

        // Worlds and Systems
        public static EntityStore GameWorld;
        public static Box2D.NetStandard.Dynamics.World.World B2DWorld;
        private List<Type> userSystemTypes;
        private protected List<BaseSystem> userSystems;
        protected List<RenderSystem> renderExtensions;
        
        // Global Vars
        public static string AppPath;
        public static string AssetPath;
        public static Entity activeCamera;
        public static PostProcess activePostProcess;
        public static Canvas canvas;
        public static Vector2 pixelSize;
        public static Vector2 virtualSize;
        private protected  static Matrix4x4 projectionMatrix;
        public static float Time;
        internal static List<Type> UserTypes;

        // Events
        public static event Action onWindowResize;
        public static event Action onSceneLoad;
        public static event Action onCanvasResize;
        
        // Flags
        private protected static bool _checkCamUpdate;
        
        // BOX2D
        private protected const float TimeStep = 1.0f / 50.0f;
        const int MAX_STEPS = 5;
        const int VelocityIterations = 8;
        const int PositionIterations = 3;
        
        // Systems
        private TRSSystem trsSystem;
        private BVHSystem bvhSystem;
        
        // Render Systems
        public static NormalsPassRenderSystem normalsRenderSystem;
        internal static MeshRenderSystem meshRenderSystem;
        public static SpriteBatchSystem spriteBatchSystem;
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

        protected private static InputDataSdl inputData = new InputDataSdl();

        // Render Passes
        RenderPass normalsPass, mainPass, mainPPPass, lightPass, fsPass;

        public Game(bool debug, List<Type> userTypes)
        {
            Instance = this;
            resourceContext = new ResourceContext();

            UserTypes = userTypes;
            userSystems = new List<BaseSystem>();
            userSystemTypes = new List<Type>();
            AppPath = System.IO.Directory.GetCurrentDirectory() + "/";
            AssetPath = AppPath + "Assets/";
            AssetPath = AssetPath.ToCommonPath();
            Game.debug = debug;
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
            normalsRenderSystem.Render(pass);
        }

        void AfterNormalsPassWork()
        {
            resourceContext.CopyDepthTexture();
        }

        void MainPassWork(RenderPass pass)
        {
            meshRenderSystem.Render(pass);
            spriteBatchSystem.Render(pass);
        }

        void AfterMainPassWork()
        {
            resourceContext.CopyScreenTexture();
        }

        void MainPPWork(RenderPass pass)
        {
            //pass.SetPipeline(GraphicsManager.FullScreenPipeline);
            //pass.SetBindGroup(0, mainPPQuadRSSet);
            //pass.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
            //pass.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.Uint16);
            //pass.DrawIndexed(6);

            /*pass.SetBindGroup(0, pipelineSet);
            spriteBatchSystem.RenderPP(pass, Graphics.renderLayers.Count - 1);
            meshRenderSystem.RenderPP(pass);*/
        }

        void LightPassWork(RenderPass pass)
        {
            pass.SetBindGroup(0, pipelineSet);
            lightRenderSystem.Render(pass);
        }

        void PreFinalWork()
        {
            if (activePostProcess != null && activePostProcess.BloomEnabled)
            {
                activePostProcess.BloomWork.BeginCompute();
            }
        }

        void FinalPassWork(RenderPass pass)
        {
            if (Game.activeCamera != null)
            {
                Camera cam = Game.activeCamera.GetComponent<Camera>();
                pass.SetViewport(Game.pixelSize.X * cam.viewport.X, Game.pixelSize.Y * cam.viewport.Y, Game.pixelSize.X * cam.viewport.Z, Game.pixelSize.Y * cam.viewport.W);
            }

            if (activePostProcess == null || !activePostProcess.BloomEnabled)
            {
                // No Post Process - HDR
                pass.SetPipeline(Graphics.fullScreenPipeline);
                pass.SetBindGroup(0, finalQuadRSSet);
                pass.SetVertexBuffer(0, Graphics.fullScreenVB);
                pass.SetIndexBuffer(Graphics.fullScreenIB, IndexFormat.Uint16);
                pass.DrawIndexed(6);
            }
            else
            {
                // Post Process - HDR
                pass.SetPipeline(PostProcess.fsPipeline);
                pass.SetBindGroup(0, activePostProcess.fsBindGroup);
                pass.SetVertexBuffer(0, Graphics.fullScreenVB);
                pass.SetIndexBuffer(Graphics.fullScreenIB, IndexFormat.Uint16);
                pass.DrawIndexed(6);
            }

            UIRender(pass);
        }

        protected private void CreateInternalRenders()
        { 
            normalsRenderSystem = new NormalsPassRenderSystem();
            meshRenderSystem = new MeshRenderSystem();
            spriteBatchSystem = new SpriteBatchSystem();
            lightRenderSystem = new LightRenderSystem();
            
            // Create Passes
            var normalsPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                IsDepthClear = true,
                ClearColor = new WGIL.Color(1, 1, 0.9f, 1),
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
            normalsPass.onPassComplete += AfterNormalsPassWork;

            var mainPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                IsDepthClear = false,
                ClearColor = new WGIL.Color(0, 0, 0, 0),
                DepthValue = 1,
                DepthAttachment = resourceContext.normalsDepthView,
                ColorAttachments = new TextureViewSet()
                {
                    TextureViews = new[]
                    {
                        resourceContext.mainRenderView,
                        resourceContext.cameraNormalView
                    }
                }
            };

            mainPass = wgil.CreateRenderPass(ref mainPassDesc);
            mainPass.JoinRenderQueue(MainPassWork);
            mainPass.onPassComplete += AfterMainPassWork;

            // Main PostProcess

            var mainPPDesc = new RenderPassDescriptor()
            {
                IsColorClear = false,
                IsDepthClear = false,
                ClearColor = new WGIL.Color(1, 1, 1, 1),
                DepthAttachment = resourceContext.normalsDepthView,
                ColorAttachments = new TextureViewSet()
                {
                    TextureViews = new[]
                    {
                        resourceContext.lightRenderView,
                    }
                }
            };

            mainPPPass = wgil.CreateRenderPass(ref mainPPDesc);
            mainPPPass.JoinRenderQueue(MainPPWork);

            var lightPassDesc = new RenderPassDescriptor()
            {
                IsDepthClear = false,
                DepthAttachment = resourceContext.normalsDepthView,
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
            lightPass.onPassComplete += PreFinalWork;

            var fsPassDesc = new RenderPassDescriptor()
            {
                IsColorClear = true,
                ClearColor = new WGIL.Color(0f, 0f, 0f, 1f),
                IsRenderSwapchain = true
            };
            fsPass = wgil.CreateRenderPass(ref fsPassDesc);
            fsPass.JoinRenderQueue(FinalPassWork);

            internalRenders = new List<RenderSystem>()
            {
                normalsRenderSystem,
                meshRenderSystem,
                spriteBatchSystem,
                lightRenderSystem
            };

            SetupRenderResources();
        }  

        protected private void SetupRenderResources()
        {
            spriteBatchSystem.SetupResources();
            meshRenderSystem.SetupResources();
            lightRenderSystem.SetupResources(resourceContext.mainRenderView, resourceContext.cameraNormalView);
        }

        private void CheckResize()
        {
            if (resize)
            {
                resize = false;
                bool matchRender = resourceContext.GetRenderSize() == Game.canvas.canvasPixelSize;
                if (!matchRender)
                {

                    // Resize render targets
                    finalQuadRSSet.Dispose();
                    foreach (var render in internalRenders)
                        render.CleanUp(true, false, true);

                    resourceContext.RecreateFrameResources((uint)canvas.canvasPixelSize.X, (uint)canvas.canvasPixelSize.Y);

                    // Update pass attachments
                    TextureViewSet newSet = new TextureViewSet();

                    normalsPass.UpdateDepthAttachment(resourceContext.normalsDepthView);
                    newSet.TextureViews = new[] { resourceContext.cameraNormalView };
                    normalsPass.UpdateColorAttachments(ref newSet);

                    mainPass.UpdateDepthAttachment(resourceContext.normalsDepthView);
                    newSet.TextureViews = new[] { resourceContext.mainRenderView, resourceContext.cameraNormalView };
                    mainPass.UpdateColorAttachments(ref newSet);


                    newSet.TextureViews = new[] { resourceContext.lightRenderView };
                    lightPass.UpdateColorAttachments(ref newSet);
                    lightPass.UpdateDepthAttachment(resourceContext.normalsDepthView);

                    mainPPPass.UpdateDepthAttachment(resourceContext.normalsDepthView);
                    mainPPPass.UpdateColorAttachments(ref newSet);

                    var finalQuadDesc = new BindGroupDescriptor()
                    {
                        BindGroupLayout = Graphics.sharedTextureLayout,
                        Entries = new BindResource[]
                        {
                        resourceContext.lightRenderView,
                        Graphics.linearSampleClamp
                        }
                    };

                    finalQuadRSSet = wgil.CreateBindGroup(ref finalQuadDesc).SetManualDispose(true);

                    SetupRenderResources();

                    pipelineData = new PipelineData()
                    {
                        Projection = Matrix4x4.Identity,
                        View = Matrix4x4.Identity,
                        PixelSize = canvas.canvasPixelSize,
                        Time = 0,
                        Padding = 0f
                    };

                    Graphics.RefreshMaterials();

                    //lineDbgPipelineAsset = new LineDbgPipelineAsset(compositeRenderFB);

                    //if (debug)
                    //    colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);

                    lightRenderSystem.Start();
                    //if (debug)
                    //    colDebugSystem.Start();

                    RefreshProjection(Game.canvas);
                }
            }
        }

        void CheckNewScene()
        {
            if(newScene)
            {
                newScene = false;

                // Clear PP
                if (activePostProcess != null)
                    activePostProcess.RemovePostProcess();

                Entities.SetImmediateDestroy(true);
                CoroutineManager.StopAllCoroutines();
                
                // Recreate assets/worlds
                CreateWorlds();
                Physics2D.ResetPhysics();

                Entities.SetImmediateDestroy(false);

                // Clean systems
                foreach (var system in userSystems)
                {
                    system.CleanUp(true, newScene);
                }

                // Clean Resources
                Assets.DisposeResources();
                wgil.DisposeResources(false);

                Graphics.ResetPipelines();

                foreach (var render in internalRenders)
                {
                    render.CleanUp(true, true, false);
                    render.SceneChange();
                }

                // Reset Camera
                Game.activeCamera = default;
                //TriggerCamCheck();

                LineDbgPipelineAsset lineDbgPipelineAsset = new LineDbgPipelineAsset();

                // Systems
                spriteBatchSystem = new SpriteBatchSystem();
                
                for (int i = renderExtensions.Count - 1; i >= 0; i--)
                {
                    BaseSystem system = renderExtensions[i];
                    system.CleanUp(true, true);
                    if (system.dontDestroyOnLoad)
                        system.SceneChange();
                    else
                        renderExtensions.RemoveAt(i);
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
                

                Assets.ClearSceneCache();
                Entities.frameSemaphore.Release();
                Entities.Init();

                Scene_Init();

                // User systems _ Reflection
                Scene_RegisterSystems();

                Scene_Setup();
                FindCamera();
                FindPostProcess();
                onSceneLoad?.Invoke();

                //Start Events
                foreach (var system in userSystems)
                {
                    system.Start();
                }

                spriteBatchSystem.Start();
                if (!Graphics.render2DOnly)
                {
                    normalsRenderSystem.Start();
                    meshRenderSystem.Start();
                }
                lightRenderSystem.Start();
            }
        }

        private protected virtual void MainLoop(float newTime, float elapsed)
        {
            // SDL2 Poll
            window.ProcessEvents(inputData);
            Input.UpdateFrameInput(inputData);

            Time = newTime;
            pipelineData.Time = Time;

            Entities.CheckEntityChanges();

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

            inputData.Clear();

            wgil.BeginRender(); // Sleep
        }

        protected private void RenderSetup(float time)
        {
            // First pass setup
            if (!activeCamera.IsNull)
            {
                var camEnt = activeCamera;
                if (!camEnt.IsNull)
                {
                    ref TRS camTrans = ref camEnt.LocalTransform;
                    Vector3 forward = Vector3.Transform(-Vector3.UnitZ, camTrans.Rotation);

                    Vector3 cameraPosition = camTrans.Position;
                    Vector3 targetPosition = cameraPosition + forward;
                    Vector3 up = Vector3.Transform(Vector3.UnitY, camTrans.Rotation);

                    Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, targetPosition, up);

                    Game.activeCamera.GetComponent<Camera>().forward = forward;

                    pipelineData.View = view;
                    pipelineData.Time = time;

                    BVHSystem.frustum = new Frustum(view * pipelineData.Projection);

                    wgil.WriteBuffer(pipelineBuffer, pipelineData);
                }
            }
        }

        float accumulator;
        float interpolation;
        protected virtual void Init(string windowName)
        {
            // ECS and Physics Worlds
            CreateWorlds();

            // Init
            Physics2D.ResetPhysics();
            Graphics.InitSettings();

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
                    Physics2D.PreFixedUpdate();
                    //rbMoveSystem.PreFixedUpdate();
                }

                //rbMoveSystem.ResetSmoothStates();

                foreach (var system in userSystems)
                {
                    system.FixedUpdate(newTime, TimeStep);
                }

                B2DWorld?.Step(TimeStep, VelocityIterations, PositionIterations);

                steps++;

                /*rbMoveSystem.FixedUpdate(newTime, TimeStep);
                camMoveSystem.FixedUpdate(newTime, TimeStep);*/

                accumulator -= TimeStep;
            }
            Physics2D.PostFixedUpdate();
        }
        

        protected virtual void Scene_RegisterSystems() { }

        protected void RegisterSystem(BaseSystem system)
        {
            Type type = system.GetType();
            if (userSystemTypes.Contains(type)) // Duplicate system
                return;

            userSystemTypes.Add(type);
            userSystems.Add(system);
        }
        

        private protected void MainUpdate(float newTime, float elapsed, float interpolation)
        {
            // Active Cam Update
            if (_checkCamUpdate)
            {
                FindCamera();
            }

            foreach (var system in userSystems)
            {
                system.Update(newTime, elapsed);
            }

            trsSystem.Update(newTime, elapsed);
            RenderSetup(newTime);
            bvhSystem.Update(newTime, elapsed);
            spriteBatchSystem.Update(newTime, elapsed);
            meshRenderSystem.Update(newTime, elapsed);
            lightRenderSystem.Update(newTime, elapsed);
        }

        private protected virtual void Render()
        {
            if(!Graphics.render2DOnly)
                normalsPass.BeginPass();
            mainPass.BeginPass();
            lightPass.BeginPass();
            mainPPPass.BeginPass();
            fsPass.BeginPass();
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
            if (activeCamera.IsNull)
                return;

            Camera camera = Game.activeCamera.GetComponent<Camera>();
            camera.OnCameraActivate();
            if (camera.cameraProjection == CameraProjection.Orthographic)
            {
                Vector2 extents = camera.compViewSize / 2f / 100f;
                projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(-extents.X, extents.X, -extents.Y, extents.Y, 0.1f, 1000f);
            }
            else
                projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, camera.compViewSize.X / camera.compViewSize.Y, 0.1f, 1000f);

            //projectionMatrix = CreatePerspective(MathF.PI / 4f, canvas.canvasSize.X / canvas.canvasSize.Y, 1000f, 0.1f);

            Game.pipelineData.Projection = Game.projectionMatrix;
            onCanvasResize?.Invoke();
        }

        protected void SetupGraphics(string windowName)
        {
            wgil = new WGILContext();
            wgil.OnStart += SetupComplete;
            wgil.OnUpdate += MainLoop;
            wgil.OnRender += Render;

            // Window and Graphics
            var flags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;
            window = new Sdl2Window(windowName, 0, 0, 1280, 720, flags, out RawWindowInfo rawWindowInfo);
            window.Closing += Window_Closing;
            window.Resized += Window_Resized;

            wgil.Start(ref rawWindowInfo);

            wgil.DisposeResources(true);
        }

        private void Window_Resized()
        {
            // Physical Size
            SDL_GL_GetDrawableSize(window.Handle, out int pw, out int ph);
            pixelSize = new Vector2(pw, ph);
            wgil.Resize((uint)pw, (uint)ph);

            SDL_GetWindowSize(window.Handle, out int w, out int h);
            virtualSize = new Vector2(w, h);
            canvas.UpdateScreenSize(virtualSize, pixelSize);
            onWindowResize?.Invoke();
            wgil.logicalSize = virtualSize;

            resize = true;
            reload = true;
        }

        private void Window_Closing()
        {
            OnGameClose();
            wgil.Stop();
        }

        protected virtual void OnGameClose()
        {
            
        }

        private protected virtual void SetupComplete()
        {
            SDL_GL_GetDrawableSize(window.Handle, out int pw, out int ph);
            SDL_GetWindowSize(window.Handle, out int w, out int h);

            pixelSize = new Vector2(pw, ph);
            virtualSize = new Vector2(w, h);
            canvas = new Canvas(w, h);
            canvas.isDynamicSize = true;
            canvas.UpdateScreenSize(virtualSize, pixelSize);
            canvas.referenceSize = new Vector2(1280f, 720f);
            wgil.logicalSize = virtualSize;

            CreateRenderResources((uint)pw, (uint)ph);

            CreateInternalRenders();

            foreach (var render in internalRenders)
                render.SceneChange();

            if(debug)
                Assets.SetCache(new DebugAssetCache());
            else
            {
                // TODO Release cache
            }

            Entities.Init();

            // Systems
            // Shared
            trsSystem = new TRSSystem();
            bvhSystem = new BVHSystem();
            renderExtensions = new List<RenderSystem>();
            
            // Once in game lifetime
            Game_Init();
            Scene_Init();

            // User systems _ Reflection
            Scene_RegisterSystems();

            Scene_Setup();
            FindCamera();
            FindPostProcess();
            onSceneLoad?.Invoke();

            RefreshProjection(Game.canvas);

            // Start Events
            foreach (var system in userSystems)
            {
                system.Start();
            }
            
            trsSystem.Start();
            bvhSystem.Start();
            spriteBatchSystem.Start();
            if (!Graphics.render2DOnly)
            {
                normalsRenderSystem.Start();
                meshRenderSystem.Start();
            }
            lightRenderSystem.Start();

            foreach (var rendExt in renderExtensions)
            {
                rendExt.Start();
            }

            reload = true;
            resize = true;
        }

        protected private void FindCamera()
        {
            var queryCams = Game.GameWorld.Query<Camera>();
            queryCams.ForEachEntity((ref Camera camera, Entity entity) =>
            {
                activeCamera = entity;
                RefreshProjection(canvas);
            });
            
            _checkCamUpdate = false;
        }

        protected private void FindPostProcess()
        {
            /*activePostProcess = null;
            var ppQ = new QueryDescription().WithAll<PostProcess, Transform>();
            GameWorld.Query(in ppQ, (ref PostProcess pp) =>
            {
                activePostProcess = pp;
                activePostProcess.InitPostProcess();
                return;
            });*/
        }

        private protected void CreateRenderResources(uint pixelWidth, uint pixelHeight)
        {
            resourceContext.RecreateFrameResources(pixelWidth, pixelHeight);
            Graphics.LoadPipelines();

            var finalQuadDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = Graphics.sharedTextureLayout,
                Entries = new BindResource[]
                {
                    resourceContext.lightRenderView,
                    Graphics.linearSampleClamp
                }
            };

            finalQuadRSSet = wgil.CreateBindGroup(ref finalQuadDesc).SetManualDispose(true);

            var ppQuadDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = Graphics.sharedTextureLayout,
                Entries = new BindResource[]
                {
                    resourceContext.mainRenderView,
                    Graphics.linearSampleClamp
                }
            };

            mainPPQuadRSSet = wgil.CreateBindGroup(ref ppQuadDesc).SetManualDispose(true);

            pipelineBuffer = wgil.CreateBuffer(144, BufferUsages.UNIFORM | BufferUsages.COPY_DST).SetManualDispose(true);

            var pipelineSetDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = Graphics.sharedPipelineLayout,
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
            GameWorld = new EntityStore();

            // TODO subscriptions
            /*GameWorld.SubscribeComponentAdded((in Entity entity, ref Transform transform) =>
            {
                transform.SetEntity(entity);
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Rigidbody rb) =>
            {
                if (rb.transform != null)
                    return;
                rb.SetTransform(entity.Get<Transform>());
                if (b2dInitSystem.started)
                    Physics2D.CreateBody(rb);
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Sprite sprite) =>
            {
                sprite.SetTransform(entity.Get<Transform>());
                if(!sprite.manualBatching)
                    Game.spriteBatchSystem.UpdateSpriteBatch(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });

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

            GameWorld.SubscribeComponentAdded((in Entity entity, ref MeshRenderer newMr) =>
            {
                meshRenderSystem.AddMesh(entity.Get<Transform>(), newMr);
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref StateMatchAnimator animator) => animator.SetTransform(entity.Get<Transform>()));

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref Sprite sprite) =>
            {
                if (!sprite.manualBatching)
                    Game.spriteBatchSystem.RemoveSprite(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref ParticleModule pm) => pm.Stop());

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref ScriptableParticleModule spm) => spm.Stop());

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref Rigidbody rb) => rb.Destroy());*/
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
            Assets.ClearSerializeDependencies();

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

            // TODO serialize
            /*var query = new QueryDescription().WithAll<Transform>();
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
            }*/

            scene.Put("Assets", Assets.SerializeAssets());
            //scene.Put("Entities", entArr.Build());

            //Console.WriteLine(scene.Build().ToString());

            return scene.Build().ToString();
        }

        protected void LoadScene(string json)
        {
            /*JValue scene = JValue.Parse(json);

            float sceneVersion = scene["Version"];
            SceneManager.sceneVersion = sceneVersion;

            // Assets
            var jAssets = scene["Assets"];
            Assets.ClearSerializeDependencies();
            Assets.DeserializeAssets(jAssets);

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
                    canvas.UpdateScreenSize(virtualSize, pixelSize);
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
            */

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

        private void UIRender(RenderPass pass)
        {
            foreach (var rendExt in renderExtensions)
            {
                rendExt.UIRender(pass);
            }
        }
    }

    public struct PipelineData
    {
        public Matrix4x4 Projection;
        public Matrix4x4 View;
        public Vector2 PixelSize;
        public float Time;
        public float Padding;
    }
}
