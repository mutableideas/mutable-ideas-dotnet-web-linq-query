using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public static class ITypeExtension
    {
		public static bool IsEnumerable(this Type itemType)
		{
            Type genericEnumerable = typeof(IEnumerable<>);

            return itemType != typeof(string)
                && (itemType.IsArray
                || (itemType.GetTypeInfo().IsGenericType && itemType.GetGenericTypeDefinition() == genericEnumerable)
                || itemType.GetInterfaces().Any(p => p.GetTypeInfo().IsGenericType && p.GetGenericTypeDefinition() == genericEnumerable));
		}

		public static bool IsNullable(this Type itemType)
		{
			return itemType.GetTypeInfo().IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static Type FirstGenericParameter(this Type itemType)
		{
			return itemType.IsArray ? itemType.GetElementType() : itemType.GenericTypeArguments.FirstOrDefault();
		}

		public static PropertyInfo GetPropertyInfo(this Type propertyType, string name)
		{
			if (propertyType.HasElementType || propertyType.IsEnumerable())
				propertyType = propertyType.FirstGenericParameter();

			return propertyType.GetRuntimeProperties().FirstOrDefault(p => p.Name.ToLower() == name.ToLower());
		}
    }
}