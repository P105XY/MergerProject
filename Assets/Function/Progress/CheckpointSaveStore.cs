using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Mech.Function
{
    [Serializable]
    internal sealed class CheckpointSaveData
    {
        public const int CurrentVersion = 1;
        public const string SceneEntryId = "$scene-entry";

        public int Version;
        public string ScenePath;
        public string CheckpointId;
        public PlayerCheckpointState Player;
        public string[] CollectedItemIds = Array.Empty<string>();

        internal bool IsValid => Version == CurrentVersion && !string.IsNullOrWhiteSpace(ScenePath)
            && !string.IsNullOrWhiteSpace(CheckpointId) && PlayerBase.IsValidCheckpointState(Player);
    }

    internal sealed class CheckpointSaveStore
    {
        private readonly string directory;
        private readonly string primaryPath;
        private readonly string backupPath;

        // 사용자 데이터 폴더에서 단일 진행 슬롯의 경로를 준비한다.
        internal CheckpointSaveStore(string directory)
        {
            this.directory = directory;
            primaryPath = Path.Combine(directory, "checkpoint.json");
            backupPath = primaryPath + ".bak";
        }

        // 주 저장 파일이 없거나 유효하지 않으면 직전 정상 저장본을 읽는다.
        internal CheckpointSaveData Read()
        {
            return ReadFile(primaryPath) ?? ReadFile(backupPath);
        }

        // 새 내용을 임시 파일에 기록한 뒤 정상 저장본을 보존하며 슬롯을 교체한다.
        internal void Write(CheckpointSaveData data)
        {
            string json = JsonUtility.ToJson(data);
            CheckpointSaveData previous = ReadFile(primaryPath);

            if (previous != null && JsonUtility.ToJson(previous) == json) {
                return;
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = primaryPath + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(primaryPath)) {
                File.Replace(temporaryPath, primaryPath, previous != null ? backupPath : null);
            } else {
                File.Move(temporaryPath, primaryPath);
            }
        }

        // 개별 파일의 버전과 저장 데이터를 읽고 손상됐으면 사용하지 않는다.
        private static CheckpointSaveData ReadFile(string path)
        {
            try {
                var data = JsonUtility.FromJson<CheckpointSaveData>(File.ReadAllText(path));
                return data != null && data.IsValid ? data : null;
            } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException) {
                return null;
            }
        }
    }
}
