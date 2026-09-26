using System.IO;
using System.Linq;
using ScrewGame.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ScrewGame.EditorTools
{
    /// <summary>
    /// Idempotent project configuration. Run from the menu or batch mode:
    /// Unity -batchmode -quit -projectPath . -executeMethod ScrewGame.EditorTools.ProjectSetup.Run
    /// Existing assets are reused (never recreated) so GUIDs and .meta files stay stable.
    /// </summary>
    public static class ProjectSetup
    {
        public const string SettingsDir = "Assets/ScrewGame/Settings";
        public const string ScenePath = "Assets/ScrewGame/Scenes/Bootstrap.unity";
        public const string PipelinePath = SettingsDir + "/ScrewURP.asset";
        public const string RendererPath = SettingsDir + "/ScrewURP_Renderer.asset";
        public const string LitMaterialPath = SettingsDir + "/ScrewLit.mat";
        /// <summary>PLACEHOLDER development identifier; release builds replace it from SCREW_BUNDLE_ID.</summary>
        public const string BundleId = "com.nyrico.projectscrew.dev";
        public const string ProductName = "Screw Workshop";

        [MenuItem("Screw Game/Configure Project")]
        public static void Run()
        {
            EnsureTmpEssentials();
            var pipeline = EnsurePipeline();
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            ConfigurePlayer();
            var mat = EnsureLitMaterial();
            EnsureScene(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] done");
        }

        private static void EnsureTmpEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null) return;
            var pkg = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(pkg)) { Debug.LogError("[ProjectSetup] TMP essentials not found at " + pkg); return; }
            AssetDatabase.ImportPackage(pkg, false);
            AssetDatabase.Refresh();
        }

        private static UniversalRenderPipelineAsset EnsurePipeline()
        {
            Directory.CreateDirectory(SettingsDir);
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (asset != null) return asset;
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            asset = UniversalRenderPipelineAsset.Create(renderer);
            asset.renderScale = 1f;
            asset.msaaSampleCount = 4;
            asset.supportsHDR = false;
            asset.shadowDistance = 25f;
            AssetDatabase.CreateAsset(asset, PipelinePath);
            return asset;
        }

        private static Material EnsureLitMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, LitMaterialPath);
            return mat;
        }

        private static void EnsureScene(Material lit)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene;
            if (File.Exists(ScenePath)) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Object.FindFirstObjectByType<GameRoot>();
            if (root == null) root = new GameObject("GameRoot").AddComponent<GameRoot>();
            root.LitTemplate = lit;
            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Nyrico";
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            // Input System only (no legacy Input Manager).
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").First());
            var handler = so.FindProperty("activeInputHandler");
            if (handler != null) { handler.intValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); }
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
        }
    }
}
