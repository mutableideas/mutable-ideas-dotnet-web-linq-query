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
				.First(p => p.Name == "Contains" && p.GetParameters().Count() == 2);
			MethodInfo genericContains = containsMethod.MakeGenericMethod(itemType);

			return genericContains;
		}

		public static bool IsNumeric(this Type itemType)
		{
			switch (Type.GetTypeCode(itemType))
			{
				case TypeCode.Byte:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}

			return false;
		}
    }
}