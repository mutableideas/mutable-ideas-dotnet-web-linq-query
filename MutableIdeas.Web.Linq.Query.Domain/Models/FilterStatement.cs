﻿using System.Collections.Generic;
using MutableIdeas.Web.Linq.Query.Domain.Enums;

namespace MutableIdeas.Web.Linq.Query.Domain.Models
{
	public class FilterStatement<T>
		where T : class
	{
		public string PropertyName { get; set; }
		public string Value { get; set; }
		public FilterOperator? Operator { get; set; }
		public FilterType Comparison { get; set; }
		public IEnumerable<FilteredProperty> FilteredProperties { get; set; }
	}
}