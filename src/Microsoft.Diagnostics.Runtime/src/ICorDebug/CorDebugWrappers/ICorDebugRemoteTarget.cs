using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug
{
    [ComImport]
    [Guid("C3ED8383-5A49-4cf5-B4B7-01864D9E582D")]
    [InterfaceType(1)]
    public interface ICorDebugRemoteTarget
    {
        //
        void GetHostName(
            [In] uint cchHostName,
            [Out] out uint pcchHostName,
            [In][Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            char[] szHostName);
    }
}