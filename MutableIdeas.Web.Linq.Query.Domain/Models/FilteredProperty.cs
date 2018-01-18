using MutableIdeas.Web.Linq.Query.Domain.Enums;
using System;

namespace MutableIdeas.Web.Linq.Query.Domain.Models
{
    public class FilteredProperty
    {
		public FilterPropertyInfo FilterPropertyInfo { get; set; }
		public string PropertyName { get; set; }
		public string PropertyKey { get; set; }
		public Type PropertyType { get; set; }
    }
}