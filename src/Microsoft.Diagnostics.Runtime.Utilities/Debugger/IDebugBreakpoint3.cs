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
    [Guid("38f5c249-b448-43bb-9835-579d4ec02249")]
    public interface IDebugBreakpoint3 : IDebugBreakpoint2
    {
        /* IDebugBreakpoint */

        [PreserveSig]
        new int GetId(
            out uint Id);

        [PreserveSig]
        new int GetType(
            out DEBUG_BREAKPOINT_TYPE BreakType,
            out uint ProcType);

        //FIX ME!!! Should try and get an enum for this
        [PreserveSig]
        new int GetAdder(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out IDebugClient Adder);

        [PreserveSig]
        new int GetFlags(
            out DEBUG_BREAKPOINT_FLAG Flags);

        [PreserveSig]
        new int AddFlags(
            DEBUG_BREAKPOINT_FLAG Flags);

        [PreserveSig]
        new int RemoveFlags(
            DEBUG_BREAKPOINT_FLAG Flags);

        [PreserveSig]
        new int SetFlags(
            DEBUG_BREAKPOINT_FLAG Flags);

        [PreserveSig]
        new int GetOffset(
            out ulong Offset);

        [PreserveSig]
        new int SetOffset(
            ulong Offset);

        [PreserveSig]
        new int GetDataParameters(
            out uint Size,
            out DEBUG_BREAKPOINT_ACCESS_TYPE AccessType);

        [PreserveSig]
        new int SetDataParameters(
            uint Size,
            DEBUG_BREAKPOINT_ACCESS_TYPE AccessType);

        [PreserveSig]
        new int GetPassCount(
            out uint Count);

        [PreserveSig]
        new int SetPassCount(
            uint Count);

        [PreserveSig]
        new int GetCurrentPassCount(
            out uint Count);

        [PreserveSig]
        new int GetMatchThreadId(
            out uint Id);

        [PreserveSig]
        new int SetMatchThreadId(
            uint Thread);

        [PreserveSig]
        new int GetCommand(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint CommandSize);

        [PreserveSig]
        new int SetCommand(
            [In][MarshalAs(UnmanagedType.LPStr)] string Command);

        [PreserveSig]
        new int GetOffsetExpression(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint ExpressionSize);

        [PreserveSig]
        new int SetOffsetExpression(
            [In][MarshalAs(UnmanagedType.LPStr)] string Expression);

        [PreserveSig]
        new int GetParameters(
            out DEBUG_BREAKPOINT_PARAMETERS Params);

        /* IDebugBreakpoint2 */

        [PreserveSig]
        new int GetCommandWide(
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer,
            int BufferSize,
            out uint CommandSize);

        [PreserveSig]
        new int SetCommandWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string Command);

        [PreserveSig]
        new int GetOffsetExpressionWide(
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer,
            int BufferSize,
            out uint ExpressionSize);

        [PreserveSig]
        new int SetOffsetExpressionWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string Command);

        /* IDebugBreakpoint3 */

        [PreserveSig]
        int GetGuid(out Guid Guid);
    }
}