
namespace WaFFL.Evaluation
{
    public interface IWaFFLRoster
    {
        double CheckRosterStatus(string search);
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
                //provider = new WaFFLRoster();
                provider = new ESPNRoster();
            }

            return provider.CheckRosterStatus(search) < 0.3;
        }
    }
}
