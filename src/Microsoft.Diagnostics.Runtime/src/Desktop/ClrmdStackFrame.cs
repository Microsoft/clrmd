﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Desktop
{
    // todo: tostring
    internal sealed class ClrmdStackFrame : ClrStackFrame
    {
        private readonly byte[] _context;

        public override ReadOnlySpan<byte> Context => _context;
        public override ulong InstructionPointer { get; }
        public override ulong StackPointer { get; }
        public override ClrStackFrameType Kind { get; }
        public override ClrMethod Method { get; }
        public override string FrameName { get; }

        public ClrmdStackFrame(byte[] context, ulong ip, ulong sp, ClrStackFrameType kind, ClrMethod method, string frameName)
        {
            _context = context;
            InstructionPointer = ip;
            StackPointer = sp;
            Kind = kind;
            Method = method;
            FrameName = frameName;
        }


        public override string ToString()
        {
            if (Kind == ClrStackFrameType.ManagedMethod)
                return Method.Signature;

            int methodLen = 0;
            int methodTypeLen = 0;

            if (Method != null)
            {
                methodLen = Method.Name.Length;
                if (Method.Type != null)
                    methodTypeLen = Method.Type.Name.Length;
            }

            StringBuilder sb = new StringBuilder(FrameName.Length + methodLen + methodTypeLen + 10);

            sb.Append('[');
            sb.Append(FrameName);
            sb.Append(']');

            if (Method != null)
            {
                sb.Append(" (");

                if (Method.Type != null)
                {
                    sb.Append(Method.Type.Name);
                    sb.Append('.');
                }

                sb.Append(Method.Name);
                sb.Append(')');
            }

            return sb.ToString();
        }
    }
}