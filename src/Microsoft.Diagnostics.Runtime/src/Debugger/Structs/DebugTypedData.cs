﻿using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct _DEBUG_TYPED_DATA
    {
        public ulong ModBase;
        public ulong Offset;
        public ulong EngineHandle;
        public ulong Data;
        public uint Size;
        public uint Flags;
        public uint TypeId;
        public uint BaseTypeId;
        public uint Tag;
        public uint Register;
        public fixed ulong Internal[9];
    }
}