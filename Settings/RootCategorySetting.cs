namespace SayTheSpire2.Settings;

public class RootCategorySetting : CategorySetting
{
    public RootCategorySetting() : base("", "Settings")
    {
    }

    public override bool IsRoot => true;
}
