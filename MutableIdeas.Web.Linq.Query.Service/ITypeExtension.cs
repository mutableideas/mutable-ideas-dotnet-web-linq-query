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

		public static Type FirstGenericParameter(this Type itemType)
		{
			if (itemType.IsArray)
				return itemType.GetElementType();

			return itemType.GenericTypeArguments.FirstOrDefault();
		}

		public static MethodInfo GetContainsMethod(this Type itemType)
		{
			Type genericItemType = itemType.FirstGenericParameter();
			MethodInfo containsMethod = typeof(Enumerable).GetRuntimeMethods()
				.First(p => p.Name == "Contains");
			MethodInfo genericContains = containsMethod.MakeGenericMethod(itemType);

			return genericContains;
		}
    }
}