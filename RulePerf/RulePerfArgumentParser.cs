// -----------------------------------------------------------------------
// <copyright file="RulePerfArgumentParser.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.ConsoleHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.CSAT.Utilities;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Model;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;

    /// <summary>
    /// The argument parser for RulePerf.exe command line
    /// </summary>
    public class RulePerfArgumentParser : ArgumentParser
    {
        /// <summary>
        /// Parses the argument.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="argument">The argument.</param>
        /// <returns>The argument parsing result</returns>
        /// <exception cref="System.ArgumentException">argument or option Object cannot be null</exception>
        public static ArgumentResult ParseArgument(string[] args, RulePerfConsoleArgument argument)
        {
            Debug.Assert(args != null, "Argument should not be null");
            if (argument == null)
            {
                throw new ArgumentException("argument or optionObject cannot be null");
            }

            return ParseArgumentInternal(args, argument);
        }

        /// <summary>
        /// Parses the argument internal.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <param name="argumentObject">The argument object.</param>
        /// <returns>Argument parsing result</returns>
        /// <exception cref="System.ArgumentException">IArgumentObject.Options cannot be null or empty</exception>
        internal static ArgumentResult ParseArgumentInternal(string[] args, RulePerfConsoleArgument argumentObject)
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

                ////#region Settings for an option
                argumentObject.BuildStep(ar.SelectedOptionName);

                CommonStep commonStep = new CommonStep();

                foreach (Step step in argumentObject.Steps)
                {
                    // Specific step's settings
                    string[] settingNames = step.SettingNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    List<SettingEntityModel> settings = SettingEntityModel.Get(settingNames);
                    foreach (SettingEntityModel setting in settings)
                    {
                        ArgumentParameterAttribute attr = new ArgumentParameterAttribute(argumentObject.GetOptionName(step));
                        attr.DefaultValue = setting.SettingValue;
                        attr.Delimiter = '|';
                        attr.Description = setting.SettingName;
                        attr.MaxOccur = 1;
                        attr.MinOccur = 0;
                        attr.ParameterName = setting.SettingName;

                        foreach (string option in attr.OptionsBindTo)
                        {
                            if (optionParameterAttribDic[option].ContainsKey(attr.ParameterName))
                            {
                                throw new ArgumentException(string.Format("option:{0} has multiple setting of parameter:{1}", option, attr.ParameterName));
                            }

                            optionParameterAttribDic[option].Add(attr.ParameterName, attr);
                        }
                    }

                    // Common settings
                    settingNames = commonStep.SettingNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    settings = SettingEntityModel.Get(settingNames);
                    foreach (SettingEntityModel setting in settings)
                    {
                        ArgumentParameterAttribute attr = new ArgumentParameterAttribute(argumentObject.GetOptionName(step));
                        attr.DefaultValue = setting.SettingValue;
                        attr.Delimiter = '|';
                        attr.Description = setting.SettingName;
                        attr.MaxOccur = 1;
                        attr.MinOccur = 0;
                        attr.ParameterName = setting.SettingName;

                        foreach (string option in attr.OptionsBindTo)
                        {
                            if (optionParameterAttribDic[option].ContainsKey(attr.ParameterName))
                            {
                                // If the setting has been set by the specifical step, then ignore the global setting process
                                // throw new ArgumentException(string.Format("option:{0} has multiple setting of parameter:{1}",
                                //    option, attr.ParameterName));
                            }
                            else
                            {
                                optionParameterAttribDic[option].Add(attr.ParameterName, attr);
                            }
                        }
                    }
                }

                /*
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
                }*/
                ////#endregion Settings for an option

                AssignValuesToArgumentObject(argumentObject, ar.SelectedOptionName, argPairs, optionParameterAttribDic[ar.SelectedOptionName]);
            }
            catch (ArgumentException ae)
            {
                ExceptionHelper.CentralProcess(ae);
                ar.ErrorMessages.Add(ae.Message);
                ar.ParseSucceeded = false;
                return ar;
            }

            ar.ParseSucceeded = true;
            return ar;
        }

        /// <summary>
        /// Builds the {argument: parameter - attributes dictionary}.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>A key-value pairs. Key is the option, value is the parameter - attributes dictionary for this option.</returns>
        private static IDictionary<string, Dictionary<string, ArgumentParameterAttribute>>
            BuildArguParamAttrDic(ICollection<string> options)
        {
            var optionParameterAttribDic =
                new Dictionary<string, Dictionary<string, ArgumentParameterAttribute>>(StringComparer.OrdinalIgnoreCase);

            foreach (var option in options)
            {
                optionParameterAttribDic.Add(option, new Dictionary<string, ArgumentParameterAttribute>(StringComparer.OrdinalIgnoreCase));
            }

            return optionParameterAttribDic;
        }

        /// <summary>
        /// Parses the argument array.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="dict">The dictionary object.</param>
        /// <returns>The option name</returns>
        /// <exception cref="System.ArgumentException">no option name can be extracted from command line.</exception>
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

        /// <summary>
        /// Validates the duplicated parameters.
        /// </summary>
        /// <param name="dict">The dictionary object.</param>
        /// <exception cref="System.ArgumentException">parameter error info</exception>
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

        /// <summary>
        /// Assigns the values to argument object.
        /// </summary>
        /// <param name="argumentObject">The argument object.</param>
        /// <param name="optionName">Name of the option.</param>
        /// <param name="argPairs">The argument pairs.</param>
        /// <param name="attrs">The attributes.</param>
        /// <exception cref="System.ArgumentException">parameter: + pair.Key +  is not defined in the argument object</exception>
        private static void AssignValuesToArgumentObject(object argumentObject, string optionName, List<KeyValuePair<string, string>> argPairs, Dictionary<string, ArgumentParameterAttribute> attrs)
        {
            foreach (var pair in argPairs)
            {
                if (!attrs.ContainsKey(pair.Key))
                {
                    throw new ArgumentException("parameter:" + pair.Key + " is not defined in the argumentobject");
                }

                try
                {
                    SettingEntityModel setting = SettingEntityModel.GetSingle(pair.Key);
                    setting.SettingValue = pair.Value;
                    setting.Update();
                    ////SetValue(argumentObject, attrs[pair.Key], pair.Value);
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

            // SaveAll may fail in the multi-thread case.
            //SettingEntityModel.SaveAll();

            // set default value
            /*
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
            }*/
        }
    }
}
