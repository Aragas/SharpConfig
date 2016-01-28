// Copyright (c) 2013-2015 Cemalettin Dervis, MIT License.
// https://github.com/cemdervis/SharpConfig

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpConfig
{
    /// <summary>
    /// Represents a group of <see cref="Setting"/> objects.
    /// </summary>
    public sealed class Section : ConfigurationElement, IEnumerable<Setting>
    {
        private readonly List<Setting> _settings = new List<Setting>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        ///
        /// <param name="name">The name of the section.</param>
        public Section(string name) : base(name)
        {

        }

        /// <summary>
        /// Creates a new instance of the <see cref="Section"/> class that is
        /// based on an existing object.
        /// Important: the section is built only from the public getter properties
        /// and fields of its type.
        /// When this method is called, all of those properties will be called
        /// and fields accessed once to obtain their values.
        /// Properties and fields that are marked with the <see cref="IgnoreAttribute"/> attribute
        /// or are of a type that is marked with that attribute, are ignored.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        /// <param name="obj"></param>
        /// <returns>The newly created section.</returns>
        public static Section FromObject<T>(string name, T obj)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("The section name must not be null or empty.", nameof(name));
            
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "obj must not be null.");
            
            var section = new Section(name);
            var type = typeof(T);

            foreach (var prop in type.GetRuntimeProperties())
            {
                if (!prop.CanRead || ShouldIgnoreMappingFor(prop))
                {
                    // Skip this property, as it can't be read from.
                    continue;
                }

                object propValue = prop.GetValue(obj, null);
                Setting setting = new Setting(prop.Name, propValue != null ? propValue.ToString() : "");

                section._settings.Add(setting);
            }

            // Repeat for each public field.
            foreach (var field in type.GetRuntimeFields())
            {
                if (ShouldIgnoreMappingFor(field))
                {
                    // Skip this field.
                    continue;
                }

                object fieldValue = field.GetValue(obj);
                Setting setting = new Setting(field.Name, fieldValue != null ? fieldValue.ToString() : "");

                section._settings.Add(setting);
            }

            return section;
        }

        /// <summary>
        /// Creates an object of a specific type, and maps the settings
        /// in this section to the public properties and writable fields of the object.
        /// Properties and fields that are marked with the <see cref="IgnoreAttribute"/> attribute
        /// or are of a type that is marked with that attribute, are ignored.
        /// </summary>
        /// 
        /// <returns>The created object.</returns>
        /// 
        /// <remarks>
        /// The specified type must have a public default constructor
        /// in order to be created.
        /// </remarks>
        public T CreateObject<T>() where T : class
        {
            Type type = typeof(T);

            try
            {
                T obj = Activator.CreateInstance<T>();
                MapTo(obj);

                return obj;
            }
            catch (Exception)
            {
                throw new ArgumentException($"The type '{type.Name}' does not have a default public constructor.");
            }
        }

        private static bool ShouldIgnoreMappingFor(MemberInfo member)
        {
            if (member.GetCustomAttributes(typeof(IgnoreAttribute), false).Any())
                return true;
            else
            {
                PropertyInfo prop = member as PropertyInfo;
                if (prop != null)
                    return prop.PropertyType.GetTypeInfo().GetCustomAttributes(typeof(IgnoreAttribute), false).Any();
                
                FieldInfo field = member as FieldInfo;
                if (field!= null)
                    return field.FieldType.GetTypeInfo().GetCustomAttributes(typeof(IgnoreAttribute), false).Any();    
            }

            return false;
        }

        /// <summary>
        /// Assigns the values of this section to an object's public properties and fields.
        /// Properties and fields that are marked with the <see cref="IgnoreAttribute"/> attribute
        /// or are of a type that is marked with that attribute, are ignored.
        /// </summary>
        /// 
        /// <param name="obj">The object that is modified based on the section.</param>
        public void MapTo<T>(T obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            
            Type type = typeof(T);

            // Scan the type's properties.
            foreach (var prop in type.GetRuntimeProperties())
            {
                if (!prop.CanWrite || ShouldIgnoreMappingFor(prop))
                    continue;
                
                var setting = GetSetting(prop.Name);

                if (setting != null)
                {
                    object value = setting.GetValueTyped(prop.PropertyType);
                    prop.SetValue(obj, value, null);
                }
            }

            // Scan the type's fields.
            foreach (var field in type.GetRuntimeFields())
            {
                // Skip readonly fields.
                if (field.IsInitOnly || ShouldIgnoreMappingFor(field))
                    continue;
                
                var setting = GetSetting(field.Name);

                if (setting != null)
                {
                    object value = setting.GetValueTyped(field.FieldType);
                    field.SetValue(obj, value);
                }
            }
        }

        /// <summary>
        /// Gets an enumerator that iterates through the section.
        /// </summary>
        public IEnumerator<Setting> GetEnumerator() => _settings.GetEnumerator();

        /// <summary>
        /// Gets an enumerator that iterates through the section.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Adds a setting to the section.
        /// </summary>
        /// <param name="setting">The setting to add.</param>
        public void Add(Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));
            
            if (Contains(setting))
                throw new ArgumentException("The specified setting already exists in the section.");
            
            _settings.Add(setting);
        }

        /// <summary>
        /// Clears the section of all settings.
        /// </summary>
        public void Clear()
        {
            _settings.Clear();
        }

        /// <summary>
        /// Determines whether a specified setting is contained in the section.
        /// </summary>
        /// <param name="setting">The setting to check for containment.</param>
        /// <returns>True if the setting is contained in the section; false otherwise.</returns>
        public bool Contains(Setting setting) => _settings.Contains(setting);

        /// <summary>
        /// Determines whether a specifically named setting is contained in the section.
        /// </summary>
        /// <param name="settingName">The name of the setting.</param>
        /// <returns>True if the setting is contained in the section; false otherwise.</returns>
        public bool Contains(string settingName) => GetSetting(settingName) != null;

        /// <summary>
        /// Removes a setting from this section by its name.
        /// </summary>
        public void Remove(string settingName)
        {
            if (string.IsNullOrEmpty(settingName))
                throw new ArgumentNullException(nameof(settingName));
            
            var setting = GetSetting(settingName);

            if (setting == null)
                throw new ArgumentException("The specified setting does not exist in the section.");
            
            _settings.Remove(setting);
        }

        /// <summary>
        /// Removes a setting from the section.
        /// </summary>
        /// <param name="setting">The setting to remove.</param>
        public void Remove(Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            if (!Contains(setting))
                throw new ArgumentException("The specified setting does not exist in the section.");
            
            _settings.Remove(setting);
        }

        /// <summary>
        /// Gets the number of settings that are in the section.
        /// </summary>
        public int SettingCount => _settings.Count;

        /// <summary>
        /// Gets or sets a setting by index.
        /// </summary>
        /// <param name="index">The index of the setting in the section.</param>
        public Setting this[int index]
        {
            get
            {
                if (index < 0 || index >= _settings.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                return _settings[index];
            }
            set
            {
                if (index < 0 || index >= _settings.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                _settings[index] = value;
            }
        }

        /// <summary>
        /// Gets or sets a setting by its name.
        /// </summary>
        ///
        /// <param name="name">The name of the setting.</param>
        ///
        /// <returns>
        /// The setting if found, otherwise a new setting with
        /// the specified name is created, added to the section and returned.
        /// </returns>
        public Setting this[string name]
        {
            get
            {
                var setting = GetSetting(name);

                if (setting == null)
                {
                    setting = new Setting(name);
                    Add(setting);
                }

                return setting;
            }
            set
            {
                // Check if there already is a setting by that name.
                var setting = GetSetting(name);

                int settingIndex = setting != null ? _settings.IndexOf(setting) : -1;

                if (settingIndex < 0)
                {
                    // A setting with that name does not exist yet; add it.
                    _settings.Add(setting);
                }
                else
                {
                    // A setting with that name exists; overwrite.
                    _settings[settingIndex] = setting;
                }
            }
        }

        private Setting GetSetting(string name) => _settings.FirstOrDefault(setting => string.Equals(setting.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        ///
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString() => ToString(false);

        /// <summary>
        /// Convert this object into a string representation.
        /// </summary>
        ///
        /// <param name="includeComment">True to include, false to exclude the comment.</param>
        ///
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
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
                    return $"{string.Join(Environment.NewLine, preCommentStrings)}\n[{Name}] {Comment}";
                }
                else if (Comment != null)
                {
                    // Include only the inline comment.
                    return $"[{Name}] {Comment}";
                }
                else if (hasPreComments)
                {
                    // Include only the pre-comments.
                    return $"{string.Join(Environment.NewLine, preCommentStrings)}\n[{Name}]";
                }
            }

            return $"[{Name}]";
        }
    }
}