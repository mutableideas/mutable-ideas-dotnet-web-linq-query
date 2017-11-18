using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public class FilterService<T> : IFilterService<T>
		where T : class
    {
        FilterOperator? _operator;
		Expression _lastExpression;

		readonly ParameterExpression _pe;

		readonly MethodInfo stringContainsMethod = typeof(string).GetRuntimeMethod("Contains", new[] { typeof(string) });
		readonly MethodInfo stringToLowerMethod = typeof(string).GetRuntimeMethod("ToLower", new Type[0]);
		protected readonly Dictionary<string, PropertyInfo> _propertyInfo;

		public FilterService()
        {
			_pe = Expression.Parameter(typeof(T), "entity");
			_propertyInfo = typeof(T).GetRuntimeProperties()
				.ToDictionary(p => p.Name.ToLower(), p => p);
		}

		public Expression<Func<T, bool>> Build()
		{
			Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(_lastExpression, _pe);

			_lastExpression = null;
			_operator = null;

			return lambda;
		}

        public void By(string propertyName, string value, FilterType filterType)
        {
            if (!_propertyInfo.ContainsKey(propertyName.ToLower()))
                throw new ArgumentException($"Property {propertyName} is not a valid property.");

			PropertyInfo propertyInfo = _propertyInfo[propertyName.ToLower()];
			Expression left = Expression.Property(_pe, propertyInfo);
			Expression right = Expression.Constant(Convert.ChangeType(value, propertyInfo.PropertyType), propertyInfo.PropertyType);
			Expression comparingExpression = GetComparingExpression(left, right, filterType);

			if (_lastExpression != null)
			{
				if (!_operator.HasValue)
					throw new InvalidOperationException("Filter operator must be assigned before adding another expression");

				_lastExpression = GetOperatorExpression(_lastExpression, comparingExpression, _operator.Value);
				_operator = null;
				return;
			}

			_lastExpression = comparingExpression;
        }

		public void And()
        {
            _operator = FilterOperator.And;
        }     

        public void Or()
        {
            _operator = FilterOperator.Or;
        }

        Expression GetComparingExpression(Expression left, Expression right, FilterType filterType)
        {
            switch(filterType)
            {
                case FilterType.Equal:
                    return Expression.Equal(left, right);
                case FilterType.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case FilterType.GreaterThanOrEqualTo:
                    return Expression.GreaterThanOrEqual(left, right);
                case FilterType.LessThan:
                    return Expression.LessThan(left, right);
                case FilterType.LessThanOrEqualTo:
                    return Expression.LessThanOrEqual(left, right);
                case FilterType.NotEqual:
                    return Expression.NotEqual(left, right);
				case FilterType.Contains:
					return Expression.Call(left, stringContainsMethod, new[] { right });
				case FilterType.ContainsIgnoreCase:
					Expression leftCase = Expression.Call(left, stringToLowerMethod);
					Expression rightCase = Expression.Call(right, stringToLowerMethod);
					return Expression.Call(leftCase, stringContainsMethod, new[] { rightCase });
            }

            throw new ArgumentException($"The filter type {filterType} does not exist.");
        }

        Expression GetOperatorExpression(Expression left, Expression right, FilterOperator filterOperator)
        {
            return filterOperator == FilterOperator.And ? Expression.AndAlso(left, right) : Expression.OrElse(left, right);
        }
    }
}