using System;
using System.Linq;
using System.Linq.Expressions;
using MutableIdeas.Web.Linq.Query.Domain.Enums;

namespace MutableIdeas.Web.Linq.Query.Service.Extensions
{
	public static class IQueryableExtension
	{
		public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string orderByProperty, SortDirection direction)                          
		{
			string command = direction == SortDirection.Descending ? "OrderByDescending" : "OrderBy";

			Type type = typeof(T);
			
			ParameterExpression parameter = Expression.Parameter(type, "p");
			Expression propertyAccess = ExpressionExtension.GetPropertyExpression(parameter, orderByProperty);
			Expression orderByExpression = Expression.Lambda(propertyAccess, parameter);
			MethodCallExpression resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, propertyAccess.Type },
										source.Expression, Expression.Quote(orderByExpression));

			return source.Provider.CreateQuery<T>(resultExpression);
		}
	}
}
