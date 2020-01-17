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
    [Guid("27fe5639-8407-4f47-8364-ee118fb08ac8")]
    public interface IDebugClient
    {
        /* IDebugClient */

        [PreserveSig]
        int AttachKernel(
            [In] DEBUG_ATTACH Flags,
            [In][MarshalAs(UnmanagedType.LPStr)] string ConnectOptions);

        [PreserveSig]
        int GetKernelConnectionOptions(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint OptionsSize);

        [PreserveSig]
        int SetKernelConnectionOptions(
            [In][MarshalAs(UnmanagedType.LPStr)] string Options);

        [PreserveSig]
        int StartProcessServer(
            [In] DEBUG_CLASS Flags,
            [In][MarshalAs(UnmanagedType.LPStr)] string Options,
            IntPtr Reserved);

        [PreserveSig]
        int ConnectProcessServer(
            [In][MarshalAs(UnmanagedType.LPStr)] string RemoteOptions,
            out ulong Server);

        [PreserveSig]
        int DisconnectProcessServer(
            ulong Server);

        [PreserveSig]
        int GetRunningProcessSystemIds(
            ulong Server,
            [Out][MarshalAs(UnmanagedType.LPArray)]
            uint[] Ids,
            uint Count,
            out uint ActualCount);

        [PreserveSig]
        int GetRunningProcessSystemIdByExecutableName(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string ExeName,
            [In] DEBUG_GET_PROC Flags,
            out uint Id);

        [PreserveSig]
        int GetRunningProcessDescription(
            ulong Server,
            uint SystemId,
            [In] DEBUG_PROC_DESC Flags,
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder ExeName,
            int ExeNameSize,
            out uint ActualExeNameSize,
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Description,
            int DescriptionSize,
            out uint ActualDescriptionSize);

        [PreserveSig]
        int AttachProcess(
            ulong Server,
            uint ProcessID,
            [In] DEBUG_ATTACH AttachFlags);

        [PreserveSig]
        int CreateProcess(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine,
            [In] DEBUG_CREATE_PROCESS Flags);

        [PreserveSig]
        int CreateProcessAndAttach(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine,
            [In] DEBUG_CREATE_PROCESS Flags,
            uint ProcessId,
            [In] DEBUG_ATTACH AttachFlags);

        [PreserveSig]
        int GetProcessOptions(
            out DEBUG_PROCESS Options);

        [PreserveSig]
        int AddProcessOptions(
            [In] DEBUG_PROCESS Options);

        [PreserveSig]
        int RemoveProcessOptions(
            [In] DEBUG_PROCESS Options);

        [PreserveSig]
        int SetProcessOptions(
            [In] DEBUG_PROCESS Options);

        [PreserveSig]
        int OpenDumpFile(
            [In][MarshalAs(UnmanagedType.LPStr)] string DumpFile);

        [PreserveSig]
        int WriteDumpFile(
            [In][MarshalAs(UnmanagedType.LPStr)] string DumpFile,
            [In] DEBUG_DUMP Qualifier);

        [PreserveSig]
        int ConnectSession(
            [In] DEBUG_CONNECT_SESSION Flags,
            uint HistoryLimit);

        [PreserveSig]
        int StartServer(
            [In][MarshalAs(UnmanagedType.LPStr)] string Options);

        [PreserveSig]
        int OutputServer(
            [In] DEBUG_OUTCTL OutputControl,
            [In][MarshalAs(UnmanagedType.LPStr)] string Machine,
            [In] DEBUG_SERVERS Flags);

        [PreserveSig]
        int TerminateProcesses();

        [PreserveSig]
        int DetachProcesses();

        [PreserveSig]
        int EndSession(
            [In] DEBUG_END Flags);

        [PreserveSig]
        int GetExitCode(
            out uint Code);

        [PreserveSig]
        int DispatchCallbacks(
            uint Timeout);

        [PreserveSig]
        int ExitDispatch(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugClient Client);

        [PreserveSig]
        int CreateClient(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out IDebugClient Client);

        [PreserveSig]
        int GetInputCallbacks(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out IDebugInputCallbacks Callbacks);

        [PreserveSig]
        int SetInputCallbacks(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugInputCallbacks Callbacks);

        /* GetOutputCallbacks could a conversion thunk from the debugger engine so we can't specify a specific interface */

        [PreserveSig]
        int GetOutputCallbacks(
            out IDebugOutputCallbacks Callbacks);

        /* We may have to pass a debugger engine conversion thunk back in so we can't specify a specific interface */

        [PreserveSig]
        int SetOutputCallbacks(
            [In] IDebugOutputCallbacks Callbacks);

        [PreserveSig]
        int GetOutputMask(
            out DEBUG_OUTPUT Mask);

        [PreserveSig]
        int SetOutputMask(
            [In] DEBUG_OUTPUT Mask);

        [PreserveSig]
        int GetOtherOutputMask(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugClient Client,
            out DEBUG_OUTPUT Mask);

        [PreserveSig]
        int SetOtherOutputMask(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugClient Client,
            [In] DEBUG_OUTPUT Mask);

        [PreserveSig]
        int GetOutputWidth(
            out uint Columns);

        [PreserveSig]
        int SetOutputWidth(
            uint Columns);

        [PreserveSig]
        int GetOutputLinePrefix(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint PrefixSize);

        [PreserveSig]
        int SetOutputLinePrefix(
            [In][MarshalAs(UnmanagedType.LPStr)] string Prefix);

        [PreserveSig]
        int GetIdentity(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint IdentitySize);

        [PreserveSig]
        int OutputIdentity(
            [In] DEBUG_OUTCTL OutputControl,
            uint Flags,
            [In][MarshalAs(UnmanagedType.LPStr)] string Format);

        /* GetEventCallbacks could a conversion thunk from the debugger engine so we can't specify a specific interface */

        [PreserveSig]
        int GetEventCallbacks(
            out IDebugEventCallbacks Callbacks);

        /* We may have to pass a debugger engine conversion thunk back in so we can't specify a specific interface */

        [PreserveSig]
        int SetEventCallbacks(
            [In] IDebugEventCallbacks Callbacks);

        [PreserveSig]
        int FlushCallbacks();
    }
}