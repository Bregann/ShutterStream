using ShutterStream.Domain.Enums;

namespace ShutterStream.Domain.Interfaces.Helpers
{
    public interface IEnvironmentalSettingHelper
    {
        Task LoadEnvironmentalSettings();
        string GetEnvironmentalSettingValue(EnvironmentalSettingEnum key);
        Task<bool> UpdateEnvironmentalSettingValue(EnvironmentalSettingEnum key, string newValue);
    }
}
