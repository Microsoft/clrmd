﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Runtime.Native.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Native
{
    public class NativeType
    {
        //private GCDesc _gcdesc;
        //private ModuleInfo _module;
        private readonly EETypeData _data;

        public NativeHeap Heap { get; }

        public ulong EEType { get; }
        public string Name { get; }
        public bool ContainsPointers => _data.ContainsPointers != 0;

        public NativeType(NativeHeap heap, ulong eeType, string name, ref EETypeData data)
        {
            Heap = heap;
            EEType = eeType;
            Name = name;
            _data = data;
        }

        /*
        public ModuleInfo Module
        {
            get
            {
                if (_module == null)
                    _module = Heap.GetModuleFromAddress(EEType);

                return _module;
            }
        }

        public ulong GetSize(ulong objRef)
        {
            var pointerSize = (uint)IntPtr.Size;

            ulong size;
            if (_data.ComponentSize == 0)
            {
                size = _data.BaseSize;
            }
            else
            {
                var countOffset = pointerSize;
                var loc = objRef + countOffset;

                if (!Heap.ReadDword(loc, out uint count))
                    throw new Exception("Could not read from heap at " + objRef.ToString("x"));

                size = count * (ulong)_data.ComponentSize + _data.BaseSize;
            }

            var minSize = pointerSize * 3;
            if (size < minSize)
                size = minSize;
            return size;
        }

        public void EnumerateRefsOfObject(ulong objRef, Action<ulong, int> callback)
        {
            if (ContainsPointers)
            {
                if (_gcdesc == null)
                    _gcdesc = GetGCDesc();

                _gcdesc.WalkObject(objRef, GetSize(objRef), ReadPointer, callback);
            }
        }

        public void EnumerateRefsOfObjectCarefully(ulong objRef, Action<ulong, int> callback)
        {
            if (ContainsPointers)
            {
                if (_gcdesc == null)
                    _gcdesc = GetGCDesc();

                _gcdesc.WalkObject(objRef, GetSize(objRef), ReadPointer, callback);
            }
        }

        private GCDesc GetGCDesc()
        {
            NativeRuntime runtime = Heap.NativeRuntime;

            if (!Heap.ReadDword(_eeType - (ulong)IntPtr.Size, out int entries))
                return null;

            // Get entries in map
            if (entries < 0)
                entries = -entries;

            var slots = 1 + entries * 2;
            var buffer = new byte[slots * IntPtr.Size];
            if (!runtime.ReadMemory(EEType - (ulong)(slots * IntPtr.Size), buffer, buffer.Length, out int read) || read != buffer.Length)
                return null;

            // Construct the gc desc
            return new GCDesc(buffer);
        }

        private ulong ReadPointer(ulong ptr)
        {
            if (Heap.ReadPointer(ptr, out ulong result))
                return result;

            return 0;
        }
        */
    }
}