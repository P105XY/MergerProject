using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildBossArenaLive
{
    // 검증 프로젝트의 PlayTestScene을 임시 macOS 실행 파일로 빌드한다.
    public static void Run()
    {
        string destination = Path.GetFullPath("BossArenaLiveBuild/BossArenaLive.app");
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        PlayerSettings.productName = "Merger Boss Arena Live Check";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/TestScene/PlayTestScene.unity" },
            locationPathName = destination,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded) {
            throw new InvalidOperationException("검증 실행 파일 빌드 결과 " + report.summary.result);
        }

        UnityEngine.Debug.Log("[BossLive] Standalone build succeeded.");
    }
}
