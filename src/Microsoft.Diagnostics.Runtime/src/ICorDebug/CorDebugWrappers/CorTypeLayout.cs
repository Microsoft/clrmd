// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug
{
    [StructLayout(LayoutKind.Sequential)]
    public struct COR_TYPE_LAYOUT
    {
        public COR_TYPEID parentID;
        public int objectSize;
        public int numFields;
        public int boxOffset;
        public CorElementType type;
    }
}