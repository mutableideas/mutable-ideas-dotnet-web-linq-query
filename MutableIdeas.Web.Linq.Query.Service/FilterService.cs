using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;
using MutableIdeas.Web.Linq.Query.Domain.Models;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public class FilterService<T> : IFilterService<T>
		where T : class
    {
        FilterOperator? _operator;
		int _parameterIndex = 0;
		FilterStatement _currentFilterStatement = null;

		readonly ParameterExpression _pe;
		readonly List<FilterStatement> _filterStatements;

		readonly MethodInfo stringContainsMethod = typeof(string).GetRuntimeMethod("Contains", new[] { typeof(string) });
		readonly MethodInfo stringToLowerMethod = typeof(string).GetRuntimeMethod("ToLower", new Type[0]);

		protected Dictionary<string, PropertyInfo> _propertyInfo;
		
		public FilterService()
        {
			_filterStatements = new List<FilterStatement>();
			_propertyInfo = new Dictionary<string, PropertyInfo>();
			_pe = Expression.Parameter(typeof(T), "entity");
		}

		public Expression<Func<T, bool>> Build()
		{
			Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(BuildStatements(), _pe);

			_filterStatements.Clear();
			_operator = null;
			_parameterIndex = 0;

			return lambda;
		}

        public void By(string propertyName, string value, FilterType filterType)
        {
			_filterStatements.Add(new FilterStatement
			{
				Comparison = filterType,
				Operator = _operator,
				PropertyName = propertyName.ToLower(),
				Value = value
			});
        }

		public void And()
        {
            _operator = FilterOperator.And;
        }     

        public void Or()
        {
            _operator = FilterOperator.Or;
        }

		Expression BuildStatements()
		{
			Expression lastExpression = null;

			foreach (FilterStatement filterStatement in _filterStatements)
			{
				_currentFilterStatement = filterStatement;
				Expression left = ParsePropertyExpression(filterStatement.PropertyName, _pe);
				ConstantExpression right = GetConstantExpression(filterStatement.PropertyName, filterStatement.Value);
				Expression comparingExpression = GetComparingExpression(left, right, filterStatement.PropertyName, filterStatement.Comparison);

				if (lastExpression != null)
				{
					if (!filterStatement.Operator.HasValue)
						throw new InvalidOperationException("Filter operator must be assigned before adding another expression");

					lastExpression = GetOperatorExpression(lastExpression, comparingExpression, filterStatement.Operator.Value);
					continue;
				}

				lastExpression = comparingExpression;
			}

			return lastExpression;
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

		Expression ParsePropertyExpression(string property, string value, Type itemType, ParameterExpression propertyExpression)
		{
			int index = 0;
			PropertyInfo propertyInfo = GetPropertyInfo(itemType, property);
			string[] properties = property.ToLower().Split('.');
			Expression leftExpression = propertyExpression;

			foreach (string propertyName in properties)
			{
				if (index > 0)
					propertyInfo = GetPropertyInfo(propertyInfo.PropertyType, propertyName);

				index++;

				if (propertyInfo.PropertyType.IsEnumerable())
				{
					return AnyExpression(leftExpression, properties, value, propertyInfo.PropertyType);
				}

				leftExpression = Expression.Property(leftExpression, propertyInfo);
			}

			if (!_propertyInfo.ContainsKey(_currentFilterStatement.PropertyName))
				_propertyInfo.Add(_currentFilterStatement.PropertyName, propertyInfo);

			return Expression.Lambda(leftExpression, propertyExpression).Body;
		}

		ConstantExpression GetConstantExpression(string property, string value)
		{
			Type propType = _propertyInfo[property.ToLower()].PropertyType;
			Type valueType = propType.FirstGenericParameter() ?? propType;

			return GetConstantExpression(value, valueType);
		}

		ConstantExpression GetConstantExpression(string value, Type valueType)
		{
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

			// need to come back to this for any bugs with an empty value
			Expression propertyExpression = ParsePropertyExpression(propertyName, string.Empty, propInfo.PropertyType, _pe);
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

		Expression AnyExpression(Expression propertyExpression, IEnumerable<string> properties, string value, Type itemType)
		{
			Type genericParameter = itemType.FirstGenericParameter();
			Type func = typeof(Func<,>);
			Type delegateFunc = func.MakeGenericType(genericParameter, typeof(bool));
			ParameterExpression pe = GetParameter(genericParameter);
			PropertyInfo propInfo = GetPropertyInfo(genericParameter, properties.First());

			string property = string.Join(".", properties.ToArray());
			Expression leftExpression = ParsePropertyExpression(property, value, propInfo.PropertyType, pe);

			return null;
		}

		ParameterExpression GetParameter(Type itemType)
		{
			string parameterName = $"entity{_parameterIndex}";
			_parameterIndex++;

			return Expression.Parameter(itemType, parameterName);
		}

		PropertyInfo GetPropertyInfo(Type propertyType, string propertyName)
		{
			if (propertyType.HasElementType)
				propertyType = propertyType.FirstGenericParameter();

			return propertyType.GetRuntimeProperties().First(p => p.Name.ToLower() == propertyName);
		}
	}
}