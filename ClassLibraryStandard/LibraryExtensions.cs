using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Tempo
{
    /// <summary>
    /// Extension methods for the Library project
    /// </summary>
    internal static class LibraryExtensions
    {
        /// <summary>
        /// Like Type.GetProperty, except the name is case insensitive
        /// </summary>
        public static PropertyInfo MyGetPropertyCaseInsensitive(this Type type, string name)
        {
            if(_propertyInfoCache == null)
            {
                _propertyInfoCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
            }

            // See if this type is in the cache
            if (!_propertyInfoCache.TryGetValue(type, out var propertiesMap))
            {
                // Add it to the cache

                var propertyInfos = type.GetProperties();
                propertiesMap = new Dictionary<string, PropertyInfo>(propertyInfos.Length);
                foreach (var pi in propertyInfos)
                {
                    propertiesMap[pi.Name.ToUpper()] = pi;
                }

                _propertyInfoCache[type] = propertiesMap;
            }

            // From the properties for this type, get the PropertyInfo or null
            propertiesMap.TryGetValue(name.ToUpper(), out var propertyInfo);
            return propertyInfo;
        }

        // Mapping from upper-cased property names to their PropertyInfo for a type
        static Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyInfoCache = null;
    }
}
