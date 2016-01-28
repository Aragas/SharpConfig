// Copyright (c) 2013-2015 Cemalettin Dervis, MIT License.
// https://github.com/cemdervis/SharpConfig

using System;
using System.Linq;
using System.Reflection;

namespace SharpConfig
{
    /// <summary>
    /// Represents a setting in a <see cref="Configuration"/>.
    /// Settings are always stored in a <see cref="Section"/>.
    /// </summary>
    public sealed class Setting : ConfigurationElement
    {
        #region Fields

        private string _rawValue;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Setting"/> class.
        /// </summary>
        public Setting(string name) : this(name, string.Empty)
        {
            _rawValue = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Setting"/> class.
        /// </summary>
        ///
        /// <param name="name"> The name of the setting.</param>
        /// <param name="value">The value of the setting.</param>
        public Setting(string name, string value) : base(name)
        {
            _rawValue = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the raw string value of this setting.
        /// </summary>
        public string StringValue
        {
            get { return _rawValue; }
            set { _rawValue = value; }
        }

        /// <summary>
        /// Gets or sets the value of this setting as an int.
        /// Note: this is a shortcut to GetValue and SetValue.
        /// </summary>
        public int IntValue
        {
            get { return GetValueTyped<int>(); }
            set { SetValue(value); }
        }

        /// <summary>
        /// Gets or sets the value of this setting as a float.
        /// Note: this is a shortcut to GetValue and SetValue.
        /// </summary>
        public float FloatValue
        {
            get { return GetValueTyped<float>(); }
            set { SetValue(value); }
        }

        /// <summary>
        /// Gets or sets the value of this setting as a double.
        /// Note: this is a shortcut to GetValue and SetValue.
        /// </summary>
        public double DoubleValue
        {
            get { return GetValueTyped<double>(); }
            set { SetValue(value); }
        }

        /// <summary>
        /// Gets or sets the value of this setting as a bool.
        /// Note: this is a shortcut to GetValue and SetValue.
        /// </summary>
        public bool BoolValue
        {
            get { return GetValueTyped<bool>(); }
            set { SetValue(value); }
        }

        /// <summary>
        /// Gets a value indicating whether this setting is an array.
        /// </summary>
        public bool IsArray => ArraySize >= 0;

        /// <summary>
        /// Gets the size of the array that this setting represents.
        /// If this setting is not an array, -1 is returned.
        /// </summary>
        public int ArraySize
        {
            get
            {
                if (string.IsNullOrEmpty(_rawValue))
                    return -1;
                
                string value = _rawValue.Trim();

                if (value[0] != '{')
                    return -1;
                
                int arraySize = 0;
                bool isInArrayBrackets = false;
                int lastCommaIdx = 0;

                for (int pos = 0; pos < value.Length; ++pos)
                {
                    char ch = value[pos];
                    
                    if (ch == '{')
                    {
                        if (isInArrayBrackets)
                            return -1;
                        
                        isInArrayBrackets = true;
                    }
                    else if (ch == '}')
                    {
                        if (pos != value.Length - 1)
                            return -1;
                        
                        isInArrayBrackets = false;
                        break;
                    }
                    else if (ch == ',')
                    {
                        bool isElementEmpty = true;

                        for (int e = lastCommaIdx + 1; e < pos; ++e)
                        {
                            if (value[e] != ' ')
                            {
                                // Okay, this is a value.
                                isElementEmpty = false;
                                break;
                            }
                        }

                        if (isElementEmpty)
                            return -1;
                        
                        lastCommaIdx = pos;
                        ++arraySize;
                    }
                }

                // Check the last element value for emptiness, since our loop
                // only considered n-1 elements.
                if (lastCommaIdx + 1 < value.Length-1)
                {
                    bool isElementEmpty = true;

                    for (int e = lastCommaIdx + 1; e < value.Length - 1; ++e)
                    {
                        if (value[e] != ' ')
                        {
                            isElementEmpty = false;
                            break;
                        }
                    }

                    if (isElementEmpty)
                        return -1;
                }
                else
                    return -1;

                return arraySize + 1;
            }
        }

        #endregion

        #region GetValueTyped

        /// <summary>
        /// Gets this setting's value as a specific type.
        /// </summary>
        ///
        /// <typeparam name="T">The type of the object to retrieve.</typeparam>
        public T GetValueTyped<T>()
        {
            Type type = typeof(T);

            if (type.IsArray)
                throw new InvalidOperationException("To obtain an array value, use GetValueArray instead of GetValueTyped.");
            
            if (IsArray)
                throw new InvalidOperationException("The setting represents an array. Use GetValueArray to obtain its value.");
            
            return (T)ConvertValue(_rawValue, type);
        }

        /// <summary>
        /// Gets this setting's value as a specific type.
        /// </summary>
        ///
        /// <param name="type">The type of the object to retrieve.</param>
        public object GetValueTyped(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            if (type.IsArray)
                throw new InvalidOperationException("To obtain an array value, use GetValueArray instead of GetValueTyped.");
            
            if (IsArray)
                throw new InvalidOperationException("The setting represents an array. Use GetValueArray to obtain its value.");
            
            return ConvertValue(_rawValue, type);
        }

        /// <summary>
        /// Gets this setting's value as an array of a specific type.
        /// Note: this only works if the setting represents an array.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of elements in the array. All values in the array are going to be converted to objects of this type.
        ///     If the conversion of an element fails, an exception is thrown.
        /// </typeparam>
        /// <returns></returns>
        public T[] GetValueArray<T>()
        {
            int myArraySize = ArraySize;

            var values = new T[myArraySize];
            int i = 0;

            int elemIndex = 1;
            int commaIndex = _rawValue.IndexOf(',');

            while (commaIndex >= 0)
            {
                string sub = _rawValue.Substring(elemIndex, commaIndex - elemIndex);
                sub = sub.Trim();

                values[i] = (T)ConvertValue(sub, typeof(T));

                elemIndex = commaIndex + 1;
                commaIndex = _rawValue.IndexOf(',', elemIndex + 1);

                i++;
            }

            if (myArraySize > 0)
            {
                // Read the last element.
                values[i] = (T)ConvertValue(_rawValue.Substring(elemIndex, _rawValue.Length - elemIndex - 1), typeof(T));
            }

            return values;
        }

        /// <summary>
        /// Gets this setting's value as an array of a specific type.
        /// Note: this only works if the setting represents an array.
        /// </summary>
        /// <param name="elementType">
        ///     The type of elements in the array. All values in the array are going to be converted to objects of this type.
        ///     If the conversion of an element fails, an exception is thrown.
        /// </param>
        /// <returns></returns>
        public object[] GetValueArray(Type elementType)
        {
            int myArraySize = this.ArraySize;

            var values = new object[myArraySize];
            int i = 0;

            int elemIndex = 1;
            int commaIndex = _rawValue.IndexOf(',');

            while (commaIndex >= 0)
            {
                string sub = _rawValue.Substring(elemIndex, commaIndex - elemIndex);
                sub = sub.Trim();

                values[i] = ConvertValue(sub, elementType);

                elemIndex = commaIndex + 1;
                commaIndex = _rawValue.IndexOf(',', elemIndex + 1);

                i++;
            }

            if (myArraySize > 0)
            {
                // Read the last element.
                values[i] = ConvertValue(_rawValue.Substring(elemIndex, _rawValue.Length - elemIndex - 1), elementType);
            }

            return values;
        }

        // Converts the value of a single element to a desired type.
        private static object ConvertValue(string value, Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                if (string.IsNullOrEmpty(value))
                {
                    // Returns Nullable<type>().
                    return null;
                }

                // Otherwise, continue with our conversion using
                // the underlying type of the nullable.
                type = underlyingType;
            }

            if (type == typeof(bool))
            {
                // Special case for bool.
                switch (value.ToLowerInvariant())
                {
                    case "off":
                    case "no":
                    case "0":
                        value = bool.FalseString;
                        break;
                    case "on":
                    case "yes":
                    case "1":
                        value = bool.TrueString;
                        break;
                }
            }
            else if (type.GetTypeInfo().BaseType == typeof(Enum))
            {
                // It's possible that the value is something like:
                // UriFormat.Unescaped
                // We, and especially Enum.Parse do not want this format.
                // Instead, it wants the clean name like:
                // Unescaped
                //
                // Because of that, let's get rid of unwanted type names.
                int indexOfLastDot = value.LastIndexOf('.');

                if (indexOfLastDot >= 0)
                    value = value.Substring(indexOfLastDot + 1, value.Length - indexOfLastDot - 1).Trim();
                
                try
                {
                    return Enum.Parse(type, value);
                }
                catch (Exception ex)
                {
                    throw new SettingValueCastException(value, type, ex);
                }
            }

            try
            {
                // Main conversion routine.
                return Convert.ChangeType(value, type, Configuration.NumberFormat);
            }
            catch (Exception ex)
            {
                throw new SettingValueCastException(value, type, ex);
            }
        }

        #endregion

        #region SetValue

        /// <summary>
        /// Sets the value of this setting via an object.
        /// </summary>
        /// 
        /// <param name="value">The value to set.</param>
        public void SetValue<T>(T value)
        {
            _rawValue = (value == null) ? string.Empty : value.ToString();
        }

        /// <summary>
        /// Sets the value of this setting via an array object.
        /// </summary>
        /// 
        /// <param name="values">The values to set.</param>
        public void SetValue<T>(T[] values)
        {
            if (values == null)
                _rawValue = string.Empty;
            else
            {
                var strings = new string[values.Length];

                for (int i = 0; i < values.Length; i++)
                    strings[i] = values[i].ToString();
                
                _rawValue = $"{{{string.Join(",", strings)}}}";
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a string that represents the setting, not including comments.
        /// </summary>
        public override string ToString() => ToString(false);

        /// <summary>
        /// Gets a string that represents the setting.
        /// </summary>
        ///
        /// <param name="includeComment">Specify true to include the comments in the string; false otherwise.</param>
        public string ToString(bool includeComment)
        {
            if (includeComment)
            {
                bool hasPreComments = _preComments != null && _preComments.Count > 0;

                string[] preCommentStrings = hasPreComments ?
                    _preComments.Select(c => c.ToString()).ToArray() : null;

                if (Comment != null && hasPreComments)
                {
                    // Include inline comment and pre-comments.
                    return $"{string.Join(Environment.NewLine, preCommentStrings)}\n{Name}={_rawValue} {Comment}";
                }
                else if (Comment != null)
                {
                    // Include only the inline comment.
                    return $"{Name}={_rawValue} {Comment}";
                }
                else if (hasPreComments)
                {
                    // Include only the pre-comments.
                    return $"{string.Join(Environment.NewLine, preCommentStrings)}\n{Name}={_rawValue}";
                }
            }

            // In every other case, include just the assignment in the string.
            return $"{Name}={_rawValue}";
        }

        #endregion
    }
}
