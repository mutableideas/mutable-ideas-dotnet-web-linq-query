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
			return itemType.IsArray
				|| (itemType.GetTypeInfo().IsGenericType && itemType.GetGenericTypeDefinition() == typeof(IEnumerable<>));
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
			if (propertyType.HasElementType)
				propertyType = propertyType.FirstGenericParameter();

			return propertyType.GetRuntimeProperties().FirstOrDefault(p => p.Name.ToLower() == name.ToLower());
		}
    }
}