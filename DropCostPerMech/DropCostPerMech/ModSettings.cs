using System.Collections.Generic;
using BattleTech;

namespace DropCostPerMech
{
    public class ModSettings
    {
        public bool Debug = false;
        public string modDirectory;

        public float percentageOfMechCost = 0.0025f;

        public bool CostByTons = false;
        public int cbillsPerTon = 500;
        public bool someFreeTonnage = false;
        public int freeTonnageAmount = 0;
        public bool NewAlgorithm = false;
        public bool BEXCE = false;
        public bool TonnageLimits = false;
        public double StartingTonnage = 200;
        public double TonnagePerStep = 25;
    }
}
