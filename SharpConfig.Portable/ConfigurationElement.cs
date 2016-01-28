// Copyright (c) 2013-2015 Cemalettin Dervis, MIT License.
// https://github.com/cemdervis/SharpConfig

using System;
using System.Collections.Generic;

namespace SharpConfig
{
    /// <summary>
    /// Represents the base class of all elements
    /// that exist in a <see cref="Configuration"/>,
    /// for example sections and settings.
    /// </summary>
    public abstract class ConfigurationElement
    {
        private string _name;
        internal List<Comment> _preComments;

        internal ConfigurationElement(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            _name = name;
        }

        /// <summary>
        /// Gets or sets the name of this element.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value));

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the comment of this element.
        /// </summary>
        public Comment? Comment { get; set; }

        /// <summary>
        /// Gets the list of comments above this element.
        /// </summary>
        public List<Comment> PreComments => _preComments ?? (_preComments = new List<Comment>());
    }
}