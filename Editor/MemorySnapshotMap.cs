using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UTJ.MemoryProfilerToCsv
{
    public class MemorySnapshotMap
    {
        internal class MemoryInfo
        {
            public ulong addressStart;
            public ulong addressEnd;
            public ulong memorySize;
            public object relatedObject;

            public MemoryInfo(ulong baseAddr,ulong size,object obj)
            {
                this.addressStart = baseAddr;
                this.addressEnd = baseAddr + size;
                this.memorySize = size;
                this.relatedObject = obj;
            }
        }

        internal List<MemoryInfo> memoryLists;

        public MemorySnapshotMap(MemorySnapshotCacheData cacheData)
        {
            int cnt = cacheData.memoryRegions.Count + cacheData.nativeAllocations.Count + cacheData.sortedManagedMemory.Count;
            memoryLists = new List<MemoryInfo>(cnt);
            foreach (var region in cacheData.memoryRegions)
            {
                var memory = new MemoryInfo(region.addressBase, region.addressSize, region);
                memoryLists.Add(memory);
            }
            foreach(var nativeAlloc in cacheData.nativeAllocations)
            {
                var memory = new MemoryInfo(nativeAlloc.address, nativeAlloc.size, nativeAlloc);
                memoryLists.Add(memory);
            }
            foreach( var nativeobj in cacheData.nativeObjects)
            {
                var memory = new MemoryInfo(nativeobj.address, nativeobj.size, nativeobj);
                memoryLists.Add(memory);
            }
            foreach (var managedAlloc in cacheData.sortedManagedMemory)
            {
                var memory = new MemoryInfo(managedAlloc.startAddress, (ulong)managedAlloc.bytes.Length, managedAlloc);
                memoryLists.Add(memory);
            }
            memoryLists.Sort((a, b) =>
            {
               if (a.addressStart < b.addressStart) { return -1; }
               if (a.addressStart > b.addressStart) { return 1; }
               if (a.addressEnd < b.addressEnd) { return -1; }
               if (a.addressEnd > b.addressEnd) { return 1; }
               return 0;
           });
        }
    }
}
