using System;
using System.Diagnostics;
using System.Reflection;
using ABEngine.ABERuntime;
using Veldrid;
using Veldrid.StartupUtilities;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using ImGuiNET;
using System.Numerics;
using Veldrid.ImageSharp;
using ABEngine.ABERuntime.Pipelines;
using ABEngine.ABERuntime.Physics;
using ABEngine.ABERuntime.Debug;
using ABEngine.ABERuntime.Tweening;
using ABEngine.ABERuntime.Components;
using ABEditor.Debug;
using ABEngine.ABEditor.ImGuiPlugins;
using Halak;
using Arch.Core;
using Arch.Core.Extensions;

namespace ABEngine.ABEditor
{
    internal partial class Editor : Game
    {
        System.Runtime.Loader.AssemblyLoadContext assemblyCtx;
        Assembly GameAssembly;
        FileSystemWatcher watcher;

        public static Entity selectedEntity = Entity.Null;
        public static string selectedAsset = null;

        // Imgui
        static ImGuiRenderer imguiRenderer;

        // Editor tools
        protected private static TilemapColliderGizmo TMColliderGizmo;
        internal static EditorActionStack EditorActions;
        private static bool isPlaying;

        static List<Type> userTypes;
        List<BaseSystem> editorSystems;
        List<BaseSystem> sharedSystems;

        float zoomSpeed = 1f;

        // Editor state
        const string EditorSettingsFile = "EditorSettings.abjs";

        string gameDir;
        string binaryPath;
        string projPath;
        string dllPath;
        string projName;
        int buildC = 0;

        string tmpJson;
        public static string EditorAssetPath;

        static bool isGameOpen = false;

        static ImFontPtr defaultFont;

        float accumulator;

        public static List<Type> GetUserTypes()
        {
            return userTypes;
        }

        public static IntPtr GetImGuiTexture(Texture texture)
        {
            return imguiRenderer.GetOrCreateImGuiBinding(GraphicsManager.rf, texture);
        }

        public static IntPtr GetImGuiTexture(TextureView texture)
        {
            return imguiRenderer.GetOrCreateImGuiBinding(GraphicsManager.rf, texture);
        }

        public Editor(string windowName) : base(true, new List<Type>())
        {
            GraphicsManager.msaaSampleCount = TextureSampleCount.Count4;
            Init(windowName);
        }

        public static ImGuiRenderer GetImGuiRenderer()
        {
            return imguiRenderer;
        }

        internal static TilemapColliderGizmo GetTilemapGizmo()
        {
            return TMColliderGizmo;
        }

        private void LoadEditorSettings()
        {
            if (!Directory.Exists(EditorAssetPath))
                Directory.CreateDirectory(EditorAssetPath);

            string settingsPath = EditorAssetPath + EditorSettingsFile;
            if(File.Exists(settingsPath))
            {
                var jSettings = JValue.Parse(File.ReadAllText(settingsPath));
                foreach (string recent in jSettings["Recents"].Array())
                {
                    FilePicker.recentPaths.Add(recent);
                }
            }
        }

        private void SaveEditorSettings()
        {
            if (!Directory.Exists(EditorAssetPath))
                Directory.CreateDirectory(EditorAssetPath);

            string settingsPath = EditorAssetPath + EditorSettingsFile;

            // Save Settings
            JsonObjectBuilder settingsJObj = new JsonObjectBuilder(500);
            JsonArrayBuilder recentsJArr = new JsonArrayBuilder(200);
            foreach (var recent in FilePicker.recentPaths)
                recentsJArr.Push(recent);
            settingsJObj.Put("Recents", recentsJArr.Build());
            File.WriteAllText(settingsPath, settingsJObj.Build().Serialize());
        }

