﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Runtime.Linux
{
    internal class ElfSymbolTable
    {
        private readonly Reader _reader;
        private readonly ulong _address;
        private readonly bool _is64Bit;
        private readonly ElfStringTable _stringTable;
        private readonly uint _symSize;

        public ElfSymbolTable(Reader reader, bool is64Bit, ulong address, ElfStringTable stringTable)
        {
            _reader = reader;
            _address = address;
            _is64Bit = is64Bit;
            _symSize = is64Bit ? 24 : 16;
            _stringTable = stringTable;
        }

        public ElfSymbol GetSymbol(uint index)
        {
            if (_is64Bit)
            {
                ElfSymbol64 s = _reader.Read<ElfSymbol64>(_address + index * _symSize);
                string name = _stringTable.GetStringAtIndex(s.Name);
                ElfSymbolBind b = (ElfSymbolBind)(s.Info >> 4);
                ElfSymbolType t = (ElfSymbolType)(s.Info & 0xF);
                return new ElfSymbol(name, b, t, s.Value, (long)s.Size);
            }
            else
            {
                ElfSymbol32 s = _reader.Read<ElfSymbol32>(_address + index * _symSize);
                string name = _stringTable.GetStringAtIndex(s.Name);
                ElfSymbolBind b = (ElfSymbolBind)(s.Info >> 4);
                ElfSymbolType t = (ElfSymbolType)(s.Info & 0xF);
                return new ElfSymbol(name, b, t, (long)s.Value, s.Size);
            }
        }
    }
}