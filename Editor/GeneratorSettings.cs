using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DoxygenGenerator
{
	[Serializable] 
    public class DoxygenConfig 
    { 
        public List<string> inputPaths = new List<string>(); 
    }

	public static class GeneratorSettings
    {

        public static string doxygenPath
        {
            get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(doxygenPath)}", string.Empty);
            set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(doxygenPath)}", value);
		}
		public static string customPath
		{
			get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(customPath)}", string.Empty);
			set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(customPath)}", value);
		}
		// For backward compatibility (optional)
		public static string inputDirectory
		{
			get => InputPaths.Count > 0 ? InputPaths[0] : string.Empty;
			set
			{
				if (InputPaths.Count == 0)
					InputPaths.Add(value);
				else
					InputPaths[0] = value;

				SaveConfig();
			}
		}
		public static List<string> InputPaths
		{
			get
			{
				// Convert relative paths back to absolute on access
				string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
				var absolutePaths = new List<string>();

				foreach (var storedPath in config.inputPaths)
				{
					if (!string.IsNullOrEmpty(storedPath) && !Path.IsPathRooted(storedPath))
						absolutePaths.Add(Path.GetFullPath(Path.Combine(projectRoot, storedPath)));
					else
						absolutePaths.Add(storedPath);
				}

				return absolutePaths;
			}
			set
			{
				config.inputPaths = value ?? new List<string> { string.Empty };
				if (config.inputPaths.Count == 0)
					config.inputPaths.Add(string.Empty);

				SaveConfig();
			}
		}
		//public static string inputDirectory
		//      {
		//          get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(inputDirectory)}", string.Empty);
		//          set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(inputDirectory)}", value);
		//      }

		public static string outputDirectory
        {
            get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(outputDirectory)}", string.Empty);
            set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(outputDirectory)}", value);
        }
		public static string configDir
        {
            get=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DoxyGen");
            set=> Directory.CreateDirectory(value);
		}
        public static string configFile = Path.Combine(configDir, "DoxySettings.json");
		private static readonly string ConfigDirectory = Path.Combine(
		   Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		   "DoxyGen");
		private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "DoxySettings.json");

		private static DoxygenConfig config;

		static GeneratorSettings()
		{
			LoadConfig();
		}

		private static void LoadConfig()
		{
			if (!Directory.Exists(ConfigDirectory))
				Directory.CreateDirectory(ConfigDirectory);

			if (File.Exists(ConfigFilePath))
			{
				string json = File.ReadAllText(ConfigFilePath);
				config = JsonUtility.FromJson<DoxygenConfig>(json);
			}

			if (config == null || config.inputPaths == null || config.inputPaths.Count == 0)
			{
				config = new DoxygenConfig();
				config.inputPaths.Add(string.Empty); // Ensure at least one entry
			}
		}

		private static void SaveConfig()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			Debug.Log($"Saving Config to: {projectRoot}");
			// Convert to relative paths
			var relativePaths = new List<string>();
			foreach (var absPath in config.inputPaths)
			{
				if (!string.IsNullOrEmpty(absPath) && absPath.StartsWith(projectRoot))
					relativePaths.Add(absPath.Substring(projectRoot.Length + 1));
				else
					relativePaths.Add(absPath);
			}

			var saveConfig = new DoxygenConfig { inputPaths = relativePaths };
			string json = JsonUtility.ToJson(saveConfig, true);
			File.WriteAllText(ConfigFilePath, json);
		}

		
		//public static void SaveConfig(DoxygenConfig config)
  //      {
		//	string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
  //          Debug.Log($"Saving Config to: {projectRoot}");
		//	// When saving:
		//	for (int i = 0; i < config.inputPaths.Count; i++)
		//	{
		//		string absPath = config.inputPaths[i];
		//		if (absPath.StartsWith(projectRoot))
		//			config.inputPaths[i] = absPath.Substring(projectRoot.Length + 1); // store relative
		//	}
		//	File.WriteAllText(configFile, JsonUtility.ToJson(config, true));
		//}

		public static string project
        {
            get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(project)}", string.Empty);
            set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(project)}", value);
        }

        public static string synopsis
        {
            get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(synopsis)}", string.Empty);
            set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(synopsis)}", value);
        }

        public static string version
        {
            get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(version)}", string.Empty);
            set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(version)}", value);
        }

        public static bool o_MarkdownSupport
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_MarkdownSupport)}", true);
            set=> EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_MarkdownSupport)}", value);
        }

        public static bool o_AlNumSorting
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_AlNumSorting)}", false);
            set => EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_AlNumSorting)}", value);
        }
        
        public static bool o_ShowReferencesRelation
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowReferencesRelation)}", false);
            set => EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowReferencesRelation)}", value);
        }

        public static bool o_ShowReferencedByRelation
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowReferencedByRelation)}", false);
            set => EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowReferencedByRelation)}", value);
        }
        
        public static bool o_ShowUsedFiles
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowUsedFiles)}", true);
            set=> EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowUsedFiles)}", value);
        }

        public static bool o_ShowFiles
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowFiles)}", true);
            set => EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowFiles)}", value);
        }

        public static bool o_ShowNamespaces
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowNamespaces)}", true);
            set => EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_ShowNamespaces)}", value);
        }

        public static bool o_HideScopeNames
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_HideScopeNames)}", false);
            set => EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_HideScopeNames)}", value);
        }

        public static bool o_HideCompoundRefs
        {
            get => EditorPrefs.GetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_HideCompoundRefs)}", false);
            set => EditorPrefs.SetBool($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_HideCompoundRefs)}", value);
        }

        public static string o_MainPage
        {
            get => EditorPrefs.GetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_MainPage)}", string.Empty);
            set => EditorPrefs.SetString($"{nameof(DoxygenGenerator)}.{nameof(GeneratorSettings)}.{nameof(o_MainPage)}", value);
        }
    }
}
