using UnityEditor;
using UnityEngine;

namespace UTJ.MemoryProfilerToCsv
{
    public class MemorySnapshotToCsvWindow : EditorWindow
    {

        private static string memoryFile = "";
        [MenuItem("Tools/MemoryCsv")]
        public static void Create()
        {
            EditorWindow.GetWindow<MemorySnapshotToCsvWindow>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("file:" + memoryFile);
            if (GUILayout.Button("File"))
            {
                memoryFile = EditorUtility.OpenFilePanel("SelectSnapShot", "", "snap");
            }
            if (GUILayout.Button("Exec") )
            {
                MemorySnapshotToCsv obj = new MemorySnapshotToCsv(memoryFile);
                EditorUtility.DisplayDialog("CSV Complete", System.IO.Directory.GetCurrentDirectory(), "OK");
            }
        }

    }
}
