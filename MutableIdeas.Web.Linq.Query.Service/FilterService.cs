using System;
using System.Collections;
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
			Expression left = ParsePropertyExpression(propertyName);
			ConstantExpression right = GetConstantExpresion(propertyName, value);
			Expression comparingExpression = GetComparingExpression(left, right, propertyName, filterType);

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

        Expression GetComparingExpression(Expression left, ConstantExpression right, string propertyName, FilterType filterType)
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
					return GetContainsExpression(left, right, propertyName);
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

		Expression ParsePropertyExpression(string property)
		{
			string[] propertyChain = property.Split('.');

			if (propertyChain.Length == 1)
				return Expression.Property(_pe, _propertyInfo[property.ToLower()]);
			
			PropertyInfo propInfo = _propertyInfo[propertyChain[0].ToLower()];
			Expression body = _pe;

			for (int index = 0; index < propertyChain.Length; index++)
			{
				string propName = propertyChain[index].ToLower();

				if (index > 0)
					propInfo = propInfo.PropertyType.GetRuntimeProperties()
						.DefaultIfEmpty(null)
						.FirstOrDefault(p => p.Name.ToLower() == propName);

				body = Expression.Property(body, propInfo);
			}

			if (!_propertyInfo.ContainsKey(property))
				_propertyInfo.Add(property.ToLower(), propInfo);

			return Expression.Lambda(body, _pe).Body;
		}

		ConstantExpression GetConstantExpresion(string property, string value)
		{
			Type propType = _propertyInfo[property.ToLower()].PropertyType;
			Type valueType = propType.FirstGenericParameter() ?? propType;
			object constantValue = Convert.ChangeType(value, valueType);

			return Expression.Constant(constantValue, valueType);
		}

		Expression GetContainsExpression(Expression left, ConstantExpression right, string propertyName)
		{
			PropertyInfo propInfo = _propertyInfo[propertyName.ToLower()];
			
			if (!propInfo.PropertyType.IsEnumerable())
				return Expression.Call(left, stringContainsMethod, right);

			// need a check to make sure the property isn't null
			ConstantExpression nullExpressionContant = Expression.Constant(null, typeof(object));
			Expression propertyExpression = ParsePropertyExpression(propertyName);
			Expression comparingExpression = GetComparingExpression(propertyExpression, nullExpressionContant, propertyName, FilterType.NotEqual);

			MethodCallExpression callExpression = Expression.Call(
				typeof(Enumerable),
				"Contains",
				new[] { propInfo.PropertyType.FirstGenericParameter() },
				left,
				right
			);

			return GetOperatorExpression(comparingExpression, callExpression, FilterOperator.And);
		}
    }
}