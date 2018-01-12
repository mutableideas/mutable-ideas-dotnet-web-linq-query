using MutableIdeas.Web.Linq.Query.Domain.Services;
using System;
using System.Collections.Generic;
using MutableIdeas.Web.Linq.Query.Domain.Models;
using System.Reflection;
using MutableIdeas.Web.Linq.Query.Domain.Enums;

namespace MutableIdeas.Web.Linq.Query.Service
{
	public class PropertyParserSevice : IPropertyParserService
	{
		public IEnumerable<FilteredProperty> GetFilterProperties<T>(string properties) where T : class
		{
			string[] propertyValues = properties.ToLower().Split('.');
			Type itemType = typeof(T);
			var filteredProperties = new List<FilteredProperty>();
			
			foreach (string property in propertyValues)
			{
				PropertyInfo propInfo = itemType.GetPropertyInfo(property);

				if (propInfo == null)
					throw new ArgumentNullException($"'{property}' in '{properties}' is not a valid property on type {itemType.Name}");

				filteredProperties.Add(new FilteredProperty
				{
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
			if (itemType.IsValueType)
				return FilterPropertyInfo.ValueType;

			if (itemType.IsEnumerable())
				return FilterPropertyInfo.Enumerable;

			if (itemType.IsGenericType)
				return FilterPropertyInfo.Generic;

			return FilterPropertyInfo.ReferenceType;
		}
	}
}