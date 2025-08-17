using System.Configuration;
using System.IO;

namespace ZapretDesktop.Properties;

internal sealed class Settings : ApplicationSettingsBase
{
    private static Settings defaultInstance = (Settings)Synchronized(new Settings());

    public static Settings Default => defaultInstance;

    [UserScopedSetting]
    [DefaultSettingValue(".")]
    public string StrategyPath
    {
        get => (string)this["StrategyPath"];
        set => this["StrategyPath"] = value;
    }
    
    [UserScopedSetting]
    [DefaultSettingValue(@".\lists")]
    public string ListPath
    {
        get => (string)this["ListPath"];
        set => this["ListPath"] = value;
    }
    
    [UserScopedSetting]
    [DefaultSettingValue(@".\bin")]
    public string BinPath
    {
        get => (string)this["BinPath"];
        set => this["BinPath"] = value;
    }
    
    [UserScopedSetting]
    public bool? IsDarkMode
    {
        get => (bool?)this["IsDarkMode"];
        set => this["IsDarkMode"] = value;
    }
    
    [UserScopedSetting]
    [DefaultSettingValue("ru")]
    public string Language
    {
        get => (string)this["Language"];
        set => this["Language"] = value;
    }
    
    [UserScopedSetting]
    public string ZapretVersion
    {
        get => (string)this["ZapretVersion"];
        set => this["ZapretVersion"] = value;
    }
}
