﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.DacInterface;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.Desktop
{
    internal class DesktopHeapType : BaseDesktopHeapType
    {
        private ulong _cachedMethodTable;
        private ulong[] _methodTables;
        private readonly Lazy<string> _name;

        private TypeAttributes _attributes;
        private readonly ulong _parent;
        private readonly uint _baseSize;
        private readonly uint _componentSize;
        private readonly bool _containsPointers;
        private readonly bool _isCollectible;
        private readonly ulong _loaderAllocatorObjectHandle;
        private byte _finalizable;

        private List<ClrInstanceField> _fields;
        private List<ClrStaticField> _statics;
        private List<ClrThreadStaticField> _threadStatics;
        private int[] _fieldNameMap;

        private int _baseArrayOffset;
        private bool? _runtimeType;
        private EnumData _enumData;
        private bool _notRCW;
        private bool _checkedIfIsRCW;
        private bool _checkedIfIsCCW;
        private bool _notCCW;

        public override ulong MethodTable
        {
            get
            {
                if (_cachedMethodTable != 0)
                    return _cachedMethodTable;

                if (Shared || ((DesktopRuntimeBase)Heap.Runtime).IsSingleDomain)
                    _cachedMethodTable = _constructedMT;
                else
                {
                    _cachedMethodTable = EnumerateMethodTables().FirstOrDefault();
                    if (_cachedMethodTable == 0)
                        _cachedMethodTable = _constructedMT;
                }

                Debug.Assert(_cachedMethodTable != 0);
                return _cachedMethodTable;
            }
        }

        public override IEnumerable<ulong> EnumerateMethodTables()
        {
            if (_methodTables == null && (Shared || ((DesktopRuntimeBase)Heap.Runtime).IsSingleDomain))
            {
                if (_cachedMethodTable == 0)
                {
                    // This should never happen, but we'll check to make sure.
                    Debug.Assert(_constructedMT != 0);

                    if (_constructedMT != 0)
                    {
                        _cachedMethodTable = _constructedMT;
                        _methodTables = new[] { _cachedMethodTable };
                        return _methodTables;
                    }
                }
            }

            return FillAndEnumerateTypeHandles();
        }

        private IEnumerable<ulong> FillAndEnumerateTypeHandles()
        {
            IList<ClrAppDomain> domains = null;
            if (_methodTables == null)
            {
                domains = Module.AppDomains;
                _methodTables = new ulong[domains.Count];
            }

            for (int i = 0; i < _methodTables.Length; i++)
            {
                if (_methodTables[i] == 0)
                {
                    if (domains == null)
                        domains = Module.AppDomains;

                    ulong value = ((DesktopModule)DesktopModule).GetMTForDomain(domains[i], this);
                    _methodTables[i] = value != 0 ? value : ulong.MaxValue;
                }

                if (_methodTables[i] == ulong.MaxValue)
                    continue;

                yield return _methodTables[i];
            }
        }

        public override ClrElementType ElementType
        {
            get
            {
                if (_elementType == ClrElementType.Unknown)
                    _elementType = DesktopHeap.GetElementType(this, 0);

                return _elementType;
            }

            internal set => _elementType = value;
        }

        public override string Name => _name.Value;
        public override ClrModule Module
        {
            get
            {
                if (DesktopModule == null)
                    return DesktopHeap.DesktopRuntime.ErrorModule;

                return DesktopModule;
            }
        }

        public override ulong GetSize(ulong objRef)
        {
            ulong size;
            uint pointerSize = (uint)DesktopHeap.PointerSize;
            if (_componentSize == 0)
            {
                size = _baseSize;
            }
            else
            {
                uint countOffset = pointerSize;
                ulong loc = objRef + countOffset;

                MemoryReader cache = DesktopHeap.MemoryReader;
                if (!cache.Contains(loc))
                {
                    MemoryReader runtimeCache = DesktopHeap.DesktopRuntime.MemoryReader;
                    if (runtimeCache.Contains(loc))
                        cache = DesktopHeap.DesktopRuntime.MemoryReader;
                }

                if (!cache.ReadDword(loc, out uint count))
                    throw new MemoryReadException(objRef);

                // Strings in v4+ contain a trailing null terminator not accounted for.
                if (DesktopHeap.StringType == this && DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v2)
                    count++;

                size = count * (ulong)_componentSize + _baseSize;
            }

            uint minSize = pointerSize * 3;
            if (size < minSize)
                size = minSize;
            return size;
        }

        public override void EnumerateRefsOfObjectCarefully(ulong objRef, Action<ulong, int> action)
        {
            if (_containsPointers)
                Heap.EnumerateObjectReferences(objRef, this, true, action);
        }

        public override void EnumerateRefsOfObject(ulong objRef, Action<ulong, int> action)
        {
            if (_containsPointers)
                Heap.EnumerateObjectReferences(objRef, this, false, action);
        }

        public override ClrHeap Heap => DesktopHeap;

        public override string ToString()
        {
            return Name;
        }

        public override bool HasSimpleValue => ElementType != ClrElementType.Struct;

        public override object GetValue(ulong address)
        {
            if (IsPrimitive)
                address += (ulong)DesktopHeap.PointerSize;

            return DesktopHeap.GetValueAtAddress(ElementType, address);
        }

        public override bool IsException
        {
            get
            {
                ClrType type = this;
                while (type != null)
                    if (type == DesktopHeap.ExceptionType)
                        return true;
                    else
                        type = type.BaseType;

                return false;
            }
        }

        public override bool IsCCW(ulong obj)
        {
            if (_checkedIfIsCCW)
                return !_notCCW;

            // The dac cannot report this information prior to v4.5.
            if (DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
                return false;

            IObjectData data = DesktopHeap.GetObjectData(obj);
            _notCCW = !(data != null && data.CCW != 0);
            _checkedIfIsCCW = true;

            return !_notCCW;
        }

        public override CcwData GetCCWData(ulong obj)
        {
            if (_notCCW)
                return null;

            // The dac cannot report this information prior to v4.5.
            if (DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
                return null;

            DesktopCCWData result = null;
            IObjectData data = DesktopHeap.GetObjectData(obj);

            if (data != null && data.CCW != 0)
            {
                ICCWData ccw = DesktopHeap.DesktopRuntime.GetCCWData(data.CCW);
                if (ccw != null)
                    result = new DesktopCCWData(DesktopHeap, data.CCW, ccw);
            }
            else if (!_checkedIfIsCCW)
            {
                _notCCW = true;
            }

            _checkedIfIsCCW = true;
            return result;
        }

        public override bool IsRCW(ulong obj)
        {
            if (_checkedIfIsRCW)
                return !_notRCW;

            // The dac cannot report this information prior to v4.5.
            if (DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
                return false;

            IObjectData data = DesktopHeap.GetObjectData(obj);
            _notRCW = !(data != null && data.RCW != 0);
            _checkedIfIsRCW = true;

            return !_notRCW;
        }

        public override RcwData GetRCWData(ulong obj)
        {
            // Most types can't possibly be RCWs.
            if (_notRCW)
                return null;

            // The dac cannot report this information prior to v4.5.
            if (DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
            {
                _notRCW = true;
                return null;
            }

            DesktopRCWData result = null;
            IObjectData data = DesktopHeap.GetObjectData(obj);

            if (data != null && data.RCW != 0)
            {
                IRCWData rcw = DesktopHeap.DesktopRuntime.GetRCWData(data.RCW);
                if (rcw != null)
                    result = new DesktopRCWData(DesktopHeap, data.RCW, rcw);
            }
            else if (!_checkedIfIsRCW) // If the first time fails, we assume that all instances of this type can't be RCWs.
            {
                _notRCW = true; // TODO FIX NOW review.  We really want to simply ask the runtime...
            }

            _checkedIfIsRCW = true;
            return result;
        }

        private class EnumData
        {
            internal ClrElementType ElementType;
            internal readonly Dictionary<string, object> NameToValue = new Dictionary<string, object>();
            internal readonly Dictionary<object, string> ValueToName = new Dictionary<object, string>();
        }

        public override ClrElementType GetEnumElementType()
        {
            if (_enumData == null)
                InitEnumData();

            return _enumData.ElementType;
        }

        public override bool TryGetEnumValue(string name, out int value)
        {
            if (TryGetEnumValue(name, out object val))
            {
                value = (int)val;
                return true;
            }

            value = int.MinValue;
            return false;
        }

        public override bool TryGetEnumValue(string name, out object value)
        {
            if (_enumData == null)
                InitEnumData();

            return _enumData.NameToValue.TryGetValue(name, out value);
        }

        public override string GetEnumName(object value)
        {
            if (_enumData == null)
                InitEnumData();

            _enumData.ValueToName.TryGetValue(value, out string result);
            return result;
        }

        public override string GetEnumName(int value)
        {
            return GetEnumName((object)value);
        }

        public override IEnumerable<string> GetEnumNames()
        {
            if (_enumData == null)
                InitEnumData();

            return _enumData.NameToValue.Keys;
        }

        private void InitEnumData()
        {
            if (!IsEnum)
                throw new InvalidOperationException("Type is not an Enum.");

            _enumData = new EnumData();
            MetaDataImport import = DesktopModule?.GetMetadataImport();
            if (import == null)
                return;

            List<string> names = new List<string>();
            foreach (int token in import.EnumerateFields((int)_token))
            {
                if (import.GetFieldProps(token, out string name, out FieldAttributes attr, out IntPtr ppvSigBlob, out int pcbSigBlob, out int pdwCPlusTypeFlag, out IntPtr ppValue))
                {
                    if ((int)attr == 0x606 && name == "value__")
                    {
                        SigParser parser = new SigParser(ppvSigBlob, pcbSigBlob);
                        if (parser.GetCallingConvInfo(out _) && parser.GetElemType(out int elemType))
                            _enumData.ElementType = (ClrElementType)elemType;
                    }

                    // public, static, literal, has default
                    if ((int)attr == 0x8056)
                    {
                        names.Add(name);

                        SigParser parser = new SigParser(ppvSigBlob, pcbSigBlob);
                        parser.GetCallingConvInfo(out _);
                        parser.GetElemType(out _);

                        Type type = ((ClrElementType)pdwCPlusTypeFlag).GetTypeForElementType();
                        if (type != null)
                        {
                            object o = Marshal.PtrToStructure(ppValue, type);
                            _enumData.NameToValue[name] = o;
                            _enumData.ValueToName[o] = name;
                        }
                    }
                }
            }
        }

        public override bool IsEnum
        {
            get
            {
                ClrType type = this;

                ClrType enumType = DesktopHeap.EnumType;
                while (type != null)
                {
                    if (enumType == null && type.Name == "System.Enum")
                    {
                        DesktopHeap.EnumType = type;
                        return true;
                    }

                    if (type == enumType)
                    {
                        return true;
                    }

                    type = type.BaseType;
                }

                return false;
            }
        }

        public override bool IsFree => this == DesktopHeap.Free;

        private const uint FinalizationSuppressedFlag = 0x40000000;

        public override bool IsFinalizeSuppressed(ulong obj)
        {
            bool result = DesktopHeap.GetObjectHeader(obj, out uint value);

            return result && (value & FinalizationSuppressedFlag) == FinalizationSuppressedFlag;
        }

        public override bool IsFinalizable
        {
            get
            {
                if (_finalizable == 0)
                {
                    foreach (ClrMethod method in Methods)
                    {
                        if (method.IsVirtual && method.Name == "Finalize")
                        {
                            _finalizable = 1;
                            break;
                        }
                    }

                    if (_finalizable == 0)
                        _finalizable = 2;
                }

                return _finalizable == 1;
            }
        }

        public override bool IsArray => _componentSize != 0 && this != DesktopHeap.StringType && this != DesktopHeap.Free;
        public override bool ContainsPointers => _containsPointers;
        public override bool IsCollectible => _isCollectible;

        public override ulong LoaderAllocatorObject
        {
            get
            {
                if (_isCollectible)
                {
                    DesktopHeap.ReadPointer(_loaderAllocatorObjectHandle, out ulong loaderAllocatorObject);
                    return loaderAllocatorObject;
                }

                return 0;
            }
        }

        public override bool IsString => this == DesktopHeap.StringType;

        public override bool GetFieldForOffset(int fieldOffset, bool inner, out ClrInstanceField childField, out int childFieldOffset)
        {
            if (!IsArray)
            {
                int offset = fieldOffset;

                if (!inner)
                    offset -= DesktopHeap.PointerSize;

                foreach (ClrInstanceField field in Fields)
                {
                    if (field.ElementType == ClrElementType.Unknown)
                        break;

                    if (field.Offset <= offset && offset < field.Offset + field.Size)
                    {
                        childField = field;
                        childFieldOffset = offset - field.Offset;
                        return true;
                    }
                }
            }

            if (BaseType != null)
                return BaseType.GetFieldForOffset(fieldOffset, inner, out childField, out childFieldOffset);

            childField = null;
            childFieldOffset = 0;
            return false;
        }

        public override int ElementSize => (int)_componentSize;
        public override IList<ClrInstanceField> Fields
        {
            get
            {
                if (_fields == null)
                    InitFields();

                return _fields;
            }
        }

        public override IList<ClrStaticField> StaticFields
        {
            get
            {
                if (_fields == null)
                    InitFields();

                if (_statics == null)
                    return Array.Empty<ClrStaticField>();

                return _statics;
            }
        }

        public override IList<ClrThreadStaticField> ThreadStaticFields
        {
            get
            {
                if (_fields == null)
                    InitFields();

                if (_threadStatics == null)
                    return Array.Empty<ClrThreadStaticField>();

                return _threadStatics;
            }
        }

        private void InitFields()
        {
            if (_fields != null)
                return;

            if (IsFree)
            {
                _fields = new List<ClrInstanceField>();
                return;
            }

            DesktopRuntimeBase runtime = DesktopHeap.DesktopRuntime;
            IFieldInfo fieldInfo = runtime.GetFieldInfo(_constructedMT);

            if (fieldInfo == null)
            {
                // Fill fields so we don't repeatedly try to init these fields on error.
                _fields = new List<ClrInstanceField>();
                return;
            }

            _fields = new List<ClrInstanceField>((int)fieldInfo.InstanceFields);

            // Add base type's fields.
            if (BaseType != null)
            {
                foreach (ClrInstanceField field in BaseType.Fields)
                    _fields.Add(field);
            }

            int count = (int)(fieldInfo.InstanceFields + fieldInfo.StaticFields) - _fields.Count;
            ulong nextField = fieldInfo.FirstField;
            int i = 0;

            MetaDataImport import = null;
            if (nextField != 0 && DesktopModule != null)
                import = DesktopModule.GetMetadataImport();

            while (i < count && nextField != 0)
            {
                IFieldData field = runtime.GetFieldData(nextField);
                if (field == null)
                    break;

                // We don't handle context statics.
                if (field.IsContextLocal)
                {
                    nextField = field.NextField;
                    continue;
                }

                // Get the name of the field.
                string name = null;
                FieldAttributes attr = FieldAttributes.PrivateScope;
                int sigLen = 0;
                IntPtr ppValue = IntPtr.Zero;
                IntPtr fieldSig = IntPtr.Zero;

                if (import != null)
                    import.GetFieldProps((int)field.FieldToken, out name, out attr, out fieldSig, out sigLen, out int pdwCPlusTypeFlab, out ppValue);

                // If we couldn't figure out the name, at least give the token.
                if (import == null || name == null)
                {
                    name = $"<ERROR:{field.FieldToken:X}>";
                }

                // construct the appropriate type of field.
                if (field.IsThreadLocal)
                {
                    if (_threadStatics == null)
                        _threadStatics = new List<ClrThreadStaticField>((int)fieldInfo.ThreadStaticFields);

                    // TODO:  Renable when thread statics are fixed.
                    //m_threadStatics.Add(new RealTimeMemThreadStaticField(m_heap, field, name));
                }
                else if (field.IsStatic)
                {
                    if (_statics == null)
                        _statics = new List<ClrStaticField>();

                    // TODO:  Enable default values.
                    /*
                    object defaultValue = null;


                    FieldAttributes sdl = FieldAttributes.Static | FieldAttributes.HasDefault | FieldAttributes.Literal;
                    if ((attr & sdl) == sdl)
                        Debugger.Break();
                    */
                    _statics.Add(new DesktopStaticField(DesktopHeap, field, this, name, attr, null, fieldSig, sigLen));
                }
                else // instance variable
                {
                    _fields.Add(new DesktopInstanceField(DesktopHeap, field, name, attr, fieldSig, sigLen));
                }

                i++;
                nextField = field.NextField;
            }

            _fields.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        }

        internal override ClrMethod GetMethod(uint token)
        {
            return Methods.FirstOrDefault(m => m.MetadataToken == token);
        }

        public override IList<ClrMethod> Methods
        {
            get
            {
                if (_methods != null)
                    return _methods;

                MetaDataImport metadata = null;
                if (DesktopModule != null)
                    metadata = DesktopModule.GetMetadataImport();

                DesktopRuntimeBase runtime = DesktopHeap.DesktopRuntime;
                IList<ulong> mdList = runtime.GetMethodDescList(_constructedMT);

                if (mdList != null)
                {
                    _methods = new List<ClrMethod>(mdList.Count);
                    foreach (ulong md in mdList)
                    {
                        if (md == 0)
                            continue;

                        IMethodDescData mdData = runtime.GetMethodDescData(md);
                        DesktopMethod method = DesktopMethod.Create(runtime, metadata, mdData);
                        if (method != null)
                            _methods.Add(method);
                    }
                }
                else
                {
                    _methods = Array.Empty<ClrMethod>();
                }

                return _methods;
            }
        }

        public override ClrStaticField GetStaticFieldByName(string name)
        {
            foreach (ClrStaticField field in StaticFields)
                if (field.Name == name)
                    return field;

            return null;
        }

        private IList<ClrMethod> _methods;

        public override ClrInstanceField GetFieldByName(string name)
        {
            if (_fields == null)
                InitFields();

            if (_fields.Count == 0)
                return null;

            if (_fieldNameMap == null)
            {
                _fieldNameMap = new int[_fields.Count];
                for (int j = 0; j < _fieldNameMap.Length; ++j)
                    _fieldNameMap[j] = j;

                Array.Sort(_fieldNameMap, (x, y) => { return _fields[x].Name.CompareTo(_fields[y].Name); });
            }

            int min = 0, max = _fieldNameMap.Length - 1;

            while (max >= min)
            {
                int mid = (max + min) / 2;

                ClrInstanceField field = _fields[_fieldNameMap[mid]];
                int comp = field.Name.CompareTo(name);
                if (comp < 0)
                    min = mid + 1;
                else if (comp > 0)
                    max = mid - 1;
                else
                    return _fields[_fieldNameMap[mid]];
            }

            return null;
        }

        public override ClrType BaseType
        {
            get
            {
                if (_parent == 0)
                    return null;

                return DesktopHeap.GetTypeByMethodTable(_parent, 0, 0);
            }
        }

        public override int GetArrayLength(ulong objRef)
        {
            Debug.Assert(IsArray);

            if (!DesktopHeap.DesktopRuntime.ReadPrimitive(objRef + (uint)DesktopHeap.DesktopRuntime.PointerSize, out uint res))
                res = 0;

            return (int)res;
        }

        public override ulong GetArrayElementAddress(ulong objRef, int index)
        {
            if (_baseArrayOffset == 0)
            {
                ClrType componentType = ComponentType;

                IObjectData data = DesktopHeap.DesktopRuntime.GetObjectData(objRef);
                if (data != null)
                {
                    _baseArrayOffset = (int)(data.DataPointer - objRef);
                    Debug.Assert(_baseArrayOffset >= 0);
                }
                else if (componentType != null)
                {
                    if (!componentType.IsObjectReference || !Heap.Runtime.HasArrayComponentMethodTables)
                        _baseArrayOffset = IntPtr.Size * 2;
                    else
                        _baseArrayOffset = IntPtr.Size * 3;
                }
                else
                {
                    return 0;
                }
            }

            return objRef + (ulong)(_baseArrayOffset + index * _componentSize);
        }

        public override object GetArrayElementValue(ulong objRef, int index)
        {
            ulong addr = GetArrayElementAddress(objRef, index);
            if (addr == 0)
                return null;

            ClrType componentType = ComponentType;
            ClrElementType cet;
            if (componentType != null)
            {
                cet = componentType.ElementType;
            }
            else
            {
                // Slow path, we need to get the element type of the array.
                IObjectData data = DesktopHeap.DesktopRuntime.GetObjectData(objRef);
                if (data == null)
                    return null;

                cet = data.ElementType;
            }

            if (cet == ClrElementType.Unknown)
                return null;

            if (cet == ClrElementType.String && !DesktopHeap.MemoryReader.ReadPtr(addr, out addr))
                return null;

            return DesktopHeap.GetValueAtAddress(cet, addr);
        }

        public override int BaseSize => (int)_baseSize;

        /// <summary>
        /// A messy version with better performance that doesn't use regular expression.
        /// </summary>
        internal static int FixGenericsWorker(string name, int start, int end, StringBuilder sb)
        {
            int parenCount = 0;
            while (start < end)
            {
                char c = name[start];
                if (c == '`')
                    break;

                if (c == '[')
                    parenCount++;

                if (c == ']')
                    parenCount--;

                if (parenCount < 0)
                    return start + 1;

                if (c == ',' && parenCount == 0)
                    return start;

                sb.Append(c);
                start++;
            }

            if (start >= end)
                return start;

            start++;
            int paramCount = 0;

            bool hasSubtypeArity;
            do
            {
                int currParamCount = 0;
                hasSubtypeArity = false;
                // Skip arity.
                while (start < end)
                {
                    char c = name[start];
                    if (c < '0' || c > '9')
                        break;

                    currParamCount = currParamCount * 10 + c - '0';
                    start++;
                }

                paramCount += currParamCount;
                if (start >= end)
                    return start;

                if (name[start] == '+')
                {
                    while (start < end && name[start] != '[')
                    {
                        if (name[start] == '`')
                        {
                            start++;
                            hasSubtypeArity = true;
                            break;
                        }

                        sb.Append(name[start]);
                        start++;
                    }

                    if (start >= end)
                        return start;
                }
            } while (hasSubtypeArity);

            if (name[start] == '[')
            {
                sb.Append('<');
                start++;
                while (paramCount-- > 0)
                {
                    if (start >= end)
                        return start;

                    bool withModule = false;
                    if (name[start] == '[')
                    {
                        withModule = true;
                        start++;
                    }

                    start = FixGenericsWorker(name, start, end, sb);

                    if (start < end && name[start] == '[')
                    {
                        start++;
                        if (start >= end)
                            return start;

                        sb.Append('[');

                        while (start < end && name[start] == ',')
                        {
                            sb.Append(',');
                            start++;
                        }

                        if (start >= end)
                            return start;

                        if (name[start] == ']')
                        {
                            sb.Append(']');
                            start++;
                        }
                    }

                    if (withModule)
                    {
                        while (start < end && name[start] != ']')
                            start++;
                        start++;
                    }

                    if (paramCount > 0)
                    {
                        if (start >= end)
                            return start;

                        //Debug.Assert(name[start] == ',');
                        sb.Append(',');
                        start++;

                        if (start >= end)
                            return start;

                        if (name[start] == ' ')
                            start++;
                    }
                }

                sb.Append('>');
                start++;
            }

            if (start + 1 >= end)
                return start;

            if (name[start] == '[' && name[start + 1] == ']')
                sb.Append("[]");

            return start;
        }

        internal static string FixGenerics(string name)
        {
            StringBuilder builder = new StringBuilder();
            FixGenericsWorker(name, 0, name.Length, builder);
            return builder.ToString();
        }

        internal DesktopHeapType(Func<string> typeNameFactory, DesktopModule module, uint token, ulong mt, IMethodTableData mtData, DesktopGCHeap heap, IMethodTableCollectibleData mtCollectibleData = null)
            : base(mt, heap, module, token)
        {
            _name = new Lazy<string>(typeNameFactory);

            Shared = mtData.Shared;
            _parent = mtData.Parent;
            _baseSize = mtData.BaseSize;
            _componentSize = mtData.ComponentSize;
            _containsPointers = mtData.ContainsPointers;

            if (mtCollectibleData != null)
            {
                _isCollectible = mtCollectibleData.Collectible;
                _loaderAllocatorObjectHandle = mtCollectibleData.LoaderAllocatorObjectHandle;
            }
        }

        public DesktopHeapType(ulong mt, DesktopGCHeap heap, DesktopBaseModule module, uint token) : base(mt, heap, module, token)
        {
        }
        private void InitFlags()
        {
            if (_attributes != 0 || DesktopModule == null)
                return;

            MetaDataImport import = DesktopModule.GetMetadataImport();
            if (import == null)
            {
                _attributes = (TypeAttributes)0x70000000;
                return;
            }

            if (!import.GetTypeDefAttributes((int)_token, out _attributes) || _attributes == 0)
                _attributes = (TypeAttributes)0x70000000;
        }

        public override bool IsInternal
        {
            get
            {
                if (_attributes == 0)
                    InitFlags();

                TypeAttributes visibility = _attributes & TypeAttributes.VisibilityMask;
                return visibility == TypeAttributes.NestedAssembly || visibility == TypeAttributes.NotPublic;
            }
        }

        public override bool IsPublic
        {
            get
            {
                if (_attributes == 0)
                    InitFlags();

                TypeAttributes visibility = _attributes & TypeAttributes.VisibilityMask;
                return visibility == TypeAttributes.Public || visibility == TypeAttributes.NestedPublic;
            }
        }

        public override bool IsPrivate
        {
            get
            {
                if (_attributes == 0)
                    InitFlags();

                TypeAttributes visibility = _attributes & TypeAttributes.VisibilityMask;
                return visibility == TypeAttributes.NestedPrivate;
            }
        }

        public override bool IsProtected
        {
            get
            {
                if (_attributes == 0)
                    InitFlags();

                TypeAttributes visibility = _attributes & TypeAttributes.VisibilityMask;
                return visibility == TypeAttributes.NestedFamily;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                if (_attributes == 0)
                    InitFlags();

                return (_attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                if (_attributes == 0)
                    InitFlags();

                return (_attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;
            }
        }

        public override bool IsInterface
        {
            get
            {
                if (_attributes == 0)
                    InitFlags();
                return (_attributes & TypeAttributes.Interface) == TypeAttributes.Interface;
            }
        }

        internal override ulong GetModuleAddress(ClrAppDomain appDomain)
        {
            if (DesktopModule == null)
                return 0;

            return DesktopModule.GetDomainModule(appDomain);
        }

        public override bool IsRuntimeType
        {
            get
            {
                if (_runtimeType == null)
                    _runtimeType = Name == "System.RuntimeType";

                return (bool)_runtimeType;
            }
        }

        public override ClrType GetRuntimeType(ulong obj)
        {
            if (!IsRuntimeType)
                return null;

            ClrInstanceField field = GetFieldByName("m_handle");
            if (field == null)
                return null;

            ulong methodTable = 0;
            if (field.ElementType == ClrElementType.NativeInt)
            {
                methodTable = (ulong)(long)field.GetValue(obj);
            }
            else if (field.ElementType == ClrElementType.Struct)
            {
                ClrInstanceField ptrField = field.Type.GetFieldByName("m_ptr");
                methodTable = (ulong)(long)ptrField.GetValue(field.GetAddress(obj, false), true);
            }

            return DesktopHeap.GetTypeByMethodTable(methodTable, 0, obj);
        }

        internal void InitMethodHandles()
        {
            DesktopRuntimeBase runtime = DesktopHeap.DesktopRuntime;
            foreach (ulong methodTable in EnumerateMethodTables())
            {
                foreach (ulong methodDesc in runtime.GetMethodDescList(methodTable))
                {
                    IMethodDescData data = runtime.GetMethodDescData(methodDesc);
                    DesktopMethod method = (DesktopMethod)GetMethod(data.MDToken);
                    if (method.Type != this)
                        continue;

                    method.AddMethodHandle(methodDesc);
                }
            }
        }
    }
}