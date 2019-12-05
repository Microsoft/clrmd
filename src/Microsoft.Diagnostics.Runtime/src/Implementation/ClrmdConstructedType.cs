﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Implementation
{
    public class ClrmdConstructedType : ClrType
    {
        private readonly int _ranks;
        public override ClrHeap Heap => ComponentType.Heap;

        public override ClrModule Module => Heap.Runtime.BaseClassLibrary;
        public override ClrType ComponentType { get; }
        public override string Name
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(ComponentType.Name);
                if (IsPointer)
                {
                    for (int i = 0; i < _ranks; i++)
                        sb.Append('*');
                }
                else
                {
                    sb.Append('[');
                    for (int i = 0; i < _ranks - 1; i++)
                        sb.Append(',');
                    sb.Append(']');
                }

                return sb.ToString();
            }
        }

        public ClrmdConstructedType(ClrType componentType, int ranks, bool pointer)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            ElementType = pointer ? ClrElementType.Pointer : ClrElementType.SZArray;
            _ranks = ranks;

            if (ranks <= 0)
                throw new ArgumentException($"{nameof(ranks)} must be 1 or greater.");
        }

        public override IClrObjectHelpers ClrObjectHelpers => ComponentType.ClrObjectHelpers;

        // We have no good way of finding this value, unfortunately
        public override ClrElementType ElementType { get; }
        public override ulong MethodTable => 0;
        public override bool IsFinalizeSuppressed(ulong obj) => false;
        public override bool IsPointer => true;
        public override IReadOnlyList<ClrInstanceField> Fields => Array.Empty<ClrInstanceField>();
        public override IReadOnlyList<ClrStaticField> StaticFields => Array.Empty<ClrStaticField>();
        public override IReadOnlyList<ClrMethod> Methods => Array.Empty<ClrMethod>();
        public override IEnumerable<ClrInterface> EnumerateInterfaces() => Array.Empty<ClrInterface>();
        public override bool IsFinalizable => false;
        public override bool IsPublic => true;
        public override bool IsPrivate => false;
        public override bool IsInternal => false;
        public override bool IsProtected => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => false;
        public override bool IsShared => false;
        public override bool IsInterface => false;
        public override ClrInstanceField? GetFieldByName(string name) => null;
        public override ClrStaticField? GetStaticFieldByName(string name) => null;
        public override ClrType? BaseType => null;
        public override ulong GetArrayElementAddress(ulong objRef, int index) => 0;
        public override object? GetArrayElementValue(ulong objRef, int index) => null;
        public override int BaseSize => IntPtr.Size;
        public override GCDesc GCDesc => default;
        public override uint MetadataToken => 0;
        public override bool IsArray => !IsPointer;
        public override int ComponentSize => IntPtr.Size;
        public override ComCallWrapper? GetCCWData(ulong obj) => null;
        public override RuntimeCallableWrapper? GetRCWData(ulong obj) => null;
        public override bool GetFieldForOffset(int fieldOffset, bool inner, out ClrInstanceField childField, out int childFieldOffset)
        {
            childField = null;
            childFieldOffset = 0;
            return false;
        }
    }
}