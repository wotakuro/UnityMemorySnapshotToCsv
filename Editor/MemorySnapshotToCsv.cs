using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.Profiling.Memory;
using UnityEditor.Profiling.Memory.Experimental;

namespace UTJ
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
            memoryFile = EditorGUILayout.TextField(memoryFile);
            if (GUILayout.Button("File"))
            {
                memoryFile = EditorUtility.OpenFilePanel("SelectSnapShot", "", "snap");
            }
            if (GUILayout.Button("Exec"))
            {
                UTJ.MemorySnapshotToCsv obj = new UTJ.MemorySnapshotToCsv(memoryFile);
                EditorUtility.DisplayDialog("CSV Complete", System.IO.Directory.GetCurrentDirectory(), "OK");
            }
        }

    }
}

namespace UTJ
{

    public class MemorySnapshotToCsv
    {
        private PackedMemorySnapshot snapshot;

        class NativeObjectEntry
        {
            public string objectName;
            public int instanceId;
            public ulong size;
            public int nativeTypeArrayIndex;
            public HideFlags hideFlag;
            public ObjectFlags flag;
            public long rootReferenceId;
            public ulong address;
        }
        class NativeTypeName
        {
            public string name;
            public int nativeBaseTypeArrayIndex;
        }

        class NativeAllocation
        {
            public int memoryRegionIndex;
            public long rootReferenceId;
            public long allocationSiteId;
            public ulong address;
            public ulong size;
            public int overhead;
            public int paddingSize;
        }

        class RootReference
        {
            public long id;
            public string areaName;
            public string objectName;
            public ulong accumulatedSize;
        }
        class MemoryRegion
        {
            public string memoryRegionName;
            public int parentIndex;
            public ulong addressBase;
            public ulong addressSize;
            public int firstAllocationIndex;
            public int numAllocations;
        }

        private List<NativeTypeName> nativeTypes;
        private List<NativeObjectEntry> nativeObjects;
        private List<NativeAllocation> nativeAllocations;
        private Dictionary<long, RootReference> rootReferences;
        private List<MemoryRegion> memoryRegions;


        public MemorySnapshotToCsv(string filePath)
        {
            snapshot = PackedMemorySnapshot.Load(filePath);
            CreateNativeObjectType(snapshot.nativeTypes);
            CreateNativeObjects(snapshot.nativeObjects);
            CreateNativeAllocation(snapshot.nativeAllocations);
            CreateRootReferences(snapshot.nativeRootReferences);
            CreateMemoryRegion(snapshot.nativeMemoryRegions);


            Save(System.IO.Path.GetFileName(filePath));                        
        }

        private void CreateNativeObjectType(NativeTypeEntries nativeTypeEntries)
        {
            int num = (int)nativeTypeEntries.GetNumEntries();
            this.nativeTypes = new List<NativeTypeName>(num);
            string[] typeName = new string[num];
            int[] nativeBaseTypeArrayIndex = new int[num];

            nativeTypeEntries.typeName.GetEntries(0,(uint)num, ref typeName);
            nativeTypeEntries.nativeBaseTypeArrayIndex.GetEntries(0, (uint)num, ref nativeBaseTypeArrayIndex);
            for (int i = 0; i< num; ++i)
            {
                var entry = new NativeTypeName();
                entry.name = typeName[i];
                entry.nativeBaseTypeArrayIndex = nativeBaseTypeArrayIndex[i];
                nativeTypes.Add(entry);
            }
        }


