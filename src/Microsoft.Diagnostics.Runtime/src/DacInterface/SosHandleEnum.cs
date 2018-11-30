﻿using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.DacInterface
{
    public sealed unsafe class SOSHandleEnum : CallableCOMWrapper
    {
        private static Guid IID_ISOSHandleEnum = new Guid("3E269830-4A2B-4301-8EE2-D6805B29B2FA");

        private readonly Next _next;

        public SOSHandleEnum(DacLibrary library, IntPtr pUnk)
            : base(library.OwningLibrary, ref IID_ISOSHandleEnum, pUnk)
        {
            var vtable = (ISOSHandleEnumVTable*)_vtable;
            InitDelegate(ref _next, vtable->Next);
        }

        public int ReadHandles(HandleData[] handles)
        {
            if (handles == null)
                throw new ArgumentNullException(nameof(handles));

            var hr = _next(Self, handles.Length, handles, out var read);
            return hr >= S_OK ? read : 0;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Next(
            IntPtr self,
            int count,
            [Out][MarshalAs(UnmanagedType.LPArray)]
            HandleData[] stackRefs,
            out int pNeeded);
    }

#pragma warning disable CS0169
#pragma warning disable CS0649

    internal struct ISOSHandleEnumVTable
    {
        private readonly IntPtr Skip;
        private readonly IntPtr Reset;
        private readonly IntPtr GetCount;
        public readonly IntPtr Next;
    }
}