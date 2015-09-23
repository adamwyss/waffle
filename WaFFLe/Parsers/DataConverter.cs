using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class DataConverter
    {
        /// <summary />
        private static Dictionary<string, string> index;

        /// <summary />
        private DataConverter()
        {
        }

        /// <summary />
        public static string ConvertToCode(string city)
        {
            Ensure();

            if (index.ContainsKey(city))
            {
                return index[city];
            }

            return "";
            throw new InvalidOperationException();
        }

        /// <summary />
        public static string ConvertToName(string code)
        {
            Ensure();

            if (index.ContainsKey(code))
            {
                return index[code];
            }

            throw new InvalidOperationException();
        }

        /// <summary />
        private static void Ensure()
        {
            if (index == null)
            {
                index = new Dictionary<string,string>();

                Assembly assembly = Assembly.GetExecutingAssembly();

                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith("CityLookup.txt"))
                    {
                        using (Stream resource = assembly.GetManifestResourceStream(name))
                        {
                            using (StreamReader sr = new StreamReader(resource))
                            {
                                string x = sr.ReadToEnd();

                                string[] x2 = x.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string x3 in x2)
                                {
                                    string[] x4 = x3.Split(new string[] {"<>"}, StringSplitOptions.None);

                                    if (x4.Length == 2)
                                    {
                                        string v1 = x4[0].Trim();
                                        string v2 = x4[1].Trim();

                                        index.Add(v1, v2);
                                        index.Add(v2, v1);
                                    }
                                }

                            }
                        }

                        break;
                    }
                }
            }
        }
    }
}