        private void CreateNativeObjects(NativeObjectEntries nativeObjectEntries)
        {
            int num = (int)nativeObjectEntries.GetNumEntries();
            this.nativeObjects = new List<NativeObjectEntry>(num);


            string[] names = new string[num];
            int[] instanceIds = new int[num];
            ulong[] size = new ulong[num];
            int[] nativeTypeArrayIndex = new int[num];
            HideFlags[] hideFlags = new HideFlags[num];
            ObjectFlags[] flags = new ObjectFlags[num];
            int[] nativeTypeArray = new int[num];
            long[] rootReferenceId = new long[num];
            ulong[] addr = new ulong[num];

            nativeObjectEntries.objectName.GetEntries(0, (uint)num, ref names);
            nativeObjectEntries.instanceId.GetEntries(0, (uint)num, ref instanceIds);
            nativeObjectEntries.size.GetEntries(0, (uint)num, ref size);
            nativeObjectEntries.nativeTypeArrayIndex.GetEntries(0, (uint)num, ref nativeTypeArrayIndex);
            nativeObjectEntries.hideFlags.GetEntries(0, (uint)num, ref hideFlags);
            nativeObjectEntries.flags.GetEntries(0, (uint)num, ref flags);
            nativeObjectEntries.rootReferenceId.GetEntries(0, (uint)num, ref rootReferenceId);
            nativeObjectEntries.nativeObjectAddress.GetEntries(0, (uint)num, ref addr);
            for (int i = 0; i < num; ++i)
            {
                NativeObjectEntry entry = new NativeObjectEntry();
                entry.objectName = names[i];
                entry.instanceId = instanceIds[i];
                entry.size = size[i];
                entry.nativeTypeArrayIndex = nativeTypeArrayIndex[i];
                entry.hideFlag = hideFlags[i];
                entry.flag = flags[i];
                entry.rootReferenceId = rootReferenceId[i];
                entry.address = addr[i];
                this.nativeObjects.Add(entry);
            }

            this.nativeObjects.Sort((a, b) => {
                if (a.address < b.address) { return -1; }
                else if (a.address > b.address) { return 1; }
                return 0;
            });
        }

        private void CreateNativeAllocation(NativeAllocationEntries allocationEntries)
        {
            int num = (int)allocationEntries.GetNumEntries();
            int[] memoryRegionIndex = new int[num];
            long[] rootReferenceId = new long[num];
            long[] allocationSiteId = new long[num];
            ulong[] address = new ulong[num];
            ulong[] size = new ulong[num];
            int[] overhead = new int[num];
            int[] paddingSize = new int[num];
            allocationEntries.memoryRegionIndex.GetEntries(0, (uint)num, ref memoryRegionIndex);
            allocationEntries.rootReferenceId.GetEntries(0, (uint)num, ref rootReferenceId);
            allocationEntries.allocationSiteId.GetEntries(0, (uint)num, ref allocationSiteId);
            allocationEntries.address.GetEntries(0, (uint)num, ref address);
            allocationEntries.size.GetEntries(0, (uint)num, ref size);
            allocationEntries.overheadSize.GetEntries(0, (uint)num, ref overhead);
            allocationEntries.paddingSize.GetEntries(0, (uint)num, ref paddingSize);

            this.nativeAllocations = new List<NativeAllocation>(num);
            for( int i = 0; i < num; ++i)
            {
                var entry = new NativeAllocation();
                entry.memoryRegionIndex = memoryRegionIndex[i];
                entry.rootReferenceId = rootReferenceId[i];
                entry.allocationSiteId = allocationSiteId[i];
                entry.address = address[i];
                entry.size = size[i];
                entry.overhead = overhead[i];
                entry.paddingSize = paddingSize[i];
                this.nativeAllocations.Add(entry);
            }
            this.nativeAllocations.Sort((a, b) => {
                if (a.address < b.address) { return -1; }
                else if (a.address > b.address) { return 1; }
                return 0;
            });
        }


        private void CreateRootReferences(NativeRootReferenceEntries rootReferenceEntries)
        {
            int num = (int)rootReferenceEntries.GetNumEntries();
            this.rootReferences = new Dictionary<long, RootReference>(num);
            long[] id = new long[num];
            string[] areaName = new string[num];
            string[] objectName = new string[num];
            ulong[] accumulatedSize = new ulong[num];
            rootReferenceEntries.id.GetEntries(0, (uint)num, ref id);
            rootReferenceEntries.areaName.GetEntries(0, (uint)num, ref areaName);
            rootReferenceEntries.objectName.GetEntries(0, (uint)num, ref objectName);
            rootReferenceEntries.accumulatedSize.GetEntries(0, (uint)num, ref accumulatedSize);

            for( int i = 0; i < num; ++i)
            {
                var entry = new RootReference();
                entry.id = id[i];
                entry.areaName = areaName[i];
                entry.objectName = objectName[i];
                entry.accumulatedSize = accumulatedSize[i];
                this.rootReferences.Add(id[i], entry);
            }
        }

