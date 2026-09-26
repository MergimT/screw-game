using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScrewGame.EditorTools
{
    /// <summary>
    /// Repeatable mobile build entry points (batch mode):
    ///   -executeMethod ScrewGame.EditorTools.BuildScripts.AndroidDevelopmentApk   -> Builds/Android/ProjectScrew-dev.apk
    ///   -executeMethod ScrewGame.EditorTools.BuildScripts.AndroidReleaseAab       -> Builds/Android/ProjectScrew.aab (needs keystore env)
    ///   -executeMethod ScrewGame.EditorTools.BuildScripts.IosXcodeExport          -> Builds/iOS/Xcode (archive/sign on macOS)
    /// Optional env: SCREW_BUILD_NUMBER, SCREW_KEYSTORE_PATH, SCREW_KEYSTORE_PASS, SCREW_KEY_ALIAS, SCREW_KEY_PASS.
    /// </summary>
    public static class BuildScripts
    {
        private static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        public static void AndroidDevelopmentApk()
        {
            ProjectSetup.Run();
            EditorUserBuildSettings.buildAppBundle = false;
            ApplyBuildNumber();
            Build(BuildTarget.Android, "Builds/Android/ProjectScrew-dev.apk", BuildOptions.Development);
        }

        public static void AndroidReleaseAab()
        {
            ProjectSetup.Run();
            RequireProductionIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
            var ks = Environment.GetEnvironmentVariable("SCREW_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(ks) || !File.Exists(ks)) Fail("SCREW_KEYSTORE_PATH is not set or missing; release AAB requires the owner's upload keystore.");
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("SCREW_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("SCREW_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("SCREW_KEY_PASS");
            EditorUserBuildSettings.buildAppBundle = true;
            ApplyBuildNumber();
            Build(BuildTarget.Android, "Builds/Android/ProjectScrew.aab", BuildOptions.None);
        }

        public static void IosXcodeExport()
        {
            ProjectSetup.Run();
            ApplyBuildNumber();
            Build(BuildTarget.iOS, "Builds/iOS/Xcode", BuildOptions.None);
        }

        /// <summary>The default identifier is a placeholder; production builds require SCREW_BUNDLE_ID.</summary>
        private static void RequireProductionIdentifier(UnityEditor.Build.NamedBuildTarget target)
        {
            var id = Environment.GetEnvironmentVariable("SCREW_BUNDLE_ID");
            if (string.IsNullOrEmpty(id)) Fail("SCREW_BUNDLE_ID is not set; '" + ProjectSetup.BundleId + "' is a placeholder and must not ship.");
            PlayerSettings.SetApplicationIdentifier(target, id);
        }

        private static void ApplyBuildNumber()
        {
            var n = Environment.GetEnvironmentVariable("SCREW_BUILD_NUMBER");
            if (int.TryParse(n, out var build))
            {
                PlayerSettings.Android.bundleVersionCode = build;
                PlayerSettings.iOS.buildNumber = build.ToString();
            }
        }

        private static void Build(BuildTarget target, string path, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                target = target,
                locationPathName = path,
                options = options,
            });
            var s = report.summary;
            Debug.Log($"[Build] {target} {s.result} size={s.totalSize} errors={s.totalErrors} warnings={s.totalWarnings} time={s.totalTime} output={path}");
            if (s.result != BuildResult.Succeeded) Fail("build failed: " + s.result);
        }

        private static void Fail(string message)
        {
            Debug.LogError("[Build] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw new InvalidOperationException(message);
        }
    }
}
