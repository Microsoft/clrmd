﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.Diagnostics.Runtime
{
    // TODO:  Make struct

    /// <summary>
    /// Information about a specific PDB instance obtained from a PE image.
    /// </summary>
    public sealed class PdbInfo :
#nullable disable // to enable use with both T and T? for reference types due to IEquatable<T> being invariant
        IEquatable<PdbInfo>
#nullable restore
    {
        /// <summary>
        /// The Guid of the PDB.
        /// </summary>
        public Guid Guid { get; }

        /// <summary>
        /// The pdb revision.
        /// </summary>
        public int Revision { get; }

        /// <summary>
        /// The file name of the pdb.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Creates an instance of the PdbInfo class with the corresponding properties initialized.
        /// </summary>
        public PdbInfo(string fileName, Guid guid, int rev)
        {
            FileName = fileName;
            Guid = guid;
            Revision = rev;
        }

        /// <summary>
        /// GetHashCode implementation.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Guid.GetHashCode() ^ Revision;
        }

        /// <summary>
        /// Override for Equals.  Returns true if the guid, age, and file names equal.  Note that this compares only the.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if the objects match, false otherwise.</returns>
        public override bool Equals(object? obj) => Equals(obj as PdbInfo);

        public bool Equals(PdbInfo? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null)
                return false;

            if (Revision == other.Revision && Guid == other.Guid)
            {
                string thisFileName = Path.GetFileName(FileName);
                string otherFileName = Path.GetFileName(other.FileName);
                return thisFileName.Equals(otherFileName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// To string implementation.
        /// </summary>
        /// <returns>Printing friendly version.</returns>
        public override string ToString()
        {
            return $"{Guid} {Revision} {FileName}";
        }

        public static bool operator ==(PdbInfo? left, PdbInfo? right)
        {
            if (right is null)
                return left is null;

            return right.Equals(left);
        }

        public static bool operator !=(PdbInfo? left, PdbInfo? right) => !(left == right);
    }
}