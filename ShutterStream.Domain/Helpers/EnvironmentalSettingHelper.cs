using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Interfaces.Helpers;

namespace ShutterStream.Domain.Helpers
{
    public class EnvironmentalSettingHelper(IServiceProvider serviceProvider) : IEnvironmentalSettingHelper
    {
        private Dictionary<EnvironmentalSettingEnum, string> _environmentalSettings = new();

        public async Task LoadEnvironmentalSettings()
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                _environmentalSettings = await dbContext.EnvironmentalSettings.ToDictionaryAsync(
                    x => Enum.Parse<EnvironmentalSettingEnum>(x.Key),
                    x => x.Value
                );
            }
        }

        public string GetEnvironmentalSettingValue(EnvironmentalSettingEnum key)
        {
            if (_environmentalSettings.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Environmental setting for key '{key}' was not found.");
        }

        public async Task<bool> UpdateEnvironmentalSettingValue(EnvironmentalSettingEnum key, string newValue)
        {
            if (!_environmentalSettings.ContainsKey(key))
            {
                return false;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.EnvironmentalSettings.Where(x => x.Key == key.ToString()).First().Value = newValue;
                await dbContext.SaveChangesAsync();
            }

            _environmentalSettings[key] = newValue;
            return true;
        }
    }
}
