using UnityEngine;

namespace Mech.Function
{
    internal sealed class ProgressAutosave : MonoBehaviour
    {
        // 씬을 이동해도 주기와 종료를 감시할 자동 저장 오브젝트를 만든다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Progress Autosave");
            DontDestroyOnLoad(host);
            host.AddComponent<ProgressAutosave>();
        }

        // 타임스케일과 별도로 흐른 프레임 시간을 자동 저장 타이머에 전달한다.
        private void Update()
        {
            CheckpointProgress.TickAutosave(Time.unscaledDeltaTime);
        }

        // Unity가 종료되기 전에 플레이 중인 진행을 저장한다.
        private void OnApplicationQuit()
        {
            CheckpointProgress.SaveBeforeQuit();
        }
    }
}
