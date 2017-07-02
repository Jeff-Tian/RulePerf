using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace Microsoft.CSAT.Utilities
{
    /// <summary>
    /// User argument object should implement this Interface and return the <optionName, optionDesc> dictionary
    /// </summary>
    /// <remarks>
    /// Better to create Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) to make the option name case insensitive
    /// </remarks>
    public interface IArgumentObject
    {
        /// <summary>
        /// OptionName, OptionDescription
        /// </summary>
        IDictionary<string, string> Options { get; }
    }

    /// <summary>
    /// This attribute should be append on the Property in ArgumentObject
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class ArgumentParameterAttribute : Attribute
    {
        /// <summary> parameter is following optionName, format in cmd: /paraName:paraValue </summary>
        public string ParameterName { get; set; }
        /// <summary> parameter helpmsg </summary>
        public string Description { get; set; }
        /// <summary> if MaxOccur > 1, specify the delimiter to split value, e.g. /fileUsage:ABC|EDF '|' is delimiter </summary>
        public char Delimiter { get; set; }
        /// <summary> which set of optionNames are this attribute bind to </summary>
        public string[] OptionsBindTo { get; private set; }
        /// <summary> minimal occurance, default = 0 </summary>
        public int MinOccur { get; set; }
        /// <summary> maximal occurance, default = 1 </summary>
        public int MaxOccur { get; set; }
        /// <summary> if no occurance, default value would be set to the Property </summary>
        public object DefaultValue { get; set; }
        /// <summary> parser would validate the input if this is provided, use IgnoreCase regex.</summary>
        public string RegexString { get; set; }

        internal PropertyInfo PropertyInfo { get; set; }
        internal int SetCount { get; set; }
        internal Regex ValidationRegex { get; set; }

        public ArgumentParameterAttribute(params string[] optionsBindTo)
        {
            MaxOccur = 1;
            OptionsBindTo = optionsBindTo;
        }

        /// <summary>
        /// Validate the Named Parameters in this Attribute, if not valid, ArgumentException would be thrown
        /// </summary>
        public void Validate()
        {
            if (MinOccur < 0)
            {
                throw new ArgumentException("MinOccur:" + MinOccur + " cannot be less than zero");
            }
            if (MaxOccur < MinOccur)
            {
                throw new ArgumentException(string.Format("MaxOccur:{0} is less than MinOccur:{1}",
                    MaxOccur,
                    MinOccur));
            }
            if (MaxOccur >= 2 && Delimiter == default(char))
            {
                throw new ArgumentException("Delimiter must be specified if MaxOccur > 1");
            }
            if (DefaultValue != null && (MaxOccur > 1 || MinOccur > 0))
            {
                throw new ArgumentException("DefaultValue is not available when MaxOccur > 1 or MinOccur > 0");
            }

            if (string.IsNullOrEmpty(ParameterName))
            {
                throw new ArgumentException("Parameter cannot be null or empty");
            }
            if (string.IsNullOrEmpty(Description))
            {
                throw new ArgumentException("Description cannot be null or empty");
            }
            if (!string.IsNullOrEmpty(RegexString))
            {
                ValidationRegex = new Regex(RegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            if (OptionsBindTo.IsNullOrContainEmpty())
            {
                throw new ArgumentException("OptionsBindTo cannot be null and every element must be non-nullempty");
            }
        }
    }

    /// <summary>
    /// The result of parsing commandline, contains succeeded and error message
    /// </summary>
    public class ArgumentResult
    {
        public string SelectedOptionName { get; internal set; }
        public List<string> ErrorMessages = new List<string>();
        public bool ParseSucceeded { get; internal set; }
        public ConsoleColor ErrorColor = ConsoleColor.Red;
        public ConsoleColor HelpColor = ConsoleColor.Yellow;
        public IDictionary<string, Dictionary<string, ArgumentParameterAttribute>> ParamAttributes { get; internal set; }
        public IDictionary<string, string> OptionDescriptions { get; internal set; }

        private void ColorWriteLine(ConsoleColor color, string value, params string[] args)
        {
            ConsoleColor origColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(value, args);
            }
            finally
            {
                Console.ForegroundColor = origColor;
            }
        }

        private void PrintErrors()
        {
            if (ErrorMessages.Count > 0)
            {
                ColorWriteLine(ErrorColor, "Argument has errors:");
                foreach (var errMsg in ErrorMessages)
                {
                    ColorWriteLine(ErrorColor, "  * " + errMsg);
                }
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        private void PrintHelpMsg()
        {
            string processName = Process.GetCurrentProcess().ProcessName + ".exe";
            ColorWriteLine(HelpColor, "Usage of " + processName + ":");
            string usageLine = string.Format("    {0} [{1}] (parameter list)",
                processName,
                string.Join("|", OptionDescriptions.Keys));
            ColorWriteLine(HelpColor, usageLine);
            ColorWriteLine(HelpColor, "    ** parameter format: /PARAMNAME:PARAMVALUE");
            ColorWriteLine(HelpColor, "    ** for bool type, /PARAMNAME /PARAMNAME:1 /PARAMNAME:true are all 'true'");
            Console.WriteLine();

            // if option is correct, only print attr associate with this option
            if (!string.IsNullOrEmpty(SelectedOptionName) && ParamAttributes.ContainsKey(SelectedOptionName))
            {
                ColorWriteLine(HelpColor, "Parameter usage of selected option name '{0}' :", SelectedOptionName);
                ColorWriteLine(HelpColor, "  Option: {0}  ({1})",
                    SelectedOptionName, OptionDescriptions[SelectedOptionName]);
                foreach (var attr in ParamAttributes[SelectedOptionName].Values)
                {
                    ColorWriteLine(HelpColor, GetArgumentParameterUsage(attr));
                }
                return; // no further help msg
            }

            foreach (var optionDesc in OptionDescriptions)
            {
                ColorWriteLine(HelpColor, "  Option: {0}  ({1})", optionDesc.Key, optionDesc.Value);
                if (ParamAttributes.ContainsKey(optionDesc.Key))
                {
                    foreach (var attr in ParamAttributes[optionDesc.Key].Values)
                    {
                        ColorWriteLine(HelpColor, GetArgumentParameterUsage(attr));
                    }
                }
                Console.WriteLine();
            }
        }

        private string GetArgumentParameterUsage(ArgumentParameterAttribute attr)
        {
            string msg = string.Format("    parameter: {0}  [{1}..{2}, {3}]  ({4})",
                attr.ParameterName,
                attr.MinOccur,
                attr.MaxOccur,
                attr.PropertyInfo.PropertyType.GetBaseType().Name,
                attr.Description);
            if (attr.DefaultValue != null && attr.MaxOccur <= 1)
            {
                msg += "\r\n      default value: " + attr.DefaultValue.ToString();
            }
            if (attr.Delimiter != default(byte) && attr.MaxOccur > 1)
            {
                msg += "\r\n      delimiter: '" + attr.Delimiter.ToString() + "'";
            }
            if (attr.ValidationRegex != null)
            {
                msg += "\r\n      regex: " + attr.ValidationRegex.ToString();
            }
            return msg;
        }

        /// <summary>
        /// Print the error
        /// </summary>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void PrintUsage()
        {
            PrintErrors();
            Console.WriteLine();
            PrintHelpMsg();
        }
    }

    /// <summary>
    /// ArgumentParser Helpers
    /// </summary>
    internal static class ArgumentParserExtensions
    {
        #region Attribute Extensions
        internal static T[] GetCustomAttributes<T>(this ICustomAttributeProvider provider)
            where T : Attribute
        {
            return GetCustomAttributes<T>(provider, true);
        }

        internal static T[] GetCustomAttributes<T>(this ICustomAttributeProvider provider, bool inherit)
            where T : Attribute
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            T[] attributes = provider.GetCustomAttributes(typeof(T), inherit) as T[];

            if (attributes == null)
            {
                return new T[0];
            }
            return attributes;
        }
        #endregion

        #region string extentions
        internal static KeyValuePair<string, string> ParseParameter(this string input)
        {
            Debug.Assert(!string.IsNullOrEmpty(input));
            if (input.Length > 1 && input.StartsWith("/"))
            {
                string[] parts = input.Substring(1).Split(new char[] { ':' }, 2);
                return new KeyValuePair<string, string>(parts[0], parts.Length > 1 ? parts[1] : string.Empty);
            }
            else
            {
                throw new ArgumentException(string.Format(
                    "argument part:'{0}' cannot be converted. Should be in format of /optionName:optionValue",
                    input));
            }
        }

        internal static bool IsNullOrContainEmpty(this string[] strArray)
        {
            if (strArray == null)
            {
                return true;
            }
            return strArray.Where(s => string.IsNullOrEmpty(s)).Count() > 0;
        }
        #endregion

        #region object extentions
        internal static string SafeToString(this object obj)
        {
            if (obj == null)
            {
                return "<NULL>";
            }
            string val = obj.ToString();
            if (val == string.Empty)
            {
                val = "<EMPTY>";
            }
            return val;
        }
        #endregion

        #region Type extentions
        internal static bool IsSupported(this Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsArray || IsNullableType(type))
            {
                type = GetBaseType(type);
            }

            return type.IsNumericalType() ||
                   type.Equals(typeof(bool)) ||
                   type.Equals(typeof(char)) ||
                   type.Equals(typeof(string)) ||
                   type.Equals(typeof(DateTime)) ||
                   type.IsEnum;
        }

        internal static Type GetBaseType(this Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            else if (type.IsNullableType())
            {
                Debug.Assert(type.GetGenericArguments().Length == 1);
                return type.GetGenericArguments()[0];
            }
            else
            {
                return type;
            }
        }

        internal static bool IsNullableType(this Type type)
        {
            return type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        internal static bool IsNumericalType(this Type type)
        {
            return type.Equals(typeof(byte)) ||
                   type.Equals(typeof(sbyte)) ||
                   type.Equals(typeof(decimal)) ||
                   type.Equals(typeof(double)) ||
                   type.Equals(typeof(float)) ||
                   type.Equals(typeof(int)) ||
                   type.Equals(typeof(uint)) ||
                   type.Equals(typeof(long)) ||
                   type.Equals(typeof(ulong)) ||
                   type.Equals(typeof(short)) ||
                   type.Equals(typeof(ushort));
        }
        #endregion
    }

    /// <summary>
    /// ArgumentParser take string array and argumentObject, set the argumentObject and return result
    /// </summary>
    public class ArgumentParser
    {
        /// <summary>
        /// Parse the argument string array
        /// </summary>
        /// <param name="args">string array comes from commandline, split by windows api</param>
        /// <param name="argumentObject">the value container of argument</param>
        /// <remarks>
        /// ArgumentException would be thrown if the declaration of ArgumentObject is not correct, this should
        /// be fixed in coding time.
        /// 
        /// If the declaration is correct, no exception is expected to be raised. 
        /// Check the ArgumentResult and determinte whether the commandline is ok or not.
        /// </remarks>
        /// <returns>The result of parsing</returns>
        public static ArgumentResult ParseArgument(string[] args, IArgumentObject argumentObject)
        {
            Debug.Assert(args != null);
            if (argumentObject == null)
            {
                throw new ArgumentException("argument or optionObject cannot be null");
            }

            return ParseArgumentInternal(args, argumentObject);
        }

        internal static ArgumentResult ParseArgumentInternal(string[] args, IArgumentObject argumentObject)
        {
            // Validate IArgumentObject.Options
            var options = argumentObject.Options;
            if (options == null || options.Count == 0)
            {
                throw new ArgumentException("IArgumentObject.Options cannot be null or empty");
            }
            if (options.Select(a => a.Key.ToLower()).Distinct().Count() != options.Count)
            {
                throw new ArgumentException("IArgumentObject.Options has duplicated option name");
            }

            // Retrieve the attributes from object's properties
            var optionParameterAttribDic = BuildArguParamAttrDic(options.Keys);

            foreach (var property in argumentObject.GetType().GetProperties())
            {
                var paramAttrs = property.GetCustomAttributes<ArgumentParameterAttribute>();
                if (paramAttrs.Length > 0) // Only validate property with ParameterOptionAttribute
                {
                    if (!property.PropertyType.IsSupported())
                    {
                        throw new ArgumentException(string.Format("Property:{0}, the Type:{1} is not supported",
                            property.Name,
                            property.PropertyType.Name));
                    }

                    foreach (var attr in paramAttrs)
                    {
                        ValidateParameterOptionAttr(property, attr, options);

                        foreach (string option in attr.OptionsBindTo)
                        {
                            if (optionParameterAttribDic[option].ContainsKey(attr.ParameterName))
                            {
                                throw new ArgumentException(string.Format("option:{0} has multiple setting of parameter:{1}",
                                    option, attr.ParameterName));
                            }
                            optionParameterAttribDic[option].Add(attr.ParameterName, attr);
                        }
                    }
                }
            }

            // Parse system commandline, after this line, no exception is thrown to caller.
            ArgumentResult ar = new ArgumentResult();
            ar.ParamAttributes = optionParameterAttribDic;
            ar.OptionDescriptions = options;
            List<KeyValuePair<string, string>> argPairs = null;
            try
            {
                string optionName = ParseArgumentArray(args, out argPairs);
                if (!optionParameterAttribDic.ContainsKey(optionName))
                {
                    throw new ArgumentException("optionName: '" + optionName + "' is not defined in argument object");
                }
                ar.SelectedOptionName = optionName;

                AssignValuesToArgumentObject(argumentObject, ar.SelectedOptionName, argPairs, optionParameterAttribDic[ar.SelectedOptionName]);
            }
            catch (ArgumentException ae)
            {
                ar.ErrorMessages.Add(ae.Message);
                ar.ParseSucceeded = false;
                return ar;
            }

            ar.ParseSucceeded = true;
            return ar;
        }

        private static void AssignValuesToArgumentObject(object argumentObject,
            string optionName,
            List<KeyValuePair<string, string>> argPairs,
            Dictionary<string, ArgumentParameterAttribute> attrs)
        {
            foreach (var pair in argPairs)
            {
                if (!attrs.ContainsKey(pair.Key))
                {
                    throw new ArgumentException("parameter:" + pair.Key + " is not defined in the argumentobject");
                }

                try
                {
                    SetValue(argumentObject, attrs[pair.Key], pair.Value);
                }
                catch (ArgumentException ae)
                {
                    throw new ArgumentException(string.Format(
                        "Failed to set value to ArgumentObject.{0} from parameter:{1} value:{2}. Detail Message:{3}",
                        attrs[pair.Key].PropertyInfo.Name,
                        pair.Key,
                        pair.Value.SafeToString(),
                        ae.Message));
                }
            }

            // set default value
            foreach (var attr in attrs.Values)
            {
                if (attr.SetCount < attr.MinOccur)
                {
                    throw new ArgumentException(string.Format("parameter:{0} has been set for {1} times, but min={2}",
                        attr.ParameterName,
                        attr.SetCount,
                        attr.MinOccur));
                }
                if (attr.SetCount == 0 && attr.DefaultValue != null)
                {
                    attr.PropertyInfo.SetValue(argumentObject, attr.DefaultValue, null);
                }
            }
        }

        private static void SetValue(object argumentObject, ArgumentParameterAttribute attr, string argValue)
        {
            string[] values = null;
            if (attr.MaxOccur > 1)
            {
                values = argValue.Split(new char[] { attr.Delimiter }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                if (string.IsNullOrEmpty(argValue) && attr.PropertyInfo.PropertyType.GetBaseType() != typeof(bool))
                {
                    values = new string[0];
                }
                else
                {
                    values = new string[1] { argValue };
                }
            }

            if (values.Length < attr.MinOccur)
            {
                throw new ArgumentException(attr.ParameterName + "'s MinOccur=" + attr.MinOccur +
                    " but argument only has " + values.Length + " occurs");
            }

            // regex validation
            if (attr.ValidationRegex != null)
            {
                foreach (string v in values)
                {
                    if (!attr.ValidationRegex.IsMatch(v))
                    {
                        throw new ArgumentException(attr.ParameterName + " has regex restriction, but value:" + v +
                            " does not match regex:" + attr.ValidationRegex.ToString());
                    }
                }
            }

            SetValueFromStrings(argumentObject, attr.PropertyInfo, values);
            attr.SetCount += values.Length;
        }

        private static void SetValueFromStrings(object argumentObject, PropertyInfo propertyInfo, params string[] values)
        {
            foreach (string value in values)
            {
                object newObject = GetCheckedValueFromString(value, propertyInfo.PropertyType.GetBaseType());
                if (propertyInfo.PropertyType.IsArray)
                {
                    Array newArray = AppendToArray((Array)propertyInfo.GetValue(argumentObject, null),
                        newObject,
                        propertyInfo.PropertyType);

                    propertyInfo.SetValue(argumentObject, newArray, null);
                }
                else
                {
                    propertyInfo.SetValue(argumentObject, newObject, null);
                }
            }
        }

        private static object GetCheckedValueFromString(string value, Type type)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (type.Equals(typeof(bool)))
            {
                // for boolean, true = 1, false = 0, string.empty = true
                if (value == string.Empty || value == "1" || value.ToLower() == bool.TrueString.ToLower())
                {
                    return true;
                }
                else if (value == "0" || value.ToLower() == bool.FalseString.ToLower())
                {
                    return false;
                }
                else
                {
                    throw new ArgumentException(value + " is not a valid bool value");
                }
            }
            else if (type.Equals(typeof(string)))
            {
                return value;
            }
            else if (type.IsEnum)
            {
                return Enum.Parse(type, value, true);
            }
            else
            {
                // numerical type, call Parse
                try
                {
                    return type.InvokeMember("Parse",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                        null,
                        null,
                        new object[] { value },
                        CultureInfo.CurrentUICulture);
                }
                catch (TargetInvocationException ex)
                {
                    throw new ArgumentException(string.Format("Failed to call {0}.Parse from string:{1}. \r\nDetail:{2}",
                        type.Name,
                        value,
                        ex.Message));
                }
            }
        }

        private static IDictionary<string, Dictionary<string, ArgumentParameterAttribute>>
            BuildArguParamAttrDic(ICollection<string> options)
        {
            var optionParameterAttribDic =
                new Dictionary<string, Dictionary<string, ArgumentParameterAttribute>>(StringComparer.OrdinalIgnoreCase);

            foreach (var option in options)
            {
                optionParameterAttribDic.Add(option,
                    new Dictionary<string, ArgumentParameterAttribute>(StringComparer.OrdinalIgnoreCase));
            }
            return optionParameterAttribDic;
        }

        private static void ValidateParameterOptionAttr(
            PropertyInfo property,
            ArgumentParameterAttribute attr,
            IDictionary<string, string> options)
        {
            try
            {
                attr.Validate();
            }
            catch (ArgumentException ae)
            {
                throw new ArgumentException(property.Name + " has invalid ParameterOptionAttribute, message:",
                    ae.Message);
            }

            foreach (string option in attr.OptionsBindTo)
            {
                if (!options.ContainsKey(option))
                {
                    throw new ArgumentException(option + " is not defined in ArgumentObject");
                }
            }

            if (attr.MaxOccur > 1 && !property.PropertyType.IsArray)
            {
                throw new ArgumentException(property.Name + " is not array type but MaxOccurs > 1");
            }

            if (attr.DefaultValue != null && attr.DefaultValue.GetType() != property.PropertyType)
            {
                throw new ArgumentException(string.Format("DefaultValue is set, type:{0} != propertyType:{1}",
                    attr.DefaultValue.GetType().Name,
                    property.PropertyType.Name));
            }
            attr.PropertyInfo = property;
        }

        private static Array AppendToArray(Array array, object value, Type type)
        {
            if (array == null)
            {
                array = Array.CreateInstance(type.GetElementType(), 0);
            }

            // Get the length of the current array
            int arrayLength = array.GetLength(0);
            // Alloc a new array with length + 1
            Array newArray = Array.CreateInstance(type.GetElementType(), arrayLength + 1);

            // Copy from old array to the new array
            Array.Copy((Array)array, newArray, arrayLength);

            // Set the value in new array
            newArray.SetValue(value, arrayLength);

            return newArray;
        }

        private static string ParseArgumentArray(string[] args, out List<KeyValuePair<string, string>> dict)
        {
            // remove null empty args
            var validArgs = from arg in args
                            where !string.IsNullOrEmpty(arg)
                            select arg;

            // Take the 1st element as option name, the rest as parameter
            if (validArgs.Count() == 0)
            {
                throw new ArgumentException("no option name can be extracted from commandline.");
            }

            string optionName = validArgs.ElementAt(0);
            validArgs = validArgs.Skip(1);

            // parse the parameter in simple rule, and not allow duplication
            dict = (from validArg in validArgs
                    select validArg.ParseParameter()).ToList();
            ValidateDuplicatedParameters(dict);

            return optionName;
        }

        private static void ValidateDuplicatedParameters(List<KeyValuePair<string, string>> dict)
        {
            var a = from pair in dict
                    group pair by pair.Key into g
                    where g.Count() > 1
                    select g.Key;
            if (a.Count() > 0)
            {
                throw new ArgumentException(string.Format(
                    "Parameter:{0} occurs multiple times",
                    a.First()));
            }
        }
    }
}
