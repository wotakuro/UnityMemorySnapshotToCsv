using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.Profiling.Memory;
using UnityEditor.Profiling.Memory.Experimental;


namespace UTJ
{

    public class MemorySnapshotCacheData
    {
        private PackedMemorySnapshot snapshot;

        internal class NativeObjectEntry
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
        internal class NativeTypeName
        {
            public string name;
            public int nativeBaseTypeArrayIndex;
        }

        internal class NativeAllocation
        {
            public int memoryRegionIndex;
            public long rootReferenceId;
            public long allocationSiteId;
            public ulong address;
            public ulong size;
            public int overhead;
            public int paddingSize;
        }

        internal class RootReference
        {
            public long id;
            public string areaName;
            public string objectName;
            public ulong accumulatedSize;
        }
        internal class MemoryRegion
        {
            public string memoryRegionName;
            public int parentIndex;
            public ulong addressBase;
            public ulong addressSize;
            public int firstAllocationIndex;
            public int numAllocations;
        }
        internal class ManagedType
        {
            public TypeFlags flags;
            public string typeDescriptionName;
            public string assembly;
            public int[] fieldIndices;
            public byte[] staticFieldBytes;
            public int baseOrElementTypeIndex;
            public int size;
            public ulong typeInfoAddress;
            public int typeIndex;
        }
        internal class ManagedFieldInfo
        {
            public string fieldDescriptionName;
            public int offset;
            public int typeIndex;
            public bool isStatic;
        }

        internal class ManagedMemory
        {
            public ulong startAddress;
            public byte[] bytes;
        }

        internal class GcHandle
        {
            public ulong address;
        }

        internal List<NativeTypeName> nativeTypes;
        internal List<NativeObjectEntry> nativeObjects;
        internal List<NativeAllocation> nativeAllocations;
        internal Dictionary<long, RootReference> rootReferences;
        internal List<MemoryRegion> memoryRegions;
        internal List<ManagedType> managedTypes;
        internal List<ManagedFieldInfo> managedFieldInfos;
        internal Dictionary<ulong, ManagedType> managedTypeByAddr;
        internal Dictionary<int, ManagedType> managedTypeByTypeIndex;

        internal List<ManagedMemory> managedHeap;
        internal List<ManagedMemory> managedStack;

        internal List<ManagedMemory> sortedManagedMemory;
        internal List<GcHandle> gcHandles;

        private string x16FormatCache = null;

        public string x16StrFormat
        {
            get
            {
                if (string.IsNullOrEmpty(x16FormatCache))
                {
                    x16FormatCache = "0x{0:X" + (snapshot.virtualMachineInformation.pointerSize * 2) + "}";
                }
                return x16FormatCache;
            }
        }

        public MemorySnapshotCacheData(string filePath)
        {
            snapshot = PackedMemorySnapshot.Load(filePath);

            CreateNativeObjectType(snapshot.nativeTypes);
            CreateNativeObjects(snapshot.nativeObjects);
            CreateNativeAllocation(snapshot.nativeAllocations);
            CreateRootReferences(snapshot.nativeRootReferences);
            CreateMemoryRegion(snapshot.nativeMemoryRegions);
            CreateManagedTypes(snapshot.typeDescriptions);
            CreateManagedFieldInfos(snapshot.fieldDescriptions);
            CreateManagedMemory(snapshot.managedHeapSections, ref managedHeap);
            CreateManagedMemory(snapshot.managedStacks, ref managedStack);
            CreateSortedMangedMemoryList();
            CreateGCHandles(snapshot.gcHandles);
        }

