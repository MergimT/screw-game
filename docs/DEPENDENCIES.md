# Dependencies

| Dependency | Version | Source / verification |
| --- | --- | --- |
| Unity Editor | 6000.3.24f1 (4e7b9b5b6244) | Unity release API; `Unity -version` output |
| com.unity.render-pipelines.universal | 17.3.0 | bundled editor manifest |
| com.unity.ugui (incl. TextMeshPro) | 2.0.0 | bundled editor manifest |
| com.unity.inputsystem | 1.20.0 | bundled editor manifest |
| com.unity.test-framework | 1.6.0 | bundled editor manifest |
| com.unity.nuget.newtonsoft-json | 3.2.2 | bundled editor manifest |
| Android OpenJDK / NDK / build-tools | 17.0.18 / r27c / 36.0.0 | Unity release API module list |
| .NET SDK (tests only) | 8.0.425 | `dotnet --version` |
| Google Mobile Ads + UMP, Firebase, Unity IAP | not yet added | to be pinned from official docs in Prompts 12–14 |

`Packages/packages-lock.json` will be generated and committed on the first licensed Unity run.
