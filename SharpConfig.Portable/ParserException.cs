// Copyright (c) 2013-2015 Cemalettin Dervis, MIT License.
// https://github.com/cemdervis/SharpConfig

using System;

namespace SharpConfig
{
    /// <summary>
    /// Represents an error that occurred during
    /// the configuration parsing stage.
    /// </summary>
    public sealed class ParserException : Exception
    {
        internal ParserException(string message, int line) : base($"Line {line}: {message}")
        {
            
        }
    }
}