﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Runtime.Linux
{
    /// <summary>
    /// The type of ELF file.
    /// </summary>
    public enum ElfHeaderType : ushort
    {
        Relocatable = 1,
        Executable = 2,
        Shared = 3,
        Core = 4
    }
}