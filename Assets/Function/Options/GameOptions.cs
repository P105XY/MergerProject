using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Mech.Function
{
    [Serializable]
    public sealed class GameOptionValues
    {
        public int Width;
        public int Height;
        public FullScreenMode DisplayMode;
        public string QualityPreset;
        public bool VSync;
        public int FrameRateLimit;
        public float MasterVolume;
        public float MusicVolume;
        public float EffectsVolume;

        // UI에서 수정할 수 있도록 현재 값의 복사본을 만든다.
        internal GameOptionValues Copy()
        {
            return (GameOptionValues)MemberwiseClone();
        }
    }

    public static class GameOptions
    {
        private const string OptionsKey = "Mech.GameOptions.v1";
        private const string MusicParameter = "MusicVolume";
        private const string EffectsParameter = "EffectsVolume";

        private static GameOptionValues defaultValues;
        private static GameOptionValues currentValues;
        private static AudioMixer audioMixer;

        // 현재 적용 요청된 옵션을 UI가 편집할 수 있는 복사본으로 반환한다.
        public static GameOptionValues GetCurrent()
        {
            CaptureDefaults();
            return currentValues.Copy();
        }

        // 옵션 값을 검증한 뒤 그래픽과 사운드 설정을 적용한다.
        public static void Apply(GameOptionValues values)
        {
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            CaptureDefaults();
            GameOptionValues next = values.Copy();
            next.Width = next.Width > 0 ? next.Width : defaultValues.Width;
            next.Height = next.Height > 0 ? next.Height : defaultValues.Height;
            next.DisplayMode = Enum.IsDefined(typeof(FullScreenMode), next.DisplayMode) ? next.DisplayMode : defaultValues.DisplayMode;
            next.FrameRateLimit = next.FrameRateLimit > 0 ? next.FrameRateLimit : -1;
            next.MasterVolume = ValidateVolume(next.MasterVolume, defaultValues.MasterVolume);
            next.MusicVolume = ValidateVolume(next.MusicVolume, defaultValues.MusicVolume);
            next.EffectsVolume = ValidateVolume(next.EffectsVolume, defaultValues.EffectsVolume);

            string[] qualityPresets = QualitySettings.names;
            int qualityIndex = Array.IndexOf(qualityPresets, next.QualityPreset);

            if (qualityIndex < 0) {
                qualityIndex = Array.IndexOf(qualityPresets, defaultValues.QualityPreset);
            }

            if (qualityIndex >= 0) {
                next.QualityPreset = qualityPresets[qualityIndex];

                if (qualityIndex != QualitySettings.GetQualityLevel()) {
                    QualitySettings.SetQualityLevel(qualityIndex);
                }
            }

            QualitySettings.vSyncCount = next.VSync ? 1 : 0;
            Application.targetFrameRate = next.FrameRateLimit;

            if (Screen.width != next.Width || Screen.height != next.Height || Screen.fullScreenMode != next.DisplayMode) {
                Screen.SetResolution(next.Width, next.Height, next.DisplayMode);
            }

            AudioListener.volume = next.MasterVolume;
            currentValues = next;
            ApplyMixerVolumes();
        }

        // 현재 적용한 옵션을 게임 진행과 별도의 사용자 설정에 저장한다.
        public static void Save()
        {
            CaptureDefaults();
            PlayerPrefs.SetString(OptionsKey, JsonUtility.ToJson(currentValues));
            PlayerPrefs.Save();
        }

        // 저장 옵션을 읽어 적용하고 빠진 값이나 깨진 데이터는 기본값으로 복구한다.
        public static void Load()
        {
            CaptureDefaults();
            GameOptionValues loaded = defaultValues.Copy();
            string json = PlayerPrefs.GetString(OptionsKey, string.Empty);

            if (!string.IsNullOrEmpty(json)) {
                try {
                    JsonUtility.FromJsonOverwrite(json, loaded);
                } catch (ArgumentException) {
                    loaded = defaultValues.Copy();
                }
            }

            Apply(loaded);
        }

        // 이번 실행의 기본 옵션을 복원하되 저장은 호출자가 결정하게 한다.
        public static void ResetToDefaults()
        {
            CaptureDefaults();
            Apply(defaultValues);
        }

        // Start 이후 BGM과 효과음 볼륨 파라미터가 노출된 오디오 믹서를 연결한다.
        public static bool TryBindAudioMixer(AudioMixer mixer)
        {
            if (mixer == null || !mixer.GetFloat(MusicParameter, out _) || !mixer.GetFloat(EffectsParameter, out _)) {
                return false;
            }

            CaptureDefaults();
            audioMixer = mixer;
            return ApplyMixerVolumes();
        }

        // 도메인 재로드 설정과 무관하게 새 실행에서 옵션과 믹서 참조를 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            defaultValues = null;
            currentValues = null;
            audioMixer = null;
        }

        // 첫 씬이 준비되면 시작 기본값을 기억하고 저장한 옵션을 복원한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadAtStartup()
        {
            CaptureDefaults();

            if (PlayerPrefs.HasKey(OptionsKey)) {
                Load();
            }
        }

        // 현재 플랫폼의 시작 화면·품질 설정을 기본값으로 한 번 기록한다.
        private static void CaptureDefaults()
        {
            if (defaultValues != null) {
                return;
            }

            defaultValues = new GameOptionValues {
                Width = Mathf.Max(1, Screen.width),
                Height = Mathf.Max(1, Screen.height),
                DisplayMode = Screen.fullScreenMode,
                QualityPreset = QualitySettings.names[QualitySettings.GetQualityLevel()],
                VSync = QualitySettings.vSyncCount > 0,
                FrameRateLimit = Application.targetFrameRate,
                MasterVolume = AudioListener.volume,
                MusicVolume = 1f,
                EffectsVolume = 1f
            };
            currentValues = defaultValues.Copy();
        }

        // 연결된 믹서의 BGM과 효과음 채널에 각 볼륨을 반영한다.
        private static bool ApplyMixerVolumes()
        {
            if (audioMixer == null) {
                return false;
            }

            bool musicApplied = audioMixer.SetFloat(MusicParameter, ToDecibels(currentValues.MusicVolume));
            bool effectsApplied = audioMixer.SetFloat(EffectsParameter, ToDecibels(currentValues.EffectsVolume));
            return musicApplied && effectsApplied;
        }

        // 선형 볼륨을 믹서의 데시벨 값으로 바꾸고 0은 감쇠 하한으로 변환한다.
        private static float ToDecibels(float volume)
        {
            return volume <= 0f ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(volume));
        }

        // 잘못된 숫자는 기본값으로 바꾸고 유효한 볼륨은 0부터 1 사이로 제한한다.
        private static float ValidateVolume(float volume, float fallback)
        {
            return float.IsNaN(volume) || float.IsInfinity(volume) ? fallback : Mathf.Clamp01(volume);
        }
    }
}
