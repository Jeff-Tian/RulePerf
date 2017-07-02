using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using System.Configuration;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    /// <summary>
    /// The model for the program's settings entry
    /// </summary>
    /// 
    [Serializable()]
    public class SettingEntityModel
    {
        public string SettingName{get;set;}
        public string SettingType{get;set;}
        public string SettingValue { get; set; }

        /// <summary>
        /// Updates this setting.
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            try
            {
                if (string.Compare(this.SettingType, "System.Collections.Specialized.StringCollection", true) == 0)
                {
                    Properties.Settings.Default[this.SettingName] = FromSettingString(this.SettingValue);
                }                
                else if (string.Compare(this.SettingType, "System.Boolean", true) == 0)
                {
                    Properties.Settings.Default[this.SettingName] = FromSettingString<System.Boolean>(this.SettingValue);
                }
                else if (string.Compare(this.SettingType, "System.Int32", true) == 0)
                {
                    Properties.Settings.Default[this.SettingName] = FromSettingString<System.Int32>(this.SettingValue);
                }
                else if (string.Compare(this.SettingType, "System.Single", true) == 0)
                {
                    Properties.Settings.Default[this.SettingName] = FromSettingString<System.Single>(this.SettingValue);
                }
                else
                {
                    Properties.Settings.Default[this.SettingName] = this.SettingValue;
                }

                return true;
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a single setting by its name.
        /// </summary>
        /// <param name="settingName">Name of the setting.</param>
        /// <returns>The setting object</returns>
        public static SettingEntityModel GetSingle(string settingName, bool escapeSpecialCharacters = false)
        {
            try
            {
                SettingEntityModel model = new SettingEntityModel();
                model.SettingName = settingName;
                model.SettingType = Properties.Settings.Default[settingName].GetType().ToString();
                model.SettingValue = ToSettingString(Properties.Settings.Default[settingName], escapeSpecialCharacters);
                return model;
            }
            catch(Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
                return null;
            }
        }

        /// <summary>
        /// Gets a list of setting objects by their names
        /// </summary>
        /// <param name="settingNames">The setting names.</param>
        /// <returns>A list of setting objects.</returns>
        public static List<SettingEntityModel> Get(params string[] settingNames)
        {
            List<SettingEntityModel> settings = new List<SettingEntityModel>();
            foreach (string settingName in settingNames)
            {
                SettingEntityModel model = SettingEntityModel.GetSingle(settingName);
                if (model != null) settings.Add(model);
            }

            return settings;
        }

        public static List<SettingEntityModel> GetAllSettings(bool escapeSpecialCharacters = false)
        {
            List<SettingEntityModel> settings = new List<SettingEntityModel>();

            try
            {
                foreach (SettingsProperty property in Properties.Settings.Default.Properties)
                {
                    SettingEntityModel model = SettingEntityModel.GetSingle(property.Name, escapeSpecialCharacters);
                    if (model != null) settings.Add(model);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
                return null;
            }

            return settings;
        }

        /// <summary>
        /// Saves all settings.
        /// </summary>
        public static void SaveAll()
        {
            // TODO: make it thread safe?
            Properties.Settings.Default.Save();
        }

        #region Helpers
        /// <summary>
        /// Convert the setting value to the type of string.
        /// </summary>
        /// <param name="o">The object of the setting value.</param>
        /// <returns>String version of the setting value.</returns>
        public static string ToSettingString(object o, bool escapeSpecialCharacters = false)
        {
            if (o is string)
            {
                return ToSettingString(o as string, escapeSpecialCharacters);
            }
            else if (o is System.Collections.Specialized.StringCollection)
            {
                return ToSettingString(o as System.Collections.Specialized.StringCollection, escapeSpecialCharacters);
            }
            else if (o is SettingsProperty) {
                return string.Empty;
            }
            else
            {
                string settingString = o.ToString();
                if (escapeSpecialCharacters)
                {
                    settingString = EscapeSpecialCharacters(settingString);
                }

                return settingString;
            }
        }

        /// <summary>
        /// Convert a string vlaue to setting text.
        /// </summary>
        /// <param name="s">the string value to be converted</param>
        /// <returns>The text converted from s.</returns>
        public static string ToSettingString(string s, bool escapeSettingString = false)
        {
            if (escapeSettingString)
            {
                s = EscapeSpecialCharacters(s);
            }
            return s;
        }

        /// <summary>
        /// Convert the StringCollection object to setting text.
        /// </summary>
        /// <param name="stringCol">The string collection object.</param>
        /// <returns>The text converted from String Collection.</returns>
        public static string ToSettingString(System.Collections.Specialized.StringCollection stringCol, bool escapeSpecialCharacters = false)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in stringCol)
            {
                //sb.AppendLine(s.Replace("\t", "        "));
                sb.AppendLine(s);
            }
            string settingValue = sb.ToString().TrimEnd('\r', '\n');
            if (escapeSpecialCharacters)
            {
                settingValue = EscapeSpecialCharacters(settingValue);
            }
            return settingValue;
        }

        private static string EscapeSpecialCharacters(string s)
        {
            string returnString = s;
            returnString = returnString.Replace("\"", "%quote%")
                .Replace('\r', ';')
                .Replace('\n', ';')
                .Replace(";;", ";")
                .Replace('\t', '=');

            return returnString;
        }

        /// <summary>
        /// Convert from a string to a StringCollection object
        /// </summary>
        /// <param name="settingValue">The text of setting value.</param>
        /// <returns>A StringCollection object</returns>
        private static System.Collections.Specialized.StringCollection FromSettingString(string settingValue)
        {
            System.Collections.Specialized.StringCollection stringCol = new System.Collections.Specialized.StringCollection();
            string[] lines = settingValue.Split(new char[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                stringCol.Add(line);
            }

            return stringCol;
        }

        /// <summary>
        /// Convert the text of setting value to a specified type.
        /// </summary>
        /// <typeparam name="T">The type that the setting value will be converted to.</typeparam>
        /// <param name="settingValue">The text of setting value.</param>
        /// <returns>An instance of the specified type.</returns>
        private static T FromSettingString<T>(string settingValue)
        {
            return (T)Convert.ChangeType(settingValue, typeof(T));
        }
        #endregion Helpers
    }
}
