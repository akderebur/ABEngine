using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ABEngine.ABEditor.Assets;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.ECS;
using ImGuiNET;

namespace ABEngine.ABEditor
{
    class CategoryDirectory
    {
        private string _assetPath;
        private string _curDir;

        public string newFileName;

        public CategoryDirectory(string dir, string catExt, string catName, Action contextMenuAction)
        {
            _assetPath = Editor.AssetPath;
            categoryExtension = catExt;
            categoryName = catName;
            ContextMenuRender = contextMenuAction;
            curDir = dir;

            newFileName = "New";
        }

        public string categoryName { get; set; }
        public string categoryExtension { get; set; }
        public Action ContextMenuRender { get; set; }
        public Action newFileAction { get; set; }
        public bool canDragDrop { get; set; }
        public string payloadName { get; set; }
        public string curDir
        {
            get { return _curDir; }
            set
            {
                _curDir = value;
                if (_curDir.StartsWith("/"))
                    _curDir = _curDir.Substring(1);

                curDirParts = _curDir.Split("/", StringSplitOptions.RemoveEmptyEntries);
                folders = Directory.GetDirectories(_assetPath + value).Select(Path.GetFileName).ToList();
                files = Directory.GetFiles(_assetPath + value, "*" + categoryExtension).Select(Path.GetFileName).ToList();
            }
        }
        //public string oldDir { get; set; }
        public string[] curDirParts { get; set; }
        public List<string> folders { get; set; }
        public List<string> files { get; set; }

        public void RenameFile(string file)
        {
            string oldPath = Editor.AssetPath + _curDir + "/" + file;
            string newPath = Editor.AssetPath + _curDir + "/" + newFileName + categoryExtension;
            File.Move(oldPath, newPath);

            newFileName = "New";
        }
    }

    public class AssetsFolderView
    {
        static FileSystemWatcher watcher;
        static string assetsPath;

        public static bool isActive = false;
        public static List<string> files = new List<string>();
        static List<string> fNames = new List<string>();

        // Fields
        static int _selClipInd = 0;
        static string selFile = "";

        static CategoryDirectory imgCatDir;
        static CategoryDirectory clipCatDir;
        static CategoryDirectory animGraphCatDir;

        public static void SetAssetsFolder(string path)
        {
            assetsPath = path;
            AssetHandler.InitFiles(path);

            files = Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories).ToList().ConvertAll(file => file.Replace("\\", "/").Replace(assetsPath, "")).ToList();
            // First load
            imgCatDir = new CategoryDirectory("", ".png", "Images", () =>
            {
                if (ImGui.MenuItem("Edit sprite"))
                {
                    // Absolute path
                    SpriteEditor.SetImage(Editor.AssetPath + imgCatDir.curDir + "/" + selFile);
                }

            });
            imgCatDir.payloadName = "SpriteFileInd";
            imgCatDir.canDragDrop = true;

            clipCatDir = new CategoryDirectory("", ".abanim2d", "Clips", () =>
            {
                if (ImGui.MenuItem("Edit clip"))
                {
                    ClipEditor.SetClip(clipCatDir.curDir + "/" + selFile);
                }
            });
            clipCatDir.payloadName = "ClipFileInd";
            clipCatDir.canDragDrop = true;

            animGraphCatDir = new CategoryDirectory("", ".abanimgraph", "Anim Graphs", () =>
            {
                if (ImGui.MenuItem("Edit graph"))
                {
                    AnimGraphEditor.SetAnimGraph(animGraphCatDir.curDir + "/" + selFile);
                }
            });
            animGraphCatDir.newFileAction = () =>
            {
                string newFPath = Editor.AssetPath + "/" + animGraphCatDir.curDir +  "/New Anim Graph.abanimgraph";
                int dupeInd = 0;
                while (File.Exists(newFPath))
                    newFPath = newFPath.Replace(".abanimgraph", ++dupeInd + ".abanimgraph");

                File.Create(newFPath);
            };
            animGraphCatDir.payloadName = "AnimGraphFileInd";
            animGraphCatDir.canDragDrop = true;
        

            //files = Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories).ToList().ConvertAll(file => file.ToLower().Replace("\\", "/").Replace(assetsPath, "")).ToList();


            //fNames = files.ConvertAll(file => Path.GetFileName(file));

