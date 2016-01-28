// Copyright (c) 2013-2015 Cemalettin Dervis, MIT License.
// https://github.com/cemdervis/SharpConfig

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpConfig
{
    /// <summary>
    /// Represents a configuration.
    /// Configurations contain one or multiple sections
    /// that in turn can contain one or multiple settings.
    /// The <see cref="Configuration"/> class is designed
    /// to work with classic configuration formats such as
    /// .ini and .cfg, but is not limited to these.
    /// </summary>
    public partial class Configuration : IEnumerable<Section>
    {
        #region Fields

        private static NumberFormatInfo _numberFormat;
        private static char[] _validCommentChars;
        private readonly List<Section> _sections;

        #endregion

        #region Construction

        static Configuration()
        {
            _numberFormat = CultureInfo.InvariantCulture.NumberFormat;
            _validCommentChars = new[] { '#', ';', '\'' };
            IgnoreInlineComments = false;
            IgnorePreComments = false;
            IgnoreDuplicateSettings = false;
            IgnoreDuplicateSettings = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration()
        {
            _sections = new List<Section>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets an enumerator that iterates through the configuration.
        /// </summary>
        public IEnumerator<Section> GetEnumerator() => _sections.GetEnumerator();

        /// <summary>
        /// Gets an enumerator that iterates through the configuration.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Adds a section to the configuration.
        /// </summary>
        /// <param name="section">The section to add.</param>
        public void Add(Section section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            if (Contains(section))
                throw new ArgumentException("The specified section already exists in the configuration.");
            
            _sections.Add(section);
        }

        /// <summary>
        /// Clears the configuration of all sections.
        /// </summary>
        public void Clear()
        {
            _sections.Clear();
        }

        /// <summary>
        /// Determines whether a specified section is contained in the configuration.
        /// </summary>
        /// <param name="section">The section to check for containment.</param>
        /// <returns>True if the section is contained in the configuration; false otherwise.</returns>
        public bool Contains(Section section) => _sections.Contains(section);

        /// <summary>
        /// Determines whether a specifically named setting is contained in the section.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>True if the setting is contained in the section; false otherwise.</returns>
        public bool Contains(string sectionName) => GetSection(sectionName) != null;

        /// <summary>
        /// Removes a section from this section by its name.
        /// </summary>
        /// <param name="sectionName">The case-sensitive name of the section to remove.</param>
        public void Remove(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException(nameof(sectionName));

            var section = GetSection(sectionName);

            if (section == null)
                throw new ArgumentException("The specified section does not exist in the section.");
            
            Remove(section);
        }

        /// <summary>
        /// Removes a section from the configuration.
        /// </summary>
        /// <param name="section">The section to remove.</param>
        public void Remove(Section section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            if (!Contains(section))
                throw new ArgumentException("The specified section does not exist in the section.");
            
            _sections.Remove(section);
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads a configuration from a text stream auto-detecting the encoding if it's unspecified and
        /// using the default parsing settings.
        /// </summary>
        ///
        /// <param name="stream">   The text stream to load the configuration from.</param>
        /// <param name="encoding"> The encoding applied to the contents of the stream. Specify null to auto-detect the encoding.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromStream(Stream stream, Encoding encoding = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var reader = encoding == null
                ? new StreamReader(stream)
                : new StreamReader(stream, encoding);

            using (reader)
                return LoadFromString(reader.ReadToEnd());
        }

        /// <summary>
        /// Loads a configuration from text (source code).
        /// </summary>
        ///
        /// <param name="source">The text (source code) of the configuration.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromString(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException(nameof(source));
            
            return Parse(source);
        }

        #endregion

        #region LoadBinary

        /// <summary>
        /// Loads a configuration from a binary stream, using a specific <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to load the configuration from.</param>
        /// <param name="reader">The reader to use. Specify null to use the default <see cref="BinaryReader"/>.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        public static Configuration LoadFromBinaryStream(Stream stream, BinaryReader reader = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            return DeserializeBinary(reader, stream);
        }

        #endregion

        #region Save

        /// <summary>
        /// Saves the configuration to a stream using the default character encoding, which is UTF8, if encoding is unspecified.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        /// <param name="encoding">The character encoding to use. Specify null to use the default encoding, which is UTF8.</param>
        public void SaveToStream(Stream stream, Encoding encoding = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            Serialize(stream, encoding);
        }

        #endregion

        #region SaveBinary

        /// <summary>
        /// Saves the configuration to a binary file, using a specific <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        /// <param name="writer">The writer to use. Specify null to use the default writer.</param>
        public void SaveToBinaryStream(Stream stream, BinaryWriter writer = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            SerializeBinary(writer, stream);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the number format that is used for value conversion in Section.GetValue().
        /// The default value is <b>CultureInfo.InvariantCulture.NumberFormat</b>.
        /// </summary>
        public static NumberFormatInfo NumberFormat
        {
            get { return _numberFormat; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                
                _numberFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the array that contains all comment delimiting characters.
        /// </summary>
        public static char[] ValidCommentChars
        {
            get { return _validCommentChars; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                
                if (value.Length == 0)
                    throw new ArgumentException("The comment chars array must not be empty.", nameof(value));
                
                _validCommentChars = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether inline-comments
        /// should be ignored when parsing a configuration.
        /// </summary>
        public static bool IgnoreInlineComments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether pre-comments
        /// should be ignored when parsing a configuration.
        /// </summary>
        public static bool IgnorePreComments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether duplicate
        /// settings should be ignored when parsing a configuration.
        /// </summary>
        public static bool IgnoreDuplicateSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether duplicate sections
        /// should be ignored when parsing a configuration
        /// </summary>
        public static bool IgnoreDuplicateSections { get; set; }

        /// <summary>
        /// Gets the number of sections that are in the configuration.
        /// </summary>
        public int SectionCount => _sections.Count;

        /// <summary>
        /// Gets or sets a section by index.
        /// </summary>
        /// <param name="index">The index of the section in the configuration.</param>
        public Section this[int index]
        {
            get
            {
                if (index < 0 || index >= _sections.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                return _sections[index];
            }
            set
            {
                if (index < 0 || index >= _sections.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                _sections[index] = value;
            }
        }

        /// <summary>
        /// Gets or sets a section by its name.
        /// </summary>
        ///
        /// <param name="name">The name of the section.</param>
        ///
        /// <returns>
        /// The section if found, otherwise a new section with
        /// the specified name is created, added to the configuration and returned.
        /// </returns>
        public Section this[string name]
        {
            get
            {
                var section = GetSection(name);

                if (section == null)
                {
                    section = new Section(name);
                    Add(section);
                }

                return section;
            }
            set
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException(nameof(name), "The section name must not be null or empty.");
                
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "The specified value must not be null.");
                
                // Check if there already is a section by that name.
                var section = GetSection(name);
                int settingIndex = section != null ? _sections.IndexOf(section) : -1;

                if (settingIndex < 0)
                {
                    // A section with that name does not exist yet; add it.
                    _sections.Add(section);
                }
                else
                {
                    // A section with that name exists; overwrite.
                    _sections[settingIndex] = section;
                }
            }
        }

        private Section GetSection(int index)
        {
            if (index < 0 || index >= _sections.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            return _sections[index];
        }

        private Section GetSection(string name) => _sections.FirstOrDefault(section => string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase));

        #endregion
    }
}