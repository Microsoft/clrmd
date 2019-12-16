﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Implementation
{
    public interface IModuleHelpers
    {
        ITypeFactory Factory { get; }
        IDataReader DataReader { get; }

        MetaDataImport? GetMetadataImport(ClrModule module);
        IReadOnlyList<(ulong, uint)> GetSortedTypeDefMap(ClrModule module);
        IReadOnlyList<(ulong, uint)> GetSortedTypeRefMap(ClrModule module);
        ClrType TryGetType(ulong mt);
        string? GetTypeName(ulong mt);
    }
}