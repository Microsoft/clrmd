﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class ModuleTests
    {
        [Fact]
        public void TestGetTypeByName()
        {
            using (var dt = TestTargets.Types.LoadFullDump())
            {
                var runtime = dt.ClrVersions.Single().CreateRuntime();
                var heap = runtime.Heap;

                var shared = runtime.GetModule("sharedlibrary.dll");
                Assert.NotNull(shared.GetTypeByName("Foo"));
                Assert.Null(shared.GetTypeByName("Types"));

                var types = runtime.GetModule("types.exe");
                Assert.NotNull(types.GetTypeByName("Types"));
                Assert.Null(types.GetTypeByName("Foo"));
            }
        }
    }
}