        private void CreateNativeObjectType(NativeTypeEntries nativeTypeEntries)
        {
            int num = (int)nativeTypeEntries.GetNumEntries();
            this.nativeTypes = new List<NativeTypeName>(num);
            if (num == 0) { return; }


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
            if (num == 0) { return; }


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
            this.nativeAllocations = new List<NativeAllocation>(num);
            if (num == 0) { return; }

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
            if (num == 0) { return; }


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
            if (num == 0) { return; }


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
        private void CreateManagedTypes(TypeDescriptionEntries typeDescriptionEntries)
        {
            int num = (int)typeDescriptionEntries.GetNumEntries();
            this.managedTypes = new List<ManagedType>(num);
            if (num == 0) { return; }

            TypeFlags[] flags = new TypeFlags[num];
            string[] typeDescriptionName = new string[num];
            string[] assembly = new string[num];
            int[][] fieldIndices = new int[num][];
            byte[][] staticFieldBytes = new byte[num][];
            int[] baseOrElementTypeIndex = new int[num];
            int[] size = new int [num];
            ulong[] typeInfoAddress = new ulong[num];
            int[] typeIndex = new int[num];

            typeDescriptionEntries.flags.GetEntries(0, (uint)num, ref flags);
            typeDescriptionEntries.typeDescriptionName.GetEntries(0, (uint)num, ref typeDescriptionName);
            typeDescriptionEntries.assembly.GetEntries(0, (uint)num, ref assembly);
            typeDescriptionEntries.fieldIndices.GetEntries(0, (uint)num, ref fieldIndices);
            typeDescriptionEntries.staticFieldBytes.GetEntries(0, (uint)num, ref staticFieldBytes);
            typeDescriptionEntries.baseOrElementTypeIndex.GetEntries(0, (uint)num, ref baseOrElementTypeIndex);
            typeDescriptionEntries.size.GetEntries(0, (uint)num, ref size);
            typeDescriptionEntries.typeInfoAddress.GetEntries(0, (uint)num, ref typeInfoAddress);
            typeDescriptionEntries.typeIndex.GetEntries(0, (uint)num, ref typeIndex);

            for(int i = 0; i < num; ++i)
            {
                var entry = new ManagedType();
                entry.flags = flags[i];
                entry.typeDescriptionName = typeDescriptionName[i];
                entry.assembly = assembly[i];
                entry.fieldIndices = fieldIndices[i];
                entry.staticFieldBytes = staticFieldBytes[i];
                entry.baseOrElementTypeIndex = baseOrElementTypeIndex[i];
                entry.size = size[i];
                entry.typeInfoAddress = typeInfoAddress[i];
                entry.typeIndex = typeIndex[i];
                this.managedTypes.Add(entry);
            }

            this.managedTypeByAddr = new Dictionary<ulong, ManagedType>(num);
            foreach(var entry in managedTypes)
            {
                this.managedTypeByAddr.Add(entry.typeInfoAddress, entry);
            }


            this.managedTypeByTypeIndex = new Dictionary<int, ManagedType>(managedTypes.Count);
            foreach (var type in this.managedTypes)
            {
                this.managedTypeByTypeIndex.Add(type.typeIndex, type);
            }
        }

        private void CreateManagedFieldInfos(FieldDescriptionEntries fieldDescriptionEntries)
        {
            int num = (int)fieldDescriptionEntries.GetNumEntries();
            this.managedFieldInfos = new List<ManagedFieldInfo>(num);
            if (num == 0) { return; }

            string[] fieldDescriptionName = new string[num];
            int[] offset = new int[num];
            int[] typeIndex = new int[num];
            bool[] isStatic = new bool[num];

            fieldDescriptionEntries.fieldDescriptionName.GetEntries(0, (uint)num, ref fieldDescriptionName);
            fieldDescriptionEntries.offset.GetEntries(0, (uint)num, ref offset);
            fieldDescriptionEntries.typeIndex.GetEntries(0, (uint)num, ref typeIndex);
            fieldDescriptionEntries.isStatic.GetEntries(0, (uint)num, ref isStatic);

            for (int i = 0; i < num; ++i)
            {
                var entry = new ManagedFieldInfo();
                entry.fieldDescriptionName = fieldDescriptionName[i];
                entry.offset = offset[i];
                entry.typeIndex = typeIndex[i];
                entry.isStatic = isStatic[i];
                this.managedFieldInfos.Add(entry);
            }
        }

        private void CreateManagedMemory(ManagedMemorySectionEntries managedMemorySection,ref List<ManagedMemory> managedMemories)
        {
            int num = (int)managedMemorySection.GetNumEntries();
            managedMemories = new List<ManagedMemory>(num);
            if (num == 0) { return; }
            ulong[] startAddress = new ulong[num];
            byte[][] bytes = new byte[num][];

            managedMemorySection.startAddress.GetEntries(0, (uint)num, ref startAddress);
            managedMemorySection.bytes.GetEntries(0, (uint)num, ref bytes);

            for( int i = 0; i < num; ++i)
            {
                var entry = new ManagedMemory();
                entry.startAddress = startAddress[i];
                entry.bytes = bytes[i];
                managedMemories.Add(entry);
            }
        }


        private void CreateSortedMangedMemoryList()
        {
            sortedManagedMemory = new List<ManagedMemory>(managedHeap.Count + managedStack.Count);
            foreach (var heap in managedHeap)
            {
                sortedManagedMemory.Add(heap);
            }
            foreach (var stack in managedStack)
            {
                sortedManagedMemory.Add(stack);
            }
            sortedManagedMemory.Sort((a, b) =>
            {
                if( a.startAddress < b.startAddress)
                {
                    return -1;
                }else if (a.startAddress > b.startAddress)
                {
                    return 1;
                }
                return 0;
            });

        }

        // managedObject
        private void CreateGCHandles(GCHandleEntries gcHandleEntries)
        {
            int num = (int)gcHandleEntries.GetNumEntries();
            this.gcHandles = new List<GcHandle>(num);
            if( num == 0) { return; }
            ulong[] addr = new ulong[num];
            gcHandleEntries.target.GetEntries(0, (uint)num, ref addr);
            for(int i = 0; i < num; ++i)
            {
                var entry = new GcHandle();
                entry.address = addr[i];
                this.gcHandles.Add(entry);
            }
        }


        internal ManagedType GetManagedTypeInfoFromAddr(ulong address,int depth = 0)
        {
            if(depth >1) { return null; }
            var managedMemory = GetManagedMemory(address);
            if (managedMemory == null) { return null; }

            int offset = (int)(address - managedMemory.startAddress);
            ulong newAddr = ReadPointer(managedMemory.bytes, offset);
            ManagedType typeInfo = null;
            if (managedTypeByAddr.TryGetValue(newAddr, out typeInfo))
            {
                return typeInfo;
            }
            return GetManagedTypeInfoFromAddr(newAddr, depth + 1) ;
        }


        internal ManagedMemory GetManagedMemory(ulong addr)
        {
            int length = sortedManagedMemory.Count;
            int minIdx = 0;
            int maxIdx = length;
            int currentIdx = length / 2;
            int cnt = 0;
            while (minIdx != maxIdx)
            {
                int result = GetManagedMemoryBiggerOrSmaller(sortedManagedMemory[currentIdx], addr);
                if(result == 0)
                {
                    return sortedManagedMemory[currentIdx];
                }
                if( result > 0)
                {
                    minIdx = currentIdx;
                }
                else if( result < 0)
                {
                    maxIdx = currentIdx;
                }
                currentIdx = (minIdx + maxIdx) / 2;
                if ( currentIdx == minIdx || currentIdx == maxIdx)
                {
                    break;
                }
                cnt++;
            }
            return null;
        }

        private int GetManagedMemoryBiggerOrSmaller(ManagedMemory memory ,ulong addr)
        {
            if (memory.startAddress > addr) {
                return -1;
            }else if(addr > memory.startAddress + (ulong)memory.bytes.Length)
            {
                return 1;
            }
            return 0;
        }
        private ulong ReadPointer(byte[] bytes,int offset)
        {
            int pointerSize = snapshot.virtualMachineInformation.pointerSize;
            if (pointerSize == 4)
                return BitConverter.ToUInt32(bytes, offset);
            if (pointerSize == 8)
                return BitConverter.ToUInt64(bytes, offset);
            throw new ArgumentException("Unexpected pointer size: " + pointerSize);
        }


    }
}