using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetAuth.OAuth1a.Framework
{
    /// <summary>
    /// Encapsulates a list of <see cref="Parameter"/>s.
    /// </summary>
    public class ParameterSet
    {
        private readonly List<Parameter> list;

        #region ctor
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterSet"/> class.
        /// </summary>
        public ParameterSet()
        {
            list = new List<Parameter>();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterSet"/> class.
        /// </summary>
        /// <param name="initialValues">The initial values.</param>
        public ParameterSet(Dictionary<string, string> initialValues)
            : this()
        {
            foreach (var i in initialValues)
                Add(i.Key, i.Value);
        }
        #endregion

        #region indexer
        /// <summary>
        /// Gets the value of parameter with the specified name.
        /// </summary>
        public string this[string name]
        {
            get
            {
                return list.Where(i => i.Name == name).Select(i => i.Value).SingleOrDefault();
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// Adds the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        public void Add(Parameter parameter)
        {
            list.Add(parameter);
        }
        /// <summary>
        /// Adds a new parameter to list by its name, value and encoder function.
        /// </summary>
        /// <param name="name">The parameter's name.</param>
        /// <param name="value">The parameter's value.</param>
        /// <param name="encoder">The encoder to calculate encoded value.</param>
        public void Add(string name, string value, Func<string, string> encoder = null)
        {
            var item = new Parameter(name, value);
            if (encoder != null)
                item.PercentEncodingFunc = encoder;
            list.Add(item);
        }
        /// <summary>
        /// Bulk adds a list of parameters.
        /// </summary>
        /// <param name="newList">The new list.</param>
        public void Add(IEnumerable<Parameter> newList)
        {
            list.AddRange(newList);
        }
        /// <summary>
        /// Returns an array of parameters.
        /// </summary>
        /// <returns></returns>
        public Parameter[] ToList()
        {
            return list.ToArray();
        }
        #endregion

        #region static methods
        /// <summary>
        /// Parses a response body and reads the parameters into a <see cref="ParameterSet"/> object.
        /// </summary>
        /// <param name="responseBody">The response body that you received as response to a request.</param>
        /// <returns>A <see cref="ParameterSet"/> object containing list of parameters read from response body.</returns>
        public static ParameterSet FromResponseBody(string responseBody)
        {
            var arguments = responseBody.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new ParameterSet();
            foreach (var i in from i in arguments
                              let parts = i.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)
                              where parts.Length == 2
                              let name = parts[0]
                              let val = parts[1]
                              select new Parameter(name, PercentEncode.Decode(val)))
                result.Add(i);
            return result;
        }
        #endregion
    }
}
