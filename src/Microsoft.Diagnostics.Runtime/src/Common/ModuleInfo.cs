﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime
{
    /// <summary>
    /// Provides information about loaded modules in a DataTarget
    /// </summary>
    [Serializable]
    public class ModuleInfo
    {
        /// <summary>
        /// The base address of the object.
        /// </summary>
        public virtual ulong ImageBase { get; set; }

        /// <summary>
        /// The file size of the image.
        /// </summary>
        public virtual uint FileSize { get; set; }

        /// <summary>
        /// The build timestamp of the image.
        /// </summary>
        public virtual uint TimeStamp { get; set; }

        /// <summary>
        /// The filename of the module on disk.
        /// </summary>
        public virtual string FileName { get; set; }

        /// <summary>
        /// Returns a PEImage from a stream constructed using instance fields of this object.
        /// If the PEImage cannot be constructed, null is returned.
        /// </summary>
        /// <returns></returns>
        public PEImage GetPEImage()
        {
            try
            {
                return new PEImage(new ReadVirtualStream(_dataReader, (long)ImageBase, FileSize), isVirtual: true);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The Linux BuildId of this module.  This will be null if the module does not have a BuildId.
        /// </summary>
        public byte[] BuildId { get; internal set; }

        /// <summary>
        /// Whether the module is managed or not.
        /// </summary>
        public virtual bool IsManaged
        {
            get
            {
                InitData();
                return _managed;
            }

            internal set => _managed = value;
        }

        /// <summary>
        /// To string.
        /// </summary>
        /// <returns>The filename of the module.</returns>
        public override string ToString()
        {
            return FileName;
        }

        /// <summary>
        /// The PDB associated with this module.
        /// </summary>
        public PdbInfo Pdb
        {
            get
            {
                InitData();
                return _pdb;
            }

            set => _pdb = value;
        }

        private void InitData()
        {
            if (_initialized)
                return;

            _initialized = true;

            if (_dataReader == null)
                return;
            
            try
            {
                PEImage image = GetPEImage();
                if (image != null && image.IsValid)
                {
                    _managed = image.OptionalHeader.ComDescriptorDirectory.VirtualAddress != 0;
                    _pdb = image.DefaultPdb;
                }
            }
            catch
            {
            }
        }

        protected virtual void InitVersion(out VersionInfo version)
        {
            _dataReader.GetVersionInfo(ImageBase, out version);
        }

        /// <summary>
        /// The version information for this file.
        /// </summary>
        public VersionInfo Version
        {
            get
            {
                if (_versionInit || _dataReader == null)
                    return _version;

                InitVersion(out _version);
                _versionInit = true;
                return _version;
            }

            set
            {
                _version = value;
                _versionInit = true;
            }
        }

        /// <summary>
        /// Empty constructor for serialization.
        /// </summary>
        public ModuleInfo()
        {
        }

        /// <summary>
        /// Creates a ModuleInfo object with an IDataReader instance.  This is used when
        /// lazily evaluating VersionInfo.
        /// </summary>
        /// <param name="reader">The data reader</param>
        /// <param name="version">The version of this module</param>
        public ModuleInfo(IDataReader reader, VersionInfo? version = null)
        {
            _dataReader = reader;
            if (version.HasValue)
            {
                _versionInit = true;
                _version = version.Value;
            }
        }

        [NonSerialized]
        protected readonly IDataReader _dataReader;
        private PdbInfo _pdb;
        protected bool _initialized;
        private bool _managed;
        private VersionInfo _version;
        private bool _versionInit;
    }
}