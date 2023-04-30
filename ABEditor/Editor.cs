using System;
using System.Diagnostics;
using System.Reflection;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.ECS;
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

namespace ABEngine.ABEditor
{
    internal partial class Editor : Game
    {
        System.Runtime.Loader.AssemblyLoadContext assemblyCtx;
        Assembly GameAssembly;
        FileSystemWatcher watcher;
        bool reload = false;

        // Imgui
        static ImGuiRenderer imguiRenderer;

        protected private static TilemapColliderGizmo TMColliderGizmo;
        private static bool isPlaying;

        static List<Type> userTypes;
        List<BaseSystem> editorSystems;
        List<BaseSystem> sharedSystems;

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

        public Editor(string windowName) : base(windowName, true, new List<Type>())
        {
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

        protected private override void Init(string windowName)
        {
            gameMode = GameMode.Editor;
            userTypes = new List<Type>();

            // Editor Resources
            EditorAssetPath = AssetPath;
            LoadEditorSettings();
        
            // ECS and Physics Worlds
            ResetWorld();
            PhysicsManager.InitSettings();
            GraphicsManager.InitSettings();

            // Graphics
            base.SetupGraphics(windowName);

            // ImGui
            imguiRenderer = new ImGuiRenderer(
              GraphicsManager.gd,
              GraphicsManager.gd.MainSwapchain.Framebuffer.OutputDescription,
              window.Width,
              window.Height);
            SetupImGuiStyle();
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            LightPipelineAsset lightPipelineAsset = new LightPipelineAsset(lightRenderFB);
            LineDbgPipelineAsset lineDbgPipelineAsset = new LineDbgPipelineAsset(compositeRenderFB);
            TMColliderPipelineAsset tmColPipelineAsset = new TMColliderPipelineAsset(compositeRenderFB);

            window.Resized += () =>
            {
                imguiRenderer.WindowResized(window.Width, window.Height);
            };


            // Systems
            // Shared
            //SpriteRenderSystem spriteRenderer = new SpriteRenderSystem();
            spriteBatchSystem = new SpriteBatchSystem(null);
            lightRenderSystem = new LightRenderSystem(lightPipelineAsset);
            particleSystem = new ParticleModuleSystem();
            colDebugSystem = new ColliderDebugSystem(lineDbgPipelineAsset);
            TMColliderGizmo = new TilemapColliderGizmo(tmColPipelineAsset);
            sharedSystems = new List<BaseSystem> { spriteBatchSystem, lightRenderSystem };
            renderExtensions = new List<RenderSystem>();

            // Editor
            editorSystems = new List<BaseSystem>();
            editorSystems.Add(new MouseDragSystem());
            editorSystems.Add(new EditorCamMoveSystem());
            //editorSystems.Add(colDebug);
            //AABBGizmoSytem aabbGizmo = new AABBGizmoSytem();

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

                        if(loadedScenePath != null)
                        {
                            File.WriteAllText(loadedScenePath, base.SaveScene());
                        }
                        else
                        {
                            fileDialogType = FileDialogType.SaveFile;
                        }
                    }
                    else if (Input.GetKeyDown(Key.B))
                    {
                        //AnimGraphEditor.isActive = !AnimGraphEditor.isActive;
                    }
                    else if(Input.GetKey(Key.ControlLeft) && Input.GetKeyDown(Key.BackSpace))
                    {
                        var ent = GameWorld.GetData<Entity>();
                        if (ent.IsValid())
                            DeleteRecursive(ent.Get<Transform>());
                    }

                    MainEditorUpdate(newTime, elapsed);

                    foreach (var system in editorSystems)
                    {
                        system.Update(newTime, elapsed);
                    }
                }

                if (!window.Exists)
                {
                    break;
                }

                // Render

                DrawBegin();
                MainRender();

                //_commandList.ClearDepthStencil(0f);
                colDebugSystem.Render();
                TMColliderGizmo.Render();
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

            SaveEditorSettings();

            //_commandList.Dispose();
            //gd.Dispose();

            // Clean up assemblies
            //string delPath = Directory.GetCurrentDirectory() + "//GameAssemblies";
            //if (Directory.Exists(delPath))
            //    Directory.Delete(delPath, true);
        }

