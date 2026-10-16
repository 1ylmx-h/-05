using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlobalDeformationDemo.Editor
{
    public static class GlobalDeformationProjectBuilder
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/GlobalDeformationDemo.unity";
        private const string SampleObjPath = "Assets/StreamingAssets/DeformationSample.obj";

        [MenuItem("Global Deformation Demo/Generate Project Assets")]
        public static void GenerateProjectAssets()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory("Assets/StreamingAssets");
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, SampleObjPath), GenerateSampleObj());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject application = new GameObject("Global Deformation Demo Application");
            application.AddComponent<GlobalDeformationDemoApp>();
            application.AddComponent<DeformationDemoRecorder>();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Could not save the generated demonstration scene.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[GlobalDeformationBuilder] Generated scene and sample OBJ: {ScenePath}");
        }

        [MenuItem("Global Deformation Demo/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            if (!File.Exists(ScenePath)) GenerateProjectAssets();
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(root, "Builds", "Windows");
            Directory.CreateDirectory(outputDirectory);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outputDirectory, "GlobalDeformationDemo.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[GlobalDeformationBuilder] Build result={report.summary.result}, errors={report.summary.totalErrors}, size={report.summary.totalSize}");
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            }
        }

        public static void BuildAll()
        {
            GenerateProjectAssets();
            BuildWindowsPlayer();
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "24 Digital Media Class 1";
            PlayerSettings.productName = "Global Deformation Demo";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            EnsureAlwaysIncludedShader("Standard");
            EnsureAlwaysIncludedShader("Sprites/Default");
            EnsureAlwaysIncludedShader("UI/Default");
        }

        private static void EnsureAlwaysIncludedShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Required shader not found: " + shaderName);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            SerializedObject settings = new SerializedObject(assets[0]);
            SerializedProperty shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            for (int index = 0; index < shaders.arraySize; index++)
            {
                if (shaders.GetArrayElementAtIndex(index).objectReferenceValue == shader) return;
            }
            int newIndex = shaders.arraySize;
            shaders.InsertArrayElementAtIndex(newIndex);
            shaders.GetArrayElementAtIndex(newIndex).objectReferenceValue = shader;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureDirectory(string assetPath)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath));
        }

        private static string GenerateSampleObj()
        {
            const int segments = 24;
            const int rings = 13;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Subdivided sample OBJ for taper, twist, bend and FFD demonstration");
            builder.AppendLine("o DeformationSample");
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float y = Mathf.Lerp(-1.6f, 1.6f, t);
                float radius = 0.62f + 0.14f * Mathf.Sin(t * Mathf.PI) - 0.08f * Mathf.Cos(t * Mathf.PI * 2f);
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment / (float)segments * Mathf.PI * 2f;
                    // Elliptical sections make the twist deformation visually obvious.
                    float x = Mathf.Cos(angle) * radius * 1.18f;
                    float z = Mathf.Sin(angle) * radius * 0.72f;
                    builder.Append("v ")
                        .Append(x.ToString("0.######", CultureInfo.InvariantCulture)).Append(' ')
                        .Append(y.ToString("0.######", CultureInfo.InvariantCulture)).Append(' ')
                        .Append(z.ToString("0.######", CultureInfo.InvariantCulture)).AppendLine();
                }
            }

            for (int ring = 0; ring < rings - 1; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    int a = ring * segments + segment + 1;
                    int b = ring * segments + next + 1;
                    int c = (ring + 1) * segments + next + 1;
                    int d = (ring + 1) * segments + segment + 1;
                    builder.AppendLine($"f {a} {b} {c} {d}");
                }
            }
            return builder.ToString();
        }
    }
}
