# Dependencies

| Dependency | Version | Used by | Source / verification |
| --- | --- | --- | --- |
| Unity Editor | 6000.3.24f1 (4e7b9b5b6244) | all Unity assemblies | Unity release API; `Unity -version` output |
| com.unity.render-pipelines.universal | 17.3.0 | Presentation, Editor | bundled editor manifest |
| com.unity.ugui (incl. TextMeshPro) | 2.0.0 | Presentation/UI | bundled editor manifest |
| com.unity.inputsystem | 1.20.0 | Presentation | bundled editor manifest |
| com.unity.test-framework | 1.6.0 | Tests | bundled editor manifest |
| com.unity.nuget.newtonsoft-json | 3.2.2 | Persistence, level JSON | bundled editor manifest |
| Android OpenJDK / NDK / SDK / build-tools | 17.0.18 / r27c / 36 / 36.0.0 | Android builds | Unity release API module list |
| .NET SDK | 8.0.425 | engine-free compile + tests (`tools/dotnet`) | `dotnet --version` |
| Newtonsoft.Json (NuGet, tests only) | 13.0.3 | `tools/dotnet` | csproj |
| NUnit / NUnit3TestAdapter / Microsoft.NET.Test.Sdk | 3.14.0 / 4.6.0 / 17.11.1 | `tools/dotnet` | csproj |
| Google Mobile Ads + UMP, Firebase Analytics/Crashlytics/Remote Config, Unity IAP | not yet added | Services | to be pinned from official docs in Prompts 12–14 |

Assembly dependency graph: see `ARCHITECTURE.md`. `Packages/packages-lock.json` will be generated and committed on the first licensed Unity run.
