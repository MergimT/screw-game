using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScrewGame.Contracts;
using ScrewGame.Core;
using ScrewGame.Persistence;
using ScrewGame.Progression;
using ScrewGame.Services;
using ScrewGame.Session;
using ScrewGame.Validation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ScrewGame.Presentation
{
    /// <summary>
    /// Composition root: owns startup order (storage -> profile -> consent -> services -> content -> UI), the only place
    /// services are constructed, and all screen flow. SDK adapters are never initialized anywhere else.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public Material LitTemplate;
        public const float DragThresholdInches = 0.06f;
        public const float DegreesPerInch = 110f;

        private SaveStore _store;
        private GameSession _session;
        private Campaign _campaign;
        private readonly Dictionary<string, CompiledLevel> _levels = new Dictionary<string, CompiledLevel>();
        private IRewardedAdService _ads;
        private LocalAnalytics _analytics;
        private DeviceHaptics _haptics;
        private IClock _clock;
        private SoundBank _sound;

        private Camera _camera;
        private CameraRig _rig;
        private BoardView _board;
        private RectTransform _safe;
        private RectTransform _screen;
        private RectTransform _hud;
        private TextMeshProUGUI _toast;
        private float _toastUntil;
        private Button _undoBtn, _hintBtn, _restartBtn;
        private TextMeshProUGUI _levelLabel, _tutorialLabel;
        private string _highlightScrew;
        private Task<HintResult> _hintTask;
        private bool _pointerDown, _dragging, _pointerOverUi;
        private Vector2 _pressPos, _lastPos;
        private string _pendingNotice;

        public GameSession Session => _session;
        public BoardView Board => _board;
        public Camera MainCamera => _camera;
        public IReadOnlyDictionary<string, CompiledLevel> Levels => _levels;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = false;
            _clock = new SystemClock();

            _store = new SaveStore(new FileDurableStorage(Path.Combine(Application.persistentDataPath, "save")));
            var profile = _store.Load(out var status);
            _pendingNotice = status == LoadStatus.RecoveredFromBackup ? "recovered" : status == LoadStatus.CorruptReset ? "reset_notice" : status == LoadStatus.NewerVersionReadOnly ? "readonly_notice" : null;

            Loc.Language = string.IsNullOrEmpty(profile.Settings.Language) ? (Application.systemLanguage == SystemLanguage.French ? "fr" : "en") : profile.Settings.Language;

            // Consent before any collection: analytics stays off until the player opts in.
            _analytics = new LocalAnalytics();
            _analytics.SetCollectionEnabled(profile.Consent.AnalyticsAllowed == true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ads = new TestRewardedAds();
#else
            _ads = new UnavailableRewardedAds();
#endif
            _ads.Load();
            _haptics = new DeviceHaptics { Enabled = profile.Settings.Haptics };

            LoadContent();
            _session = new GameSession(_store, profile, new GuidIds(), _campaign);
            _session.Analytics += (n, p) => _analytics.LogEvent(n, p);

            BuildWorld();
            BuildUi();
            _sound = gameObject.AddComponent<SoundBank>();
            _sound.Enabled = profile.Settings.Sound;
            ApplySettings();
            ShowHome();
            if (_pendingNotice != null) Toast(Loc.T(_pendingNotice), 4f);
        }

        private void LoadContent()
        {
            var entries = new List<CampaignEntry>();
            foreach (var ta in Resources.LoadAll<TextAsset>("Levels").OrderBy(t => t.name, StringComparer.Ordinal))
            {
                var def = LevelJson.Parse(ta.text);
                var report = LevelValidator.Validate(def);
                if (!report.IsValid)
                {
                    Debug.LogError("Level " + ta.name + " invalid: " + string.Join("; ", report.Errors));
                    continue;
                }
                var level = CompiledLevel.Compile(def);
                _levels[def.Id] = level;
                entries.Add(new CampaignEntry { LevelId = def.Id, ObjectId = def.ObjectId, Name = def.Name, Family = def.Family });
            }
            _campaign = new Campaign(entries);
        }

        private void BuildWorld()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            _camera = camGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Palette.Background;
            _camera.fieldOfView = 40f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 60f;
            camGo.AddComponent<AudioListener>();
            _rig = camGo.AddComponent<CameraRig>();
            _rig.Camera = _camera;

            var lightGo = new GameObject("Key Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.60f, 0.58f);

            var boardGo = new GameObject("Board");
            _board = boardGo.AddComponent<BoardView>();
            var trayAnchor = new GameObject("TrayAnchor").transform;
            trayAnchor.SetParent(camGo.transform, false);
            trayAnchor.localPosition = new Vector3(0f, -2.35f, 8f);
            trayAnchor.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            trayAnchor.localScale = Vector3.one * 0.75f;
            _board.TrayAnchor = trayAnchor;

            if (LitTemplate == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                LitTemplate = new Material(shader);
            }
        }

        private void BuildUi()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                DontDestroyOnLoad(es);
            }
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _safe = UiKit.Empty(canvasGo.transform, "SafeArea");
            _safe.gameObject.AddComponent<SafeArea>();

            var toastRt = UiKit.Panel(_safe, "Toast", new Color(0.12f, 0.12f, 0.12f, 0.85f));
            UiKit.Place(toastRt, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.87f));
            _toast = UiKit.Label(toastRt, "", 36f);
            _toast.color = Color.white;
            toastRt.gameObject.SetActive(false);
            toastRt.GetComponent<Image>().raycastTarget = false;
        }

        // ---------------------------------------------------------------- screens

        private RectTransform NewScreen(bool opaque)
        {
            if (_screen != null) Destroy(_screen.gameObject);
            if (_hud != null) { Destroy(_hud.gameObject); _hud = null; }
            _screen = UiKit.Panel(_safe, "Screen", opaque ? Palette.Background : new Color(0, 0, 0, 0));
            _screen.SetAsFirstSibling();
            return _screen;
        }

        private void ShowHome()
        {
            _board.Clear();
            var s = NewScreen(true);
            UiKit.Column(s, 28f, 90);
            UiKit.Heading(s, Loc.T("title"), 88f, 260f);
            var cur = _campaign.Current(_session.Profile);
            UiKit.Button(s, cur != null ? Loc.T("play") + "  -  " + Loc.F("level", _campaign.IndexOf(cur.LevelId) + 1) : Loc.T("levels"),
                () => { if (cur != null) StartLevel(cur.LevelId, false, ""); else ShowLevels(); }, new Color(0.98f, 0.78f, 0.35f), 48f);
            UiKit.Button(s, Loc.T("levels"), ShowLevels);
            var today = DailyChallenge.DateKey(_clock.UtcNow);
            var dailyBtn = UiKit.Button(s, DailyChallenge.IsCompleted(_session.Profile, today) ? Loc.T("daily_done") : Loc.T("daily"), () => StartDaily(today));
            dailyBtn.interactable = !DailyChallenge.IsCompleted(_session.Profile, today) && _session.Profile.Progress.CompletedLevels.Count >= 3;
            UiKit.Button(s, Loc.T("collection"), ShowCollection);
            UiKit.Button(s, Loc.T("settings"), ShowSettings);
        }

        private void ShowLevels()
        {
            _board.Clear();
            var s = NewScreen(true);
            UiKit.Column(s, 16f, 60);
            UiKit.Heading(s, Loc.T("levels"), 72f, 180f);
            for (int i = 0; i < _campaign.Entries.Count; i++)
            {
                var e = _campaign.Entries[i];
                bool unlocked = _campaign.IsUnlocked(_session.Profile, e.LevelId);
                bool done = _campaign.IsCompleted(_session.Profile, e.LevelId);
                var text = Loc.F("level", i + 1) + "  " + e.Name + (done ? "  [x]" : unlocked ? "" : "  - " + Loc.T("locked"));
                var id = e.LevelId;
                var b = UiKit.Button(s, text, () => StartLevel(id, false, ""), done ? new Color(0.80f, 0.92f, 0.80f) : (Color?)null, 34f);
                b.GetComponent<LayoutElement>().preferredHeight = 100f;
                b.GetComponent<LayoutElement>().minHeight = 100f;
                b.interactable = unlocked;
            }
            UiKit.Button(s, Loc.T("back"), ShowHome);
        }

        private void ShowCollection()
        {
            _board.Clear();
            var s = NewScreen(true);
            UiKit.Column(s, 16f, 60);
            var p = _session.Profile;
            UiKit.Heading(s, Loc.T("collection"), 72f, 160f);
            UiKit.Heading(s, Loc.F("collected", p.Progress.CollectedObjects.Count, _campaign.Entries.Count), 40f, 80f);
            foreach (var family in _campaign.Entries.GroupBy(e => e.Family))
            {
                UiKit.Heading(s, family.Key, 42f, 70f);
                foreach (var e in family)
                {
                    bool owned = p.Progress.CollectedObjects.Contains(e.ObjectId);
                    UiKit.Heading(s, owned ? e.Name : "?  ?  ?", 34f, 56f).color = owned ? Palette.Ink : new Color(0.5f, 0.5f, 0.5f);
                }
            }
            UiKit.Button(s, Loc.T("back"), ShowHome);
        }

        private void ShowSettings()
        {
            _board.Clear();
            var s = NewScreen(true);
            UiKit.Column(s, 20f, 80);
            UiKit.Heading(s, Loc.T("settings"), 72f, 180f);
            var st = _session.Profile.Settings;
            Toggle(s, "sound", () => st.Sound, v => st.Sound = v);
            Toggle(s, "haptics", () => st.Haptics, v => st.Haptics = v);
            Toggle(s, "reduced_motion", () => st.ReducedMotion, v => st.ReducedMotion = v);
            Toggle(s, "symbols", () => st.ColorSymbols, v => st.ColorSymbols = v);
            UiKit.Button(s, Loc.T("language") + ": " + (Loc.Language == "fr" ? "Français" : "English"), () =>
            {
                var next = Loc.Language == "fr" ? "en" : "fr";
                _session.SaveProfile(p => p.Settings.Language = next);
                Loc.Language = next;
                ShowSettings();
            });
            var consent = _session.Profile.Consent.AnalyticsAllowed == true;
            UiKit.Button(s, Loc.T("privacy") + ": " + (consent ? Loc.T("on") : Loc.T("off")), () =>
            {
                _session.SaveProfile(p => p.Consent.AnalyticsAllowed = !consent);
                _analytics.SetCollectionEnabled(!consent);
                ShowSettings();
            });
            UiKit.Button(s, Loc.T("back"), ShowHome);
        }

        private void Toggle(RectTransform parent, string key, Func<bool> get, Action<bool> set)
        {
            UiKit.Button(parent, Loc.T(key) + ": " + (get() ? Loc.T("on") : Loc.T("off")), () =>
            {
                bool v = !get();
                if (!_session.SaveProfile(_ => set(v))) Toast(Loc.T("save_failed"));
                ApplySettings();
                ShowSettings();
            });
        }

        private void ApplySettings()
        {
            var st = _session.Profile.Settings;
            _board.ReducedMotion = st.ReducedMotion;
            _haptics.Enabled = st.Haptics;
            if (_sound != null) _sound.Enabled = st.Sound;
            _board.ShowSymbols = st.ColorSymbols;
        }

        // ---------------------------------------------------------------- gameplay

        private void StartDaily(string dateKey)
        {
            var pool = _campaign.Entries.Skip(2).Select(e => e.LevelId).ToList();
            string id = null;
            _session.SaveProfile(p => id = DailyChallenge.Resolve(p, dateKey, pool, 1));
            if (id != null) StartLevel(id, true, dateKey);
        }

        public void StartLevel(string levelId, bool daily, string dateKey)
        {
            var level = _levels[levelId];
            var kind = _session.Start(level, daily, dateKey);
            _board.Build(level, LitTemplate);
            _board.ShowSymbols = _session.Profile.Settings.ColorSymbols;
            _rig.Configure(level.Definition.Camera);
            _board.Rebuild(_session.Engine.State);
            BuildHud(level, daily);
            _analytics.LogEvent("level_view", new Dictionary<string, object> { ["level_id"] = levelId, ["resumed"] = kind == StartKind.Resumed });
            if (kind == StartKind.ReplacedIncompatible) Toast(Loc.T("replaced_notice"), 3f);
            ShowTutorialFor(level);
            RefreshHud();
            if (_session.Engine.Outcome == Outcome.Lost) ShowStuck();
        }

        private void BuildHud(CompiledLevel level, bool daily)
        {
            var s = NewScreen(false);
            s.GetComponent<Image>().raycastTarget = false;
            _hud = UiKit.Empty(_safe, "Hud");
            var top = UiKit.Empty(_hud, "Top");
            UiKit.Place(top, new Vector2(0f, 0.9f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-24f, -12f));
            UiKit.Row(top, 16f);
            var menu = UiKit.Button(top, Loc.T("menu"), () => { CancelHint(); ShowHome(); }, null, 34f);
            menu.GetComponent<LayoutElement>().preferredWidth = 220f;
            menu.GetComponent<LayoutElement>().flexibleWidth = 0f;
            var titleHolder = UiKit.Empty(top, "Title");
            titleHolder.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _levelLabel = UiKit.Label(titleHolder, daily ? Loc.T("daily") : Loc.F("level", _campaign.IndexOf(level.Definition.Id) + 1) + "  " + level.Definition.Name, 40f);

            var tut = UiKit.Empty(_hud, "Tutorial");
            UiKit.Place(tut, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.9f));
            _tutorialLabel = UiKit.Label(tut, "", 34f);

            var bottom = UiKit.Empty(_hud, "Bottom");
            UiKit.Place(bottom, new Vector2(0f, 0f), new Vector2(1f, 0.075f), new Vector2(24f, 16f), new Vector2(-24f, 0f));
            UiKit.Row(bottom, 18f);
            _undoBtn = UiKit.Button(bottom, Loc.T("undo"), OnUndo, null, 36f);
            _hintBtn = UiKit.Button(bottom, Loc.T("hint"), OnHint, null, 36f);
            _restartBtn = UiKit.Button(bottom, Loc.T("restart"), OnRestart, null, 36f);
            _hud.SetSiblingIndex(1);
        }

        private void ShowTutorialFor(CompiledLevel level)
        {
            int idx = _campaign.IndexOf(level.Definition.Id);
            var t = _session.Profile.Tutorial;
            if (_tutorialLabel == null) return;
            if (idx >= 0 && idx < 4 && !t.Completed)
            {
                _tutorialLabel.text = Loc.T("tut_" + (idx + 1));
                if (t.Step < idx + 1) _session.SaveProfile(p => { p.Tutorial.Step = idx + 1; p.Tutorial.Completed = idx == 3; });
            }
            else _tutorialLabel.text = "";
        }

        private void RefreshHud()
        {
            if (_hud == null || _session.Engine == null) return;
            var h = _session.Help;
            bool open = !_session.Attempt.Closed;
            int undo = h.Available(HelpKind.Undo), hint = h.Available(HelpKind.Hint);
            UiKit.SetText(_undoBtn, Loc.T("undo") + " (" + undo + ")");
            UiKit.SetText(_hintBtn, _hintTask != null ? Loc.T("thinking") : Loc.T("hint") + " (" + hint + ")");
            _undoBtn.interactable = open && _session.Engine.UndoDepth > 0;
            _hintBtn.interactable = open && _hintTask == null;
            _restartBtn.interactable = true;
        }

        private void OnUndo()
        {
            CancelHint();
            if (_session.Help.Available(HelpKind.Undo) == 0) { OfferReward(); return; }
            var r = _session.Undo(_session.Engine.Revision);
            HandleResult(r);
        }

        private void OnHint()
        {
            if (_hintTask != null) return;
            if (!_session.CanRequestHint) { OfferReward(); return; }
            var level = _session.Level;
            var st = _session.Engine.State.Clone();
            var attempt = _session.Attempt.AttemptId;
            var rev = _session.Engine.Revision;
            _hintTask = Task.Run(() => HintService.Compute(level, st, attempt, rev));
            RefreshHud();
        }

        private void CancelHint()
        {
            _hintTask = null;
            _highlightScrew = null;
            _board.Highlight(null);
        }

        private void PollHint()
        {
            if (_hintTask == null || !_hintTask.IsCompleted) return;
            var t = _hintTask;
            _hintTask = null;
            if (t.IsFaulted) { Toast(Loc.T("hint_unknown")); RefreshHud(); return; }
            var h = t.Result;
            if (h.Status == HintStatus.Suggested)
            {
                if (_session.ApplyHint(h))
                {
                    _highlightScrew = h.ScrewId;
                    _board.Highlight(h.ScrewId);
                    _sound.Play(SoundBank.Cue.Hint);
                }
            }
            else if (h.Status == HintStatus.Unsolvable) Toast(Loc.T("hint_none"));
            else Toast(Loc.T("hint_unknown"));
            RefreshHud();
        }

        private void OnRestart()
        {
            CancelHint();
            CloseOverlay();
            var r = _session.Restart();
            if (!r.Accepted) { Toast(Loc.T("save_failed")); return; }
            _board.Rebuild(_session.Engine.State);
            RefreshHud();
        }

        private void OfferReward()
        {
            if (_ads.Availability != AdAvailability.Ready) { Toast(Loc.T("no_help") + " " + Loc.T("ads_unavailable"), 3f); return; }
            var op = "reward-" + Guid.NewGuid().ToString("N");
            var attempt = _session.Attempt.AttemptId;
            _ads.Show(op, (id, result) =>
            {
                if (result != AdShowResult.Earned) { Toast(Loc.T("ads_unavailable")); return; }
                if (_session.GrantReward(id)) Toast(Loc.T("ad_granted") + (_ads.IsTestAdapter ? " (test)" : ""));
                RefreshHud();
            });
        }

        private void TryRemove(string screwId)
        {
            if (_session.Attempt.Closed) return;
            CancelHint();
            var r = _session.Remove(screwId, _session.Engine.Revision);
            HandleResult(r);
        }

        private void HandleResult(CommandResult r)
        {
            if (!r.Accepted)
            {
                switch (r.Reason)
                {
                    case RejectReason.Blocked: Toast(Loc.T("blocked")); _haptics.Play(HapticKind.Error); _sound.Play(SoundBank.Cue.Error); break;
                    case RejectReason.NoDestination: Toast(Loc.T("full")); _haptics.Play(HapticKind.Error); _sound.Play(SoundBank.Cue.Error); break;
                    case RejectReason.SaveFailed: Toast(Loc.T("save_failed"), 3f); break;
                }
                RefreshHud();
                return;
            }
            bool replaced = r.Events.Any(e => e is StateReplacedEvent);
            if (replaced) _board.Rebuild(_session.Engine.State);
            else _board.Play(r.Events, _session.Engine.State);
            _sound.Play(r.Events.Any(e => e is TrayCompletedEvent) ? SoundBank.Cue.Complete : SoundBank.Cue.Unscrew);
            _haptics.Play(HapticKind.Selection);
            RefreshHud();
            if (_session.Engine.Outcome == Outcome.Won) Invoke(nameof(ShowWin), _board.ReducedMotion ? 0.2f : 0.9f);
            else if (_session.Engine.Outcome == Outcome.Lost) Invoke(nameof(ShowStuck), 0.5f);
            else CloseOverlay();
        }

        private RectTransform _overlay;

        private void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }

        private RectTransform Overlay()
        {
            CloseOverlay();
            _overlay = UiKit.Panel(_safe, "Overlay", new Color(0f, 0f, 0f, 0.45f));
            var card = UiKit.Panel(_overlay, "Card", new Color(1f, 0.98f, 0.94f));
            UiKit.Place(card, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.7f));
            UiKit.Column(card, 22f, 50);
            return card;
        }

        private void ShowWin()
        {
            if (_session.Engine?.Outcome != Outcome.Won) return;
            _sound.Play(SoundBank.Cue.Win);
            _haptics.Play(HapticKind.Success);
            var def = _session.Level.Definition;
            var card = Overlay();
            UiKit.Heading(card, Loc.T("won"), 64f, 120f);
            UiKit.Heading(card, Loc.F("won_body", def.Name), 38f, 110f);
            var next = _session.Attempt.IsDaily ? null : _campaign.Next(def.Id);
            if (next != null) UiKit.Button(card, Loc.T("next"), () => { CloseOverlay(); StartLevel(next.LevelId, false, ""); }, new Color(0.98f, 0.78f, 0.35f));
            UiKit.Button(card, Loc.T("menu"), () => { CloseOverlay(); ShowHome(); });
        }

        private void ShowStuck()
        {
            if (_session.Engine?.Outcome != Outcome.Lost) return;
            var card = Overlay();
            UiKit.Heading(card, Loc.T("stuck"), 60f, 110f);
            UiKit.Heading(card, Loc.T("stuck_body"), 36f, 120f);
            if (_session.Engine.UndoDepth > 0)
                UiKit.Button(card, Loc.T("undo") + " (" + _session.Help.Available(HelpKind.Undo) + ")", () => { CloseOverlay(); OnUndo(); });
            UiKit.Button(card, Loc.T("restart"), OnRestart, new Color(0.98f, 0.78f, 0.35f));
            if (_ads.Availability == AdAvailability.Ready) UiKit.Button(card, Loc.T("watch_ad"), OfferReward);
        }

        private void Toast(string text, float seconds = 2f)
        {
            _toast.text = text;
            _toast.transform.parent.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + seconds;
        }

        // ---------------------------------------------------------------- input

        private void Update()
        {
            if (_toast != null && _toast.transform.parent.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toast.transform.parent.gameObject.SetActive(false);
            PollHint();
            if (_hud == null || _session.Engine == null || _overlay != null) { _pointerDown = false; return; }

            var pointer = Pointer.current;
            if (pointer == null) return;
            var press = pointer.press;
            var pos = pointer.position.ReadValue();
            if (press.wasPressedThisFrame)
            {
                _pointerDown = true;
                _dragging = false;
                _pressPos = _lastPos = pos;
                _pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointer.deviceId) || OverUi(pos);
            }
            if (!_pointerDown) return;
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            if (press.isPressed)
            {
                if (!_dragging && !_pointerOverUi && (pos - _pressPos).magnitude > DragThresholdInches * dpi) _dragging = true;
                if (_dragging) _rig.Rotate((pos - _lastPos) / dpi * DegreesPerInch);
                _lastPos = pos;
            }
            if (press.wasReleasedThisFrame)
            {
                _pointerDown = false;
                if (!_dragging && !_pointerOverUi) Pick(pos);
            }
        }

        private static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

        private static bool OverUi(Vector2 pos)
        {
            if (EventSystem.current == null) return false;
            UiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pos }, UiHits);
            return UiHits.Count > 0;
        }

        /// <summary>Nearest-hit picking: a screw behind a visible part is never selected.</summary>
        public void Pick(Vector2 screenPos)
        {
            var ray = _camera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 100f)) return;
            var sv = hit.collider.GetComponent<ScrewView>();
            if (sv != null) { TryRemove(sv.ScrewId); return; }
            // A part was hit first; tell the player if a screw is hidden right behind it.
            var hits = Physics.RaycastAll(ray, 100f).OrderBy(h => h.distance);
            foreach (var h in hits)
            {
                if (h.collider.GetComponent<ScrewView>() != null) { Toast(Loc.T("hidden")); return; }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // Every accepted command is already durable. On resume, snap to the settled state.
            if (!paused && _session?.Engine != null && _hud != null) _board.Rebuild(_session.Engine.State);
            if (paused) CancelHint();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus && _session?.Engine != null && _hud != null && !_board.IsAnimating) _board.Rebuild(_session.Engine.State);
        }
    }
}
