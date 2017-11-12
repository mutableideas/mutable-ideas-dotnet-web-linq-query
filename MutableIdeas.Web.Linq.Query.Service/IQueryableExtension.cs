using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MutableIdeas.Web.Linq.Query.Domain.Enums;

namespace MutableIdeas.Web.Linq.Query.Service.Extensions
{
	public static class IQueryableExtension
	{
		public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string orderByProperty, SortDirection direction)                          
		{
			string command = direction == SortDirection.Descending ? "OrderByDescending" : "OrderBy";

			Type type = typeof(T);
			PropertyInfo property = type.GetRuntimeProperties()
				.Where(p => p.Name.ToLower() == orderByProperty.Trim().ToLower())
				.First();

			ParameterExpression parameter = Expression.Parameter(type, "p");
			MemberExpression propertyAccess = Expression.MakeMemberAccess(parameter, property);
			Expression orderByExpression = Expression.Lambda(propertyAccess, parameter);
			MethodCallExpression resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, property.PropertyType },
										source.Expression, Expression.Quote(orderByExpression));

			return source.Provider.CreateQuery<T>(resultExpression);
		}
	}
}
