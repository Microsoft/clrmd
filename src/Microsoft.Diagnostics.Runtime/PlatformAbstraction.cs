﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Runtime
{
    internal abstract class PlatformFunctions
    {
        public abstract bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch);
        public abstract bool TryGetWow64(IntPtr proc, out bool result);
        public abstract IntPtr LoadLibrary(string lpFileName);
        public abstract bool FreeLibrary(IntPtr module);

        public abstract IntPtr GetProcAddress(IntPtr module, string method);


        public virtual bool IsEqualFileVersion(string file, VersionInfo version)
        {
            if (!GetFileVersion(file, out int major, out int minor, out int revision, out int patch))
                return false;

            return major == version.Major && minor == version.Minor && revision == version.Revision && patch == version.Patch;
        }
    }

    internal sealed class LinuxFunctions : PlatformFunctions
    {
        public override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
        {
            //TODO

            major = minor = revision = patch = 0;
            return true;
        }

        public override bool TryGetWow64(IntPtr proc, out bool result)
        {
            result = false;
            return true;
        }

        public override IntPtr LoadLibrary(string filename) => dlopen(filename, RTLD_NOW);

        public override bool FreeLibrary(IntPtr module) => dlclose(module) == 0;

        public override IntPtr GetProcAddress(IntPtr module, string method) => dlsym(module, method);

        [DllImport("libdl.so")]
        static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl.so")]
        static extern int dlclose(IntPtr module);

        [DllImport("libdl.so")]
        static extern IntPtr dlsym(IntPtr handle, string symbol);

        const int RTLD_NOW = 2;
    }

    internal sealed class WindowsFunctions : PlatformFunctions
    {
        public override bool FreeLibrary(IntPtr module) => NativeMethods.FreeLibrary(module);



        public override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
        {
            major = minor = revision = patch = 0;

            int len = NativeMethods.GetFileVersionInfoSize(dll, out int handle);
            if (len <= 0)
                return false;

            byte[] data = new byte[len];
            if (!NativeMethods.GetFileVersionInfo(dll, handle, len, data))
                return false;

            if (!NativeMethods.VerQueryValue(data, "\\", out IntPtr ptr, out len))
                return false;

            byte[] vsFixedInfo = new byte[len];
            Marshal.Copy(ptr, vsFixedInfo, 0, len);

            minor = (ushort)Marshal.ReadInt16(vsFixedInfo, 8);
            major = (ushort)Marshal.ReadInt16(vsFixedInfo, 10);
            patch = (ushort)Marshal.ReadInt16(vsFixedInfo, 12);
            revision = (ushort)Marshal.ReadInt16(vsFixedInfo, 14);

            return true;
        }

        public override IntPtr GetProcAddress(IntPtr module, string method) => NativeMethods.GetProcAddress(module, method);

        public override IntPtr LoadLibrary(string lpFileName) => NativeMethods.LoadLibraryEx(lpFileName, 0, NativeMethods.LoadLibraryFlags.NoFlags);

        public class NativeMethods
        {
            const string Kernel32LibraryName = "kernel32.dll";

            public const uint FILE_MAP_READ = 4;

            [DllImportAttribute(Kernel32LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeLibrary(IntPtr hModule);

            public static IntPtr LoadLibrary(string lpFileName)
            {
                return LoadLibraryEx(lpFileName, 0, LoadLibraryFlags.NoFlags);
            }

            [DllImportAttribute(Kernel32LibraryName, SetLastError = true)]
            public static extern IntPtr LoadLibraryEx(String fileName, int hFile, LoadLibraryFlags dwFlags);

            [Flags]
            public enum LoadLibraryFlags : uint
            {
                NoFlags = 0x00000000,
                DontResolveDllReferences = 0x00000001,
                LoadIgnoreCodeAuthzLevel = 0x00000010,
                LoadLibraryAsDatafile = 0x00000002,
                LoadLibraryAsDatafileExclusive = 0x00000040,
                LoadLibraryAsImageResource = 0x00000020,
                LoadWithAlteredSearchPath = 0x00000008
            }

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool isWow64);

            [DllImport("version.dll")]
            public static extern bool GetFileVersionInfo(string sFileName, int handle, int size, byte[] infoBuffer);

            [DllImport("version.dll")]
            public static extern int GetFileVersionInfoSize(string sFileName, out int handle);

            [DllImport("version.dll")]
            public static extern bool VerQueryValue(byte[] pBlock, string pSubBlock, out IntPtr val, out int len);

            const int VS_FIXEDFILEINFO_size = 0x34;
            public static short IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14;


            [DllImport("kernel32.dll")]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        }

        public override bool TryGetWow64(IntPtr proc, out bool result)
        {
            if (Environment.OSVersion.Version.Major > 5 ||
                (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1))
            {
                return NativeMethods.IsWow64Process(proc, out result);
            }
            else
            {
                result = false;
                return false;
            }
        }
    }

}
