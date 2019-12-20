using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.Profiling.Memory;
using UnityEditor.Profiling.Memory.Experimental;


namespace UTJ
{

    public class MemorySnapshotToCsv
    {
        private MemorySnapshotCacheData cacheSnapshot;



        public MemorySnapshotToCsv(string filePath)
        {
            cacheSnapshot = new MemorySnapshotCacheData(filePath);
            
            Save(System.IO.Path.GetFileName(filePath));                        
        }
        
        private void Save(string originFile)
        {
            var str = originFile.Remove(originFile.Length - 5);
            SaveNativeObjects(str);
            SaveNativeAllocation(str);
            SaveManagedAllocations(str);
            SaveManagedTypeList(str);
            SaveManagedObjectList(str);

            // debug
            //SaveFieldInfo(str);
        }


        private void SaveNativeObjects(string origin)
        {
            CsvStringGenerator csvGenerator = new CsvStringGenerator();
            csvGenerator.AppendColumn("address").AppendColumn("size").AppendColumn("name").AppendColumn("typeName").AppendColumn("instanceId");
            csvGenerator.AppendColumn("rootReferenceId").AppendColumn("rootAreaName").AppendColumn("rootObjectName").AppendColumn("rootaccumulatedSize");
            csvGenerator.NextRow();
            foreach (var entry in cacheSnapshot.nativeObjects)
            {
                csvGenerator.AppendColumn(string.Format(cacheSnapshot.x16StrFormat, entry.address));
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
                csvGenerator.AppendColumn(string.Format(cacheSnapshot.x16StrFormat, entry.address));
                csvGenerator.AppendColumn(entry.size);
                csvGenerator.AppendColumn(entry.overhead);
                csvGenerator.AppendColumn(entry.paddingSize);

                // region
                csvGenerator.AppendColumn(entry.memoryRegionIndex);
                MemorySnapshotCacheData.MemoryRegion region = null;
                region = cacheSnapshot.memoryRegions[entry.memoryRegionIndex];
                if (region != null)
                {
                    csvGenerator.AppendColumn(region.memoryRegionName).AppendColumn(string.Format(cacheSnapshot.x16StrFormat, region.addressBase)).AppendColumn(region.addressSize);
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
                    AppendColumn(string.Format(cacheSnapshot.x16StrFormat, entry.startAddress)).AppendColumn(entry.bytes.Length);
                csvGenerator.NextRow();
            }
            foreach (var entry in cacheSnapshot.managedStack)
            {
                csvGenerator.AppendColumn("Stack").
                    AppendColumn(string.Format(cacheSnapshot.x16StrFormat, entry.startAddress)).AppendColumn(entry.bytes.Length);
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
                AppendColumn("staticFieldSize").AppendColumn("").AppendColumn("field");
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
                // fields
                csvGenerator.AppendColumn("");
                if (entry.fieldIndices != null)
                {
                    for (int i = 0; i < entry.fieldIndices.Length; ++i)
                    {
                        var field = cacheSnapshot.managedFieldInfos[entry.fieldIndices[i]];
                        if (field.isStatic)
                        {
                            csvGenerator.AppendColumn("static " + cacheSnapshot.managedTypeByTypeIndex[field.typeIndex].typeDescriptionName);
                        }
                        else
                        {
                            csvGenerator.AppendColumn(cacheSnapshot.managedTypeByTypeIndex[field.typeIndex].typeDescriptionName);
                        }
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
            csvGenerator.AppendColumn("address").AppendColumn("type");
            csvGenerator.NextRow();
            foreach (var entry in cacheSnapshot.gcHandles)
            {
                var managedMemory = cacheSnapshot.GetManagedMemory(entry.address);
                csvGenerator.AppendColumn(string.Format(cacheSnapshot.x16StrFormat, entry.address));
                if( managedMemory != null)
                {
                    var typeInfo = cacheSnapshot.GetManagedTypeInfoFromAddr(entry.address);
                    if (typeInfo != null)
                    {
                        csvGenerator.AppendColumn(typeInfo.typeDescriptionName);
                    }
                }
                csvGenerator.NextRow();
            }

            // get From static field
            foreach(var entry in cacheSnapshot.managedTypes)
            {
                int idx = entry.typeIndex;
                if(cacheSnapshot.managedTypeByTypeIndex[idx].fieldIndices == null){ continue; }
//                managedTypeByTypeIndex[idx].staticFieldBytes;
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
        
        


    }
}