        protected override void Init(string windowName)
        {
            gameMode = GameMode.Editor;
            userTypes = new List<Type>();

            // Editor Resources
            EditorAssetPath = AssetPath;
            LoadEditorSettings();
        
            // ECS and Physics Worlds
            ResetWorld();
            PhysicsManager.ResetPhysics();
            GraphicsManager.InitSettings();

            // Graphics
            base.SetupGraphics(windowName);
            CreateInternalRenders();

            foreach (var render in internalRenders)
                render.SceneSetup();

            EntityManager.Init();


            // ImGui
            imguiRenderer = new ImGuiRenderer(
              GraphicsManager.gd,
              GraphicsManager.gd.MainSwapchain.Framebuffer.OutputDescription,
              window.Width,
              window.Height);
            SetupImGuiStyle();
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            LineDbgPipelineAsset lineDbgPipelineAsset = new LineDbgPipelineAsset(compositeRenderFB);
            TMColliderPipelineAsset tmColPipelineAsset = new TMColliderPipelineAsset(compositeRenderFB);

            window.Resized += () =>
            {
                imguiRenderer.WindowResized(window.Width, window.Height);
            };


            // Systems
            // Shared
            particleSystem = new ParticleModuleSystem();
            spriteAnimSystem = new SpriteAnimSystem();

            colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);
            TMColliderGizmo = new TilemapColliderGizmo(tmColPipelineAsset);
            sharedSystems = new List<BaseSystem> { spriteBatchSystem, lightRenderSystem, particleSystem, spriteAnimSystem };
            renderExtensions = new List<RenderSystem>();

            // Editor
            editorSystems = new List<BaseSystem>();
            editorSystems.Add(new MouseDragSystem());
            editorSystems.Add(new EditorCamMoveSystem());

            // Editor UI Inits
            SpriteEditor.Init();
            ClipEditor.Init();

            SubscribeSystems();

            InputSnapshot snapshot = window.PumpEvents();
            Input.UpdateFrameInput(snapshot);

            colDebugSystem.Start();
            TMColliderGizmo.Start();

            //aabbGizmo.Start();
            foreach (var system in editorSystems)
            {
                system.Start();
            }

            // Game Loop
            Stopwatch sw = Stopwatch.StartNew();
            float previousTime = (float)sw.Elapsed.TotalSeconds;
            while (window.Exists)
            {
                float newTime = (float)sw.Elapsed.TotalSeconds;
                float elapsed = newTime - previousTime;
                Time = newTime;

                previousTime = newTime;
                snapshot = window.PumpEvents();
                Input.UpdateFrameInput(snapshot);

                imguiRenderer.Update(elapsed, snapshot); // [2]

                EntityManager.CheckEntityChanges();

                UpdateEditorUI();

                // TODO reload later
                //if (reload && !isPlaying)
                //{
                //    tmpJson = base.SaveScene();
                //    BuildGameAssembly();
                //    ResetGameAssembly();
                //    ResetWorld();
                //    base.LoadScene(tmpJson, userTypes);
                //    DepthSearch();

                //    // Reset animator first frames
                //    //spriteRenderer.Start();

                //    reload = false;
                //}

                if (reload)
                {
                    reload = false;

                    gd.WaitForIdle();

                    if (resize)
                    {
                        resize = false;

                        // Resize render targets
                        compositeRenderTexture.Dispose();
                        finalQuadRSSet.Dispose();
                        compositeRSSetLight.Dispose();

                        foreach (var render in internalRenders)
                            render.CleanUp(true, false, true);


                        // Resources
                        var mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;
                       

                        compositeRenderTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                           mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                            mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled));

                       
                        finalQuadRSSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                               GraphicsManager.sharedTextureLayout,
                               compositeRenderTexture, gd.LinearSampler
                               ));

                        SetupRenderResources();

                        pipelineData = new PipelineData()
                        {
                            Projection = Matrix4x4.Identity,
                            View = Matrix4x4.Identity,
                            Resolution = screenSize,
                            Time = elapsed,
                            Padding = 0f
                        };


                        GraphicsManager.RefreshMaterials();

                        lineDbgPipelineAsset = new LineDbgPipelineAsset(compositeRenderFB);
                        tmColPipelineAsset = new TMColliderPipelineAsset(compositeRenderFB);

