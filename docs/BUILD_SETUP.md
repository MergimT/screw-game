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

## Unity
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
Status 2026-09-26 (Student Plan license activated in Unity Hub): ProjectSetup exit 0; EditMode 99/99; iOS Xcode export Succeeded (0 errors); Linux64 smoke player: `-buildTarget Linux64 -buildLinux64Player Builds/Linux/ScrewWorkshop.x86_64`. Android APK fails: `Missing CMake 3.22.1`.

Build stages are distinct: Unity compilation ≠ Xcode export ≠ Xcode archive ≠ signed device install; Android development APK ≠ release AAB. Achieved: Unity compilation and Xcode export. Not achieved: Xcode archive, signed install, Android APK/AAB.
