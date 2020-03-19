﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Runtime.DacInterface;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class DataTargetTests
    {
        [Fact]
        public void EnsureFinalReleaseOfInterfaces()
        {
            using DataTarget dt = TestTargets.Types.LoadFullDump();

            RefCountedFreeLibrary library;
            SOSDac sosDac;

            using (ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime())
            {
                library = runtime.DacLibrary.OwningLibrary;
                sosDac = runtime.DacLibrary.SOSDacInterface;

                // Keep library alive
                library.AddRef();
            }

            sosDac.Dispose();
            Assert.Equal(0, library.Release());
        }

        [LinuxFact]
        public void CreateSnapshotAndAttach_ThrowsPlatformNotSupportedException()
        {
            _ = Assert.Throws<PlatformNotSupportedException>(() => DataTarget.CreateSnapshotAndAttach(Process.GetCurrentProcess().Id));
        }

        [WindowsFact]
        public void LoadCoreDump_ThrowsPlatformNotSupportedException()
        {
            _ = Assert.Throws<PlatformNotSupportedException>(() => DataTarget.LoadCoreDump(TestTargets.Types.BuildDumpName(GCMode.Workstation, true)));
        }

        [LinuxFact]
        public void LoadCrashDump_ThrowsPlatformNotSupportedException()
        {
            _ = Assert.Throws<PlatformNotSupportedException>(() => DataTarget.LoadCrashDump(TestTargets.Types.BuildDumpName(GCMode.Workstation, true)));
        }

        [LinuxFact]
        public void LoadCoreDump_ThrowsInvalidDataException()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, Environment.NewLine);
            _ = Assert.Throws<InvalidDataException>(() => DataTarget.LoadCoreDump(path));
        }

        [WindowsFact]
        public void LoadCrashDump_ThrowsInvalidDataException()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, Environment.NewLine);
            _ = Assert.Throws<InvalidDataException>(() => DataTarget.LoadCrashDump(path));
        }

        [WindowsFact]
        public void LoadCrashDump_ThrowsInvalidDataExceptionForEmptyFile()
        {
            string path = Path.GetTempFileName();
            _ = Assert.Throws<InvalidDataException>(() => DataTarget.LoadCrashDump(path));
        }
    }
}
