// Copyright (c) 2013-2015 Cemalettin Dervis, MIT License.
// https://github.com/cemdervis/SharpConfig

using System;

namespace SharpConfig
{
    internal sealed class SettingValueCastException : Exception
    {
        public SettingValueCastException(string stringValue, Type destType, Exception innerException) : base(CreateMessage(stringValue, destType), innerException)
        {
            
        }

        private static string CreateMessage(string stringValue, Type destType) => $"Failed to convert value '{stringValue}' to type {destType.FullName}.";
    }
}