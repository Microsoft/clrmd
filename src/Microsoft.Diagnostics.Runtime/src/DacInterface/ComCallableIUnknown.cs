﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Diagnostics.Runtime.DacInterface
{
    internal unsafe class ComCallableIUnknown : ComHelper
    {
        private readonly GCHandle _handle;
        private int _refCount;

        private readonly Dictionary<Guid, IntPtr> _interfaces = new Dictionary<Guid, IntPtr>();
        private readonly List<Delegate> _delegates = new List<Delegate>();

        public IntPtr IUnknownObject { get; }
        public IUnknownVTable IUnknown => **(IUnknownVTable**)IUnknownObject;

        public ComCallableIUnknown()
        {
            _handle = GCHandle.Alloc(this);

            IUnknownVTable* vtable = (IUnknownVTable*)Marshal.AllocHGlobal(sizeof(IUnknownVTable)).ToPointer();
            QueryInterfaceDelegate qi = QueryInterfaceImpl;
            vtable->QueryInterface = Marshal.GetFunctionPointerForDelegate(qi);
            _delegates.Add(qi);

            AddRefDelegate addRef = new AddRefDelegate(AddRefImpl);
            vtable->AddRef = Marshal.GetFunctionPointerForDelegate(addRef);
            _delegates.Add(addRef);

            ReleaseDelegate release = new ReleaseDelegate(ReleaseImpl);
            vtable->Release = Marshal.GetFunctionPointerForDelegate(release);
            _delegates.Add(release);

            IUnknownObject = Marshal.AllocHGlobal(IntPtr.Size);
            *(void**)IUnknownObject = vtable;

            _interfaces.Add(IUnknownGuid, IUnknownObject);
        }

        public VTableBuilder AddInterface(Guid guid)
        {
            return new VTableBuilder(this, guid);
        }

        internal void RegisterInterface(Guid guid, IntPtr clsPtr, List<Delegate> keepAlive)
        {
            _interfaces.Add(guid, clsPtr);
            _delegates.AddRange(keepAlive);
        }

        private int QueryInterfaceImpl(IntPtr self, ref Guid guid, out IntPtr ptr)
        {
            if (_interfaces.TryGetValue(guid, out IntPtr value))
            {
                Interlocked.Increment(ref _refCount);
                ptr = value;
                return S_OK;
            }

            ptr = IntPtr.Zero;
            return E_NOINTERFACE;
        }

        private int ReleaseImpl(IntPtr self)
        {
            int count = Interlocked.Decrement(ref _refCount);
            if (count <= 0)
            {
                foreach (IntPtr ptr in _interfaces.Values)
                {
                    IntPtr* val = (IntPtr*)ptr;
                    Marshal.FreeHGlobal(*val);
                    Marshal.FreeHGlobal(ptr);
                }

                _handle.Free();
            }

            return count;
        }

        private int AddRefImpl(IntPtr self)
        {
            return Interlocked.Increment(ref _refCount);
        }
    }
}