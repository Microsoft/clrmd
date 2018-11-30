﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable 1591

namespace Microsoft.Diagnostics.Runtime.Interop
{
    public enum SymTag : uint
    {
        Null,                //  0
        Exe,                 //  1
        Compiland,           //  2
        CompilandDetails,    //  3
        CompilandEnv,        //  4
        Function,            //  5
        Block,               //  6
        Data,                //  7
        Annotation,          //  8
        Label,               //  9
        PublicSymbol,        // 10
        UDT,                 // 11
        Enum,                // 12
        FunctionType,        // 13
        PointerType,         // 14
        ArrayType,           // 15
        BaseType,            // 16
        Typedef,             // 17
        BaseClass,           // 18
        Friend,              // 19
        FunctionArgType,     // 20
        FuncDebugStart,      // 21
        FuncDebugEnd,        // 22
        UsingNamespace,      // 23
        VTableShape,         // 24
        VTable,              // 25
        Custom,              // 26
        Thunk,               // 27
        CustomType,          // 28
        ManagedType,         // 29
        Dimension,           // 30
        CallSite,            // 31
        InlineSite,          // 32
        BaseInterface,       // 33
        VectorType,          // 34
        MatrixType,          // 35
        HLSLType,            // 36
        SymTagMax
    }
}