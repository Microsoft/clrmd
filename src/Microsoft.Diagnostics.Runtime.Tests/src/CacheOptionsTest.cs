﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class CacheOptionsTest
    {
        [Fact]
        public void MethodCachingTest()
        {
            {
                // Test that when we cache method names they are not re-read
                using DataTarget dt = TestTargets.Types.LoadFullDump();
                dt.CacheOptions.CacheMethods = true;

                // We want to make sure we are getting the same string because it was cached,
                // not because it was interned
                dt.CacheOptions.CacheMethodNames = StringCaching.Cache;

                using ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                Assert.NotEqual(0ul, method.MethodDesc);  // Sanity test

                ClrMethod method2 = type.GetMethod("Bar");
                Assert.Equal(method, method2);
                Assert.Same(method, method2);


                string signature1 = method.Signature;
                string signature2 = method2.Signature;
                Assert.NotNull(signature1);
                Assert.Equal(signature1, signature2);

                Assert.Equal(signature1, method.Signature);
                Assert.Same(signature1, method.Signature);
            }

            {
                using DataTarget dt = TestTargets.Types.LoadFullDump();
                dt.CacheOptions.CacheMethods = false;
                dt.CacheOptions.CacheMethodNames = StringCaching.None;

                using ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();


                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                Assert.NotEqual(0ul, method.MethodDesc);  // Sanity test

                ClrMethod method2 = type.GetMethod("Bar");
                Assert.Equal(method, method2);
                Assert.NotSame(method, method2);


                string signature1 = method.Signature;
                string signature2 = method2.Signature;
                Assert.NotNull(signature1);
                Assert.Equal(signature1, signature2);

                Assert.Equal(signature1, method.Signature);
                Assert.NotSame(signature1, method.Signature);
                Assert.NotSame(method2.Signature, method.Signature);

                // Ensure that we can swap this at runtime and that we get interned strings
                dt.CacheOptions.CacheMethodNames = StringCaching.Intern;

                Assert.NotNull(method.Signature);
                Assert.Same(method2.Signature, method.Signature);
                Assert.Same(method.Signature, string.Intern(method.Signature));
            }
        }
    }
}
