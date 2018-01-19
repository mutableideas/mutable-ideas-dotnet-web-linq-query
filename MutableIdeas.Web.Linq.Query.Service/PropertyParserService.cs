using System;
using System.Collections.Generic;
using System.Reflection;

using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Models;

namespace MutableIdeas.Web.Linq.Query.Service
{
	internal class PropertyParserService
	{
		public IEnumerable<FilteredProperty> GetFilterProperties<T>(string properties) where T : class
		{
			string[] propertyValues = properties.ToLower().Split('.');
			Type itemType = typeof(T);
			var filteredProperties = new List<FilteredProperty>();
			var propertyKeys = new List<string>();

			foreach (string property in propertyValues)
			{
				// property info may be wrong for nullables
				PropertyInfo propInfo = itemType.GetPropertyInfo(property);

				if (propInfo == null)
					throw new ArgumentNullException($"'{property}' in '{properties}' is not a valid property on type {itemType.Name}");

				propertyKeys.Add(property);

				filteredProperties.Add(new FilteredProperty
				{
					PropertyKey = string.Join(".", propertyKeys),
					FilterPropertyInfo = GetFilteredPropertyInfo(propInfo.PropertyType),
					PropertyName = propInfo.Name,
					PropertyType = propInfo.PropertyType
				});

				itemType = propInfo.PropertyType;
			}

			return filteredProperties;
		}

		FilterPropertyInfo GetFilteredPropertyInfo(Type itemType)
		{
			if (itemType.IsNullable())
				return FilterPropertyInfo.Nullable;

			if (itemType.IsEnumerable())
				return FilterPropertyInfo.Enumerable;

			if (itemType.IsValueType)
				return FilterPropertyInfo.ValueType;

			return FilterPropertyInfo.ReferenceType;
		}
	}
}