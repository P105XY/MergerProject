using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    public static class CheckpointProgress
    {
        private const float AutosaveIntervalSeconds = 180f;
        private static readonly HashSet<string> collectedItemIds = new HashSet<string>(StringComparer.Ordinal);
        private static CheckpointSaveStore saveStore;
        private static CheckpointSaveData pendingSave;
        private static float? travelingHealth;
        private static AsyncOperation newGameLoad;
        private static bool waitingForFirstSave;
        private static PlayerBase gameplayPlayer;
        private static string currentCheckpointId = CheckpointSaveData.SceneEntryId;
        private static float autosaveElapsed;

        public static bool IsLoading => pendingSave != null || newGameLoad != null;
        public static event Action<bool> LoadFinished;

        private static CheckpointSaveStore Store =>
            saveStore ?? (saveStore = new CheckpointSaveStore(Application.persistentDataPath));

        // 이어하기 메뉴에서 사용할 수 있는 저장 슬롯이 있는지 확인한다.
        public static bool HasSave()
        {
            return ReadLoadableSave() != null;
        }

        // 현재 진행에서 고유 ID의 일회성 아이템을 획득했는지 확인한다.
        public static bool HasCollectedItem(string itemId)
        {
            return collectedItemIds.Contains(itemId);
        }

        // 로드 중이 아닐 때 유효한 아이템 ID의 최초 획득을 기록한다.
        public static bool TryRecordItemPickup(string itemId)
        {
            return Application.isPlaying && !IsLoading && !string.IsNullOrWhiteSpace(itemId)
                && collectedItemIds.Add(itemId);
        }

        // 저장 슬롯을 보존한 채 첫 스테이지를 초기 상태로 연다.
        public static bool TryStartNewGame(string scenePath)
        {
            if (!Application.isPlaying || IsLoading || string.IsNullOrWhiteSpace(scenePath)) {
                return false;
            }

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            if (buildIndex < 0) {
                return false;
            }

            try {
                newGameLoad = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
                collectedItemIds.Clear();
                waitingForFirstSave = true;
                autosaveElapsed = 0f;
                newGameLoad.completed += _ => newGameLoad = null;
                return true;
            } catch (Exception error) when (error is ArgumentException || error is InvalidOperationException) {
                Debug.LogWarning($"첫 스테이지를 열 수 없습니다. {error.Message}");
                return false;
            }
        }

        // 마지막 체크포인트의 씬을 비동기로 다시 열고 복원을 예약한다.
        public static bool TryContinue()
        {
            if (!Application.isPlaying || IsLoading) {
                return false;
            }

            CheckpointSaveData save = ReadLoadableSave();

            if (save == null) {
                return false;
            }

            return TryLoadProgress(save, null);
        }

        // 지역 이동 중에는 현재 진행과 체력을 가져가고 도착 위치에서 자동 저장한다.
        public static bool TryTravelToRegion(PlayerBase player, string scenePath, string arrivalCheckpointId = null)
        {
            if (!Application.isPlaying || !CanSavePlayer(player) || string.IsNullOrWhiteSpace(scenePath) || scenePath == player.gameObject.scene.path
                || SceneUtility.GetBuildIndexByScenePath(scenePath) < 0) {
                return false;
            }

            string checkpointId = string.IsNullOrWhiteSpace(arrivalCheckpointId)
                ? CheckpointSaveData.SceneEntryId : arrivalCheckpointId;
            return TryLoadProgress(CaptureProgress(player, scenePath, checkpointId), player.CurrentHealth);
        }

        // 저장 복원과 지역 이동에 공통으로 사용할 비동기 씬 로드를 시작한다.
        private static bool TryLoadProgress(CheckpointSaveData save, float? healthOnArrival)
        {
            pendingSave = save;
            travelingHealth = healthOnArrival;
            SceneManager.sceneLoaded += RestoreLoadedScene;

            try {
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(save.ScenePath);
                AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
                operation.completed += _ => FinishUnmatchedLoad(save);
                return true;
            } catch (Exception error) when (error is ArgumentException || error is InvalidOperationException) {
                Debug.LogWarning($"체크포인트 씬을 열 수 없습니다. {error.Message}");
                FinishLoad(false);
                return false;
            }
        }

        // 저장 중복과 씬 구성을 확인하고 현재 진행을 단일 슬롯에 쓴다.
        public static void SaveAt(Checkpoint checkpoint, PlayerBase player)
        {
            Scene scene = checkpoint.gameObject.scene;

            if (!CanSavePlayer(player) || player.gameObject.scene != scene) {
                return;
            }

            if (SceneUtility.GetBuildIndexByScenePath(scene.path) < 0
                || string.IsNullOrWhiteSpace(checkpoint.CheckpointId)
                || checkpoint.CheckpointId == CheckpointSaveData.SceneEntryId
                || FindCheckpoint(scene, checkpoint.CheckpointId) != checkpoint) {
                Debug.LogWarning("자동 저장에는 빌드에 등록된 씬과 씬 내 고유한 체크포인트 ID가 필요합니다.", checkpoint);
                return;
            }

            currentCheckpointId = checkpoint.CheckpointId;
            WriteProgress(CaptureProgress(player, scene.path, currentCheckpointId));
        }

        // 현재 지역의 마지막 체크포인트 또는 시작 위치를 기준으로 자동 저장한다.
        public static void SaveCurrentProgress(PlayerBase player)
        {
            if (CanSavePlayer(player) && SceneUtility.GetBuildIndexByScenePath(player.gameObject.scene.path) >= 0) {
                WriteProgress(CaptureProgress(player, player.gameObject.scene.path, currentCheckpointId));
            }
        }

        // 실제 플레이가 누적 3분에 도달할 때마다 현재 진행을 저장한다.
        public static void TickAutosave(float elapsedSeconds)
        {
            if (Time.timeScale <= 0f || !CanSavePlayer(gameplayPlayer)) {
                return;
            }

            autosaveElapsed += elapsedSeconds;

            if (autosaveElapsed >= AutosaveIntervalSeconds) {
                autosaveElapsed %= AutosaveIntervalSeconds;
                SaveCurrentProgress(gameplayPlayer);
            }
        }

        // 정상 종료 시 플레이 중인 지역의 진행을 디스크에 기록한다.
        public static void SaveBeforeQuit()
        {
            SaveCurrentProgress(gameplayPlayer);
        }

        // 타이틀이나 로드·사망 중에 기존 저장을 덮어쓰지 않도록 확인한다.
        private static bool CanSavePlayer(PlayerBase player)
        {
            return !IsLoading && player != null && player.isActiveAndEnabled && !player.IsDead
                && player.gameObject.scene == SceneManager.GetActiveScene();
        }

        // 현재 플레이어와 획득 목록을 지정된 도착 지점의 저장 데이터로 캡처한다.
        private static CheckpointSaveData CaptureProgress(PlayerBase player, string scenePath, string checkpointId)
        {
            var savedItemIds = new string[collectedItemIds.Count];
            collectedItemIds.CopyTo(savedItemIds);
            Array.Sort(savedItemIds, StringComparer.Ordinal);

            return new CheckpointSaveData {
                Version = CheckpointSaveData.CurrentVersion,
                ScenePath = scenePath,
                CheckpointId = checkpointId,
                Player = player.CaptureCheckpointState(),
                CollectedItemIds = savedItemIds
            };
        }

        // 유효한 진행을 단일 슬롯에 쓰고 실패하면 이전 슬롯을 유지한다.
        private static void WriteProgress(CheckpointSaveData save)
        {
            try {
                Store.Write(save);
                waitingForFirstSave = false;
            } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is NotSupportedException) {
                Debug.LogWarning($"진행을 자동 저장할 수 없습니다. {error.Message}");
            }
        }

        // 새 플레이 세션에서 복원 상태를 지우고 플레이어 사망에 다시 연결한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            collectedItemIds.Clear();
            saveStore = null;
            pendingSave = null;
            travelingHealth = null;
            newGameLoad = null;
            waitingForFirstSave = false;
            gameplayPlayer = null;
            currentCheckpointId = CheckpointSaveData.SceneEntryId;
            autosaveElapsed = 0f;
            LoadFinished = null;
            SceneManager.sceneLoaded -= TrackGameplayScene;
            SceneManager.sceneLoaded += TrackGameplayScene;
            SceneManager.sceneLoaded -= RestoreLoadedScene;
            PlayerBase.Died -= RetryOnDeath;
            PlayerBase.Died += RetryOnDeath;
        }

        // 플레이어 사망 시 메뉴의 이어하기와 같은 경로로 마지막 저장을 불러온다.
        private static void RetryOnDeath(PlayerBase player)
        {
            if (!waitingForFirstSave && player.gameObject.scene == SceneManager.GetActiveScene()) {
                TryContinue();
            }
        }

        // 저장 슬롯을 읽고 해당 씬이 현재 빌드에서 열리는지 확인한다.
        private static CheckpointSaveData ReadLoadableSave()
        {
            CheckpointSaveData save = Store.Read();
            return save != null && SceneUtility.GetBuildIndexByScenePath(save.ScenePath) >= 0 ? save : null;
        }

        // 대상 씬의 초기 오브젝트가 준비되면 체크포인트에서 플레이어를 복원한다.
        private static void RestoreLoadedScene(Scene scene, LoadSceneMode mode)
        {
            if (pendingSave == null || mode != LoadSceneMode.Single || scene.path != pendingSave.ScenePath) {
                return;
            }

            bool useSceneEntry = pendingSave.CheckpointId == CheckpointSaveData.SceneEntryId;
            Checkpoint checkpoint = useSceneEntry ? null : FindCheckpoint(scene, pendingSave.CheckpointId);
            PlayerBase player = FindGameplayPlayer(scene);

            if (player == null || (!useSceneEntry && checkpoint == null)) {
                Debug.LogWarning("진행을 복원하려면 대상 씬에 유효한 도착 지점과 활성 플레이어 한 명이 필요합니다.");
                FinishLoad(false);
                return;
            }

            Vector3 arrivalPosition = useSceneEntry ? player.transform.position : checkpoint.SpawnPosition;
            player.RestoreCheckpointState(pendingSave.Player, arrivalPosition, travelingHealth);
            RestoreCollectedItems(scene, pendingSave.CollectedItemIds);
            gameplayPlayer = player;
            currentCheckpointId = pendingSave.CheckpointId;

            if (travelingHealth.HasValue) {
                WriteProgress(pendingSave);
            } else {
                waitingForFirstSave = false;
            }

            FinishLoad(true);
        }

        // 새 지역의 활성 플레이어를 찾고 기본 복귀 지점을 지역 시작 위치로 지정한다.
        private static void TrackGameplayScene(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) {
                return;
            }

            currentCheckpointId = CheckpointSaveData.SceneEntryId;
            gameplayPlayer = FindGameplayPlayer(scene);
        }

        // 지정된 씬에 활성 플레이어가 정확히 한 명이면 반환한다.
        private static PlayerBase FindGameplayPlayer(Scene scene)
        {
            PlayerBase player = null;

            foreach (PlayerBase candidate in UnityEngine.Object.FindObjectsByType<PlayerBase>()) {
                if (candidate.gameObject.scene != scene || !candidate.isActiveAndEnabled) {
                    continue;
                }

                if (player != null) {
                    return null;
                }

                player = candidate;
            }

            return player;
        }

        // 저장된 획득 목록으로 되돌리고 해당 씬의 획득 완료 아이템을 숨긴다.
        private static void RestoreCollectedItems(Scene scene, string[] savedItemIds)
        {
            collectedItemIds.Clear();

            foreach (string itemId in savedItemIds ?? Array.Empty<string>()) {
                if (!string.IsNullOrWhiteSpace(itemId)) {
                    collectedItemIds.Add(itemId);
                }
            }

            foreach (OneTimePickup pickup in UnityEngine.Object.FindObjectsByType<OneTimePickup>()) {
                if (pickup.gameObject.scene == scene) {
                    pickup.HideIfCollected();
                }
            }
        }

        // 씬에서 지정 ID를 사용하는 활성 체크포인트 하나를 찾는다.
        private static Checkpoint FindCheckpoint(Scene scene, string id)
        {
            Checkpoint found = null;

            foreach (Checkpoint candidate in UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None)) {
                if (candidate.gameObject.scene == scene && candidate.isActiveAndEnabled && candidate.CheckpointId == id) {
                    if (found != null) {
                        return null;
                    }

                    found = candidate;
                }
            }

            return found;
        }

        // 씬 로드는 끝났지만 대상 씬을 받지 못하면 대기 상태를 해제한다.
        private static void FinishUnmatchedLoad(CheckpointSaveData save)
        {
            if (ReferenceEquals(pendingSave, save)) {
                FinishLoad(false);
            }
        }

        // 복원 작업을 마치고 UI 또는 호출자에게 성공 여부를 알린다.
        private static void FinishLoad(bool succeeded)
        {
            pendingSave = null;
            travelingHealth = null;
            SceneManager.sceneLoaded -= RestoreLoadedScene;
            LoadFinished?.Invoke(succeeded);
        }
    }
}
