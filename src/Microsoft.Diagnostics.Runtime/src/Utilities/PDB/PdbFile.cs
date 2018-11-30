// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities.Pdb
{
    internal class PdbFile
    {
        private PdbFile() // This class can't be instantiated.
        {
        }

        private static void LoadGuidStream(
            BitAccess bits,
            out Guid doctype,
            out Guid language,
            out Guid vendor,
            out Guid algorithmId,
            out byte[] checksum,
            out byte[] embeddedSource)
        {
            bits.ReadGuid(out language);
            bits.ReadGuid(out vendor);
            bits.ReadGuid(out doctype);
            bits.ReadGuid(out algorithmId);

            int checksumSize;
            bits.ReadInt32(out checksumSize);
            int sourceSize;
            bits.ReadInt32(out sourceSize);
            checksum = new byte[checksumSize];
            bits.ReadBytes(checksum);
            embeddedSource = new byte[sourceSize];
            bits.ReadBytes(embeddedSource);
        }

        private static Dictionary<string, int> LoadNameIndex(BitAccess bits, out int ver, out int sig, out int age, out Guid guid)
        {
            var result = new Dictionary<string, int>();
            bits.ReadInt32(out ver); //  0..3  Version
            bits.ReadInt32(out sig); //  4..7  Signature
            bits.ReadInt32(out age); //  8..11 Age
            bits.ReadGuid(out guid); // 12..27 GUID

            //if (ver != 20000404) {
            //  throw new PdbDebugException("Unsupported PDB Stream version {0}", ver);
            //}

            // Read string buffer.
            int buf;
            bits.ReadInt32(out buf); // 28..31 Bytes of Strings

            var beg = bits.Position;
            var nxt = bits.Position + buf;

            bits.Position = nxt;

            // Read map index.
            int cnt; // n+0..3 hash size.
            int max; // n+4..7 maximum ni.

            bits.ReadInt32(out cnt);
            bits.ReadInt32(out max);

            var present = new BitSet(bits);
            var deleted = new BitSet(bits);
            if (!deleted.IsEmpty)
            {
                throw new PdbDebugException("Unsupported PDB deleted bitset is not empty.");
            }

            var j = 0;
            for (var i = 0; i < max; i++)
            {
                if (present.IsSet(i))
                {
                    int ns;
                    int ni;
                    bits.ReadInt32(out ns);
                    bits.ReadInt32(out ni);

                    string name;
                    var saved = bits.Position;
                    bits.Position = beg + ns;
                    bits.ReadCString(out name);
                    bits.Position = saved;

                    result.Add(name.ToUpperInvariant(), ni);
                    j++;
                }
            }

            if (j != cnt)
            {
                throw new PdbDebugException("Count mismatch. ({0} != {1})", j, cnt);
            }

            return result;
        }

        private static Dictionary<int, string> LoadNameStream(BitAccess bits)
        {
            var ht = new Dictionary<int, string>();

            uint sig;
            int ver;
            bits.ReadUInt32(out sig); //  0..3  Signature
            bits.ReadInt32(out ver); //  4..7  Version

            // Read (or skip) string buffer.
            int buf;
            bits.ReadInt32(out buf); //  8..11 Bytes of Strings

            if (sig != 0xeffeeffe || ver != 1)
            {
                throw new PdbDebugException(
                    "Unsupported Name Stream version. " +
                    "(sig={0:x8}, ver={1})",
                    sig,
                    ver);
            }

            var beg = bits.Position;
            var nxt = bits.Position + buf;
            bits.Position = nxt;

            // Read hash table.
            int siz;
            bits.ReadInt32(out siz); // n+0..3 Number of hash buckets.
            nxt = bits.Position;

            for (var i = 0; i < siz; i++)
            {
                int ni;
                string name;

                bits.ReadInt32(out ni);

                if (ni != 0)
                {
                    var saved = bits.Position;
                    bits.Position = beg + ni;
                    bits.ReadCString(out name);
                    bits.Position = saved;

                    ht.Add(ni, name);
                }
            }

            bits.Position = nxt;

            return ht;
        }

        private static readonly PdbFunction s_match = new PdbFunction();

        private static int FindFunction(PdbFunction[] funcs, ushort sec, uint off)
        {
            s_match.Segment = sec;
            s_match.Address = off;

            return Array.BinarySearch(funcs, s_match, PdbFunction.byAddress);
        }

        private static void LoadManagedLines(
            PdbFunction[] funcs,
            Dictionary<int, string> names,
            BitAccess bits,
            MsfDirectory dir,
            Dictionary<string, int> nameIndex,
            PdbStreamHelper reader,
            uint limit,
            Dictionary<string, PdbSource> sources)
        {
            Array.Sort(funcs, PdbFunction.byAddressAndToken);

            var checks = new Dictionary<int, PdbSource>();

            // Read the files first
            var begin = bits.Position;
            while (bits.Position < limit)
            {
                int sig;
                int siz;
                bits.ReadInt32(out sig);
                bits.ReadInt32(out siz);
                var place = bits.Position;
                var endSym = bits.Position + siz;

                switch ((DEBUG_S_SUBSECTION)sig)
                {
                    case DEBUG_S_SUBSECTION.FILECHKSMS:
                        while (bits.Position < endSym)
                        {
                            CV_FileCheckSum chk;

                            var ni = bits.Position - place;
                            bits.ReadUInt32(out chk.name);
                            bits.ReadUInt8(out chk.len);
                            bits.ReadUInt8(out chk.type);

                            var name = names[(int)chk.name];
                            PdbSource src;
                            if (!sources.TryGetValue(name.ToUpperInvariant(), out src))
                            {
                                int guidStream;
                                var doctypeGuid = Guid.Empty;
                                var languageGuid = Guid.Empty;
                                var vendorGuid = Guid.Empty;
                                var algorithmId = Guid.Empty;
                                byte[] checksum = null;
                                byte[] source = null;

                                if (nameIndex.TryGetValue("/SRC/FILES/" + name.ToUpperInvariant(), out guidStream))
                                {
                                    var guidBits = new BitAccess(0x100);
                                    dir._streams[guidStream].Read(reader, guidBits);
                                    LoadGuidStream(guidBits, out doctypeGuid, out languageGuid, out vendorGuid, out algorithmId, out checksum, out source);
                                }

                                src = new PdbSource( /*(uint)ni,*/ name, doctypeGuid, languageGuid, vendorGuid, algorithmId, checksum, source);
                                sources.Add(name.ToUpperInvariant(), src);
                            }

                            checks.Add(ni, src);
                            bits.Position += chk.len;
                            bits.Align(4);
                        }

                        bits.Position = endSym;
                        break;

                    default:
                        bits.Position = endSym;
                        break;
                }
            }

            // Read the lines next.
            bits.Position = begin;
            while (bits.Position < limit)
            {
                int sig;
                int siz;
                bits.ReadInt32(out sig);
                bits.ReadInt32(out siz);
                var endSym = bits.Position + siz;

                switch ((DEBUG_S_SUBSECTION)sig)
                {
                    case DEBUG_S_SUBSECTION.LINES:
                    {
                        CV_LineSection sec;

                        bits.ReadUInt32(out sec.off);
                        bits.ReadUInt16(out sec.sec);
                        bits.ReadUInt16(out sec.flags);
                        bits.ReadUInt32(out sec.cod);
                        var funcIndex = FindFunction(funcs, sec.sec, sec.off);
                        if (funcIndex < 0) break;

                        var func = funcs[funcIndex];
                        if (func.SequencePoints == null)
                        {
                            while (funcIndex > 0)
                            {
                                var f = funcs[funcIndex - 1];
                                if (f.SequencePoints != null || f.Segment != sec.sec || f.Address != sec.off) break;

                                func = f;
                                funcIndex--;
                            }
                        }
                        else
                        {
                            while (funcIndex < funcs.Length - 1 && func.SequencePoints != null)
                            {
                                var f = funcs[funcIndex + 1];
                                if (f.Segment != sec.sec || f.Address != sec.off) break;

                                func = f;
                                funcIndex++;
                            }
                        }

                        if (func.SequencePoints != null) break;

                        // Count the line blocks.
                        var begSym = bits.Position;
                        var blocks = 0;
                        while (bits.Position < endSym)
                        {
                            CV_SourceFile file;
                            bits.ReadUInt32(out file.index);
                            bits.ReadUInt32(out file.count);
                            bits.ReadUInt32(out file.linsiz); // Size of payload.
                            var linsiz = (int)file.count * (8 + ((sec.flags & 1) != 0 ? 4 : 0));
                            bits.Position += linsiz;
                            blocks++;
                        }

                        func.SequencePoints = new PdbSequencePointCollection[blocks];
                        var block = 0;

                        bits.Position = begSym;
                        while (bits.Position < endSym)
                        {
                            CV_SourceFile file;
                            bits.ReadUInt32(out file.index);
                            bits.ReadUInt32(out file.count);
                            bits.ReadUInt32(out file.linsiz); // Size of payload.

                            var src = checks[(int)file.index];
                            var tmp = new PdbSequencePointCollection(src, file.count);
                            func.SequencePoints[block++] = tmp;
                            var lines = tmp.Lines;

                            var plin = bits.Position;
                            var pcol = bits.Position + 8 * (int)file.count;

                            for (var i = 0; i < file.count; i++)
                            {
                                CV_Line line;
                                var column = new CV_Column();

                                bits.Position = plin + 8 * i;
                                bits.ReadUInt32(out line.offset);
                                bits.ReadUInt32(out line.flags);

                                var lineBegin = line.flags & (uint)CV_Line_Flags.linenumStart;
                                var delta = (line.flags & (uint)CV_Line_Flags.deltaLineEnd) >> 24;
                                //bool statement = ((line.flags & (uint)CV_Line_Flags.fStatement) == 0);
                                if ((sec.flags & 1) != 0)
                                {
                                    bits.Position = pcol + 4 * i;
                                    bits.ReadUInt16(out column.offColumnStart);
                                    bits.ReadUInt16(out column.offColumnEnd);
                                }

                                lines[i] = new PdbSequencePoint(
                                    line.offset,
                                    lineBegin,
                                    column.offColumnStart,
                                    lineBegin + delta,
                                    column.offColumnEnd);
                            }
                        }

                        break;
                    }
                }

                bits.Position = endSym;
            }
        }

        private static void LoadFuncsFromDbiModule(
            BitAccess bits,
            DbiModuleInfo info,
            Dictionary<int, string> names,
            List<PdbFunction> funcList,
            bool readStrings,
            MsfDirectory dir,
            Dictionary<string, int> nameIndex,
            PdbStreamHelper reader,
            Dictionary<string, PdbSource> sources)
        {
            PdbFunction[] funcs = null;
            bits.Position = 0;
            int sig;
            bits.ReadInt32(out sig);
            if (sig != 4)
            {
                throw new PdbDebugException("Invalid signature. (sig={0})", sig);
            }

            bits.Position = 4;
            // Console.WriteLine("{0}:", info.moduleName);
            funcs = PdbFunction.LoadManagedFunctions( /*info.moduleName,*/
                bits,
                (uint)info.cbSyms,
                readStrings);
            if (funcs != null)
            {
                bits.Position = info.cbSyms + info.cbOldLines;
                LoadManagedLines(
                    funcs,
                    names,
                    bits,
                    dir,
                    nameIndex,
                    reader,
                    (uint)(info.cbSyms + info.cbOldLines + info.cbLines),
                    sources);

                for (var i = 0; i < funcs.Length; i++)
                {
                    funcList.Add(funcs[i]);
                }
            }
        }

        private static void LoadDbiStream(
            BitAccess bits,
            out DbiModuleInfo[] modules,
            out DbiDbgHdr header,
            bool readStrings)
        {
            var dh = new DbiHeader(bits);
            header = new DbiDbgHdr();

            //if (dh.sig != -1 || dh.ver != 19990903) {
            //  throw new PdbException("Unsupported DBI Stream version, sig={0}, ver={1}",
            //                         dh.sig, dh.ver);
            //}

            // Read gpmod section.
            var modList = new List<DbiModuleInfo>();
            var end = bits.Position + dh.gpmodiSize;
            while (bits.Position < end)
            {
                var mod = new DbiModuleInfo(bits, readStrings);
                modList.Add(mod);
            }

            if (bits.Position != end)
            {
                throw new PdbDebugException(
                    "Error reading DBI stream, pos={0} != {1}",
                    bits.Position,
                    end);
            }

            if (modList.Count > 0)
            {
                modules = modList.ToArray();
            }
            else
            {
                modules = null;
            }

            // Skip the Section Contribution substream.
            bits.Position += dh.secconSize;

            // Skip the Section Map substream.
            bits.Position += dh.secmapSize;

            // Skip the File Info substream.
            bits.Position += dh.filinfSize;

            // Skip the TSM substream.
            bits.Position += dh.tsmapSize;

            // Skip the EC substream.
            bits.Position += dh.ecinfoSize;

            // Read the optional header.
            end = bits.Position + dh.dbghdrSize;
            if (dh.dbghdrSize > 0)
            {
                header = new DbiDbgHdr(bits);
            }

            bits.Position = end;
        }

        internal static PdbFunction[] LoadFunctions(
            Stream read,
            bool readAllStrings,
            out int ver,
            out int sig,
            out int age,
            out Guid guid,
            out IEnumerable<PdbSource> sources)
        {
            var bits = new BitAccess(512 * 1024);
            return LoadFunctions(read, bits, readAllStrings, out ver, out sig, out age, out guid, out sources);
        }

        internal static PdbFunction[] LoadFunctions(
            Stream read,
            BitAccess bits,
            bool readAllStrings,
            out int ver,
            out int sig,
            out int age,
            out Guid guid,
            out IEnumerable<PdbSource> sources)
        {
            sources = null;
            var head = new PdbFileHeader(read, bits);
            var reader = new PdbStreamHelper(read, head.PageSize);
            var dir = new MsfDirectory(reader, head, bits);
            DbiModuleInfo[] modules = null;
            DbiDbgHdr header;

            dir._streams[1].Read(reader, bits);
            var nameIndex = LoadNameIndex(bits, out ver, out sig, out age, out guid);
            int nameStream;
            if (!nameIndex.TryGetValue("/NAMES", out nameStream))
            {
                throw new PdbException("No `name' stream");
            }

            dir._streams[nameStream].Read(reader, bits);
            var names = LoadNameStream(bits);

            dir._streams[3].Read(reader, bits);
            LoadDbiStream(bits, out modules, out header, readAllStrings);

            var funcList = new List<PdbFunction>();
            var sourceDictionary = new Dictionary<string, PdbSource>();
            if (modules != null)
            {
                for (var m = 0; m < modules.Length; m++)
                {
                    if (modules[m].stream > 0)
                    {
                        dir._streams[modules[m].stream].Read(reader, bits);
                        LoadFuncsFromDbiModule(
                            bits,
                            modules[m],
                            names,
                            funcList,
                            readAllStrings,
                            dir,
                            nameIndex,
                            reader,
                            sourceDictionary);
                    }
                }
            }

            var funcs = funcList.ToArray();
            sources = sourceDictionary.Values;

            // After reading the functions, apply the token remapping table if it exists.
            if (header.snTokenRidMap != 0 && header.snTokenRidMap != 0xffff)
            {
                dir._streams[header.snTokenRidMap].Read(reader, bits);
                var ridMap = new uint[dir._streams[header.snTokenRidMap].Length / 4];
                bits.ReadUInt32(ridMap);

                foreach (var func in funcs)
                {
                    func.Token = 0x06000000 | ridMap[func.Token & 0xffffff];
                }
            }

            //
            Array.Sort(funcs, PdbFunction.byAddressAndToken);
            //Array.Sort(funcs, PdbFunction.byToken);
            return funcs;
        }
    }
}