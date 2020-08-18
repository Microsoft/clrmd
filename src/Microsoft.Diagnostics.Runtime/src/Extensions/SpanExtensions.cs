﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
#if !NETCOREAPP
using System.Buffers;
using System.IO;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !NETCOREAPP
using System.Text;
#endif

namespace Microsoft.Diagnostics.Runtime
{
    internal static class SpanExtensions
    {
#if !NETCOREAPP
        public static int Read(this Stream stream, Span<byte> span)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(span.Length);
            try
            {
                int numRead = stream.Read(buffer, 0, span.Length);
                new ReadOnlySpan<byte>(buffer, 0, numRead).CopyTo(span);
                return numRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
#endif

#if !NETCOREAPP
        public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
                return string.Empty;

            fixed (byte* bytesPtr = bytes)
            {
                return encoding.GetString(bytesPtr, bytes.Length);
            }
        }
#endif

        public static unsafe ulong AsPointer(this Span<byte> span, int offset = 0)
        {
            if (offset > 0)
                span = span.Slice(offset);

            DebugOnly.Assert(span.Length >= IntPtr.Size);
            DebugOnly.Assert(unchecked((int)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span))) % IntPtr.Size == 0);
            return IntPtr.Size == 4
                ? Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(span))
                : Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(span));
        }

        public static unsafe int AsInt32(this Span<byte> span)
        {
            DebugOnly.Assert(span.Length >= sizeof(int));
            DebugOnly.Assert(unchecked((int)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span))) % sizeof(int) == 0);
            return Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(span));
        }

        public static unsafe uint AsUInt32(this Span<byte> span)
        {
            DebugOnly.Assert(span.Length >= sizeof(uint));
            DebugOnly.Assert(unchecked((int)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span))) % sizeof(uint) == 0);
            return Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(span));
        }
    }
}
