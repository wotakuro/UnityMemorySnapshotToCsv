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
            SetupStaticField();

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
                    AddManagedObject(managedObjectInfo);
                }
            }
        }
        private void SetupStaticField()
        {
            foreach( var type in cacheSnapshot.managedTypes)
            {
                if( type.staticFieldBytes == null || type.staticFieldInfos == null) { continue; }
                if( type.staticFieldBytes.Length == 0) { continue; }

                foreach (var field in type.staticFieldInfos)
                {
                    bool isValue = ((field.fieldType.flags & UnityEditor.Profiling.Memory.Experimental.TypeFlags.kValueType) != 0);
                    if (isValue) { continue; }
                    if (field.offset < 0) { continue; }
                    var addr = cacheSnapshot.ReadPointer(type.staticFieldBytes, field.offset);
                    var obj = GetManagedObjectInfoFromAddr(addr);
                    this.AddManagedObject(obj);
                }
            }
        }

        private bool AddManagedObject(ManagedObjectInfo obj)
        {
            if( obj == null) { return false; }
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