﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.MacOS
{
    internal sealed class MacOSProcessDataReader : CommonMemoryReader, IDataReader, IDisposable
    {
        private readonly int _task;

        private List<MemoryRegion> _memoryRegions;

        private bool _suspended;
        private bool _disposed;

        public MacOSProcessDataReader(int processId, bool suspend)
        {
            int status = Native.kill(processId, 0);
            if (status < 0 && Marshal.GetLastWin32Error() != Native.EPERM)
                throw new ArgumentException("The process is not running");

            ProcessId = (uint)processId;
            _memoryRegions = LoadMemoryRegions();

            int kr = Native.task_for_pid(Native.mach_task_self(), processId, out int task);
            if (kr != 0)
                throw new ClrDiagnosticsException($"task_for_pid failed with status code 0x{kr:x}");

            _task = task;

            if (suspend)
            {
                status = Native.ptrace(Native.PT_ATTACH, processId);

                if (status >= 0)
                    status = Native.waitpid(processId, IntPtr.Zero, 0);

                if (status < 0)
                {
                    int errno = Marshal.GetLastWin32Error();
                    throw new ClrDiagnosticsException($"Could not attach to process {processId}, errno: {errno}", errno);
                }

                _suspended = true;
            }
        }

        ~MacOSProcessDataReader() => Dispose(false);

        public string DisplayName => $"pid:{ProcessId:x}";

        public bool IsThreadSafe => false;

        public OSPlatform TargetPlatform => OSPlatform.OSX;

        public Architecture Architecture => Architecture.Amd64;

        public uint ProcessId { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool _)
        {
            if (_disposed)
                return;

            if (_suspended)
            {
                int status = Native.ptrace(Native.PT_DETACH, (int)ProcessId);
                if (status < 0)
                {
                    int errno = Marshal.GetLastWin32Error();
                    throw new ClrDiagnosticsException($"Could not detach from process {ProcessId}, errno: {errno}", errno);
                }

                _suspended = false;
            }

            _disposed = true;
        }

        public void FlushCachedData()
        {
            _memoryRegions = LoadMemoryRegions();
        }

        public IEnumerable<ModuleInfo> EnumerateModules()
        {
            int taskInfoCount = Native.TASK_DYLD_INFO_COUNT;
            int kr = Native.task_info(_task, Native.TASK_DYLD_INFO, out Native.task_dyld_info dyldInfo, ref taskInfoCount);
            if (kr != 0)
                throw new ClrDiagnosticsException();

            Native.dyld_all_image_infos infos = Read<Native.dyld_all_image_infos>(dyldInfo.all_image_info_addr);
            for (uint i = 0; i < infos.infoArrayCount; i++)
            {
                Native.dyld_image_info info = Read<Native.dyld_image_info>(infos.infoArray, i);
                ulong imageAddress = info.imageLoadAddress;
                string imageFilePath = ReadNullTerminatedAscii(info.imageFilePath);
                yield return new ModuleInfo(imageAddress, imageFilePath, true, 0, 0, ImmutableArray<byte>.Empty);
            }

            unsafe T Read<T>(ulong address, uint index = 0)
                where T : unmanaged
            {
                T result;
                if (Native.vm_read_overwrite(_task, address + index * (uint)sizeof(T), sizeof(T), &result, out _) != 0)
                    throw new ClrDiagnosticsException();

                return result;
            }

            unsafe string ReadNullTerminatedAscii(ulong address)
            {
                StringBuilder builder = new StringBuilder(64);
                byte* bytes = stackalloc byte[64];

                bool done = false;
                while (!done && (Native.vm_read_overwrite(_task, address, 64, bytes, out long read) == 0))
                {
                    address += (ulong)read;
                    for (int i = 0; !done && i < read; i++)
                    {
                        if (bytes[i] != 0)
                            builder.Append((char)bytes[i]);
                        else
                            done = true;
                    }
                }

                return builder.ToString();
            }
        }

        public ImmutableArray<byte> GetBuildId(ulong baseAddress) => ImmutableArray<byte>.Empty;

        public bool GetVersionInfo(ulong baseAddress, out VersionInfo version)
        {
            // TODO
            version = default;
            return false;
        }

        public override unsafe int Read(ulong address, Span<byte> buffer)
        {
            DebugOnly.Assert(!buffer.IsEmpty);

            int readable = GetReadableBytesCount(address, buffer.Length);
            if (readable <= 0)
            {
                return 0;
            }

            fixed (byte* ptr = buffer)
            {
                int kr = Native.vm_read_overwrite(_task, address, readable, ptr, out long read);
                if (kr != 0)
                    return 0;

                return (int)read;
            }
        }

        public bool GetThreadContext(uint threadID, uint contextFlags, Span<byte> context)
        {
            // TODO
            return false;
        }

        private int GetReadableBytesCount(ulong address, int bytesRequested)
        {
            if (bytesRequested < 1)
            {
                return 0;
            }

            ulong endAddress = address + (ulong)bytesRequested - 1;
            int bytesReadable = 0;
            ulong prevEndAddr = default;

            int startIndex = -1;
            for (int i = 0; i < _memoryRegions.Count; i++)
            {
                MemoryRegion entry = _memoryRegions[i];
                ulong entryBeginAddr = entry.BeginAddress;
                ulong entryEndAddr = entry.EndAddress;
                if (entryBeginAddr <= address && address < entryEndAddr && entry.IsReadable)
                {
                    int regionSize = (int)(entryEndAddr - address);
                    if (regionSize >= bytesRequested)
                    {
                        return bytesRequested;
                    }

                    startIndex = i;
                    bytesRequested -= regionSize;
                    bytesReadable = regionSize;
                    prevEndAddr = entryEndAddr;
                    break;
                }
            }

            if (startIndex < 0)
            {
                return 0;
            }

            for (int i = startIndex + 1; i < _memoryRegions.Count; i++)
            {
                MemoryRegion entry = _memoryRegions[i];
                ulong entryBeginAddr = entry.BeginAddress;
                ulong entryEndAddr = entry.EndAddress;
                if (entryBeginAddr > endAddress || entryBeginAddr != prevEndAddr || !entry.IsReadable)
                {
                    break;
                }

                int regionSize = (int)(entryEndAddr - entryBeginAddr);
                if (regionSize >= bytesRequested)
                {
                    bytesReadable += bytesRequested;
                    break;
                }

                bytesRequested -= regionSize;
                bytesReadable += regionSize;
                prevEndAddr = entryEndAddr;
            }

            return bytesReadable;
        }

        private List<MemoryRegion> LoadMemoryRegions()
        {
            List<MemoryRegion> result = new List<MemoryRegion>();

            int kr = Native.task_for_pid(Native.mach_task_self(), (int)ProcessId, out int task);
            if (kr != 0)
                throw new ClrDiagnosticsException();

            ulong address = 0;
            int infoCount = Native.VM_REGION_BASIC_INFO_COUNT_64;
            while (true)
            {
                kr = Native.mach_vm_region(task, ref address, out ulong size, Native.VM_REGION_BASIC_INFO_64, out Native.vm_region_basic_info_64 info, ref infoCount, out _);
                if (kr != 0)
                    if (kr != Native.KERN_INVALID_ADDRESS)
                        throw new ClrDiagnosticsException();
                    else
                        break;

                ulong endAddress = address + size;
                result.Add(new MemoryRegion
                {
                    BeginAddress = address,
                    EndAddress = endAddress,
                    Permission = info.protection,
                });

                address = endAddress;
            }

            return result;
        }

        internal static class Native
        {
            internal const int EPERM = 1;

            internal const int KERN_INVALID_ADDRESS = 1;

            internal const int PROT_READ = 0x01;

            internal const int PT_ATTACH = 10; // TODO: deprecated
            internal const int PT_DETACH = 11;

            internal const int TASK_DYLD_INFO = 17;
            internal const int VM_REGION_BASIC_INFO_64 = 9;

            internal static readonly unsafe int TASK_DYLD_INFO_COUNT = sizeof(task_dyld_info) / sizeof(uint);
            internal static readonly unsafe int VM_REGION_BASIC_INFO_COUNT_64 = sizeof(vm_region_basic_info_64) / sizeof(int);

            private const string LibSystem = "libSystem.dylib";

            [DllImport(LibSystem, SetLastError = true)]
            internal static extern int kill(int pid, int sig);

            [DllImport(LibSystem)]
            internal static extern int mach_task_self();

            [DllImport(LibSystem, SetLastError = true)]
            internal static extern int ptrace(int request, int pid, IntPtr addr = default, int data = default);

            [DllImport(LibSystem)]
            internal static extern int task_for_pid(int parent, int pid, out int task);

            [DllImport(LibSystem)]
            internal static extern int task_info(int target_task, uint flavor, out /*int*/task_dyld_info task_info, ref /*uint*/int task_info_count);

            [DllImport(LibSystem)]
            internal static extern unsafe int vm_read_overwrite(int target_task, /*UIntPtr*/ulong address, /*UIntPtr*/long size, /*UIntPtr*/void* data, out /*UIntPtr*/long data_size);

            [DllImport(LibSystem)]
            internal static extern int mach_vm_region(int target_task, ref /*UIntPtr*/ulong address, out /*UIntPtr*/ulong size, int flavor, out /*int*/vm_region_basic_info_64 info, ref /*uint*/int info_count, out int object_name);

            [DllImport(LibSystem)]
            internal static extern int waitpid(int pid, IntPtr status, int options);

            internal readonly struct dyld_all_image_infos
            {
                internal readonly uint version;
                internal readonly uint infoArrayCount;
                internal readonly ulong infoArray;

                // We don't need the rest of this struct so we do not define the rest of the fields.
            }

            internal readonly struct dyld_image_info
            {
                internal readonly ulong imageLoadAddress;
                internal readonly ulong imageFilePath;
                internal readonly ulong imageFileModDate;
            }

            internal readonly struct task_dyld_info
            {
                internal readonly ulong all_image_info_addr;
                internal readonly ulong all_image_info_size;
                internal readonly int all_image_info_format;
            }

            internal readonly struct vm_region_basic_info_64
            {
                internal readonly int protection;
                internal readonly int max_protection;
                internal readonly uint inheritance;
                internal readonly uint shared;
                internal readonly uint reserved;
                internal readonly ulong offset;
                internal readonly int behavior;
                internal readonly ushort user_wired_count;
            }
        }

        internal sealed class MemoryRegion
        {
            public ulong BeginAddress { get; set; }
            public ulong EndAddress { get; set; }
            public int Permission { get; set; }

            public bool IsReadable => (Permission & Native.PROT_READ) != 0;
        }
    }
}
