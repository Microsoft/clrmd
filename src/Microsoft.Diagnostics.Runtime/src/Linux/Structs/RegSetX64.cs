﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct RegSetX64
    {
        public readonly ulong R15;
        public readonly ulong R14;
        public readonly ulong R13;
        public readonly ulong R12;
        public readonly ulong Rbp;
        public readonly ulong Rbx;
        public readonly ulong R11;
        public readonly ulong R10;
        public readonly ulong R8;
        public readonly ulong R9;
        public readonly ulong Rax;
        public readonly ulong Rcx;
        public readonly ulong Rdx;
        public readonly ulong Rsi;
        public readonly ulong Rdi;
        public readonly ulong OrigRax;
        public readonly ulong Rip;
        public readonly ulong CS;
        public readonly ulong EFlags;
        public readonly ulong Rsp;
        public readonly ulong SS;
        public readonly ulong FSBase;
        public readonly ulong GSBase;
        public readonly ulong DS;
        public readonly ulong ES;
        public readonly ulong FS;
        public readonly ulong GS;

        public bool CopyContext(Span<byte> buffer)
        {
            if (buffer.Length < AMD64Context.Size)
                return false;

            ref AMD64Context context = ref Unsafe.As<byte, AMD64Context>(ref MemoryMarshal.GetReference(buffer));

            context.ContextFlags = AMD64Context.ContextControl | AMD64Context.ContextInteger | AMD64Context.ContextSegments;
            context.R15 = R15;
            context.R14 = R14;
            context.R13 = R13;
            context.R12 = R12;
            context.Rbp = Rbp;
            context.Rbx = Rbx;
            context.R11 = R11;
            context.R10 = R10;
            context.R9 = R9;
            context.R8 = R8;
            context.Rax = Rax;
            context.Rcx = Rcx;
            context.Rdx = Rdx;
            context.Rsi = Rsi;
            context.Rdi = Rdi;
            context.Rip = Rip;
            context.Rsp = Rsp;
            context.Cs = (ushort)CS;
            context.Ds = (ushort)DS;
            context.Ss = (ushort)SS;
            context.Fs = (ushort)FS;
            context.Gs = (ushort)GS;

            return true;
        }
    }
}