﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Implementation;

namespace Microsoft.Diagnostics.Runtime.DacInterface
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MethodTableData
    {
        public readonly uint IsFree; // everything else is NULL if this is true.
        public readonly ulong Module;
        public readonly ulong EEClass;
        public readonly ulong ParentMethodTable;
        public readonly ushort NumInterfaces;
        public readonly ushort NumMethods;
        public readonly ushort NumVtableSlots;
        public readonly ushort NumVirtuals;
        public readonly uint BaseSize;
        public readonly uint ComponentSize;
        public readonly uint Token;
        public readonly uint AttrClass;
        public readonly uint Shared; // flags & enum_flag_DomainNeutral
        public readonly uint Dynamic;
        public readonly uint ContainsPointers;
    }
}