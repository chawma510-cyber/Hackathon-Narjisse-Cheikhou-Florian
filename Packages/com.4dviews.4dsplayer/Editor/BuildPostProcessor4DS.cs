
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using UnityEngine;
using UnityEditor.Build;

#if UNITY_IOS || UNITY_VISIONOS

using UnityEditor.iOS.Xcode;

using System.IO;

namespace unity4dv
{
	public class BuildPostProcessor4DS
	{
		[PostProcessBuildAttribute(1)]
		public static void OnPostProcessBuild(BuildTarget Target, string PathToBuiltProject)
		{
			if (Target != BuildTarget.iOS && Target != BuildTarget.VisionOS)
			{
				return;
			}
			
			// Read.
				
			// https://docs.unity3d.com/ScriptReference/iOS.Xcode.PBXProject.GetPBXProjectPath.html
			// Note: On VisionOS Platforms this will not return the expected
			// "BUILDPATH/Unity-VisionOS.xcodeproj/project.pbxproj", but will instead return
			// “BUILDPATH/Unity-iPhone.xcodeproj/project.pbxproj”, which is not correct.
			// You should not use this method for visionOS.
			string projectPath = PBXProject.GetPBXProjectPath(PathToBuiltProject);
			
			if (Target == BuildTarget.VisionOS)
			{
				// Dirty fix to the issue mentioned above.
				projectPath = projectPath.Replace("iPhone", "VisionOS");
			}
			
			PBXProject project = new PBXProject();
			project.ReadFromString(File.ReadAllText(projectPath));
				
#if UNITY_2019_3_OR_NEWER
			string targetGUID = project.GetUnityMainTargetGuid();
#else
			string targetName = PBXProject.GetUnityTargetName();
			string targetGUID = project.TargetGuidByName(targetName);
#endif
				
			AddFrameworks(project, targetGUID);

			// Write.
			File.WriteAllText(projectPath, project.WriteToString());
		}

		static void AddFrameworks(PBXProject Project, string TargetGUID)
		{
			Project.AddFrameworkToProject(TargetGUID, "libz.tbd", false);
		}
	}
}
#elif UNITY_ANDROID
using System.IO;
using UnityEditor.Build.Reporting;

namespace unity4dv
{
    public class BuildPreProcessor4DS : IPreprocessBuildWithReport
    {
        static void ToggleAndroidCompatibility(string file, bool enable)
        {
            var lines = File.ReadAllLines(file);
            var pluginImporter = AssetImporter.GetAtPath(file) as PluginImporter;
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, enable);
            EditorUtility.SetDirty(pluginImporter);
            pluginImporter.SaveAndReimport();
        }

        public int callbackOrder { get { return 100; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            string file_arm64 = "Packages/com.4dviews.4dsplayer/Plugins/Android/libs/arm64/libBridgeCodec4DS.so";
            string file_arm64_api29 = "Packages/com.4dviews.4dsplayer/Plugins/Android/libs_api29/arm64/libBridgeCodec4DS.so";
            string file_x86_64 = "Packages/com.4dviews.4dsplayer/Plugins/Android/libs/x86_64/libBridgeCodec4DS.so";
            string file_x86_64_api29 = "Packages/com.4dviews.4dsplayer/Plugins/Android/libs_api29/x86_64/libBridgeCodec4DS.so";

            try {
                if (PlayerSettings.Android.minSdkVersion <= AndroidSdkVersions.AndroidApiLevel29) {
                    //disable recent lib
                    ToggleAndroidCompatibility(file_arm64, false);
                    ToggleAndroidCompatibility(file_x86_64, false);
                    //enable old one
                    ToggleAndroidCompatibility(file_arm64_api29, true);
                    ToggleAndroidCompatibility(file_x86_64_api29, true);
                } else {
                    //enable recent lib
                    ToggleAndroidCompatibility(file_arm64, true);
                    ToggleAndroidCompatibility(file_x86_64, true);
                    //disable old one
                    ToggleAndroidCompatibility(file_arm64_api29, false);
                    ToggleAndroidCompatibility(file_x86_64_api29, false);
                }
            } catch (Exception e) {
                Debug.Log(e.Message);
            }
        }
    }
}
#endif
