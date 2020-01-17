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
    [Guid("e3acb9d7-7ec2-4f0c-a0da-e81e0cbbe628")]
    public interface IDebugClient5 : IDebugClient4
    {
        /* IDebugClient */

        [PreserveSig]
        new int AttachKernel(
            [In] DEBUG_ATTACH Flags,
            [In][MarshalAs(UnmanagedType.LPStr)] string ConnectOptions);

        [PreserveSig]
        new int GetKernelConnectionOptions(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint OptionsSize);

        [PreserveSig]
        new int SetKernelConnectionOptions(
            [In][MarshalAs(UnmanagedType.LPStr)] string Options);

        [PreserveSig]
        new int StartProcessServer(
            [In] DEBUG_CLASS Flags,
            [In][MarshalAs(UnmanagedType.LPStr)] string Options,
            IntPtr Reserved);

        [PreserveSig]
        new int ConnectProcessServer(
            [In][MarshalAs(UnmanagedType.LPStr)] string RemoteOptions,
            out ulong Server);

        [PreserveSig]
        new int DisconnectProcessServer(
            ulong Server);

        [PreserveSig]
        new int GetRunningProcessSystemIds(
            ulong Server,
            [Out][MarshalAs(UnmanagedType.LPArray)]
            uint[] Ids,
            uint Count,
            out uint ActualCount);

        [PreserveSig]
        new int GetRunningProcessSystemIdByExecutableName(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string ExeName,
            [In] DEBUG_GET_PROC Flags,
            out uint Id);

        [PreserveSig]
        new int GetRunningProcessDescription(
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
        new int AttachProcess(
            ulong Server,
            uint ProcessID,
            [In] DEBUG_ATTACH AttachFlags);

        [PreserveSig]
        new int CreateProcess(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine,
            [In] DEBUG_CREATE_PROCESS Flags);

        [PreserveSig]
        new int CreateProcessAndAttach(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine,
            [In] DEBUG_CREATE_PROCESS Flags,
            uint ProcessId,
            [In] DEBUG_ATTACH AttachFlags);

        [PreserveSig]
        new int GetProcessOptions(
            out DEBUG_PROCESS Options);

        [PreserveSig]
        new int AddProcessOptions(
            [In] DEBUG_PROCESS Options);

        [PreserveSig]
        new int RemoveProcessOptions(
            [In] DEBUG_PROCESS Options);

        [PreserveSig]
        new int SetProcessOptions(
            [In] DEBUG_PROCESS Options);

        [PreserveSig]
        new int OpenDumpFile(
            [In][MarshalAs(UnmanagedType.LPStr)] string DumpFile);

        [PreserveSig]
        new int WriteDumpFile(
            [In][MarshalAs(UnmanagedType.LPStr)] string DumpFile,
            [In] DEBUG_DUMP Qualifier);

        [PreserveSig]
        new int ConnectSession(
            [In] DEBUG_CONNECT_SESSION Flags,
            uint HistoryLimit);

        [PreserveSig]
        new int StartServer(
            [In][MarshalAs(UnmanagedType.LPStr)] string Options);

        [PreserveSig]
        new int OutputServer(
            [In] DEBUG_OUTCTL OutputControl,
            [In][MarshalAs(UnmanagedType.LPStr)] string Machine,
            [In] DEBUG_SERVERS Flags);

        [PreserveSig]
        new int TerminateProcesses();

        [PreserveSig]
        new int DetachProcesses();

        [PreserveSig]
        new int EndSession(
            [In] DEBUG_END Flags);

        [PreserveSig]
        new int GetExitCode(
            out uint Code);

        [PreserveSig]
        new int DispatchCallbacks(
            uint Timeout);

        [PreserveSig]
        new int ExitDispatch(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugClient Client);

        [PreserveSig]
        new int CreateClient(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out IDebugClient Client);

        [PreserveSig]
        new int GetInputCallbacks(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out IDebugInputCallbacks Callbacks);

        [PreserveSig]
        new int SetInputCallbacks(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugInputCallbacks Callbacks);

        /* GetOutputCallbacks could a conversion thunk from the debugger engine so we can't specify a specific interface */

        [PreserveSig]
        new int GetOutputCallbacks(
            out IDebugOutputCallbacks Callbacks);

        /* We may have to pass a debugger engine conversion thunk back in so we can't specify a specific interface */

        [PreserveSig]
        new int SetOutputCallbacks(
            [In] IDebugOutputCallbacks Callbacks);

        [PreserveSig]
        new int GetOutputMask(
            out DEBUG_OUTPUT Mask);

        [PreserveSig]
        new int SetOutputMask(
            [In] DEBUG_OUTPUT Mask);

        [PreserveSig]
        new int GetOtherOutputMask(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugClient Client,
            out DEBUG_OUTPUT Mask);

        [PreserveSig]
        new int SetOtherOutputMask(
            [In][MarshalAs(UnmanagedType.Interface)]
            IDebugClient Client,
            [In] DEBUG_OUTPUT Mask);

        [PreserveSig]
        new int GetOutputWidth(
            out uint Columns);

        [PreserveSig]
        new int SetOutputWidth(
            uint Columns);

        [PreserveSig]
        new int GetOutputLinePrefix(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint PrefixSize);

        [PreserveSig]
        new int SetOutputLinePrefix(
            [In][MarshalAs(UnmanagedType.LPStr)] string Prefix);

        [PreserveSig]
        new int GetIdentity(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint IdentitySize);

        [PreserveSig]
        new int OutputIdentity(
            [In] DEBUG_OUTCTL OutputControl,
            uint Flags,
            [In][MarshalAs(UnmanagedType.LPStr)] string Format);

        /* GetEventCallbacks could a conversion thunk from the debugger engine so we can't specify a specific interface */

        [PreserveSig]
        new int GetEventCallbacks(
            out IDebugEventCallbacks Callbacks);

        /* We may have to pass a debugger engine conversion thunk back in so we can't specify a specific interface */

        [PreserveSig]
        new int SetEventCallbacks(
            [In] IDebugEventCallbacks Callbacks);

        [PreserveSig]
        new int FlushCallbacks();

        /* IDebugClient2 */

        [PreserveSig]
        new int WriteDumpFile2(
            [In][MarshalAs(UnmanagedType.LPStr)] string DumpFile,
            [In] DEBUG_DUMP Qualifier,
            [In] DEBUG_FORMAT FormatFlags,
            [In][MarshalAs(UnmanagedType.LPStr)] string Comment);

        [PreserveSig]
        new int AddDumpInformationFile(
            [In][MarshalAs(UnmanagedType.LPStr)] string InfoFile,
            [In] DEBUG_DUMP_FILE Type);

        [PreserveSig]
        new int EndProcessServer(
            ulong Server);

        [PreserveSig]
        new int WaitForProcessServerEnd(
            uint Timeout);

        [PreserveSig]
        new int IsKernelDebuggerEnabled();

        [PreserveSig]
        new int TerminateCurrentProcess();

        [PreserveSig]
        new int DetachCurrentProcess();

        [PreserveSig]
        new int AbandonCurrentProcess();

        /* IDebugClient3 */

        [PreserveSig]
        new int GetRunningProcessSystemIdByExecutableNameWide(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPWStr)] string ExeName,
            [In] DEBUG_GET_PROC Flags,
            out uint Id);

        [PreserveSig]
        new int GetRunningProcessDescriptionWide(
            ulong Server,
            uint SystemId,
            [In] DEBUG_PROC_DESC Flags,
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder ExeName,
            int ExeNameSize,
            out uint ActualExeNameSize,
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Description,
            int DescriptionSize,
            out uint ActualDescriptionSize);

        [PreserveSig]
        new int CreateProcessWide(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine,
            [In] DEBUG_CREATE_PROCESS CreateFlags);

        [PreserveSig]
        new int CreateProcessAndAttachWide(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine,
            [In] DEBUG_CREATE_PROCESS CreateFlags,
            uint ProcessId,
            [In] DEBUG_ATTACH AttachFlags);

        /* IDebugClient4 */

        [PreserveSig]
        new int OpenDumpFileWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string FileName,
            ulong FileHandle);

        [PreserveSig]
        new int WriteDumpFileWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string DumpFile,
            ulong FileHandle,
            [In] DEBUG_DUMP Qualifier,
            [In] DEBUG_FORMAT FormatFlags,
            [In][MarshalAs(UnmanagedType.LPWStr)] string Comment);

        [PreserveSig]
        new int AddDumpInformationFileWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string FileName,
            ulong FileHandle,
            [In] DEBUG_DUMP_FILE Type);

        [PreserveSig]
        new int GetNumberDumpFiles(
            out uint Number);

        [PreserveSig]
        new int GetDumpFile(
            uint Index,
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint NameSize,
            out ulong Handle,
            out uint Type);

        [PreserveSig]
        new int GetDumpFileWide(
            uint Index,
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer,
            int BufferSize,
            out uint NameSize,
            out ulong Handle,
            out uint Type);

        /* IDebugClient5 */

        [PreserveSig]
        int AttachKernelWide(
            [In] DEBUG_ATTACH Flags,
            [In][MarshalAs(UnmanagedType.LPWStr)] string ConnectOptions);

        [PreserveSig]
        int GetKernelConnectionOptionsWide(
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer,
            int BufferSize,
            out uint OptionsSize);

        [PreserveSig]
        int SetKernelConnectionOptionsWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string Options);

        [PreserveSig]
        int StartProcessServerWide(
            [In] DEBUG_CLASS Flags,
            [In][MarshalAs(UnmanagedType.LPWStr)] string Options,
            IntPtr Reserved);

        [PreserveSig]
        int ConnectProcessServerWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string RemoteOptions,
            out ulong Server);

        [PreserveSig]
        int StartServerWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string Options);

        [PreserveSig]
        int OutputServersWide(
            [In] DEBUG_OUTCTL OutputControl,
            [In][MarshalAs(UnmanagedType.LPWStr)] string Machine,
            [In] DEBUG_SERVERS Flags);

        /* GetOutputCallbacks could a conversion thunk from the debugger engine so we can't specify a specific interface */

        [PreserveSig]
        int GetOutputCallbacksWide(
            out IDebugOutputCallbacksWide Callbacks);

        /* We may have to pass a debugger engine conversion thunk back in so we can't specify a specific interface */

        [PreserveSig]
        int SetOutputCallbacksWide(
            [In] IDebugOutputCallbacksWide Callbacks);

        [PreserveSig]
        int GetOutputLinePrefixWide(
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer,
            int BufferSize,
            out uint PrefixSize);

        [PreserveSig]
        int SetOutputLinePrefixWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string Prefix);

        [PreserveSig]
        int GetIdentityWide(
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer,
            int BufferSize,
            out uint IdentitySize);

        [PreserveSig]
        int OutputIdentityWide(
            [In] DEBUG_OUTCTL OutputControl,
            uint Flags,
            [In][MarshalAs(UnmanagedType.LPWStr)] string Machine);

        /* GetEventCallbacks could a conversion thunk from the debugger engine so we can't specify a specific interface */

        [PreserveSig]
        int GetEventCallbacksWide(
            out IDebugEventCallbacksWide Callbacks);

        /* We may have to pass a debugger engine conversion thunk back in so we can't specify a specific interface */

        [PreserveSig]
        int SetEventCallbacksWide(
            [In] IDebugEventCallbacksWide Callbacks);

        [PreserveSig]
        int CreateProcess2(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine,
            [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer,
            uint OptionsBufferSize,
            [In][MarshalAs(UnmanagedType.LPStr)] string InitialDirectory,
            [In][MarshalAs(UnmanagedType.LPStr)] string Environment);

        [PreserveSig]
        int CreateProcess2Wide(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine,
            [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer,
            uint OptionsBufferSize,
            [In][MarshalAs(UnmanagedType.LPWStr)] string InitialDirectory,
            [In][MarshalAs(UnmanagedType.LPWStr)] string Environment);

        [PreserveSig]
        int CreateProcessAndAttach2(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine,
            [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer,
            uint OptionsBufferSize,
            [In][MarshalAs(UnmanagedType.LPStr)] string InitialDirectory,
            [In][MarshalAs(UnmanagedType.LPStr)] string Environment,
            uint ProcessId,
            [In] DEBUG_ATTACH AttachFlags);

        [PreserveSig]
        int CreateProcessAndAttach2Wide(
            ulong Server,
            [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine,
            [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer,
            uint OptionsBufferSize,
            [In][MarshalAs(UnmanagedType.LPWStr)] string InitialDirectory,
            [In][MarshalAs(UnmanagedType.LPWStr)] string Environment,
            uint ProcessId,
            [In] DEBUG_ATTACH AttachFlags);

        [PreserveSig]
        int PushOutputLinePrefix(
            [In][MarshalAs(UnmanagedType.LPStr)] string NewPrefix,
            out ulong Handle);

        [PreserveSig]
        int PushOutputLinePrefixWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string NewPrefix,
            out ulong Handle);

        [PreserveSig]
        int PopOutputLinePrefix(
            ulong Handle);

        [PreserveSig]
        int GetNumberInputCallbacks(
            out uint Count);

        [PreserveSig]
        int GetNumberOutputCallbacks(
            out uint Count);

        [PreserveSig]
        int GetNumberEventCallbacks(
            [In] DEBUG_EVENT Flags,
            out uint Count);

        [PreserveSig]
        int GetQuitLockString(
            [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer,
            int BufferSize,
            out uint StringSize);

        [PreserveSig]
        int SetQuitLockString(
            [In][MarshalAs(UnmanagedType.LPStr)] string LockString);

        [PreserveSig]
        int GetQuitLockStringWide(
            [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer,
            int BufferSize,
            out uint StringSize);

        [PreserveSig]
        int SetQuitLockStringWide(
            [In][MarshalAs(UnmanagedType.LPWStr)] string LockString);
    }
}