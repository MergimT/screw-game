# Owner setup (things only the owner can provide)

| Item | Needed for | Status | How |
| --- | --- | --- | --- |
| Unity license usable in batch mode | Compile, EditMode tests, scene start, Android APK, iOS export | **Blocking** | `UNITY_EMAIL`/`UNITY_PASSWORD` are saved and sign-in works, but the account has no entitlement usable headless (`com.unity.editor.headless` not found). Either (a) open the Devin Desktop tab, accept Unity Hub terms yourself and activate a free Personal license, or (b) provide a Pro/Build Server serial or license server. |
| macOS + Xcode 16+ machine | iOS archive, signing, TestFlight | Blocking for iOS only | Run `BuildScripts.IosXcodeExport`, then archive in Xcode. |
| Apple Developer team ID, bundle ID, provisioning | iOS release | Pending | Set `SCREW_BUNDLE_ID`; configure signing in Xcode. |
| Android upload keystore | Release AAB | Pending | Provide `SCREW_KEYSTORE_PATH`, `SCREW_KEYSTORE_PASS`, `SCREW_KEY_ALIAS`, `SCREW_KEY_PASS` (never commit). |
| AdMob app IDs + rewarded unit IDs (iOS/Android) | Prompt 13 live ads | Pending | Google test IDs will be used until provided. |
| Firebase project (`GoogleService-Info.plist`, `google-services.json`) | Prompt 12 | Pending | Files are git-ignored; supply via secure channel. |
| App Store Connect / Play Console non-consumable product ID for the cosmetic pack | Prompt 14 | Pending | Development product ID used until then. |
| Physical iPhone and Android phone | Device verification | Pending | No device evidence exists yet. |
