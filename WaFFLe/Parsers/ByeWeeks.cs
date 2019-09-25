using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace WaFFL.Evaluation.Parsers
{
    public class ByeWeeks
    {
        /// <summary />
        private static Dictionary<string, int> index;

        public static int ByeWeek(string team)
        {
            Ensure();

            int byeWeek;
            bool success = index.TryGetValue(team, out byeWeek);
            if (success)
            {
                return byeWeek;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsByeWeek(string team, int week)
        {
            Ensure();

            int byeWeek;
            bool success = index.TryGetValue(team, out byeWeek);
            return success && byeWeek == week;
        }

        private static void Ensure()
        {
            if (index == null)
            {
                index = new Dictionary<string, int>();

                Assembly assembly = Assembly.GetExecutingAssembly();

                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith("ByeWeeks.txt"))
                    {
                        using (Stream resource = assembly.GetManifestResourceStream(name))
                        {
                            using (StreamReader sr = new StreamReader(resource))
                            {
                                string x = sr.ReadToEnd();

                                string[] x2 = x.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string x3 in x2)
                                {
                                    string[] x4 = x3.Split(new string[] { "=" }, StringSplitOptions.None);

                                    if (x4.Length == 2)
                                    {
                                        string v1 = x4[0].Trim();
                                        string v2 = x4[1].Trim();

                                        int intVal;
                                        bool success = int.TryParse(v2, out intVal);
                                        if (success) {
                                            index.Add(v1, intVal);
                                        } 
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
