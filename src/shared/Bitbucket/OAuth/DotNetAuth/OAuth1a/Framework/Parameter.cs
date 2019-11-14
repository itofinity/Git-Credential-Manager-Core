using System;

namespace DotNetAuth.OAuth1a.Framework
{
    /// <summary>
    /// A name/value pair.
    /// </summary>
    public class Parameter
    {
        #region fields
        private string value;
        private string encodedValue;
        #endregion

        #region ctor
        /// <summary>
        /// Constructs a parameter object by name and value values.
        /// </summary>
        /// <param name="name">The parameter's name.</param>
        /// <param name="value">The parameter's value.</param>
        public Parameter(string name, string value)
            : this()
        {
            Name = name;
            this.value = value;
        }
        /// <summary>
        /// Constructs a parameter object.
        /// </summary>
        public Parameter()
        {
            PercentEncodingFunc = PercentEncode.Encode;
        }
        #endregion

        #region properties
        /// <summary>
        /// Parameter's name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Parameter's value.
        /// </summary>
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                encodedValue = null; // Forces re calculation of EncodedValue using the PercentEncodingFunc
            }
        }
        /// <summary>
        /// Parameter's value encoded using <see cref="PercentEncodingFunc"/> function.
        /// </summary>
        public string EncodedValue
        {
            get { return encodedValue ?? (encodedValue = PercentEncodingFunc(Value)); }
        }
        /// <summary>
        /// A function to encode <see cref="Value"/>, when the encoded value is required.
        /// </summary>
        public Func<string, string> PercentEncodingFunc { get; set; }
        #endregion
    }
}