        private void MainEditorUpdate(float newTime, float elapsed)
        {
            // Active Cam Update
            if (_checkCamUpdate)
            {
                var camQ = GameWorld.CreateQuery().Has<Camera>().Has<Transform>();
                if (camQ.EntityCount > 0)
                {
                    var camEnt = camQ.GetEntities().FirstOrDefault(c => c.Get<Camera>().isActive);
                    if (camEnt.IsValid())
                        activeCam = camEnt.Get<Transform>();
                }
                else
                    activeCam = null;
                _checkCamUpdate = false;
            }

            particleSystem.Update(newTime, elapsed);
            spriteBatchSystem.Update(newTime, elapsed);
            lightRenderSystem.Update(newTime, elapsed);
            colDebugSystem.Update(newTime, elapsed);
            foreach (var editorSystem in editorSystems)
            {
                editorSystem.Update(newTime, elapsed);
            }
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
            var query = GameWorld.CreateQuery().Has<Tilemap>().Has<Transform>();
            query.Foreach((ref Tilemap tilemap, ref Transform trans) =>
            {
                tilemap.ResetRenderLayers();
            }
            );

            string sceneData = base.SaveScene();

            // Recover tilemap render layers
            query.Foreach((ref Tilemap tilemap, ref Transform trans) =>
            {
                tilemap.RecoverRenderLayers();
            }
            );

            return sceneData;
        }

        void ResetWorld()
        {
            GameWorld.Destroy();

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
            GameWorld.OnSet((Entity entity, ref Transform newTrans) => newTrans.SetEntity(entity));
            GameWorld.OnSet((Entity entity, ref Rigidbody newRb) => newRb.SetEntity(entity));
            GameWorld.OnSet((Entity entity, ref Sprite sprite) =>
            {
                sprite.SetTransform(entity.transform);
                if(!sprite.manualBatching)
                    Game.spriteBatchSystem.UpdateSpriteBatch(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });
            GameWorld.OnSet((Entity entity, ref Tilemap tilemap) =>
            {
                tilemap.SetTransform(entity.transform);
            });
            GameWorld.OnSet((Entity entity, ref ParticleModule pm) =>
            {
                pm.Init(entity.transform);
            });
            GameWorld.OnSet((Entity entity, ref Camera newCam) => TriggerCamCheck());
            GameWorld.OnSet((Entity entity, ref AABB newBB) =>
            {
                if (!newBB.sizeSet && entity.Has<Sprite>())
                {
                    var spriteSize = entity.Get<Sprite>().size;
                    newBB.size = spriteSize;
                    //newBB.width = spriteSize.X;
                    //newBB.height = spriteSize.Y;
                    newBB.sizeSet = true;
                }
            });
            GameWorld.OnRemove((Sprite sprite) => Game.spriteBatchSystem.RemoveSprite(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID));

            GameWorld.OnEnable((Entity entity, Sprite sprite) =>
            {
                Game.spriteBatchSystem.UpdateSpriteBatch(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });


            GameWorld.OnDisable((Entity entity, Sprite sprite) =>
            {
                Game.spriteBatchSystem.RemoveSprite(sprite, sprite.renderLayerIndex, sprite.texture, sprite.sharedMaterial.instanceID);
            });

            PrefabWorld = World.Create();
            PrefabWorld.OnSet((Entity entity, ref Transform newTrans) => newTrans.SetEntity(entity));


            // Systems
            BaseSystem.SetECSWorld(GameWorld);
        }

        void OpenGameDummy(string path)
        {
            gameDir = path;
            projName = Path.GetFileNameWithoutExtension(path);

            // AppPath
            AppPath = path.Replace("\\", "/");
            if (!AppPath.EndsWith("/"))
                AppPath += "/";
            AssetPath = AppPath + "Assets/";

            if (!Directory.Exists(AssetPath))
                Directory.CreateDirectory(AssetPath);

            AssetCache.InitAssetCache();
            AssetsFolderView.SetAssetsFolder(AssetPath);

            AnimGraphEditor.Init();

            // Sprite renderer
            spriteBatchSystem.Start();
            lightRenderSystem.Start();
            particleSystem.Start();
            isGameOpen = true;
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
            AppPath = binaryPath.Replace("\\", "/") + "/";
            AssetPath = AppPath + "Assets/";
            AssetCache.InitAssetCache();
            AssetsFolderView.SetAssetsFolder(AssetPath);

            var imgFiles = Directory.EnumerateFiles(AssetPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".jpg") || s.EndsWith(".png"));

            foreach (var imgFile in imgFiles)
            {
                string imgFileRep = imgFile.Replace("\\", "/");
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

            // Sprite renderer
            spriteBatchSystem.Start();
            lightRenderSystem.Start();
            particleSystem.Start();
            isGameOpen = true;
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
