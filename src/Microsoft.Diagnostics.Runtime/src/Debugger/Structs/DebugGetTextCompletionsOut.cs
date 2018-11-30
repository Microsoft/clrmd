﻿using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DEBUG_GET_TEXT_COMPLETIONS_OUT
    {
        public DEBUG_GET_TEXT_COMPLETIONS Flags;
        public uint ReplaceIndex;
        public uint MatchCount;
        public uint Reserved1;
        public ulong Reserved2;
        public ulong Reserved3;
    }
}