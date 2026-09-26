# Build setup

## Toolchain (as installed in the Devin Linux VM, 2026-09-26)
- Unity Editor 6000.3.24f1 (changeset 4e7b9b5b6244) at `~/Unity/6000.3.24f1/Editor/Unity`, from
  `https://download.unity3d.com/download_unity/4e7b9b5b6244/LinuxEditorInstaller/Unity-6000.3.24f1.tar.xz`.
- iOS Build Support → `Editor/Data/PlaybackEngines/iOSSupport` (export only; archive needs macOS/Xcode).
- Android Build Support → `Editor/Data/PlaybackEngines/AndroidPlayer` (extracted from the official `.pkg` via `7z` + `cpio`), with
  OpenJDK 17.0.18 (`OpenJDK/`), NDK r27c (`NDK/`), SDK build-tools 36.0.0, platform 36, platform-tools, cmdline-tools (`SDK/`).
- .NET SDK 8.0.425 at `~/.dotnet` for engine-free tests.

## Engine-free verification (works now)
```bash
cd tools/dotnet
export PATH=$HOME/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test ScrewGame.Tests                       # 99/99 passed on 2026-09-26
dotnet run --project ScrewGame.Cli -c Release -- validate \
  ../../Assets/ScrewGame/Resources/Levels ../../reports/level-validation.json 2000000
```

## Unity (blocked until a license is activated)
```bash
U=$HOME/Unity/6000.3.24f1/Editor/Unity
# one-time, idempotent project configuration (URP asset, player settings, Bootstrap scene)
xvfb-run -a $U -batchmode -quit -projectPath . -executeMethod ScrewGame.EditorTools.ProjectSetup.Run -logFile -
# EditMode tests
xvfb-run -a $U -batchmode -projectPath . -runTests -testPlatform EditMode -testResults reports/editmode.xml -logFile -
# Android development APK -> Builds/Android/ProjectScrew-dev.apk
xvfb-run -a $U -batchmode -quit -projectPath . -buildTarget Android -executeMethod ScrewGame.EditorTools.BuildScripts.AndroidDevelopmentApk -logFile -
# Android release AAB (needs SCREW_BUNDLE_ID + keystore env)
xvfb-run -a $U -batchmode -quit -projectPath . -buildTarget Android -executeMethod ScrewGame.EditorTools.BuildScripts.AndroidReleaseAab -logFile -
# iOS Xcode export -> Builds/iOS/Xcode (archive/sign on macOS)
xvfb-run -a $U -batchmode -quit -projectPath . -buildTarget iOS -executeMethod ScrewGame.EditorTools.BuildScripts.IosXcodeExport -logFile -
```
Last attempted: `Unity -batchmode -quit -nographics -username … -password …` → exit 198, "No valid Unity Editor license found" (entitlement `com.unity.editor.headless` not found).

Build stages are distinct: Unity compilation ≠ Xcode export ≠ Xcode archive ≠ signed device install; Android development APK ≠ release AAB. None have been achieved yet.
