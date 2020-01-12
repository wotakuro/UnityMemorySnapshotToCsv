using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ManagedObjectInfo = UTJ.MemoryProfilerToCsv.MemorySnapshotCacheData.ManagedObjectInfo;

namespace UTJ.MemoryProfilerToCsv
{
    internal class ManagedObjectCrawler 
    {
        private Dictionary<ulong, ManagedObjectInfo> managedObjectByAddr;

        private MemorySnapshotCacheData cacheSnapshot;

        public ManagedObjectCrawler(MemorySnapshotCacheData cacheData)
        {
            this.cacheSnapshot = cacheData;
        }

        internal Dictionary<ulong,ManagedObjectInfo> Execute()
        {
            managedObjectByAddr = new Dictionary<ulong, ManagedObjectInfo>();
            SetupFromGcHandle();

            return managedObjectByAddr;
        }


        private void SetupFromGcHandle()
        {
            foreach (var entry in cacheSnapshot.gcHandles)
            {
                var managedMemory = cacheSnapshot.GetManagedMemory(entry.address);
                if (managedMemory != null)
                {
                    var managedObjectInfo = GetManagedObjectInfoFromAddr(entry.address);
                    if (managedObjectInfo != null)
                    {
                        AddManagedObject(managedObjectInfo);
                    }
                }
            }
        }

        private bool AddManagedObject(ManagedObjectInfo obj)
        {
            if(this.managedObjectByAddr.ContainsKey(obj.address))
            {
                return false;
            }
            this.managedObjectByAddr.Add(obj.address, obj);
            return true;
        }

        internal ManagedObjectInfo GetManagedObjectInfoFromAddr(ulong address)
        {
            int offset = 0;
            MemorySnapshotCacheData.ManagedMemory memory = null;
            ManagedObjectInfo managedObjectInfo = new ManagedObjectInfo();


            ulong newAddr = cacheSnapshot.ReadPointerByAddress(address, out memory, out offset);
            managedObjectInfo.address = address;
            managedObjectInfo.memoryBlock = memory;
            managedObjectInfo.offset = offset;

            MemorySnapshotCacheData.ManagedType typeInfo = null;
            if (cacheSnapshot.managedTypeByAddr.TryGetValue(newAddr, out typeInfo))
            {
                managedObjectInfo.typeInfo = typeInfo;
            }
            ulong typeInfoAddr = cacheSnapshot.ReadPointerByAddress(newAddr, out memory, out offset);
            
            if (cacheSnapshot.managedTypeByAddr.TryGetValue(typeInfoAddr, out typeInfo))
            {
                managedObjectInfo.typeInfo = typeInfo;
                managedObjectInfo.isArray = typeInfo.IsArray;
            }

            return managedObjectInfo;
        }
    }
}