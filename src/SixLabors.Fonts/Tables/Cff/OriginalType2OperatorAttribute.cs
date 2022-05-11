// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    internal class OriginalType2OperatorAttribute : Attribute
    {
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        public OriginalType2OperatorAttribute(Type2Operator1 _)
        {
        }

        public OriginalType2OperatorAttribute(Type2Operator2 _)
        {
        }
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    }
}