                        if (debug)
                        {
                            colDebugSystem.CleanUp(true, false);
                            colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);
                            colDebugSystem.Start();
                        }

                        TMColliderGizmo.UpdatePipeline(tmColPipelineAsset);

                        RefreshProjection(Game.canvas);
                    }
                    continue;

                }

                // TODO Enter play mode
                if (Input.GetKey(Key.ControlLeft) && Input.GetKeyDown(Key.P))
                {
                    isPlaying = !isPlaying;
                    if (isPlaying)
                    {
                        tmpJson = base.SaveScene();
                        foreach (var system in sharedSystems)
                        {
                            system.Start();
                        }

                        foreach (var system in userSystems)
                        {
                            system.Start();
                        }
                        camMoveSystem.Start();

                        window.Title = "ABEngine - Play Mode";
                    }
                    else
                    {
                        window.Title = "ABEngine - Editor";
                        ResetWorld();
                        base.LoadScene(tmpJson);
                        DepthSearch();
                        //spriteRenderer.Start();
                        spriteBatchSystem.Start();
                    }
                }


                // TODO Render extensions

                if (isPlaying) // TODO Play Mode
                {
                    MainFixedUpdate(newTime, elapsed);
                    float interpolation = accumulator / TimeStep;
                    MainUpdate(newTime, elapsed, interpolation);

                    //float fixedElapsed = newTime - previousFixedTime;
                    //if (fixedElapsed >= fixedTimeStep)
                    //{
                    //    PhysicsUpdate();
                    //    previousFixedTime = newTime;

                    //    rbMove.FixedUpdate(newTime, fixedElapsed);
                    //    foreach (var system in userSystems)
                    //    {
                    //        system.FixedUpdate(newTime, fixedElapsed);
                    //    }
                    //    camMove.FixedUpdate(newTime, elapsed);
                    //}

                    //foreach (var system in userSystems)
                    //{
                    //    system.Update(newTime, elapsed);
                    //}
                    //spriteAnim.Update(newTime, elapsed);
                    //camMove.Update(newTime, elapsed);
                }
                else // Editor
                {
                    if (Input.GetKey(Key.ControlLeft) && Input.GetKeyDown(Key.S))
                    {
                        //tmpJson = base.SaveScene();

                        if (loadedScenePath != null)
                        {
                            File.WriteAllText(loadedScenePath, base.SaveScene());
                        }
                        else
                        {
                            fileDialogType = FileDialogType.SaveFile;
                        }
                    }
                    else if (Input.GetKey(Key.ControlLeft) && Input.GetKeyDown(Key.Z))
                    {
                        EditorActions.Undo();
                    }
                    else if (Input.GetKey(Key.ControlLeft) && Input.GetKeyDown(Key.Y))
                    {
                        EditorActions.Redo();
                    }
                    else if (Input.GetKey(Key.ControlLeft) && Input.GetKeyDown(Key.BackSpace))
                    {
                        var ent = Editor.selectedEntity;
                        if (ent != Entity.Null)
                            DeleteRecursive(ent.Get<Transform>());
                    }

                    if (!ImGui.GetIO().WantCaptureKeyboard)
                    {
                        if (Input.GetKey(Key.Up))
                        {
                            zoomFactor = Math.Clamp(zoomFactor - elapsed * zoomSpeed, 0.2f, 1.8f);
                            Zoom();
                        }
                        else if (Input.GetKey(Key.Down))
                        {
                            zoomFactor = Math.Clamp(zoomFactor + elapsed * zoomSpeed, 0.2f, 1.8f);
                            Zoom();
                        }
                    }

                    if (!ImGui.GetIO().WantCaptureMouse)
                    {
                        if (Input.MouseScrollDelta != 0)
                        {
                            zoomFactor = Math.Clamp(zoomFactor - elapsed * -Input.MouseScrollDelta, 0.2f, 1.8f);
                            Zoom();
                        }
                    }

                    MainEditorUpdate(newTime, elapsed);
                }

                if (!window.Exists)
                {
                    break;
                }

                // Render

                DrawBegin();
                MainRender();

                //_commandList.ClearDepthStencil(0f);
                //colDebugSystem.Render();
                //TMColliderGizmo.Render();
                // TODO Render extensions

         
                if(!isPlaying)
                {
                    //Editor draws
                    ClipEditor.Draw(newTime);
                    AnimGraphEditor.Draw(newTime);
                    //GraphicsManager.cl.SetFramebuffer(mainRenderFB);
                    //GraphicsManager.cl.SetFullViewports();
                }

                ImGui.PopFont();

                FinalRender();

            }

            // Clean up Veldrid resources
            CleanUp();
            imguiRenderer.Dispose();
            gd.Dispose();

            SaveEditorSettings();

            //_commandList.Dispose();
            //gd.Dispose();

            // Clean up assemblies
            //string delPath = Directory.GetCurrentDirectory() + "//GameAssemblies";
            //if (Directory.Exists(delPath))
            //    Directory.Delete(delPath, true);
        }

        private void Zoom()
        {
            float canvasWidth = canvas.canvasSize.X / 100f;
            float canvasHeight = canvas.canvasSize.Y / 100f;
            float zoomedWidth = canvasWidth * zoomFactor;
            float zoomedHeight = canvasHeight * zoomFactor;

            float left = (canvasWidth - zoomedWidth) / 2f;
            float right = left + zoomedWidth;
            float bottom = (canvasHeight - zoomedHeight) / 2f;
            float top = bottom + zoomedHeight;

            projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1000f, 1000f);
            Game.pipelineData.Projection = Game.projectionMatrix;

            if (GameWorld != null)
            {
                var query = new QueryDescription().WithAll<Sprite, Transform>();

                Game.GameWorld.Query(in query, (ref Transform transform) =>
                {
                    if (transform.name.StartsWith("Line_"))
                        transform.localScale = new Vector3(1f, zoomFactor * canvasWidth / 12.8f , 1f);
                }
                );
            }
        }

        private void MainEditorUpdate(float newTime, float elapsed)
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

            particleSystem.Update(newTime, elapsed);
            spriteAnimSystem.Update(newTime, elapsed);

            foreach (var editorSystem in editorSystems)
            {
                editorSystem.Update(newTime, elapsed);
            }

            normalsRenderSystem.Update(newTime, elapsed);
            spriteBatchSystem.Update(newTime, elapsed);
            meshRenderSystem.Update(newTime, elapsed);
            lightRenderSystem.Update(newTime, elapsed);
            colDebugSystem.Update(newTime, elapsed);
        }

        private void FinalRender()
        {
            GraphicsManager.cl.SetFramebuffer(GraphicsManager.gd.MainSwapchain.Framebuffer);
            GraphicsManager.cl.SetFullViewports();
            GraphicsManager.cl.ClearColorTarget(0, RgbaFloat.Black);
            GraphicsManager.cl.SetPipeline(GraphicsManager.FullScreenPipeline);

            GraphicsManager.cl.SetGraphicsResourceSet(0, finalQuadRSSet);
            GraphicsManager.cl.SetVertexBuffer(0, GraphicsManager.fullScreenVB);
            GraphicsManager.cl.SetIndexBuffer(GraphicsManager.fullScreenIB, IndexFormat.UInt16);
            GraphicsManager.cl.DrawIndexed(6, 1, 0, 0, 0);

            imguiRenderer.Render(GraphicsManager.gd, GraphicsManager.cl);

            GraphicsManager.cl.End();
            GraphicsManager.gd.SubmitCommands(GraphicsManager.cl);
            GraphicsManager.gd.WaitForIdle();
            GraphicsManager.gd.SwapBuffers();
        }

        protected override string SaveScene()
        {
            // Reset tilemap render layers
            var query = new QueryDescription().WithAll<Tilemap, Transform>();
            Game.GameWorld.Query(in query, (ref Tilemap tilemap, ref Transform trans) =>
            {
                tilemap.ResetRenderLayers();
            }
            );

            string sceneData = base.SaveScene();

            // Recover tilemap render layers
            Game.GameWorld.Query(in query, (ref Tilemap tilemap, ref Transform trans) =>
            {
                tilemap.RecoverRenderLayers();
            }
            );

            return sceneData;
        }

        void ResetWorld()
        {
            if(GameWorld != null)
                World.Destroy(GameWorld);
            PrefabManager.ClearScene();

            if (isPlaying)
                CreateWorlds();
            else
                CreateEditorWorlds();
        }

        protected override void CreateWorlds()
        {
            base.CreateWorlds();
        }

        private void CreateEditorWorlds()
        {
            // ECS World
            GameWorld = World.Create();
            GameWorld.SubscribeComponentAdded((in Entity entity, ref Transform transform) =>
            {
                transform.SetEntity(entity);
            });


            GameWorld.SubscribeComponentAdded((in Entity entity, ref Rigidbody rb) =>
            {
                if (rb == null || rb.transform != null)
                    return;

                rb.SetEntity(entity.Get<Transform>());
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Sprite sprite) =>
            {
                if (sprite == null || sprite.transform != null)
                    return;

                sprite.SetTransform(entity.Get<Transform>());
                if (!sprite.manualBatching)
                    Game.spriteBatchSystem.UpdateSpriteBatch(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Tilemap tilemap) =>
            {
                tilemap.SetTransform(entity.Get<Transform>());
            });


            GameWorld.SubscribeComponentAdded((in Entity entity, ref ParticleModule pm) => pm.Init(entity.Get<Transform>()));

            GameWorld.SubscribeComponentAdded((in Entity entity, ref Camera cam) => TriggerCamCheck());

            GameWorld.SubscribeComponentAdded((in Entity entity, ref AABB newBB) =>
            {
                if (!newBB.sizeSet && entity.Has<Sprite>())
                {
                    var spriteSize = entity.Get<Sprite>().size;
                    newBB.size = spriteSize;
                    newBB.sizeSet = true;
                }
            });

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref Sprite sprite) =>
            {
                if (!sprite.manualBatching)
                    Game.spriteBatchSystem.RemoveSprite(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });

            GameWorld.SubscribeComponentRemoved((in Entity entity, ref ParticleModule pm) => pm.Stop());
            //GameWorld.OnEnable((Entity entity, Sprite sprite) =>
            //{
            //    Game.spriteBatchSystem.UpdateSpriteBatch(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            //});


            //GameWorld.OnDisable((Entity entity, Sprite sprite) =>
            //{
            //    Game.spriteBatchSystem.RemoveSprite(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            //});

            PrefabManager.SceneInit();
        }

        void OpenGameDummy(string path)
        {
            gameDir = path;
            projName = Path.GetFileNameWithoutExtension(path);

            // AppPath
            AppPath = path.ToCommonPath();
            if (!AppPath.EndsWith("/"))
                AppPath += "/";
            AssetPath = AppPath + "Assets/";

            if (!Directory.Exists(AssetPath))
                Directory.CreateDirectory(AssetPath);

            AssetCache.InitAssetCache();
            AssetsFolderView.SetAssetsFolder(AssetPath);

            AnimGraphEditor.Init();

            // Start systems
            spriteBatchSystem.Start();
            lightRenderSystem.Start();
            particleSystem.Start();
            spriteAnimSystem.Start();
            isGameOpen = true;

            EditorActions = new EditorActionStack();
        }


        void OpenGame(string setDir)
        {
            //gameDir = @"/Users/akderebur/Projects/ABEGameTest";
            //gameDir = setDir;

            string slnPath = null;
            foreach (var file in Directory.EnumerateFiles(setDir, "*", SearchOption.AllDirectories))
            {
                if(Path.GetExtension(file).ToLower().Equals(".sln"))
                {
                    slnPath = file;
                    break;
                }
            }

            if (slnPath == null)
                return;

            gameDir = Path.GetDirectoryName(slnPath);
            projName = Path.GetFileNameWithoutExtension(slnPath);
            string projectDir = gameDir + "/GameAssembly/";
            projPath = projectDir + "GameAssembly.csproj";
            binaryPath = gameDir + "/" + projName + "/bin/Debug/net6.0";

            //gameDir = Directory.GetCurrentDirectory() + "/../../../../../ABEGameTest";
            //projName = Path.GetFileName(gameDir);
            //string projectDir = gameDir + "/GameAssembly/";
            //projPath = projectDir + "GameAssembly.csproj";
            //scenePath = gameDir + "/ABEGameTest/bin/Debug/net6.0";

            // AppPath
            AppPath = binaryPath.ToCommonPath() + "/";
            AssetPath = AppPath + "Assets/";
            AssetCache.InitAssetCache();
            AssetsFolderView.SetAssetsFolder(AssetPath);

            var imgFiles = Directory.EnumerateFiles(AssetPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".jpg") || s.EndsWith(".png"));

            foreach (var imgFile in imgFiles)
            {
                string imgFileRep = imgFile.ToCommonPath();
                //string fName = Path.GetFileName(imgFileRep);
                //string strip = imgFile.Replace("/" + fName, "");
                //string strip = Path.GetDirectoryName(imgFile);
                string assetsPath = imgFileRep.Replace(AssetPath, "");

                //var img = new ImageSharpTexture(imgFileRep);
                //var dimg = img.CreateDeviceTexture(_gd, _gd.ResourceFactory);
                //imgDict.Add(assetsPath, dimg);
            }


            watcher = new FileSystemWatcher(projectDir);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;

            watcher.Filter = "*.cs";
            watcher.EnableRaisingEvents = true;

            BuildGameAssembly();

            dllPath = @"/GameAssemblies/" + projName + buildC + "/GameAssembly.dll";
            //tmpJson = File.ReadAllText(scenePath);

            // GameAssembly
            //dllPath = @"/Users/akderebur/Projects/ABEGameTest/GameAssembly/bin/Debug/netstandard2.1/GameAssembly.dll";
            LoadGameAssembly();
            //base.LoadScene(tmpJson, userTypes);
            DepthSearch();

            AnimGraphEditor.Init();

            // Start systems
            spriteBatchSystem.Start();
            lightRenderSystem.Start();
            particleSystem.Start();
            spriteAnimSystem.Start();
            isGameOpen = true;

            EditorActions = new EditorActionStack();
        }

        #region GameAssembly

        bool wait = false;


        void BuildGameAssembly()
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo();
            procStartInfo.FileName = "dotnet";
            procStartInfo.Arguments = "build " + projPath + " /p:DebugType=None /p:DebugSymbols=false  --output " + "GameAssemblies/" + projName + buildC;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = false;

            Process pr = new Process();
            pr.StartInfo = procStartInfo;
            pr.Start();

            pr.WaitForExit();
        }

        void ResetGameAssembly()
        {
            dllPath = @"/GameAssemblies/" + projName + buildC + "/GameAssembly.dll";
            if (assemblyCtx != null)
            {
                wait = true;
                assemblyCtx.Unloading += AssemblyCtx_Unloading;
                assemblyCtx.Unload();
            }

            while (wait) { }

            LoadGameAssembly();
        }

        void LoadGameAssembly()
        {
            string editorDir = Directory.GetCurrentDirectory();
            //GameAssembly = Assembly.LoadFile(editorDir + dllPath);

            assemblyCtx = new System.Runtime.Loader.AssemblyLoadContext("GameAssemblyCtx", true);
            GameAssembly = assemblyCtx.LoadFromAssemblyPath(editorDir + dllPath);
            //Directory.Delete(Path.GetDirectoryName(editorDir + dllPath), true);
            buildC++;


            // User systems
            userSystems = new List<BaseSystem>();
            var systemTypes = (from t in GameAssembly.GetTypes()
                               where t.IsClass && t.IsSubclassOf(typeof(BaseSystem))
                               select t).ToList();
            foreach (var systemType in systemTypes)
            {
                userSystems.Add((BaseSystem)GameAssembly.CreateInstance(systemType.ToString()));
            }

            // User types
            userTypes = (from t in GameAssembly.GetTypes()
                         where t.IsClass && t.IsSubclassOf(typeof(JSerializable))
                         select t).ToList();

            // User pipelines
            var userPipelines = (from t in GameAssembly.GetTypes()
                         where t.IsClass && t.IsSubclassOf(typeof(PipelineAsset))
                         select t).ToList();
            foreach (var pipeline in userPipelines)
            {
                GameAssembly.CreateInstance(pipeline.ToString());
            }

            // Scene loading
            //scenePath = @"/Users/akderebur/Projects/ABEGameTest/ABEGameTest/bin/Debug/netcoreapp3.1/scene.json";
            //base.LoadScene(tmpJson, userTypes);
            //DepthSearch();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed || reload)
            {
                return;
            }
            reload = true;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (reload)
                return;

            string value = $"Created: {e.FullPath}";
            reload = true;
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (reload)
                return;
            reload = true;
        }

        private void AssemblyCtx_Unloading(System.Runtime.Loader.AssemblyLoadContext obj)
        {
            wait = false;
        }

        #endregion
    }
}
