using System;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    public class ConfigChangeModel
    {
        public ConfigObjectTypeEnum ConfigObjectType { get; set; }
        public ConfigVerbEnum ConfigVerb { get; set; }
        public int Key { get; set; }
        public string NewConfig { get; set; }

        #region user-defined conversions to or from a base class are not allowed
        /*
        public static explicit operator ConfigChangeModel(object o)
        {
            try
            {
                Type type = o.GetType();
                ConfigChangeModel model = new ConfigChangeModel();
                ConfigObjectTypeEnum configObjectType;
                if (Enum.TryParse(type.GetProperty("ConfigObjectType").GetValue(o, null).ToString(), out configObjectType))
                    model.ConfigObjectType = configObjectType;
                else
                    return null;
                ConfigVerbEnum configVerb;
                if (Enum.TryParse(type.GetProperty("ConfigVerb").GetValue(o, null).ToString(), out configVerb))
                    model.ConfigVerb = configVerb;
                else
                    return null;
                int key;
                if (int.TryParse(type.GetProperty("Key").GetValue(o, null).ToString(), out key))
                    model.Key = key;
                else
                    return null;
                model.NewConfig = type.GetProperty("NewConfig").GetValue(o, null).ToString();

                return model;
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
                return null;
            }
        }*/
        #endregion
    }

    public class RiMEConfigModel
    {
        public int ConfigObjectType { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime UpdatedTime { get; set; }
    }

    public static class ConfigChangeModelExtensions
    {
        public static ConfigChangeModel CastToConfigChangeModel(this object o)
        {
            try
            {
                Type type = o.GetType();
                ConfigChangeModel model = new ConfigChangeModel();
                ConfigObjectTypeEnum configObjectType;
                if (Enum.TryParse(type.GetProperty("ConfigObjectType").GetValue(o, null).ToString(), out configObjectType))
                    model.ConfigObjectType = configObjectType;
                else
                    return null;
                ConfigVerbEnum configVerb;
                if (Enum.TryParse(type.GetProperty("ConfigVerb").GetValue(o, null).ToString(), out configVerb))
                    model.ConfigVerb = configVerb;
                else
                    return null;
                int key;
                string keyString = type.GetProperty("Key").GetValue(o, null).ToString();
                if (string.IsNullOrEmpty(keyString) || string.IsNullOrWhiteSpace(keyString))
                {
                    model.Key = 0;
                }
                else
                {
                    if (int.TryParse(keyString, out key))
                        model.Key = key;
                    else
                        return null;
                }

                model.NewConfig = type.GetProperty("NewConfig").GetValue(o, null).ToString();

                return model;
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
                return null;
            }
        }
    }

    public enum ConfigObjectTypeEnum
    {
        CriteriaCode = 2,
        DerivedAttribute = 4,
        EvaluationRule = 8,
        AggregationDefinition = 128,
        ListDefinition = 512
    }

    public enum ConfigVerbEnum
    {
        Add,
        Update,
        Remove
    }
}
