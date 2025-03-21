using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DoxygenGenerator
{
    public class GeneratorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Thread doxygenThread;
        private string generateButtonName = "Generate";
        //MainPage Selector Vars
        private string setMainPage { get => GeneratorSettings.o_MainPage; set => GeneratorSettings.o_MainPage = value; }
        private bool isSelectingMainPage = false;
        private List<string> mainPageCandidates = new List<string>();
		private List<string> inputPaths = new List<string>();

		//Option GUIs
		private GUIContent o_MarkdownSupportLabel;
        private GUIContent o_AlNumSortingLabel;
        private GUIContent o_showReferencedByRelationLabel;
        private GUIContent o_showReferencesRelationLabel;
        private GUIContent o_showUsedFilesLabel;
        private GUIContent o_showFilesLabel;
        private GUIContent o_showNamespacesLabel;
        private GUIContent o_hideScopeNamesLabel;
        private GUIContent o_hideCompoundRefsLabel;
        //Styles
        private GUIStyle o_ToggleStyle { get => EditorStyles.toolbarButton; }

        private bool canGenerate => File.Exists(doxygenPath)
	        && inputPaths.All(p => !string.IsNullOrEmpty(p) && Directory.Exists(GetAbsolutePath(p)))
			//&& allInputsValid//Directory.Exists(inputDirectory)
			&& Directory.Exists(outputDirectory)
            && doxygenThread == null;

		public bool allInputsValid => inputPaths.Count > 0 && inputPaths.All(p =>
						 !string.IsNullOrEmpty(p) && Directory.Exists(GetAbsolutePath(p)));


        #region Settings
        private string doxygenPath
        {
            get => GeneratorSettings.doxygenPath;
            set => GeneratorSettings.doxygenPath = value;
        }
		private string customPath
		{
			get => GeneratorSettings.customPath;
			set => GeneratorSettings.customPath = value;
		}
		private string inputDirectory
        {
            get => GeneratorSettings.inputDirectory;
            set => GeneratorSettings.inputDirectory = value;
        }
        private static List<string> staticInputPaths
		{
			get => GeneratorSettings.InputPaths;
			set => GeneratorSettings.InputPaths = value;
		}
		private string outputDirectory
        {
            get => GeneratorSettings.outputDirectory;
            set => GeneratorSettings.outputDirectory = value;
        }

        private string project
        {
            get => GeneratorSettings.project;
            set => GeneratorSettings.project = value;
        }

        private string synopsis
        {
            get => GeneratorSettings.synopsis;
            set => GeneratorSettings.synopsis = value;
        }

        private string version
        {
            get => GeneratorSettings.version;
            set => GeneratorSettings.version = value;
        }

        private bool o_MarkdownSupport
        {
            get => GeneratorSettings.o_MarkdownSupport;
            set => GeneratorSettings.o_MarkdownSupport = value;
        }

        private bool o_AlNumSorting
        {
            get => GeneratorSettings.o_AlNumSorting;
            set => GeneratorSettings.o_AlNumSorting = value;
        }

        private bool o_ShowReferencedByRelation
        {
            get => GeneratorSettings.o_ShowReferencedByRelation;
            set => GeneratorSettings.o_ShowReferencedByRelation = value;
        }

        private bool o_ShowReferencesRelation
        {
            get => GeneratorSettings.o_ShowReferencesRelation;
            set => GeneratorSettings.o_ShowReferencesRelation = value;
        }

        private bool o_ShowUsedFiles
        {
            get => GeneratorSettings.o_ShowUsedFiles;
            set => GeneratorSettings.o_ShowUsedFiles = value;
        }

        private bool o_ShowFiles
        {
            get => GeneratorSettings.o_ShowFiles;
            set => GeneratorSettings.o_ShowFiles = value;
        }

        private bool o_ShowNamespaces
        {
            get => GeneratorSettings.o_ShowNamespaces;
            set => GeneratorSettings.o_ShowNamespaces = value;
        }

        private bool o_HideScopeNames
        {
            get => GeneratorSettings.o_HideScopeNames;
            set => GeneratorSettings.o_HideScopeNames = value;
        }

        private bool o_HideCompoundReference
        {
            get => GeneratorSettings.o_HideCompoundRefs;
            set => GeneratorSettings.o_HideCompoundRefs = value;
        }

		#endregion

		[MenuItem("Window/Doxygen Generator")]
        public static void Initialize()
        {
			var window = GetWindow<GeneratorWindow>("Doxygen Generator");
            window.minSize = new Vector2(420, 245);
            window.Show();
        }
        private void InitGuiContent()
        {
            o_MarkdownSupportLabel = new GUIContent("Markdown Support", "Allow Doxygen to read child md files and add them to the generated output");
            o_AlNumSortingLabel = new GUIContent("AlphaNumeric Sorting", "Should use alphanumeric sorting for all members in classes");
            o_showReferencedByRelationLabel = new GUIContent("Referenced By Relations", "Toggle to show or hide the list of functions, variables, or classes that reference the current entity in the documentation.");
            o_showReferencesRelationLabel = new GUIContent("References Relations", "Toggle to display or conceal the list of functions, variables, or classes that the current entity references in the documentation.");
            o_showUsedFilesLabel = new GUIContent("Show Doc Used Files", "Toggle to include a list of files used by the documented entity at the foot of the document.");
            o_showFilesLabel = new GUIContent("Show Files", "Toggle to display a list of all the documented files in the heirarchy.");
            o_showNamespacesLabel = new GUIContent("Show Namespaces", "Toggle to include documentation for all namespaces.");
            o_hideScopeNamesLabel = new GUIContent("Hide Scope Names", "Toggle to hide scope names in the generated documentation for a cleaner look.");
            o_hideCompoundRefsLabel = new GUIContent("Hide Compound References", "Toggle to hide references to compound types, like classes or structs, within the documentation.");
        }
		private void InitDoxyConfig()
		{
			DoxygenConfig config = File.Exists(GeneratorSettings.configFile)
	        ? JsonUtility.FromJson<DoxygenConfig>(File.ReadAllText(GeneratorSettings.configFile))
	        : new DoxygenConfig();
			if (config == null || config.inputPaths == null || config.inputPaths.Count == 0)
            {
                Debug.Log("Default Doxy Config Loaded :O");
				config.inputPaths = new List<string> { "" }; // ensure at least one entry
            }
		}

		private void OnEnable()
        {
			inputPaths = new List<string>(GeneratorSettings.InputPaths);

			InitGuiContent();
            InitDoxyConfig();
		}
		private void OnDisable()
		{
			GeneratorSettings.InputPaths = inputPaths;
		}
		private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Select your doxygen install location
            DoxygenInstallPathGUI();
            DoxyFilePathGUI();
            // Setup the directories
            //SetupTheDirectoriesGUI();
            SetupMultipleDirectoriesGUI();
			// Set your project settings
			ProjectSettingsGUI();

            using (var splitScope = new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                using (var verticalScope = new EditorGUILayout.VerticalScope( GUILayout.Width((EditorGUIUtility.currentViewWidth * 0.5f))))
                {
                    // Generate the API
                    DocumentationGUI();
                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                }
                using (var leftScope = new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    // Change how the docs output
                    ContentOptionsGUI();
                }
            }

            EditorGUILayout.EndScrollView();
        }
		private static string GetAbsolutePath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(projectRoot, path));
		}
		private void DoxygenInstallPathGUI()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            GUILayout.Label("Doxygen Install Path", EditorStyles.boldLabel);

            // Doxygen not selected error
            if (!File.Exists(doxygenPath))
            {
                doxygenPath = default;
                EditorGUILayout.HelpBox("No doxygen install path is selected. Please install Doxygen and select it below.", MessageType.Error, true);
                if (GUILayout.Button("Download Doxygen", GUILayout.MaxWidth(150)))
                {
                    Application.OpenURL("https://www.doxygen.nl/download.html");
                }
            }

            // Doxygen Path
            EditorGUILayout.BeginHorizontal();
            doxygenPath = EditorGUILayout.DelayedTextField("doxygen.exe", doxygenPath);
            if (GUILayout.Button("...", EditorStyles.miniButtonRight, GUILayout.Width(22)))
            {
                doxygenPath = EditorUtility.OpenFilePanel("Select your doxygen.exe", string.Empty, string.Empty);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DoxyFilePathGUI()
        {
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
			GUILayout.Label("Custom DoxyFile Path (Optional)", EditorStyles.boldLabel);
			// Doxygen not selected error
			if (!Directory.Exists(customPath))
			{
				customPath = default;
				EditorGUILayout.HelpBox("No custom path is selected. So wont look for the DoxyFile there.", MessageType.Info, true);
			}
			EditorGUILayout.BeginHorizontal();
			customPath = EditorGUILayout.DelayedTextField("Custom Doxyfile path", customPath);
			if (GUILayout.Button("...", EditorStyles.miniButtonRight, GUILayout.Width(22)))
			{
				customPath = EditorUtility.OpenFolderPanel("Select your doxyFile", string.Empty, string.Empty);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void SetupMultipleDirectoriesGUI()
        {
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
			GUILayout.Label("Setup the Input Directories", EditorStyles.boldLabel);

			for (int i = 0; i < inputPaths.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				string label = (i == 0) ? "Input Directory" : $"Input Directory {i + 1}";

				string newPath = EditorGUILayout.DelayedTextField(label, inputPaths[i]);

				if (GUILayout.Button("...", EditorStyles.miniButtonLeft, GUILayout.Width(22)))
				{
					string selected = EditorUtility.OpenFolderPanel("Select Input Directory", "", "");
					if (!string.IsNullOrEmpty(selected))
					{
						newPath = selected;
						GUI.FocusControl(null);
					}
				}

				if (newPath != inputPaths[i])
				{
					inputPaths[i] = newPath;
					GeneratorSettings.InputPaths = new List<string>(inputPaths); // explicit save
				}

				if (inputPaths.Count > 1 && GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(22)))
				{
					inputPaths.RemoveAt(i--);
					GeneratorSettings.InputPaths = new List<string>(inputPaths); // explicit save
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add Input Path"))
			{
				inputPaths.Add(string.Empty);
				GeneratorSettings.InputPaths = new List<string>(inputPaths); // explicit save
			}

			bool allInputsValid = inputPaths.All(p => !string.IsNullOrEmpty(p) && Directory.Exists(GetAbsolutePath(p)));
			if (!allInputsValid)
			{
				EditorGUILayout.HelpBox("One or more input directories are not set or do not exist. Please select valid directories.", MessageType.Error);
			}
		}
		private void SetupTheDirectoriesGUI()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            GUILayout.Label("Setup the Directories", EditorStyles.boldLabel);

            // Input not selected error
            if (!Directory.Exists(inputDirectory))
            {
                inputDirectory = default;
                EditorGUILayout.HelpBox("No input directory selected. Please select a directory you would like your API to be generated from.", MessageType.Error, true);
            }

            // Input Directory
            EditorGUILayout.BeginHorizontal();
            inputDirectory = EditorGUILayout.DelayedTextField("Input Directory", inputDirectory);
            if (GUILayout.Button("...", EditorStyles.miniButtonRight, GUILayout.Width(22)))
            {
                inputDirectory = EditorUtility.OpenFolderPanel("Select your Input Directory", string.Empty, string.Empty);
            }
            EditorGUILayout.EndHorizontal();

            // Output not selected error
            if (!Directory.Exists(outputDirectory))
            {
                outputDirectory = default;
                EditorGUILayout.HelpBox("No output directory selected. Please select a directory you would like your API to be generated to.", MessageType.Error, true);
            }

            // Output Directory
            EditorGUILayout.BeginHorizontal();
            outputDirectory = EditorGUILayout.DelayedTextField("Output Directory", outputDirectory);
            if (GUILayout.Button("...", EditorStyles.miniButtonRight, GUILayout.Width(22)))
            {
                outputDirectory = EditorUtility.OpenFolderPanel("Select your Output Directory", string.Empty, string.Empty);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ProjectSettingsGUI()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            GUILayout.Label("Project Settings", EditorStyles.boldLabel);
            project = EditorGUILayout.TextField("Name", project);
            synopsis = EditorGUILayout.TextField("Synopsis", synopsis);
            version = EditorGUILayout.TextField("Version", version);
        }

        private void DocumentationGUI()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            GUILayout.Label("Documentation", EditorStyles.boldLabel);

            // Update the doxygen thread
            if (doxygenThread != null)
            {
                switch (doxygenThread.ThreadState)
                {
                    case ThreadState.Aborted:
                    case ThreadState.Stopped:
                        doxygenThread = null;
                        generateButtonName = "Generate";
                        break;
                }
            }

            // Generate Button
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button(generateButtonName, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
            {
                doxygenThread = Generator.GenerateAsync();
                generateButtonName = "Generating...";
            }
            EditorGUI.EndDisabledGroup();

            // Open Button
            EditorGUI.BeginDisabledGroup(!Directory.Exists(outputDirectory) || doxygenThread != null);
            if (GUILayout.Button("Open", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
            {
                System.Diagnostics.Process.Start(outputDirectory);
            }
            EditorGUI.EndDisabledGroup();

            // View Log Button
            var logPath = $"{outputDirectory}/Log.txt";
            EditorGUI.BeginDisabledGroup(!File.Exists(logPath) || doxygenThread != null);
            if (GUILayout.Button("View Log", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
            {
                Application.OpenURL($"File://{logPath}");
            }
            EditorGUI.EndDisabledGroup();

            // Browse Button
            var browsePath = $"{outputDirectory}/docs/annotated.html";
            EditorGUI.BeginDisabledGroup(!File.Exists(browsePath) || doxygenThread != null);
            if (GUILayout.Button("Browse", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
            {
                Application.OpenURL($"File://{browsePath}");
            }
            EditorGUI.EndDisabledGroup();
        }
        
        private void ContentOptionsGUI()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            GUILayout.Label("Options", EditorStyles.boldLabel);
            o_MarkdownSupport = EditorGUILayout.ToggleLeft(o_MarkdownSupportLabel, o_MarkdownSupport, o_ToggleStyle);
            o_AlNumSorting = EditorGUILayout.ToggleLeft(o_AlNumSortingLabel, o_AlNumSorting, o_ToggleStyle);
            o_ShowFiles = EditorGUILayout.ToggleLeft(o_showFilesLabel, o_ShowFiles, o_ToggleStyle);
            o_ShowNamespaces = EditorGUILayout.ToggleLeft(o_showNamespacesLabel, o_ShowNamespaces, o_ToggleStyle);
            o_ShowUsedFiles = EditorGUILayout.ToggleLeft(o_showUsedFilesLabel, o_ShowUsedFiles, o_ToggleStyle);
            o_ShowReferencedByRelation = EditorGUILayout.ToggleLeft(o_showReferencedByRelationLabel, o_ShowReferencedByRelation, o_ToggleStyle);
            o_ShowReferencesRelation = EditorGUILayout.ToggleLeft(o_showReferencesRelationLabel, o_ShowReferencesRelation, o_ToggleStyle);
            o_HideScopeNames = EditorGUILayout.ToggleLeft(o_hideScopeNamesLabel, o_HideScopeNames, o_ToggleStyle);
            o_HideCompoundReference = EditorGUILayout.ToggleLeft(o_hideCompoundRefsLabel, o_HideCompoundReference, o_ToggleStyle);
        }
    }
}
