﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime
{
    public sealed class DacLibrary : IDisposable
    {
        private bool _disposed;
        private SosDac _sos;

        internal DacDataTargetWrapper DacDataTarget { get; }

        internal RefCountedFreeLibrary OwningLibrary { get; }

        internal ClrDataProcess InternalDacPrivateInterface { get; }

        public ClrDataProcess DacPrivateInterface => new ClrDataProcess(InternalDacPrivateInterface);

        internal SosDac GetSOSInterfaceNoAddRef()
        {
            if (_sos == null)
                _sos = InternalDacPrivateInterface.GetSOSDacInterface();

            return _sos;
        }

        public SosDac SOSDacInterface
        {
            get
            {
                SosDac sos = GetSOSInterfaceNoAddRef();

                return sos != null ? new SosDac(sos) : null;
            }
        }

        public T GetInterface<T>(ref Guid riid)
            where T : CallableComWrapper
        {
            IntPtr pUnknown = InternalDacPrivateInterface.QueryInterface(ref riid);
            if (pUnknown == IntPtr.Zero)
                return null;

            T t = (T)Activator.CreateInstance(typeof(T), this, pUnknown);
            return t;
        }

        internal static IntPtr TryGetDacPtr(object ix)
        {
            if (!(ix is IntPtr pUnk))
            {
                if (Marshal.IsComObject(ix))
                    pUnk = Marshal.GetIUnknownForObject(ix);
                else
                    pUnk = IntPtr.Zero;
            }

            if (pUnk == IntPtr.Zero)
                throw new ArgumentException("clrDataProcess not an instance of IXCLRDataProcess");

            return pUnk;
        }

        internal DacLibrary(DataTargetImpl dataTarget, IntPtr pUnk)
        {
            InternalDacPrivateInterface = new ClrDataProcess(this, pUnk);
        }

        public DacLibrary(DataTarget dataTarget, string dacDll)
        {
            if (dataTarget.ClrVersions.Count == 0)
                throw new ClrDiagnosticsException("Process is not a CLR process!");

            IntPtr dacLibrary = DataTarget.PlatformFunctions.LoadLibrary(dacDll);
            if (dacLibrary == IntPtr.Zero)
                throw new ClrDiagnosticsException("Failed to load dac: " + dacLibrary);

            OwningLibrary = new RefCountedFreeLibrary(dacLibrary);
            dataTarget.AddDacLibrary(this);

            IntPtr initAddr = DataTarget.PlatformFunctions.GetProcAddress(dacLibrary, "DAC_PAL_InitializeDLL");
            if (initAddr == IntPtr.Zero)
                initAddr = DataTarget.PlatformFunctions.GetProcAddress(dacLibrary, "PAL_InitializeDLL");

            if (initAddr != IntPtr.Zero)
            {
                IntPtr dllMain = DataTarget.PlatformFunctions.GetProcAddress(dacLibrary, "DllMain");
                DllMain main = (DllMain)Marshal.GetDelegateForFunctionPointer(dllMain, typeof(DllMain));
                int result = main(dacLibrary, 1, IntPtr.Zero);
            }

            IntPtr addr = DataTarget.PlatformFunctions.GetProcAddress(dacLibrary, "CLRDataCreateInstance");
            DacDataTarget = new DacDataTargetWrapper(dataTarget);

            CreateDacInstance func = (CreateDacInstance)Marshal.GetDelegateForFunctionPointer(addr, typeof(CreateDacInstance));
            Guid guid = new Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7");
            int res = func(ref guid, DacDataTarget.IDacDataTarget, out IntPtr iUnk);

            if (res != 0)
                throw new ClrDiagnosticsException("Failure loading DAC: CreateDacInstance failed 0x" + res.ToString("x"), ClrDiagnosticsException.HR.DacError);

            InternalDacPrivateInterface = new ClrDataProcess(this, iUnk);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DacLibrary()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                InternalDacPrivateInterface?.Dispose();
                _sos?.Dispose();
                OwningLibrary?.Release();

                _disposed = true;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int DllMain(IntPtr instance, int reason, IntPtr reserved);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PAL_Initialize();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateDacInstance(
            ref Guid riid,
            IntPtr dacDataInterface,
            out IntPtr ppObj);
    }
}