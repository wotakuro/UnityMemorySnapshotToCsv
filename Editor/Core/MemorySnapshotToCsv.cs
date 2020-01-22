using System.Collections.Generic;


namespace UTJ.MemoryProfilerToCsv
{

    public class MemorySnapshotToCsv
    {
        private MemorySnapshotCacheData cacheSnapshot;
        private System.Action<float> progressCallback;


        public MemorySnapshotToCsv(string filePath,System.Action<float> pcallback)
        {
            cacheSnapshot = new MemorySnapshotCacheData(filePath, pcallback);
            this.progressCallback = pcallback;
        }

        public void Save(string savePath)
        {
            string dir = System.IO.Path.GetDirectoryName(savePath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            MemorySnapshotToCsv.Progress(70.0f, this.progressCallback);
            SaveNativeObjects(savePath);
            MemorySnapshotToCsv.Progress(75.0f, this.progressCallback);
            SaveNativeAllocation(savePath);
            MemorySnapshotToCsv.Progress(80.0f, this.progressCallback);
            SaveManagedAllocations(savePath);
            MemorySnapshotToCsv.Progress(85.0f, this.progressCallback);
            SaveManagedTypeList(savePath);
            MemorySnapshotToCsv.Progress(90.0f, this.progressCallback);
            SaveManagedObjectList(savePath);
            MemorySnapshotToCsv.Progress(95.0f, this.progressCallback);
            SaveMergedMemoryImageInfo(savePath);
            MemorySnapshotToCsv.Progress(100.0f, this.progressCallback);
        }

        public static void Progress(float progress, System.Action<float> pcallback)
        {
            if(pcallback != null) { pcallback(progress); }
        }


        private void SaveNativeObjects(string origin)
        {
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("address").AppendColumn("size").AppendColumn("name").AppendColumn("typeName").AppendColumn("instanceId");
            csvGenerator.AppendColumn("rootReferenceId").AppendColumn("rootAreaName").AppendColumn("rootObjectName").AppendColumn("rootaccumulatedSize");
            csvGenerator.NextRow();
            foreach (var entry in cacheSnapshot.nativeObjects)
            {
                csvGenerator.AppendColumn(cacheSnapshot.GetAddressStr( entry.address));
                csvGenerator.AppendColumn(entry.size);
                csvGenerator.AppendColumn(entry.objectName);
                // type
                csvGenerator.AppendColumn(cacheSnapshot.nativeTypes[entry.nativeTypeArrayIndex].name);
                csvGenerator.AppendColumn(entry.instanceId);
                // root Reference
                csvGenerator.AppendColumn(entry.rootReferenceId);
                MemorySnapshotCacheData.RootReference rootReference = null;
                if (cacheSnapshot.rootReferences.TryGetValue(entry.rootReferenceId, out rootReference))
                {
                    csvGenerator.AppendColumn(rootReference.areaName).AppendColumn(rootReference.objectName).AppendColumn(rootReference.accumulatedSize);
                }
                else
                {
                    csvGenerator.AppendColumn("").AppendColumn("").AppendColumn("");
                }
                csvGenerator.NextRow();
            }

            System.IO.File.WriteAllText(origin + "-nativeObjects.csv", csvGenerator.ToString());
        }
        private void SaveNativeAllocation(string origin)
        {
            // nativeAllocations
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("address").AppendColumn("Size").AppendColumn("overhead size").AppendColumn("padding size");
            csvGenerator.AppendColumn("regionId").AppendColumn("regionName").AppendColumn("regionBase").AppendColumn("regionSize");
            csvGenerator.AppendColumn("rootReferenceId").AppendColumn("rootAreaName").AppendColumn("rootObjectName").AppendColumn("rootaccumulatedSize");
            csvGenerator.NextRow();
            foreach (var entry in cacheSnapshot.nativeAllocations)
            {
                csvGenerator.AppendColumn(cacheSnapshot.GetAddressStr( entry.address));
                csvGenerator.AppendColumn(entry.size);
                csvGenerator.AppendColumn(entry.overhead);
                csvGenerator.AppendColumn(entry.paddingSize);

                // region
                csvGenerator.AppendColumn(entry.memoryRegionIndex);
                MemorySnapshotCacheData.MemoryRegion region = null;
                region = cacheSnapshot.memoryRegions[entry.memoryRegionIndex];
                if (region != null)
                {
                    csvGenerator.AppendColumn(region.memoryRegionName).AppendColumn(cacheSnapshot.GetAddressStr(region.addressBase)).AppendColumn(region.addressSize);
                }
                else
                {
                    csvGenerator.AppendColumn("").AppendColumn("").AppendColumn("");
                }
                // root reference
                csvGenerator.AppendColumn(entry.rootReferenceId);
                MemorySnapshotCacheData.RootReference rootReference = null;
                if (cacheSnapshot.rootReferences.TryGetValue(entry.rootReferenceId, out rootReference))
                {
                    csvGenerator.AppendColumn(rootReference.areaName).AppendColumn(rootReference.objectName).AppendColumn(rootReference.accumulatedSize);
                }
                else
                {
                    csvGenerator.AppendColumn("").AppendColumn("").AppendColumn("");
                }
                csvGenerator.NextRow();
            }
            System.IO.File.WriteAllText(origin + "-nativeAllocations.csv", csvGenerator.ToString());

        }


        private void SaveManagedAllocations(string origin)
        {
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("type").AppendColumn("address").AppendColumn("size");
            csvGenerator.NextRow();
            foreach (var entry in cacheSnapshot.managedHeap)
            {
                csvGenerator.AppendColumn("Heap").
                    AppendColumn(cacheSnapshot.GetAddressStr(entry.startAddress)).AppendColumn(entry.bytes.Length);
                csvGenerator.NextRow();
            }
            foreach (var entry in cacheSnapshot.managedStack)
            {
                csvGenerator.AppendColumn("Stack").
                    AppendColumn(cacheSnapshot.GetAddressStr(entry.startAddress)).AppendColumn(entry.bytes.Length);
                csvGenerator.NextRow();
            }
            System.IO.File.WriteAllText(origin + "-managedAllocation.csv", csvGenerator.ToString());
        }

        private void SaveManagedTypeList(string origin)
        {
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            var copyList = new List<MemorySnapshotCacheData.ManagedType>(cacheSnapshot.managedTypes);
            copyList.Sort((a, b) =>
            {
                int val = string.Compare(a.assembly, b.assembly);
                if (val != 0) { return val; }
                return string.Compare(a.typeDescriptionName, b.typeDescriptionName);
            });

            csvGenerator.AppendColumn("assembly").
                AppendColumn("typeName").
                AppendColumn("size").
                AppendColumn("staticFieldSize").AppendColumn("flags").AppendColumn("").AppendColumn("field");
            csvGenerator.NextRow();

            int cnt = 0;
            foreach (var entry in copyList)
            {
                csvGenerator.AppendColumn(entry.assembly).
                    AppendColumn(entry.typeDescriptionName).
                    AppendColumn(entry.size);
                if(entry.staticFieldBytes != null)
                {
                    csvGenerator.AppendColumn(entry.staticFieldBytes.Length);
                }
                else
                {
                    csvGenerator.AppendColumn("");
                }
                csvGenerator.AppendColumn(entry.flags.ToString());

                // fields
                csvGenerator.AppendColumn("");
                if (entry.staticFieldInfos != null)
                {
                    foreach(var staticFiled in entry.staticFieldInfos)
                    {
                        csvGenerator.AppendColumn("static " + staticFiled.fieldType.typeDescriptionName);
                        csvGenerator.AppendColumn(staticFiled.fieldDescriptionName);
                        csvGenerator.AppendColumn(staticFiled.offset);
                    }
                }
                if( entry.instanceFieldInfos != null)
                {
                    foreach( var field in entry.instanceFieldInfos)
                    {
                        csvGenerator.AppendColumn(field.fieldType.typeDescriptionName);
                        csvGenerator.AppendColumn(field.fieldDescriptionName);
                        csvGenerator.AppendColumn(field.offset);
                    }
                }
                csvGenerator.NextRow();
            }

            System.IO.File.WriteAllText(origin + "-managedTypes.csv", csvGenerator.ToString());
        }


        private void SaveManagedObjectList(string origin)
        {
            CsvStringGenerator csvGenerator = new CsvStringGenerator();

            csvGenerator.NextRow();

            foreach( var managedObject in this.cacheSnapshot.managedObjectByAddr.Values)
            {
                if(managedObject.address == 0) { continue; }
                int offset = managedObject.offset;
                csvGenerator.AppendColumn(cacheSnapshot.GetAddressStr(managedObject.address));
                if (managedObject.typeInfo != null)
                {
                    csvGenerator.AppendColumn(managedObject.typeInfo.typeDescriptionName);
                }
                csvGenerator.NextRow();
            }

            System.IO.File.WriteAllText(origin + "-managedObjects.csv", csvGenerator.ToString());
        }

        private void SaveMergedMemoryImageInfo()
        {
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("address").AppendColumn("size");
        }

        private void SaveFieldInfo(string origin)
        {
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("type").AppendColumn("field").AppendColumn("isStatic").AppendColumn("offset");
            csvGenerator.NextRow();

            foreach (var entry in cacheSnapshot.managedFieldInfos)
            {
                var typeManagedType = cacheSnapshot.managedTypeByTypeIndex[entry.typeIndex];

                csvGenerator.AppendColumn(typeManagedType.typeDescriptionName);
                csvGenerator.AppendColumn(entry.fieldDescriptionName).
                    AppendColumn(entry.isStatic).AppendColumn(entry.offset);
                csvGenerator.NextRow();
            }
            System.IO.File.WriteAllText(origin + "-managedFieldInfo.csv", csvGenerator.ToString());
        }

        private void SaveMergedMemoryImageInfo(string origin)
        {
            MemorySnapshotMap memorySnapshotMap = new MemorySnapshotMap(cacheSnapshot);
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("idx").AppendColumn("parentIdx").AppendColumn("StartAddress").AppendColumn("EndAddress").AppendColumn("Size").AppendColumn("type");
            csvGenerator.NextRow();
            ulong lastEnd = 0;
            int lastIdx = 0;
            int idx = 0;
            foreach( var memory in memorySnapshotMap.memoryLists)
            {
                if( memory.memorySize == 0) { continue; }

                csvGenerator.AppendColumn(idx);

                if (lastEnd > memory.addressStart)
                {
                    csvGenerator.AppendColumn(lastIdx);
                }
                else
                {
                    csvGenerator.AppendColumn(-1);
                }
                if (lastEnd < memory.addressEnd)
                {
                    lastIdx = idx;
                    lastEnd = memory.addressEnd;
                }

                csvGenerator.AppendColumn(cacheSnapshot.GetAddressStr(memory.addressStart ) ).
                    AppendColumn(cacheSnapshot.GetAddressStr(memory.addressEnd) ).
                    AppendColumn(memory.memorySize).AppendColumn(memory.relatedObject.GetType().Name);
                csvGenerator.NextRow();
                ++idx;
            }

            System.IO.File.WriteAllText(origin + "-memoryImage.csv", csvGenerator.ToString());
        }




    }
}