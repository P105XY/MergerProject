// Unity MCP의 플레이 모드에서 옵션 저장과 실제 엔진 설정을 검증한다.
if (!EditorApplication.isPlaying || EditorApplication.isPaused) {
    throw new System.InvalidOperationException("실행 중인 플레이 모드가 필요합니다.");
}

const string key = "Mech.GameOptions.v1";
bool hadOriginalPreference = PlayerPrefs.HasKey(key);
string originalPreference = PlayerPrefs.GetString(key, string.Empty);
var originalOptions = Mech.Function.GameOptions.GetCurrent();
var results = new System.Collections.Generic.List<object>();

// 기대값과 실제값을 기록하고 값이 다르면 검증을 실패시킨다.
System.Action<string, object, object> check = (input, expected, actual) => {
    string message = string.Format("입력 {0}, 기대 {1}, 실제 {2}", input, expected, actual);
    Debug.Log("[Game Options] " + message);
    results.Add(new { input, expected, actual });
    bool passed = expected is float && actual is float
        ? Mathf.Abs((float)expected - (float)actual) < 0.0001f
        : object.Equals(expected, actual);
    UnityEngine.Assertions.Assert.IsTrue(passed, message);

    if (!passed) {
        throw new System.InvalidOperationException(message);
    }
};

try {
    Mech.Function.GameOptions.ResetToDefaults();
    var defaults = Mech.Function.GameOptions.GetCurrent();
    var edit = Mech.Function.GameOptions.GetCurrent();
    int alternativeQuality = QualitySettings.GetQualityLevel() == 0 ? 1 : 0;
    edit.QualityPreset = QualitySettings.names[alternativeQuality];
    edit.VSync = false;
    edit.FrameRateLimit = 75;
    edit.MasterVolume = 0.4f;
    edit.MusicVolume = 0.25f;
    edit.EffectsVolume = 0.7f;

    // 편집 중인 복사본은 적용 전 엔진과 현재 옵션에 영향을 주지 않는다.
    check("Apply 이전 편집본 볼륨 변경", defaults.MasterVolume, Mech.Function.GameOptions.GetCurrent().MasterVolume);
    Mech.Function.GameOptions.Apply(edit);
    // 적용 후 품질과 프레임 설정, 전체 볼륨이 엔진에 반영되어야 한다.
    check("품질 프리셋 적용", alternativeQuality, QualitySettings.GetQualityLevel());
    check("VSync 끄기", 0, QualitySettings.vSyncCount);
    check("75 FPS 제한", 75, Application.targetFrameRate);
    check("전체 볼륨 0.4 적용", 0.4f, AudioListener.volume);
    Mech.Function.GameOptions.Save();
    Mech.Function.GameOptions.ResetToDefaults();
    check("저장 뒤 기본값 복원", defaults.MasterVolume, AudioListener.volume);
    Mech.Function.GameOptions.Load();
    var restored = Mech.Function.GameOptions.GetCurrent();

    // 저장 뒤 기본값을 적용해도 불러오면 저장했던 품질과 세 볼륨을 복원해야 한다.
    check("저장된 품질 이름 복원", edit.QualityPreset, restored.QualityPreset);
    check("저장된 전체 볼륨 복원", 0.4f, AudioListener.volume);
    check("저장된 BGM 볼륨 복원", 0.25f, restored.MusicVolume);
    check("저장된 효과음 볼륨 복원", 0.7f, restored.EffectsVolume);

    // 저장 데이터에 빠진 항목은 시작 기본값을 사용한다.
    PlayerPrefs.SetString(key, "{\"MasterVolume\":0.3}");
    Mech.Function.GameOptions.Load();
    check("일부 필드만 있는 저장 데이터의 볼륨", 0.3f, AudioListener.volume);
    check("일부 필드만 있는 저장 데이터의 누락된 BGM 볼륨", defaults.MusicVolume, Mech.Function.GameOptions.GetCurrent().MusicVolume);

    // 깨진 데이터는 기본값으로 복구하고 잘못된 옵션 범위는 검증한다.
    PlayerPrefs.SetString(key, "{unclosed");
    Mech.Function.GameOptions.Load();
    check("깨진 JSON의 기본 볼륨 복원", defaults.MasterVolume, AudioListener.volume);
    var invalid = Mech.Function.GameOptions.GetCurrent();
    invalid.Width = -1;
    invalid.DisplayMode = (FullScreenMode)999;
    invalid.QualityPreset = "Unknown Test Preset";
    invalid.MasterVolume = 3f;
    invalid.MusicVolume = -0.2f;
    invalid.EffectsVolume = float.NaN;
    invalid.FrameRateLimit = 0;
    invalid.VSync = true;
    Mech.Function.GameOptions.Apply(invalid);
    var sanitized = Mech.Function.GameOptions.GetCurrent();
    check("잘못된 화면 너비", defaults.Width, sanitized.Width);
    check("알 수 없는 화면 모드", defaults.DisplayMode, sanitized.DisplayMode);
    check("알 수 없는 품질 이름", defaults.QualityPreset, sanitized.QualityPreset);
    check("범위보다 큰 전체 볼륨", 1f, AudioListener.volume);
    check("음수 BGM 볼륨", 0f, sanitized.MusicVolume);
    check("숫자가 아닌 효과음 볼륨", defaults.EffectsVolume, sanitized.EffectsVolume);
    check("0 FPS 제한", -1, Application.targetFrameRate);
    check("VSync 켜기", 1, QualitySettings.vSyncCount);

    // 믹서용 선형 볼륨은 0.5에서 약 -6.02 dB, 0에서 -80 dB가 되어야 한다.
    var decibels = typeof(Mech.Function.GameOptions).GetMethod("ToDecibels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    check("선형 볼륨 0.5의 믹서 감쇠", -6.0206f, (float)decibels.Invoke(null, new object[] { 0.5f }));
    check("선형 볼륨 0의 믹서 감쇠", -80f, (float)decibels.Invoke(null, new object[] { 0f }));
    return new { passed = results.Count, results };
} finally {
    if (hadOriginalPreference) {
        PlayerPrefs.SetString(key, originalPreference);
    } else {
        PlayerPrefs.DeleteKey(key);
    }

    PlayerPrefs.Save();
    Mech.Function.GameOptions.Apply(originalOptions);
}
