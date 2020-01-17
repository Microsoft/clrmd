﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("716d14c9-119b-4ba5-af1f-0890e672416a")]
    public interface IDebugAdvanced2 : IDebugAdvanced
    {
        /* IDebugAdvanced */
        [PreserveSig]
        new int GetThreadContext(
            IntPtr Context,
            int ContextSize);

        [PreserveSig]
        new int SetThreadContext(
            IntPtr Context,
            int ContextSize);

        /* IDebugAdvanced2 */

        [PreserveSig]
        int Request(
            [In] DEBUG_REQUEST Request,
            [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            byte[] inBuffer,
            int InBufferSize,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
            byte[] outBuffer,
            int OutBufferSize,
            out int OutSize);

        [PreserveSig]
        int GetSourceFileInformation(
            [In] DEBUG_SRCFILE Which,
            [In][MarshalAs(UnmanagedType.LPStr)] string SourceFile,
            ulong Arg64,
            uint Arg32,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]
            byte[] buffer,
            int BufferSize,
            out int InfoSize);

        [PreserveSig]
        int FindSourceFileAndToken(
            uint StartElement,
            ulong ModAddr,
            [In][MarshalAs(UnmanagedType.LPStr)] string File,
            [In] DEBUG_FIND_SOURCE Flags,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]
            byte[] buffer,
            int FileTokenSize,
            out int FoundElement,
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out int FoundSize);

        [PreserveSig]
        int GetSymbolInformation(
            [In] DEBUG_SYMINFO Which,
            ulong Arg64,
            uint Arg32,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
            byte[] buffer,
            int BufferSize,
            out int InfoSize,
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder StringBuffer,
            int StringBufferSize,
            out int StringSize);

        [PreserveSig]
        int GetSystemObjectInformation(
            [In] DEBUG_SYSOBJINFO Which,
            ulong Arg64,
            uint Arg32,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
            byte[] buffer,
            int BufferSize,
            out int InfoSize);
    }
}