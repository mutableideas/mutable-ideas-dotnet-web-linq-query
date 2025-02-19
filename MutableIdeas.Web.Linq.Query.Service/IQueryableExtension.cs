﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using MutableIdeas.Web.Linq.Query.Domain.Enums;
using NaturalSort.Extension;

namespace MutableIdeas.Web.Linq.Query.Service.Extensions
{
	public static class IQueryableExtension
	{
        static IComparer<string> _comparer = StringComparer.OrdinalIgnoreCase.WithNaturalSort();

        static MethodInfo NaturalSortCompare()
        {
            return typeof(IComparer<string>).GetMethod("Compare");
        }

		public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string orderByProperty, SortDirection direction)                          
		{
			string command = direction == SortDirection.Descending || direction == SortDirection.NatrualDesc ? "OrderByDescending" : "OrderBy";

			Type type = typeof(T);

			ParameterExpression parameter = Expression.Parameter(type, "p");
			Expression propertyAccess = ExpressionExtension.GetPropertyExpression(parameter, orderByProperty);
			Expression orderByExpression = Expression.Lambda(propertyAccess, parameter);
            List<Expression> expressionParams = new List<Expression> {
                source.Expression,
                Expression.Quote(orderByExpression)
            };

            List<Type> parameterTypes = new List<Type> {
                type,
                propertyAccess.Type
            };

            if (propertyAccess.Type != typeof(string) && (direction == SortDirection.NatrualDesc || direction == SortDirection.NaturalAsc))
                throw new FormatException("Natural Sorting must be on a type that is a string.");

            if (propertyAccess.Type == typeof(string) && (direction == SortDirection.NatrualDesc || direction == SortDirection.NaturalAsc))
            {
                Expression naturalSort = Expression.Constant(StringComparer.OrdinalIgnoreCase.WithNaturalSort());
                expressionParams.Add(naturalSort);
            }

            MethodCallExpression resultExpression = Expression.Call(
                typeof(Queryable),
                command,
                parameterTypes.ToArray(),
				expressionParams.ToArray());

            return source.Provider.CreateQuery<T>(resultExpression);
		}
	}
}
