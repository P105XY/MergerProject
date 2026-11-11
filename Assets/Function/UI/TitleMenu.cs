using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Mech.Function
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class TitleMenu : MonoBehaviour
    {
        [SerializeField] private string firstStage = "Assets/Scenes/TestScene/PlayTestScene.unity";

        private static readonly string[] KoreanFontNames = {
            "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic"
        };

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private readonly List<FullScreenMode> modes = new List<FullScreenMode>();
        private readonly List<int> frameLimits = new List<int>();
        private VisualElement mainMenu;
        private VisualElement optionsMenu;
        private Button newGame;
        private Button continueGame;
        private Button openOptions;
        private Button backOptions;
        private Button applyOptions;
        private Label saveStatus;
        private Label optionsStatus;
        private DropdownField resolution;
        private DropdownField displayMode;
        private DropdownField quality;
        private DropdownField frameLimit;
        private Toggle vSync;
        private Slider masterVolume;
        private Slider musicVolume;
        private Slider effectsVolume;
        private Font systemFont;
        private FontAsset fontAsset;
        private bool loading;

        // 타이틀과 옵션 화면의 컨트롤을 찾고 버튼에 동작을 연결한다.
        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.style.flexGrow = 1;
            ConfigureKoreanFont(root);
            mainMenu = root.Q("main-menu");
            optionsMenu = root.Q("options-menu");
            newGame = root.Q<Button>("new-game");
            continueGame = root.Q<Button>("continue-game");
            openOptions = root.Q<Button>("open-options");
            backOptions = root.Q<Button>("back-options");
            applyOptions = root.Q<Button>("apply-options");
            saveStatus = root.Q<Label>("save-status");
            optionsStatus = root.Q<Label>("options-status");
            resolution = root.Q<DropdownField>("resolution");
            displayMode = root.Q<DropdownField>("display-mode");
            quality = root.Q<DropdownField>("quality");
            frameLimit = root.Q<DropdownField>("frame-limit");
            vSync = root.Q<Toggle>("v-sync");
            masterVolume = root.Q<Slider>("master-volume");
            musicVolume = root.Q<Slider>("music-volume");
            effectsVolume = root.Q<Slider>("effects-volume");
            newGame.clicked += StartNewGame;
            continueGame.clicked += ContinueGame;
            openOptions.clicked += ShowOptions;
            backOptions.clicked += ShowTitle;
            applyOptions.clicked += SaveOptions;
            SetLoading(false);
            newGame.schedule.Execute(newGame.Focus);
        }

        // 타이틀 문서가 비활성화되면 버튼 구독을 해제한다.
        private void OnDisable()
        {
            if (newGame == null) {
                return;
            }

            newGame.clicked -= StartNewGame;
            continueGame.clicked -= ContinueGame;
            openOptions.clicked -= ShowOptions;
            backOptions.clicked -= ShowTitle;
            applyOptions.clicked -= SaveOptions;
        }

        // 타이틀에서 임시로 만든 글꼴과 글꼴 에셋을 정리한다.
        private void OnDestroy()
        {
            Destroy(fontAsset);
            Destroy(systemFont);
        }

        // 사용 가능한 운영체제 한글 글꼴을 UI와 펼침 메뉴에 적용한다.
        private void ConfigureKoreanFont(VisualElement root)
        {
            if (systemFont == null) {
                systemFont = Font.CreateDynamicFontFromOSFont(KoreanFontNames, 24);

                if (systemFont != null && systemFont.HasCharacter('새')) {
                    fontAsset = FontAsset.CreateFontAsset(systemFont);
                }
            }

            if (fontAsset != null) {
                StyleFontDefinition font = new StyleFontDefinition(FontDefinition.FromSDFFont(fontAsset));
                root.style.unityFontDefinition = font;

                if (root.panel != null) {
                    root.panel.visualTree.style.unityFontDefinition = font;
                }
            }
        }

        // 새 게임을 시작하고 장면이 전환되는 동안 중복 요청을 막는다.
        private void StartNewGame()
        {
            if (!loading) {
                bool started = CheckpointProgress.TryStartNewGame(firstStage);
                SetLoading(started);

                if (!started) {
                    saveStatus.text = "첫 스테이지를 열 수 없습니다.";
                }
            }
        }

        // 유효한 저장을 불러오고 요청이 거부되면 버튼 상태를 갱신한다.
        private void ContinueGame()
        {
            if (!loading && continueGame.enabledInHierarchy) {
                bool started = CheckpointProgress.TryContinue();
                SetLoading(started);

                if (!started) {
                    saveStatus.text = "저장된 진행을 불러올 수 없습니다.";
                }
            }
        }

        // 로드 중에는 타이틀 버튼을 잠그고 대기 중에는 저장 유무를 반영한다.
        private void SetLoading(bool value)
        {
            loading = value;
            bool canContinue = !loading && CheckpointProgress.HasSave();
            newGame.SetEnabled(!loading);
            continueGame.SetEnabled(canContinue);
            openOptions.SetEnabled(!loading);
            saveStatus.text = loading ? "불러오는 중…" : canContinue ? "마지막 체크포인트에서 이어집니다." : "저장된 진행이 없습니다.";
        }

        // 현재 옵션을 편집 화면에 불러오고 타이틀 버튼을 숨긴다.
        private void ShowOptions()
        {
            GameOptionValues current = GameOptions.GetCurrent();
            PopulateGraphicsChoices(current);
            vSync.value = current.VSync;
            masterVolume.value = current.MasterVolume;
            musicVolume.value = current.MusicVolume;
            effectsVolume.value = current.EffectsVolume;
            optionsStatus.text = "";
            mainMenu.AddToClassList("is-hidden");
            optionsMenu.RemoveFromClassList("is-hidden");
            resolution.schedule.Execute(resolution.Focus);
        }

        // 현재 장치의 해상도와 게임에서 지원하는 화면 옵션을 채운다.
        private void PopulateGraphicsChoices(GameOptionValues current)
        {
            resolutions.Clear();

            foreach (Resolution mode in Screen.resolutions) {
                var size = new Vector2Int(mode.width, mode.height);

                if (!resolutions.Contains(size)) {
                    resolutions.Add(size);
                }
            }

            var currentSize = new Vector2Int(current.Width, current.Height);

            if (!resolutions.Contains(currentSize)) {
                resolutions.Add(currentSize);
            }

            resolution.choices = resolutions.ConvertAll(size => $"{size.x} × {size.y}");
            resolution.index = resolutions.IndexOf(currentSize);
            modes.Clear();
            modes.Add(FullScreenMode.Windowed);
            modes.Add(FullScreenMode.FullScreenWindow);

            if (!modes.Contains(current.DisplayMode)) {
                modes.Add(current.DisplayMode);
            }

            displayMode.choices = modes.ConvertAll(FormatDisplayMode);
            displayMode.index = modes.IndexOf(current.DisplayMode);
            quality.choices = new List<string>(QualitySettings.names);
            quality.index = quality.choices.IndexOf(current.QualityPreset);
            frameLimits.Clear();
            frameLimits.AddRange(new[] { -1, 30, 60, 120, 144 });

            if (!frameLimits.Contains(current.FrameRateLimit)) {
                frameLimits.Add(current.FrameRateLimit);
            }

            frameLimit.choices = frameLimits.ConvertAll(limit => limit > 0 ? $"{limit} FPS" : "기본값");
            frameLimit.index = frameLimits.IndexOf(current.FrameRateLimit);
        }

        // 화면 모드마다 중복되지 않는 선택지 이름을 반환한다.
        private static string FormatDisplayMode(FullScreenMode mode)
        {
            return mode switch {
                FullScreenMode.Windowed => "창 모드",
                FullScreenMode.ExclusiveFullScreen => "독점 전체 화면",
                FullScreenMode.MaximizedWindow => "최대화",
                _ => "전체 화면"
            };
        }

        // 화면에서 선택한 옵션을 적용하고 저장한 뒤 타이틀로 돌아간다.
        private void SaveOptions()
        {
            GameOptionValues previous = GameOptions.GetCurrent();
            GameOptionValues edited = previous.Copy();
            Vector2Int size = resolutions[resolution.index];
            edited.Width = size.x;
            edited.Height = size.y;
            edited.DisplayMode = modes[displayMode.index];
            edited.QualityPreset = quality.value;
            edited.FrameRateLimit = frameLimits[frameLimit.index];
            edited.VSync = vSync.value;
            edited.MasterVolume = masterVolume.value;
            edited.MusicVolume = musicVolume.value;
            edited.EffectsVolume = effectsVolume.value;
            GameOptions.Apply(edited);

            try {
                GameOptions.Save();
            } catch (PlayerPrefsException) {
                GameOptions.Apply(previous);
                optionsStatus.text = "옵션을 저장할 수 없습니다.";
                return;
            }

            ShowTitle();
        }

        // 옵션 편집을 닫고 저장 상태가 반영된 타이틀로 돌아간다.
        private void ShowTitle()
        {
            optionsMenu.AddToClassList("is-hidden");
            mainMenu.RemoveFromClassList("is-hidden");
            SetLoading(false);
            openOptions.schedule.Execute(openOptions.Focus);
        }
    }
}
