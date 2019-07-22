using Harmony;
using System.Reflection;

namespace DropCostPerMech
{
    public class DropCostPerMech
    {
        internal static string ModDirectory;
        public static void Init(string directory, string settingsJSON) {
            var harmony = HarmonyInstance.Create("DropCostPerMech");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModDirectory = directory;
        }
    }
}
