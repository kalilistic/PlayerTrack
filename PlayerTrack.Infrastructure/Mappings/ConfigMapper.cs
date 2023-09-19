using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

using Dalamud.Logging;

public static class ConfigMapper
{
    public static PluginConfig ToModel(ConfigEntryDTO[] configEntryDTOs)
    {
        PluginLog.LogVerbose("Entering ConfigMapper.ToModel()");
        var config = new PluginConfig();
        foreach (var property in typeof(PluginConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var configEntryDTO = configEntryDTOs.FirstOrDefault(dto => dto.key == property.Name);
            if (configEntryDTO != null)
            {
                object typedValue;
                if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string))
                {
                    typedValue = Convert.ChangeType(configEntryDTO.value, property.PropertyType);
                }
                else
                {
                    typedValue = JsonConvert.DeserializeObject(configEntryDTO.value, property.PropertyType) ?? string.Empty;
                }

                property.SetValue(config, typedValue);
            }
        }

        return config;
    }

    public static IEnumerable<ConfigEntryDTO> ToDTOs(PluginConfig pluginConfig)
    {
        PluginLog.LogVerbose("Entering ConfigMapper.ToDTOs()");
        var configEntryDTOs = new List<ConfigEntryDTO>();

        foreach (var property in typeof(PluginConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var ignoreAttribute = property.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignoreAttribute != null)
            {
                continue;
            }

            var key = property.Name;
            var propertyValue = property.GetValue(pluginConfig);

            string value;
            if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string))
            {
                value = Convert.ToString(propertyValue) ?? string.Empty;
            }
            else
            {
                value = JsonConvert.SerializeObject(propertyValue);
            }

            if (!string.IsNullOrEmpty(value))
            {
                configEntryDTOs.Add(new ConfigEntryDTO
                {
                    key = key,
                    value = value,
                });
            }
        }

        return configEntryDTOs.ToArray();
    }
}
