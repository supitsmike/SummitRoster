using System.Collections.Generic;
using Zorro.Settings;

namespace SummitRoster;

internal enum ProgressBarDisplayMode
{
    Full, Centered
}

internal class ProgressBarDisplayModeSetting : EnumSetting<ProgressBarDisplayMode>, IExposedSetting
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