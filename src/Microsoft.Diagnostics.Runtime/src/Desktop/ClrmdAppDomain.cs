﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Runtime.Desktop
{
    internal class ClrmdAppDomain : ClrAppDomain
    {
        private readonly IAppDomainHelpers _helpers;

        public override ClrRuntime Runtime { get; }
        public override ulong Address { get; }
        public override int Id { get; }
        public override string Name { get; }
        public override IReadOnlyList<ClrModule> Modules { get; }

        public override string ConfigurationFile => _helpers.GetConfigFile(this);
        public override string ApplicationBase => _helpers.GetApplicationBase(this);

        public ClrmdAppDomain(ClrRuntime runtime, IAppDomainData data)
        {
            _helpers = data.Helpers;
            Runtime = runtime;
            Id = data.Id;
            Address = data.Address;
            Name = data.Name;
            Runtime = runtime;
            Modules = _helpers.EnumerateModules(this).ToArray();
        }
    }
}