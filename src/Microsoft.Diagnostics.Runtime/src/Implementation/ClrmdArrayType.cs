﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Runtime.Implementation
{
    public sealed class ClrmdArrayType : ClrmdType
    {
        private int _baseArrayOffset;
        private ClrType? _componentType;

        public override int ComponentSize { get; }
        public override ClrType? ComponentType => _componentType;

        public ClrmdArrayType(ClrHeap heap, ClrType? baseType, ClrModule module, ITypeData data)
            : base(heap, baseType, module, data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            ComponentSize = data.ComponentSize;
            ulong componentMT = data.ComponentMethodTable;
            if (componentMT != 0)
                _componentType = Helpers.Factory.GetOrCreateType(heap, componentMT, 0);
        }

        public void SetComponentType(ClrType? type) => _componentType = type;

        public override ulong GetArrayElementAddress(ulong objRef, int index)
        {
            //todo: remove?
            if (_baseArrayOffset == 0)
            {
                ClrType? componentType = ComponentType;

                IObjectData data = Helpers.GetObjectData(objRef);
                if (data != null)
                {
                    _baseArrayOffset = (int)(data.DataPointer - objRef);
                    DebugOnly.Assert(_baseArrayOffset >= 0);
                }
                else if (componentType != null)
                {
                    if (!componentType.IsObjectReference)
                        _baseArrayOffset = IntPtr.Size * 2;
                }
                else
                {
                    return 0;
                }
            }

            return objRef + (ulong)(_baseArrayOffset + index * ComponentSize);
        }

        public override object? GetArrayElementValue(ulong objRef, int index)
        {
            ulong addr = GetArrayElementAddress(objRef, index);
            if (addr == 0)
                return null;

            ClrType? componentType = ComponentType;
            ClrElementType cet;
            if (componentType != null)
            {
                cet = componentType.ElementType;
            }
            else
            {
                // Slow path, we need to get the element type of the array.
                IObjectData data = Helpers.GetObjectData(objRef);
                if (data is null)
                    return null;

                cet = data.ElementType;
            }

            if (cet == ClrElementType.Unknown)
                return null;

            if (cet == ClrElementType.String)
                addr = DataReader.ReadPointer(addr);

            return ValueReader.GetValueAtAddress(Heap, DataReader, cet, addr);
        }

        public override T[]? GetArrayElementsValues<T>(ulong objRef, int count)
        {
            ulong addr = GetArrayElementAddress(objRef, 0);
            ClrType? componentType = ComponentType;
            ClrElementType cet;
            if (componentType != null)
            {
                cet = componentType.ElementType;
            }
            else
            {
                // Slow path, we need to get the element type of the array.
                IObjectData data = Helpers.GetObjectData(objRef);
                if (data is null)
                    return null;

                cet = data.ElementType;
            }

            if (cet == ClrElementType.Unknown)
                return null;

            return ValueReader.GetValuesFromAddress<T>(DataReader, addr, count);
        }
    }
}
