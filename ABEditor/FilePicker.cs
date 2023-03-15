using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using Veldrid;
using Num = System.Numerics;

namespace ABEngine.ABEditor
{
	public class FilePicker
	{
		static readonly Dictionary<object, FilePicker> _filePickers = new Dictionary<object, FilePicker>();

		public string RootFolder;
		public string CurrentFolder;
		public string SelectedFile;
		public List<string> AllowedExtensions;
		public bool OnlyAllowFolders;
		static Num.Vector4 yelloColor = new Num.Vector4(1, 1, 0, 1);

        public bool SaveFile;
        public string SaveFileName = "Scene.abscene";

		public static FilePicker GetFolderPicker(object o, string startingPath)
			=> GetFilePicker(o, startingPath, null, true);

		public static FilePicker GetFileSaver(object o, string startingPath)
		{
            var saver = GetFilePicker(o, startingPath, ".abscene");
			saver.SaveFile = true;

			return saver;
		}

        public static FilePicker GetFilePicker(object o, string startingPath, string searchFilter = null, bool onlyAllowFolders = false)
		{
			if (File.Exists(startingPath))
			{
				startingPath = new FileInfo(startingPath).DirectoryName;
			}
			else if (string.IsNullOrEmpty(startingPath) || !Directory.Exists(startingPath))
			{
				startingPath = Environment.CurrentDirectory;
				if (string.IsNullOrEmpty(startingPath))
					startingPath = AppContext.BaseDirectory;
			}

			if (!_filePickers.TryGetValue(o, out FilePicker fp))
			{
				fp = new FilePicker();
				fp.RootFolder = Path.GetPathRoot(startingPath);
				fp.CurrentFolder = startingPath;
				fp.OnlyAllowFolders = onlyAllowFolders;

				if (searchFilter != null)
				{
					if (fp.AllowedExtensions != null)
						fp.AllowedExtensions.Clear();
					else
						fp.AllowedExtensions = new List<string>();

					fp.AllowedExtensions.AddRange(searchFilter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
				}

				//_filePickers.Add(o, fp);
			}

			return fp;
		}

		public static void RemoveFilePicker(object o) => _filePickers.Remove(o);

		public int Draw()
		{
			ImGui.Text("Current Folder: " + CurrentFolder);
			int result = -1;

			if (SaveFile)
			{
				ImGui.Text("File Name:");
				ImGui.SameLine();
				ImGui.InputText("##SaveFileName", ref SaveFileName, 300);
			}

			if (ImGui.BeginChildFrame(1, new Num.Vector2(400, 400)))
			{
				var di = new DirectoryInfo(CurrentFolder);
				if (di.Exists)
				{
					if (di.Parent != null && CurrentFolder != RootFolder)
					{
						
						ImGui.PushStyleColor(ImGuiCol.Text, yelloColor);
						if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
							CurrentFolder = di.Parent.FullName;

						ImGui.PopStyleColor();
					}

					var fileSystemEntries = GetFileSystemEntries(di.FullName);
					foreach (var fse in fileSystemEntries)
					{
						if (Directory.Exists(fse))
						{
							var name = Path.GetFileName(fse);
							ImGui.PushStyleColor(ImGuiCol.Text, yelloColor);
							if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
								CurrentFolder = fse;
							ImGui.PopStyleColor();
						}
						else
						{
							var name = Path.GetFileName(fse);
							bool isSelected = SelectedFile == fse;
							if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
							{
								SelectedFile = fse;
								SaveFileName = name;
							}

							if (ImGui.IsMouseDoubleClicked(0))
							{
								result = 1;
								ImGui.CloseCurrentPopup();
							}
						}
					}
				}
			}
			ImGui.EndChildFrame();


			if (ImGui.Button("Cancel"))
			{
				result = 0;
				ImGui.CloseCurrentPopup();
			}


			if (OnlyAllowFolders)
			{
				ImGui.SameLine();

				if (ImGui.Button("Open"))
				{
					result = 1;
					SelectedFile = CurrentFolder;
					ImGui.CloseCurrentPopup();
				}

			}
			else if (SaveFile)
			{
				ImGui.SameLine();

				if (ImGui.Button("Save"))
				{
					result = 1;
					SelectedFile = CurrentFolder + "/" + SaveFileName;
					ImGui.CloseCurrentPopup();
				}
			}
			else if (SelectedFile != null)
			{
				ImGui.SameLine();

				if (ImGui.Button("Open"))
				{
					result = 1;
					ImGui.CloseCurrentPopup();
				}
			}

			return result;
		}

		bool TryGetFileInfo(string fileName, out FileInfo realFile)
		{
			try
			{
				realFile = new FileInfo(fileName);
				return true;
			}
			catch
			{
				realFile = null;
				return false;
			}
		}

		List<string> GetFileSystemEntries(string fullName)
		{
			var files = new List<string>();
			var dirs = new List<string>();

			foreach (var fse in Directory.GetFileSystemEntries(fullName, ""))
			{
				if (Directory.Exists(fse))
				{
					dirs.Add(fse);
				}
				else if (!OnlyAllowFolders)
				{
					if (AllowedExtensions != null)
					{
						var ext = Path.GetExtension(fse);
						if (AllowedExtensions.Contains(ext))
							files.Add(fse);
					}
					else
					{
						files.Add(fse);
					}
				}
			}

			var ret = new List<string>(dirs);
			ret.AddRange(files);

			return ret;
		}

	}
}
