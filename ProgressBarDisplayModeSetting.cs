using Zorro.Settings;
using Zorro.Core;
using Zorro.Core.CLI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProgressMap
{
    enum ProgressBarDisplayMode
    {
        Full, Centered
    }

    class ProgressBarDisplayModeSetting : EnumSetting<ProgressBarDisplayMode>, IExposedSetting
    {
        public string GetDisplayName()
        {
            return "Summit Roster display mode";
        }

        public string GetCategory()
        {
            return "General";
        }

        protected override ProgressBarDisplayMode GetDefaultValue()
        {
            return ProgressBarDisplayMode.Full;
        }

        public override List<UnityEngine.Localization.LocalizedString> GetLocalizedChoices()
        {
            return null;
        }

        public override void ApplyValue()
        {
            //
        }
    }
}