        private void CreateMemoryRegion(NativeMemoryRegionEntries nativeMemoryRegionEntries)
        {
            int num = (int)nativeMemoryRegionEntries.GetNumEntries();
            this.memoryRegions = new List<MemoryRegion>(num);


            string[] memoryRegionName = new string[num];
            int[] parentIndex = new int[num];
            ulong[] addressBase = new ulong[num];
            ulong[] addressSize = new ulong[num];
            int[] firstAllocationIndex = new int[num];
            int[] numAllocations = new int[num];
            nativeMemoryRegionEntries.memoryRegionName.GetEntries(0, (uint)num, ref memoryRegionName);
            nativeMemoryRegionEntries.parentIndex.GetEntries(0, (uint)num, ref parentIndex);
            nativeMemoryRegionEntries.addressBase.GetEntries(0, (uint)num, ref addressBase);
            nativeMemoryRegionEntries.addressSize.GetEntries(0, (uint)num, ref addressSize);
            nativeMemoryRegionEntries.firstAllocationIndex.GetEntries(0, (uint)num, ref firstAllocationIndex);
            nativeMemoryRegionEntries.numAllocations.GetEntries(0, (uint)num, ref numAllocations);

            for (int i = 0; i < num; ++i) {
                var entry = new MemoryRegion();
                entry.memoryRegionName = memoryRegionName[i];
                entry.parentIndex = parentIndex[i];
                entry.addressBase = addressBase[i];
                entry.addressSize = addressSize[i];
                entry.firstAllocationIndex = firstAllocationIndex[i];
                entry.numAllocations = numAllocations[i];
                this.memoryRegions.Add(entry);
            }

        }



        private void Save(string originFile)
        {
            var str = originFile.Remove(originFile.Length - 5);
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("address").AppendColumn("size").AppendColumn("name").AppendColumn("typeName").AppendColumn("instanceId");
            csvGenerator.AppendColumn("rootReferenceId").AppendColumn("rootAreaName").AppendColumn("rootObjectName").AppendColumn("rootaccumulatedSize");
            csvGenerator.NextRow();
            foreach (var entry in nativeObjects)
            {
                csvGenerator.AppendColumn(string.Format("0x{0:X16}", entry.address));
                csvGenerator.AppendColumn(entry.size);
                csvGenerator.AppendColumn(entry.objectName);
                // type
                csvGenerator.AppendColumn(this.nativeTypes[entry.nativeTypeArrayIndex].name);
                csvGenerator.AppendColumn(entry.instanceId);
                // root Reference
                csvGenerator.AppendColumn(entry.rootReferenceId);
                RootReference rootReference = null;
                if (this.rootReferences.TryGetValue(entry.rootReferenceId, out rootReference))
                {
                    csvGenerator.AppendColumn(rootReference.areaName).AppendColumn(rootReference.objectName).AppendColumn(rootReference.accumulatedSize);
                }
                else
                {
                    csvGenerator.AppendColumn("").AppendColumn("").AppendColumn("");
                }
                csvGenerator.NextRow();
            }
            System.IO.File.WriteAllText(str + "-nativeObjects.csv", csvGenerator.ToString());

            // 
            csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("address").AppendColumn("Size").AppendColumn("overhead size").AppendColumn("padding size");
            csvGenerator.AppendColumn("regionId").AppendColumn("regionName").AppendColumn("regionBase").AppendColumn("regionSize");
            csvGenerator.AppendColumn("rootReferenceId").AppendColumn("rootAreaName").AppendColumn("rootObjectName").AppendColumn("rootaccumulatedSize");
            csvGenerator.NextRow();
            foreach( var entry in this.nativeAllocations)
            {
                csvGenerator.AppendColumn(string.Format("0x{0:X16}", entry.address) );
                csvGenerator.AppendColumn(entry.size);
                csvGenerator.AppendColumn(entry.overhead);
                csvGenerator.AppendColumn(entry.paddingSize);

                // region
                csvGenerator.AppendColumn(entry.memoryRegionIndex);
                MemoryRegion region = null;
                region = this.memoryRegions[ entry.memoryRegionIndex];
                if( region != null)
                {
                    csvGenerator.AppendColumn(region.memoryRegionName).AppendColumn( string.Format("{0:X16}",region.addressBase) ).AppendColumn(region.addressSize);
                }
                else
                {
                    csvGenerator.AppendColumn("").AppendColumn("").AppendColumn("");
                }
                // root reference
                csvGenerator.AppendColumn(entry.rootReferenceId);
                RootReference rootReference = null;
                if ( this.rootReferences.TryGetValue(entry.rootReferenceId,out rootReference))
                {
                    csvGenerator.AppendColumn(rootReference.areaName).AppendColumn(rootReference.objectName).AppendColumn(rootReference.accumulatedSize);
                }
                else
                {
                    csvGenerator.AppendColumn("").AppendColumn("").AppendColumn("");
                }
                csvGenerator.NextRow();
            }
            System.IO.File.WriteAllText(str + "-nativeAllocations.csv", csvGenerator.ToString());

        }

    }
}