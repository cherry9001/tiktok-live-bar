#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;

namespace TikTokLiveGame.Editor
{
    public static class CreateTikTokScene
    {
        [MenuItem("TikTok Live Game/Create Main Scene")]
        public static void CreateScene()
        {
            CreateRuntimeMaterials();
            CreateDjAnimatorController();
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        }

        [MenuItem("TikTok Live Game/Build Windows Game")]
        public static void BuildWindowsGame()
        {
            CreateRuntimeMaterials();
            CreateDjAnimatorController();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            ForceWindowsX64BuildProfile();
            string outputPath = Path.GetFullPath("Builds/TikTokLiveGameUnity.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            string[] scenes = System.Array.ConvertAll(
                System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled),
                scene => scene.path
            );
            BuildReport report = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException($"Windows build failed: {report.summary.result}");
        }

        [MenuItem("TikTok Live Game/Build Commercial Windows Game (IL2CPP)")]
        public static void BuildCommercialWindowsGame()
        {
            CreateRuntimeMaterials();
            CreateDjAnimatorController();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            ForceWindowsX64BuildProfile();
            NamedBuildTarget target = NamedBuildTarget.Standalone;
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(target);
            ManagedStrippingLevel previousStripping = PlayerSettings.GetManagedStrippingLevel(target);
            bool previousStripEngineCode = PlayerSettings.stripEngineCode;
            string previousCompany = PlayerSettings.companyName;
            string previousProduct = PlayerSettings.productName;

            try
            {
                PlayerSettings.companyName = "ÔNG CHÚ MMO";
                PlayerSettings.productName = "ÔNG CHÚ MMO Live";
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);
                PlayerSettings.stripEngineCode = true;

                string outputPath = Path.GetFullPath("CommercialBuild/OngChuMMO_Live.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                string[] scenes = System.Array.ConvertAll(
                    System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled),
                    scene => scene.path
                );
                BuildReport report = BuildPipeline.BuildPlayer(
                    scenes,
                    outputPath,
                    BuildTarget.StandaloneWindows64,
                    BuildOptions.CompressWithLz4HC
                );
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.InvalidOperationException($"Commercial Windows build failed: {report.summary.result}");
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(target, previousBackend);
                PlayerSettings.SetManagedStrippingLevel(target, previousStripping);
                PlayerSettings.stripEngineCode = previousStripEngineCode;
                PlayerSettings.companyName = previousCompany;
                PlayerSettings.productName = previousProduct;
                AssetDatabase.SaveAssets();
            }
        }

        public static void DumpWindowsBuildArchitecture()
        {
            System.Type settingsType = System.Type.GetType(
                "UnityEditor.WindowsStandalone.WindowsPlatformSettings, UnityEditor.WindowsStandalone.Extensions");
            if (settingsType == null)
            {
                Debug.LogError("WindowsPlatformSettings type was not found.");
                return;
            }

            Debug.Log($"WINDOWS_SETTINGS_TYPE={settingsType.FullName}");
            foreach (System.Reflection.FieldInfo field in settingsType.GetFields(
                         System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.Static |
                         System.Reflection.BindingFlags.Public |
                         System.Reflection.BindingFlags.NonPublic))
            {
                Debug.Log($"WINDOWS_FIELD={field.Name}|{field.FieldType.FullName}");
                if (field.IsStatic)
                    Debug.Log($"WINDOWS_STATIC_VALUE={field.Name}|{field.GetValue(null)}");
                if (field.FieldType.IsEnum)
                    Debug.Log($"WINDOWS_ENUM={field.FieldType.FullName}|{string.Join(",", System.Enum.GetNames(field.FieldType))}|{string.Join(",", System.Enum.GetValues(field.FieldType).Cast<object>())}");
            }

            foreach (System.Reflection.PropertyInfo property in settingsType.GetProperties(
                         System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.Static |
                         System.Reflection.BindingFlags.Public |
                         System.Reflection.BindingFlags.NonPublic))
            {
                Debug.Log($"WINDOWS_PROPERTY={property.Name}|{property.PropertyType.FullName}");
                if (property.PropertyType.IsEnum)
                    Debug.Log($"WINDOWS_ENUM={property.PropertyType.FullName}|{string.Join(",", System.Enum.GetNames(property.PropertyType))}|{string.Join(",", System.Enum.GetValues(property.PropertyType).Cast<object>())}");
            }

            System.Type profileType = System.Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor");
            if (profileType != null)
            {
                foreach (System.Reflection.MethodInfo method in profileType.GetMethods(
                             System.Reflection.BindingFlags.Instance |
                             System.Reflection.BindingFlags.Static |
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.NonPublic))
                    Debug.Log($"BUILD_PROFILE_METHOD={method}");
                foreach (System.Reflection.PropertyInfo property in profileType.GetProperties(
                             System.Reflection.BindingFlags.Instance |
                             System.Reflection.BindingFlags.Static |
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.NonPublic))
                    Debug.Log($"BUILD_PROFILE_PROPERTY={property.Name}|{property.PropertyType.FullName}");
                foreach (System.Reflection.FieldInfo field in profileType.GetFields(
                             System.Reflection.BindingFlags.Instance |
                             System.Reflection.BindingFlags.Static |
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.NonPublic))
                    Debug.Log($"BUILD_PROFILE_FIELD={field.Name}|{field.FieldType.FullName}");
            }
        }

        private static void ForceWindowsX64BuildProfile()
        {
            // Unity 6 Build Profiles can override BuildTarget.StandaloneWindows64
            // with a previously selected x86 architecture. Set the active Windows
            // platform component explicitly so automated commercial builds stay x64.
            System.Type settingsType = System.Type.GetType(
                "UnityEditor.WindowsStandalone.WindowsPlatformSettings, UnityEditor.WindowsStandalone.Extensions");
            System.Type profileType = System.Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor");
            if (settingsType == null || profileType == null)
                throw new System.InvalidOperationException("Unity Windows Build Profile API is unavailable.");

            System.Reflection.PropertyInfo platformSettingsProperty = profileType.GetProperty(
                "platformBuildProfile",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            System.Reflection.MethodInfo isActiveMethod = profileType.GetMethod(
                "IsActiveBuildProfileOrPlatform",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            object settings = null;
            foreach (UnityEngine.Object profile in Resources.FindObjectsOfTypeAll(profileType))
            {
                bool isActive = isActiveMethod != null && (bool)isActiveMethod.Invoke(profile, null);
                object candidate = platformSettingsProperty?.GetValue(profile);
                if (candidate != null && settingsType.IsInstanceOfType(candidate) && isActive)
                {
                    settings = candidate;
                    break;
                }
                if (settings == null && candidate != null && settingsType.IsInstanceOfType(candidate))
                    settings = candidate;
            }
            System.Reflection.PropertyInfo architecture = settingsType.GetProperty(
                "architecture",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (settings == null || architecture == null)
                throw new System.InvalidOperationException("Cannot update the Windows Build Profile architecture.");

            object x64 = System.Enum.Parse(architecture.PropertyType, "x64");
            architecture.SetValue(settings, x64);
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1);
            EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", "x64");
            Debug.Log("WINDOWS_BUILD_ARCHITECTURE=x64");
        }

        private static void CreateRuntimeMaterials()
        {
            Directory.CreateDirectory("Assets/Resources/Materials");
            CreateMaterial(
                "Assets/Shaders/ClubColor.shader",
                "Assets/Resources/Materials/ClubColor.mat"
            );
            CreateMaterial(
                "Assets/Shaders/ClubLit.shader",
                "Assets/Resources/Materials/ClubLit.mat"
            );
            CreateMaterial(
                "Assets/Shaders/CharacterSprite.shader",
                "Assets/Resources/Materials/CharacterSprite.mat"
            );
            CreateMaterial(
                "Assets/Shaders/CircularAvatar.shader",
                "Assets/Resources/Materials/CircularAvatar.mat"
            );
            AssetDatabase.SaveAssets();
        }

        private static void CreateMaterial(string shaderPath, string materialPath)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null) throw new FileNotFoundException($"Shader not found: {shaderPath}");
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
            {
                existing.shader = shader;
                EditorUtility.SetDirty(existing);
                return;
            }
            AssetDatabase.CreateAsset(new Material(shader), materialPath);
        }

        private static void CreateDjAnimatorController()
        {
            const string modelPath = "Assets/Resources/DJ/Megan_DJ.fbx";
            const string controllerPath = "Assets/Resources/DJ/Megan_DJ_Controller.controller";
            if (!File.Exists(modelPath)) return;

            const string textureDirectory = "Assets/Resources/DJ/Textures";
            Directory.CreateDirectory(textureDirectory);
            ModelImporter modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (modelImporter != null && Directory.GetFiles(textureDirectory).Length == 0)
            {
                modelImporter.ExtractTextures(textureDirectory);
                AssetDatabase.Refresh();
            }
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__"));
            if (clip == null) throw new FileNotFoundException("DJ animation clip was not found in Megan_DJ.fbx");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = stateMachine.states.Select(item => item.state).FirstOrDefault(item => item.name == "DJ Performance");
            if (state == null) state = stateMachine.AddState("DJ Performance");
            state.motion = clip;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
