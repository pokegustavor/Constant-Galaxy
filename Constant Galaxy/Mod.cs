using PulsarModLoader;

namespace Constant_Galaxy
{
    public class Mod : PulsarMod
    {
        public override string Version => "1.0";

        public override string Author => "pokegustavo";

        public override string ShortDescription => "Makes the galaxy factions constantly spread";

        public override string Name => "Active Factions";

        public override string HarmonyIdentifier()
        {
            return "pokegustavo.activefactions";
        }
    }
}
