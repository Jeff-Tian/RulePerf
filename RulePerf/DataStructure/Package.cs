using System;
using System.Collections.Generic;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.DataStructure
{
    public class Package<TLabel, TItem>
    {
        public TLabel Lable { get; set; }
        public IList<TItem> Items { get; set; }

        public Package() {
            this.Lable = default(TLabel);
            this.Items = new List<TItem>();
        }

        public Package(TLabel label, TItem item)
        {
            this.Lable = label;
            this.Items = new List<TItem>();
            this.Items.Add(item);
        }

        public delegate bool SplitDelegate<T, TDelimiter>(T line, out TLabel label, out TItem item, params TDelimiter[] delimiters);

        public static Dictionary<TLabel, Package<TLabel, TItem>> FromArray<TArrayItem, TDelimiter>(IList<TArrayItem> array, SplitDelegate<TArrayItem, TDelimiter> splitDelegate, params TDelimiter[] delimiters)
        {
            Dictionary<TLabel, Package<TLabel, TItem>> packages = new Dictionary<TLabel, Package<TLabel, TItem>>();
            for (int i = 0; i < array.Count; i++)
            {
                TLabel label;
                TItem item;
                if (splitDelegate(array[i], out label, out item, delimiters))
                {
                    if (packages.ContainsKey(label))
                        packages[label].Items.Add(item);
                    else
                        packages.Add(label, new Package<TLabel, TItem>(label, item));
                }
            }
            return packages;
        }

        public static bool StringSplit(string input, out string label, out string item, params string[] delimiters)
        {
            label = "";
            item = "";

            if (delimiters.Length > 0)
            {
                string[] items = input.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length >= 2)
                {
                    label = items[0];
                    item = items[1];

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static Dictionary<string, Package<string, string>> FromStringArray(IList<string> array, params string[] delimiters)
        {
            Dictionary<string, Package<string, string>> packages = new Dictionary<string, Package<string, string>>();
            for (int i = 0; i < array.Count; i++)
            {
                string label;
                string item;
                if (StringSplit(array[i], out label, out item, delimiters))
                {
                    if (packages.ContainsKey(label))
                        packages[label].Items.Add(item);
                    else
                        packages.Add(label, new Package<string, string>(label, item));
                }
            }
            return packages;
        }
    }
}
