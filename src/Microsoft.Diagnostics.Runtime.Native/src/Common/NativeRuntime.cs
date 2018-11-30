﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Native.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Native
{
    public class NativeRuntime
    {
        private NativeHeap _heap;
        private NativeThread[] _threads;
        private readonly IDataReader _dataReader;
        private readonly ModuleInfo[] _modules;

        public ModuleInfo Module { get; }
        public DataTarget DataTarget { get; }
        internal SosNative SOSNative { get; }

        public NativeHeap Heap
        {
            get
            {
                if (_heap == null)
                    _heap = new NativeHeap(this);

                return _heap;
            }
        }

        public NativeRuntime(ModuleInfo module, DataTarget dt, DacLibrary lib)
        {
            Module = module;
            DataTarget = dt;
            SOSNative = lib.GetInterface<SosNative>(ref SosNative.IID_ISOSNative);
            _dataReader = dt.DataReader;
            _modules = dt.EnumerateModules().ToArray();

            if (SOSNative == null)
                throw new ClrDiagnosticsException("Unsupported dac version.", ClrDiagnosticsException.HR.DacError);
        }

        public IReadOnlyList<NativeThread> Threads
        {
            get
            {
                if (_threads == null)
                    InitThreads();

                return _threads;
            }
        }

        private void InitThreads()
        {
            var threads = new List<NativeThread>();

            if (SOSNative.GetThreadStoreData(out var data))
            {
                var seen = new HashSet<ulong> {0};

                var addr = data.FirstThread;
                while (seen.Add(addr))
                {
                    if (!SOSNative.GetThreadData(addr, out var threadData))
                        break;

                    var thread = new NativeThread(this, ref threadData, addr, addr == data.FinalizerThread);
                    threads.Add(thread);
                    addr = threadData.NextThread;
                }
            }

            _threads = threads.ToArray();
        }

        /*

        internal unsafe IList<ClrRoot> EnumerateStackRoots(ClrThread thread)
        {
            int contextSize;

            var plat = _dataReader.GetArchitecture();
            if (plat == Architecture.Amd64)
                contextSize = 0x4d0;
            else if (plat == Architecture.X86)
                contextSize = 0x2d0;
            else if (plat == Architecture.Arm)
                contextSize = 0x1a0;
            else
                throw new InvalidOperationException("Unexpected architecture.");

            var context = new byte[contextSize];
            _dataReader.GetThreadContext(thread.OSThreadId, 0, (uint)contextSize, context);

            var walker = new NativeStackRootWalker(_heap.Value, GetRhAppDomain(), thread);
            THREADROOTCALLBACK del = new THREADROOTCALLBACK(walker.Callback);
            IntPtr callback = Marshal.GetFunctionPointerForDelegate(del);

            fixed (byte* b = &context[0])
            {
                IntPtr ctx = new IntPtr(b);
                SosNative.TraverseStackRoots(thread.Address, ctx, contextSize, callback, IntPtr.Zero);
            }

            GC.KeepAlive(del);

            return walker.Roots;
        }

        internal IList<ClrRoot> EnumerateStaticRoots(bool resolveStatics)
        {
            var walker = new NativeStaticRootWalker(this, resolveStatics);
            STATICROOTCALLBACK del = new STATICROOTCALLBACK(walker.Callback);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(del);
            SosNative.TraverseStaticRoots(ptr);
            GC.KeepAlive(del);

            return walker.Roots;
        }

        internal IEnumerable<ClrRoot> EnumerateHandleRoots()
        {
            var walker = new NativeHandleRootWalker(this, _dacRawVersion != 10);
            HANDLECALLBACK callback = new HANDLECALLBACK(walker.RootCallback);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(callback);
            SosNative.TraverseHandleTable(ptr, IntPtr.Zero);
            GC.KeepAlive(callback);

            return walker.Roots;
        }

        public IEnumerable<ClrException> EnumerateSerializedExceptions()
        {
            if (_sosNativeSerializedExceptionSupport == null)
            {
                return new ClrException[0];
            }

            var exceptionById = new Dictionary<ulong, NativeException>();
            ISerializedExceptionEnumerator serializedExceptionEnumerator = _sosNativeSerializedExceptionSupport.GetSerializedExceptions();
            while (serializedExceptionEnumerator.HasNext())
            {
                var serializedException = serializedExceptionEnumerator.Next();

                //build the stack frames
                IList<ClrStackFrame> stackFrames = new List<ClrStackFrame>();
                var serializedStackFrameEnumerator = serializedException.StackFrames;
                while (serializedStackFrameEnumerator.HasNext())
                {
                    var serializedStackFrame = serializedStackFrameEnumerator.Next();

                    NativeModule nativeModule = _heap.GetModuleFromAddress(serializedStackFrame.IP);
                    string symbolName = null;
                    if (nativeModule != null)
                    {
                        if (nativeModule.Pdb != null)
                        {
                            try
                            {
                                var resolver = DataTarget.SymbolProvider.GetSymbolResolver(nativeModule.Pdb.FileName, nativeModule.Pdb.Guid, nativeModule.Pdb.Revision);

                                if (resolver != null)
                                {
                                    symbolName = resolver.GetSymbolNameByRVA((uint)(serializedStackFrame.IP - nativeModule.ImageBase));
                                }
                                else
                                {
                                    Trace.WriteLine(
                                        $"Unable to find symbol resolver for PDB [Filename:{nativeModule.Pdb.FileName}, GUID:{nativeModule.Pdb.Guid}, Revision:{nativeModule.Pdb.Revision}]");
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine(
                                    $"Error in finding the symbol resolver for PDB [Filename:{nativeModule.Pdb.FileName}, GUID:{nativeModule.Pdb.Guid}, Revision:{nativeModule.Pdb.Revision}]: {e.Message}");
                                Trace.WriteLine("Check previous traces for additional information");
                            }
                        }
                        else
                        {
                            Trace.WriteLine(String.Format("PDB not found for IP {0}, Module={1}", serializedStackFrame.IP, nativeModule.Name));
                        }
                    }
                    else
                    {
                        Trace.WriteLine(String.Format("Unable to resolve module for IP {0}", serializedStackFrame.IP));
                    }

                    stackFrames.Add(new NativeStackFrame(serializedStackFrame.IP, symbolName, nativeModule));
                }

                //create a new exception and populate the fields

                exceptionById.Add(
                    serializedException.ExceptionId,
                    new NativeException(
                        _heap.GetTypeByMethodTable(serializedException.ExceptionEEType),
                        serializedException.ExceptionCCWPtr,
                        serializedException.HResult,
                        serializedException.ThreadId,
                        serializedException.ExceptionId,
                        serializedException.InnerExceptionId,
                        serializedException.NestingLevel,
                        stackFrames));
            }

            var usedAsInnerException = new HashSet<ulong>();

            foreach (var nativeException in exceptionById.Values)
            {
                if (nativeException.InnerExceptionId > 0)
                {
                    if (exceptionById.TryGetValue(nativeException.InnerExceptionId, out var innerException))
                    {
                        nativeException.SetInnerException(innerException);
                        usedAsInnerException.Add(innerException.ExceptionId);
                    }
                }
            }

            return exceptionById.Keys.Except(usedAsInnerException).Select(id => exceptionById[id]).Cast<ClrException>().ToList();
        }
        */
    }
}