            //LoadFiles();

            watcher = new FileSystemWatcher(path);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Created += ReloadFiles;
            watcher.Deleted += ReloadFiles;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            isActive = true;
        }

        private static void ReloadFiles(object sender, FileSystemEventArgs e)
        {
            LoadFiles();
        }

        static void LoadFiles()
        {


            //files = Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories).ToList().ConvertAll(file => file.ToLower().Replace("\\", "/")).ToList();
            //fNames = files.ConvertAll(file => Path.GetFileName(file));

            files = Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories).ToList().ConvertAll(file => file.Replace("\\", "/").Replace(assetsPath, "")).ToList();
            animGraphCatDir.curDir = animGraphCatDir.curDir;
            clipCatDir.curDir = clipCatDir.curDir;
            imgCatDir.curDir = imgCatDir.curDir;
        }

        static bool renaming = false;

        static unsafe void DrawAssetTab(CategoryDirectory catDir)
        {
            if (ImGui.BeginTabItem(catDir.categoryName))
            {
                if (ImGui.Button("Assets"))
                {
                    catDir.curDir = "";
                    renaming = false;
                }

                for (int i = 0; i < catDir.curDirParts.Length; i++)
                {
                    string folder = catDir.curDirParts[i];
                    ImGui.SameLine();
                    ImGui.Text(" > ");
                    ImGui.SameLine();
                    if (ImGui.Button(folder))
                    {
                        string subFolder = "";
                        for (int f = i; f < catDir.curDirParts.Length; f++)
                        {

                        }
                    }
                }

                bool changedDir = false;
                string folderName = "";

                ImGui.Separator();
                foreach (var folder in catDir.folders)
                {
                    if (ImGui.Button(folder))
                    {
                        changedDir = true;
                        folderName = folder;
                    }
                }

                foreach (var file in catDir.files)
                {
                    if (renaming && selFile == file)
                    {
                        ImGui.InputText("", ref catDir.newFileName, 200);
                        ImGui.SetKeyboardFocusHere();
                    }
                    else
                    {
                        ImGui.Selectable(file);

                        if (ImGui.IsItemHovered(0))
                        {
                            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                            {
                                selFile = file;

                                Game.GameWorld.SetData<Entity>(default(Entity));
                                Game.GameWorld.SetData<string>(catDir.curDir + "/" + selFile);
                            }
                        }
                        //if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) // Left clik - details
                        //{
                        //    selFile = file;
                        //}
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) // Right click - context menu
                        {
                            renaming = false;

                            selFile = file;
                            ImGui.OpenPopup("FileContext");
                        }

                        if (catDir.canDragDrop)
                        {
                            if (ImGui.BeginDragDropSource())
                            {
                                renaming = false;

                                ImGui.Text(file);

                                string path = string.IsNullOrEmpty(catDir.curDir) ? "" : catDir.curDir + "/";
                                int fileInd = files.IndexOf(path + file);
                                ImGui.SetDragDropPayload(catDir.payloadName, (IntPtr)(&fileInd), sizeof(int));
                                ImGui.EndDragDropSource();
                            }
                        }
                    }
                }

                if(ImGui.BeginPopup("FileContext"))
                {
                    catDir.ContextMenuRender();
                    if(ImGui.MenuItem("Rename"))
                    {
                        renaming = true;
                        catDir.newFileName = Path.GetFileNameWithoutExtension(selFile);
                    }
                    ImGui.EndPopup();
                }

                if (changedDir)
                {
                    catDir.curDir += "/" + folderName;
                }

 

                if (renaming)
                {
                    if (ImGui.IsKeyPressed(ImGuiKey.Enter))
                    {
                        renaming = false;
                        catDir.RenameFile(selFile);
                    }
                }

                // Create new
                if (catDir.newFileAction != null)
                {
                    if (ImGui.Button("Create New"))
                        catDir.newFileAction();
                }

                ImGui.EndTabItem();
            }
        }

        public static void Draw()
        {
            if (!isActive)
                return;

            ImGui.Begin("Assets");
            if (ImGui.BeginTabBar("Types"))
            {
                DrawAssetTab(imgCatDir);
                DrawAssetTab(clipCatDir);
                DrawAssetTab(animGraphCatDir);

                ImGui.EndTabBar();
            }
            ImGui.End();
        }
    }
}
