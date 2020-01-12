using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.Profiling.Memory;
using UnityEngine.Profiling.Memory.Experimental;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace UTJ.MemoryProfilerToCsv
{
    public class MemorySnapshotToCsvWindow : EditorWindow
    {
        class SnapInfo
        {
            public string filePath;
            public Texture2D texture;
            public DateTime dateTime;
            public MetaData metadata;
            public SnapInfo(string file)
            {
                this.filePath = file;
            }
        }
        private static string memoryFile = "";

        List<SnapInfo> infoList;


        [MenuItem("Tools/MemoryCsv")]
        public static void Create()
        {
            EditorWindow.GetWindow<MemorySnapshotToCsvWindow>();
        }
#if UNITY_2019_1_OR_NEWER

        private VisualTreeAsset itemTreeAsset;

        private void OnEnable()
        {
            string path = "Packages/com.utj.memorysnapshot2csv/Editor/UI/UXML";
            string rootElemFile = System.IO.Path.Combine(path, "MemoryToCsv.uxml");
            VisualTreeAsset rootElem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(rootElemFile);
            this.rootVisualElement.Add(rootElem.CloneTree());

            string itemElemeFile = System.IO.Path.Combine(path, "SnapshotElement.uxml");
            this.itemTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(itemElemeFile);

            this.rootVisualElement.Q<Button>("RefleshBtn").clicked += this.Reflesh;
            this.rootVisualElement.Q<Button>("NewFile").clicked += () =>
            {
                memoryFile = EditorUtility.OpenFilePanel("SelectSnapShot", "", "snap");
                OpenFile(memoryFile);
            };
            this.Reflesh();
        }

        private void Reflesh()
        {
            var scrollView = this.rootVisualElement.Q<ScrollView>("SnapList");
            scrollView.Clear();
            this.RefleshList();
            foreach( var info in infoList)
            {
                var itemView = itemTreeAsset.CloneTree();

                SetupItem(itemView, info);
                scrollView.Add(itemView);

            }
        }
        private void SetupItem(VisualElement item ,SnapInfo info)
        {
            item.Q<VisualElement>("Left").style.backgroundImage = info.texture;
            item.Q<Label>("Filename").text = System.IO.Path.GetFileNameWithoutExtension(info.filePath);
            if (info.metadata != null)
            {
                item.Q<Label>("Platform").text = info.metadata.platform;
                item.Q<Label>("DateTime").text = info.dateTime.ToString();
            }

            item.Q<Button>("Open").clicked += () =>
            {
                OpenFile(info.filePath);
            };
        }
        private void OpenFile(string path)
        {
            MemorySnapshotToCsv obj = new MemorySnapshotToCsv(path);
            obj.Save(path);
            // System.IO.Directory.GetCurrentDirectory()
            string outputDir = path;
            EditorUtility.DisplayDialog("CSV Complete", outputDir, "OK");
            EditorUtility.RevealInFinder(outputDir);

        }
#endif

#if !UNITY_2019_1_OR_NEWER
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
                obj.Save(memoryFile);
                EditorUtility.DisplayDialog("CSV Complete", System.IO.Directory.GetCurrentDirectory(), "OK");
            }
        }
#endif

        private void RefleshList()
        {
            this.infoList = ScanMemorysanpshotFiles("MemoryCaptures");
        }

        private List<SnapInfo> ScanMemorysanpshotFiles(string dir)
        {
            List<SnapInfo> snapfiles = new List<SnapInfo>();
            var files = Directory.GetFiles(dir,"*.snap");
            foreach( var file in files)
            {
                var info = new SnapInfo(file);
                snapfiles.Add(info);
            }
            return snapfiles;
        }

    }
}
