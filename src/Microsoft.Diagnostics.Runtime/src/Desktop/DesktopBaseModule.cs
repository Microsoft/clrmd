﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Desktop
{
    internal abstract class DesktopBaseModule : ClrModule
    {
        protected ClrRuntimeImpl _runtime;

        public override ClrRuntime Runtime => _runtime;

        internal abstract ulong GetDomainModule(ClrAppDomain appDomain);

        internal ulong ModuleId { get; set; }

        internal virtual MetaDataImport GetMetadataImport()
        {
            //todo: remove
            return null;
        }

        public int Revision { get; set; }

        public DesktopBaseModule(ClrRuntimeImpl runtime)
        {
            _runtime = runtime;
        }
    }
}