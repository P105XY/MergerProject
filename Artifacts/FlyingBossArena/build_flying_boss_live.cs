using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildFlyingBossLive
{
    // 복사본의 비행 보스 씬을 백그라운드 검증용 macOS 실행 파일로 만든다.
    public static void Run()
    {
        string destination = Path.GetFullPath("FlyingBossLiveBuild/FlyingBossLive.app");
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        PlayerSettings.productName = "Merger Flying Boss Live Check";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/TestScene/FlyingBossTestScene.unity" },
            locationPathName = destination,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded) {
            throw new InvalidOperationException("비행 보스 검증 빌드 실패 " + report.summary.result);
        }
    }
}
