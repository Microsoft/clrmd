// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Utilities
{
    /// <summary>
    /// FileVersionInfo reprents the extended version formation that is optionally placed in the PE file resource area.
    /// </summary>
    public sealed unsafe class FileVersionInfo
    {
        /// <summary>
        /// The verison string
        /// </summary>
        public string FileVersion { get; }

        /// <summary>
        /// The version of this module
        /// </summary>
        public VersionInfo VersionInfo { get; }

        /// <summary>
        /// Comments to supplement the file version
        /// </summary>
        public string Comments { get; }

        unsafe internal FileVersionInfo(byte[] data, int dataLen)
        {
            FileVersion = "";

            var dataAsString = Encoding.Unicode.GetString(data);

            FileVersion = GetDataString(dataAsString, "FileVersion");
            Comments = GetDataString(dataAsString, "Comments");

            fixed (byte *ptr = data)
                VersionInfo = GetVersionInfo(ptr, dataLen, dataAsString);
        }

        private static VersionInfo GetVersionInfo(byte* data, int dataLen, string dataAsString)
        {
            const string vsVersionInfo = "VS_VERSION_INFO";
            int fileVersionIndex = dataAsString.IndexOf(vsVersionInfo);
            if (fileVersionIndex < 0)
                return default;

            int byteIndex = (fileVersionIndex + vsVersionInfo.Length) * sizeof(char);

            int minor = (ushort)Marshal.ReadInt16(new IntPtr(data + byteIndex + 12));
            int major = (ushort)Marshal.ReadInt16(new IntPtr(data + byteIndex + 14));
            int patch = (ushort)Marshal.ReadInt16(new IntPtr(data + byteIndex + 16));
            int revision = (ushort)Marshal.ReadInt16(new IntPtr(data + byteIndex + 18));

            return new VersionInfo(major, minor, revision, patch);
        }

        [Obsolete]
        internal FileVersionInfo(byte* data, int dataLen)
        {
            FileVersion = "";
            if (dataLen <= 0x5c)
                return;

            // See http://msdn.microsoft.com/en-us/library/ms647001(v=VS.85).aspx
            byte* stringInfoPtr = data + 0x5c; // Gets to first StringInfo

            // TODO search for FileVersion string ... 
            string dataAsString = new string((char*)stringInfoPtr, 0, (dataLen - 0x5c) / 2);

            FileVersion = GetDataString(dataAsString, "FileVersion");
            Comments = GetDataString(dataAsString, "Comments");
        }

        private static string GetDataString(string dataAsString, string fileVersionKey)
        {
            int fileVersionIdx = dataAsString.IndexOf(fileVersionKey);
            if (fileVersionIdx >= 0)
            {
                int valIdx = fileVersionIdx + fileVersionKey.Length;
                for (; ; )
                {
                    valIdx++;
                    if (valIdx >= dataAsString.Length)
                        return null;

                    if (dataAsString[valIdx] != (char)0)
                        break;
                }

                int varEndIdx = dataAsString.IndexOf((char)0, valIdx);
                if (varEndIdx < 0)
                    return null;

                return dataAsString.Substring(valIdx, varEndIdx - valIdx);
            }

            return null;
        }

        public override string ToString() => FileVersion;
    }
}