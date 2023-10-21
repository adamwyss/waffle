
namespace WaFFL.Evaluation
{
    public interface IWaFFLRoster
    {
        string CheckRosterStatus(string search);
    }

    /// <summary />
    public static class WaFFLTeam
    {
        /// <summary />
        private static IWaFFLRoster provider;

        /// <summary />
        public static bool IsRostered(string search)
        {
            if (provider == null)
            {
                provider = new ESPNRoster();
            }

            return provider.CheckRosterStatus(search) != null;
        }

        /// <summary />
        public static string IsRosteredOn(string search)
        {
            if (provider == null)
            {
                provider = new ESPNRoster();
            }

            return provider.CheckRosterStatus(search);
        }
    }
}
