﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Memory
{
    /**
     * Thanks To Roman_Ablo @ GuidedHacking
     * https://guidedhacking.com/threads/hyperscan-fast-vast-memory-scanner.9659/
     * i converted to C#
     */
    public class Scanner
    {
        public enum ScanAlignment
        {
            AlignmentByte = 0x1,
            Alignment2Bytes = 0x2,
            Alignment4Bytes = 0x4,
            Alignment8Bytes = 0x8
        }
        public enum ScanType
        {
            TypeExact = 0x00E,
            //TypeSmaller = 0x0E,
            //TypeBigger = 0x000E,
            //TypeDifferent = 0x0000A,
            //TypeAll = 0xABCDEF
        }

        private readonly Memory _memory;

        public Scanner(Memory mem)
        {
            _memory = mem;
        }

        private Task<List<IntPtr>> ScanMemory(long scanValue, ScanAlignment scanAlign, ScanType scanType, IntPtr beginAddress, IntPtr endAddress)
        {
            const long chunk = 0x10000;

            return Task.Run(() => 
            {
                var ret = new List<IntPtr>();

                // Get Memory Regions
                var currentAddress = beginAddress;
                var memRegions = new List<KeyValuePair<IntPtr, Win32.MemoryBasicInformation>>();
                uint mbiSize = (uint)Marshal.SizeOf<Win32.MemoryBasicInformation>();

                while (Win32.VirtualQueryEx(_memory.ProcessHandle, currentAddress, out var info, mbiSize) != 0 &&
                       beginAddress.ToInt64() < endAddress.ToInt64() + chunk)
                {

                    if ((info.State & (int)Win32.MemoryState.MemCommit) != 0 &&
                        (info.Protect & (int)Win32.MemoryProtection.PageNoAccess) == 0)
                        memRegions.Add(new KeyValuePair<IntPtr, Win32.MemoryBasicInformation>(currentAddress, info));

                    currentAddress = (IntPtr)(info.BaseAddress + info.RegionSize);
                }

                // Scan
                var lockObj = new object();
                var bytesToCmp = BitConverter.GetBytes(scanValue);
                var ret1 = ret;
                Parallel.ForEach(memRegions, (memRegion, loopState) =>
                {
                    var (address, memInfo) = memRegion;

                    bool read = _memory.ReadBytes(address, (long)memInfo.RegionSize, out var memoryBlock);
                    if (!read)
                        return;

                    for (int scanIndex = 0; scanIndex < memoryBlock.Length; scanIndex++)
                    {
                        int k = 0;
                        switch (scanType)
                        {
                            case ScanType.TypeExact:
                                int index = scanIndex * (int)scanAlign;

                                if (index > memoryBlock.Length - bytesToCmp.Length)
                                    break;

                                for (int j = 0; j < bytesToCmp.Length; j++)
                                {
                                    if (memoryBlock[index + j] != bytesToCmp[k]) continue;

                                    // Did we find it?
                                    if (++k != bytesToCmp.Length) continue;

                                    var curAddress = address + scanIndex * (int)scanAlign;

                                    lock (lockObj)
                                        ret1.Add(curAddress);
                                }
                                break;
                        }
                    }
                });

                ret = ret.OrderBy(a => a.ToInt64()).ToList();
                return ret;
            });
        }
        private Task<List<IntPtr>> ScanModules(long scanValue, ScanAlignment scanAlign, ScanType scanType)
        {
            return Task.Run(async () => 
            {
                var ret = new List<IntPtr>();
                if (!_memory.IsValidProcess())
                    return ret;

                var modules = _memory.GetModuleList();

                foreach (var processModule in modules)
                {
                    ret.AddRange(await ScanMemory(
                        scanValue,
                        scanAlign,
                        scanType,
                        processModule.BaseAddress,
                        processModule.BaseAddress + processModule.ModuleMemorySize));
                }

                return ret;
            });
        }

        public Task<List<IntPtr>> Scan(long scanValue, ScanAlignment scanAlign, ScanType scanType, IntPtr beginAddress, IntPtr endAddress)
        {
            return ScanMemory(scanValue, scanAlign, scanType, beginAddress, endAddress);
        }
        public Task<List<IntPtr>> Scan(long scanValue, ScanAlignment scanAlign, ScanType scanType)
        {
            return Scan(scanValue, scanAlign, scanType, new IntPtr(0x0), new IntPtr(_memory.Is64Bit ? 0x7fffffffffff : 0x7fffffff));
        }
    }
}
