using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    public class BepInExValheimModUpdater : ValheimModUpdater
    {
        public BepInExValheimModUpdater(ComponentManageContext componentManageContext)
            : base(componentManageContext, "BepInExPack_Valheim", "BepInExPack_Valheim")
        {
        }
    }
}