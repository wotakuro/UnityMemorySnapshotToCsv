using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.Profiling.Memory;
using UnityEngine.Profiling.Memory.Experimental;
using System.Threading.Tasks;

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
                string pngPath = file.Replace(".snap", ".png");
                if( System.IO.File.Exists(pngPath) ){
                    this.texture = new Texture2D(8,8);
                    ImageConversion.LoadImage(this.texture, System.IO.File.ReadAllBytes(pngPath));
                }
            }
        }
        private const string GeneratedCsvDir = "MemorySnapshotCsv/";

        private List<SnapInfo> infoList;


        [MenuItem("Tools/MemoryCsv")]
        public static void Create()
        {
            EditorWindow.GetWindow<MemorySnapshotToCsvWindow>();
        }

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
                string memoryFile = EditorUtility.OpenFilePanel("SelectSnapShot", "", "snap");
                var task = OpenFile(memoryFile, this.rootVisualElement.Q<VisualElement>("NewFileArea").Q<VisualElement>("Execute"));
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
                var executeVisualElement = item.Q<VisualElement>("Execute");

                var task = OpenFile(info.filePath, executeVisualElement);
            };
        }
        


        private async Task OpenFile(string path,VisualElement executeVisualElement )
        {
            executeVisualElement.visible = true;
            IProgress<float> progress = new Progress<float>( (p)=>
            {
                executeVisualElement.Q<UnityEditor.UIElements.ProgressBar>("ProgressBar").value = p;
            });
            await Task.Run(() =>
            {
                var parser = new MemorySnapshotToCsv(path, (p)=> { progress.Report(p); });
                parser.Save(GetNewPath(path));
                progress.Report(100f);
            });
            executeVisualElement.visible = false;
            string openPath = GetNewPath(path) + "-nativeObjects.csv";
#if UNITY_EDITOR_WIN
//            openPath = Path.Combine(Directory.GetCurrentDirectory(), GetNewPath(path) + "nativeObjects.csv");
            EditorUtility.RevealInFinder(openPath);
#endif
#if UNITY_EDITOR_MAC
            EditorUtility.RevealInFinder(openPath);
#endif
        }

        private void WatchExecute()
        {
        }

        private static string GetNewPath(string path)
        {
            string newPath = GeneratedCsvDir + System.IO.Path.GetFileNameWithoutExtension(path) + "/result";
            return newPath;
        }
        
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
