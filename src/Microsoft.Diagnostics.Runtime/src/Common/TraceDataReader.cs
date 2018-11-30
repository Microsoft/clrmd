﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Runtime
{
    internal class TraceDataReader : IDataReader
    {
        private readonly IDataReader _reader;
        private readonly StreamWriter _file;

        public TraceDataReader(IDataReader reader)
        {
            _reader = reader;
            _file = File.CreateText("datareader.txt");
            _file.AutoFlush = true;
            _file.WriteLine(reader.GetType().ToString());
        }

        public void Close()
        {
            _file.WriteLine("Close");
            _reader.Close();
        }

        public void Flush()
        {
            _file.WriteLine("Flush");
            _reader.Flush();
        }

        public Architecture GetArchitecture()
        {
            var arch = _reader.GetArchitecture();
            _file.WriteLine("GetArchitecture - {0}", arch);
            return arch;
        }

        public uint GetPointerSize()
        {
            var ptrsize = _reader.GetPointerSize();
            _file.WriteLine("GetPointerSize - {0}", ptrsize);
            return ptrsize;
        }

        public IList<ModuleInfo> EnumerateModules()
        {
            var modules = _reader.EnumerateModules();

            var hash = 0;
            foreach (var module in modules)
                hash ^= module.FileName.ToLower().GetHashCode();

            _file.WriteLine("EnumerateModules - {0} {1:x}", modules.Count, hash);
            return modules;
        }

        public void GetVersionInfo(ulong baseAddress, out VersionInfo version)
        {
            _reader.GetVersionInfo(baseAddress, out version);
            _file.WriteLine("GetVersionInfo - {0:x} {1}", baseAddress, version.ToString());
        }

        public bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
        {
            var result = _reader.ReadMemory(address, buffer, bytesRequested, out bytesRead);

            var sb = new StringBuilder();
            var count = bytesRead > 8 ? 8 : bytesRead;
            for (var i = 0; i < count; ++i)
                sb.Append(buffer[i].ToString("x"));

            _file.WriteLine("ReadMemory {0}- {1:x} {2} {3}", result ? "" : "failed ", address, bytesRead, sb);

            return result;
        }

        public bool ReadMemory(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
        {
            var result = _reader.ReadMemory(address, buffer, bytesRequested, out bytesRead);
            _file.WriteLine("ReadMemory {0}- {1:x} {2}", result ? "" : "failed ", address, bytesRead);
            return result;
        }

        public bool IsMinidump { get; }

        public ulong GetThreadTeb(uint thread)
        {
            var teb = _reader.GetThreadTeb(thread);
            _file.WriteLine("GetThreadTeb - {0:x} {1:x}", thread, teb);
            return teb;
        }

        public IEnumerable<uint> EnumerateAllThreads()
        {
            var threads = new List<uint>(_reader.EnumerateAllThreads());

            var first = true;
            var sb = new StringBuilder();
            foreach (var id in threads)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(id.ToString("x"));
            }

            _file.WriteLine("Threads: {0} {1}", threads.Count, sb);
            return threads;
        }

        public bool VirtualQuery(ulong addr, out VirtualQueryData vq)
        {
            var result = _reader.VirtualQuery(addr, out vq);
            _file.WriteLine("VirtualQuery {0}: {1:x} {2:x} {3}", result ? "" : "failed ", addr, vq.BaseAddress, vq.Size);
            return result;
        }

        public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context)
        {
            var result = _reader.GetThreadContext(threadID, contextFlags, contextSize, context);
            _file.WriteLine("GetThreadContext - {0}", result);
            return result;
        }

        public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context)
        {
            var result = _reader.GetThreadContext(threadID, contextFlags, contextSize, context);
            _file.WriteLine("GetThreadContext - {0}", result);
            return result;
        }

        public ulong ReadPointerUnsafe(ulong addr)
        {
            var result = _reader.ReadPointerUnsafe(addr);
            _file.WriteLine("ReadPointerUnsafe - {0}: {1}", addr, result);
            return result;
        }

        public uint ReadDwordUnsafe(ulong addr)
        {
            var result = _reader.ReadDwordUnsafe(addr);
            _file.WriteLine("ReadDwordUnsafe - {0}: {1}", addr, result);
            return result;
        }
    }
}