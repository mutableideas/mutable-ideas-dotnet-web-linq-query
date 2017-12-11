using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;
using MutableIdeas.Web.Linq.Query.Domain.Models;
using System.Collections;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public class FilterService<T> : IFilterService<T>
		where T : class
    {
        FilterOperator? _operator;
		int _parameterIndex = 0;
		FilterStatement _currentFilterStatement = null;
		bool _isEnumerable = false;

		readonly ParameterExpression _pe;
		readonly List<FilterStatement> _filterStatements;

		readonly MethodInfo stringContainsMethod = typeof(string).GetRuntimeMethod("Contains", new[] { typeof(string) });
		readonly MethodInfo arrayContainsMethod = typeof(IList).GetRuntimeMethod("Contains", new[] { typeof(object) });
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
				_isEnumerable = false;
				_currentFilterStatement = filterStatement;

				var properties = new List<MemberExpression>();
				Expression comparingExpression = ParsePropertyExpression(filterStatement.PropertyName, filterStatement.Value, typeof(T), _pe, properties.Add);

				if (!_isEnumerable)
				{
					ConstantExpression right = GetConstantExpression(filterStatement.PropertyName, filterStatement.Value);
					comparingExpression = GetComparingExpression(comparingExpression, right, filterStatement.Comparison);
				}

				if (lastExpression != null)
				{
					if (!filterStatement.Operator.HasValue)
						throw new InvalidOperationException("Filter operator must be assigned before adding another expression");

					lastExpression = GetOperatorExpression(lastExpression, comparingExpression, filterStatement.Operator.Value);
					continue;
				}

				lastExpression = comparingExpression;

				if (properties.Count() > 0)
				{
					Expression expression = NullCheckProperties(properties);
					lastExpression = GetOperatorExpression(expression, lastExpression, FilterOperator.And);
				}
			}

			return lastExpression;
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
					return GetContainsExpression(left, right as ConstantExpression);
				case FilterType.ContainsIgnoreCase:
					Expression leftCase = Expression.Call(left, stringToLowerMethod);
					Expression rightCase = Expression.Call(right, stringToLowerMethod);
					return Expression.Call(leftCase, stringContainsMethod, new[] { rightCase });
				case FilterType.In:
					return EnumerableContains(right as ConstantExpression, left as MemberExpression);
            }

            throw new ArgumentException($"The filter type {filterType} does not exist.");
        }

        Expression GetOperatorExpression(Expression left, Expression right, FilterOperator filterOperator)
        {
            return filterOperator == FilterOperator.And ? Expression.AndAlso(left, right) : Expression.OrElse(left, right);
        }

		Expression ParsePropertyExpression(
			string property,
			string value,
			Type itemType,
			ParameterExpression propertyExpression,
			Action<MemberExpression> action)
		{
			int index = 0;
			string[] properties = property.ToLower().Split('.');

			Expression leftExpression = propertyExpression;
			PropertyInfo propertyInfo = GetPropertyInfo(itemType, properties[0]);

			foreach (string propertyName in properties)
			{
				if (propertyInfo == null) break;

				if (index > 0)
					propertyInfo = GetPropertyInfo(propertyInfo.PropertyType, propertyName);

				index++;

				if (propertyInfo.PropertyType.IsEnumerable())
				{
					_isEnumerable = true;
					string[] propNames = properties.Skip(index - 1).ToArray();
					return AnyExpression(leftExpression, propNames, value, propertyInfo);
				}

				leftExpression = Expression.Property(leftExpression, propertyInfo);

				if (!propertyInfo.PropertyType.IsNumeric())
					action(leftExpression as MemberExpression);
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
			object constantValue;

			if (value != null && value.StartsWith("[") && value.EndsWith("]"))
			{
				MethodInfo methodInfo = GetType().GetRuntimeMethods().First(p => p.Name == "GetArrayConstantValue");
				MethodInfo genericMethod = methodInfo.MakeGenericMethod(valueType);

				return genericMethod.Invoke(null, new[] { value }) as ConstantExpression;
			}

			constantValue = Convert.ChangeType(value, valueType);
			return Expression.Constant(constantValue, valueType);
		}

		static ConstantExpression GetArrayConstantValue<V>(string value)
		{
			Type valueType = typeof(V);
			var values = value.Replace("[", string.Empty)
				.Replace("]", string.Empty)
				.Split(',')
				.Select(p => (V)Convert.ChangeType(p.UnescapeUrlValue(), valueType))
				.ToList();

			return Expression.Constant(values.ToArray(), typeof(V[]));
		}

		Expression GetContainsExpression(Expression left, ConstantExpression right)
		{
			return Expression.Call(left, stringContainsMethod, right);
		}

		Expression EnumerableContains(ConstantExpression constantExpression, MemberExpression memberExpression)
		{
			Type genericType = constantExpression.Type.FirstGenericParameter();
			MethodInfo containsMethod = constantExpression.Type.GetContainsMethod();
			ParameterExpression parameter = GetParameter(genericType);
			Expression lambda = Expression.Lambda(GetComparingExpression(memberExpression, parameter, FilterType.Equal), parameter);

			return Expression.Call(
				typeof(Enumerable),
				"Any",
				new[] { genericType },
				constantExpression,
				lambda
			);
		}

		Expression AnyExpression(Expression propertyExpression, IEnumerable<string> properties, string value, PropertyInfo propertyInfo)
		{
			List<MemberExpression> memberProperties = new List<MemberExpression>();
			Type genericParameter = propertyInfo.PropertyType.FirstGenericParameter();
			Type func = typeof(Func<,>);
			Type delegateFunc = func.MakeGenericType(genericParameter, typeof(bool));

			ParameterExpression pe = GetParameter(genericParameter);
			string property = string.Join(".", properties.ToArray().Skip(1));
			Expression objPropertyExpression = ParsePropertyExpression(property, value, genericParameter, pe, memberProperties.Add);

			MemberExpression entityPropertyExpression = Expression.Property(propertyExpression, propertyInfo.Name);

			if (objPropertyExpression is MemberExpression || objPropertyExpression is ParameterExpression)
			{
				ConstantExpression constant = GetConstantExpression(value, objPropertyExpression.Type);
				Expression comparingExpression = GetComparingExpression(objPropertyExpression, constant, _currentFilterStatement.Comparison);

				if (memberProperties.Count() > 0)
				{
					Expression nullPropertyExpressions = NullCheckProperties(memberProperties);
					comparingExpression = GetOperatorExpression(nullPropertyExpressions, comparingExpression, FilterOperator.And);
				}

				objPropertyExpression = Expression.Lambda(comparingExpression, pe);
			}
			else
			{
				objPropertyExpression = Expression.Lambda(objPropertyExpression, pe);
			}

			Expression anyExpression = Expression.Call(
				typeof(Enumerable),
				"Any",
				new[] { genericParameter },
				entityPropertyExpression,
				objPropertyExpression
			);

			Expression nullExpression = GetNotNullExpression(entityPropertyExpression);
			return GetOperatorExpression(nullExpression, anyExpression, FilterOperator.And);
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

			return propertyType.GetRuntimeProperties().FirstOrDefault(p => p.Name.ToLower() == propertyName);
		}

		Expression GetNotNullExpression(Expression property)
		{
			ConstantExpression nullConstant = GetConstantExpression(null, typeof(object));
			return GetComparingExpression(property, nullConstant, FilterType.NotEqual);
		}

		Expression NullCheckProperties(IEnumerable<MemberExpression> expressions)
		{
			Expression operatorExpression = null;

			foreach (MemberExpression memberExpression in expressions)
			{
				Expression notNull = GetNotNullExpression(memberExpression);
				if (operatorExpression == null)
				{
					operatorExpression = notNull;
					continue;
				}

				operatorExpression = GetOperatorExpression(operatorExpression, notNull, FilterOperator.And);
			}

			return operatorExpression;
		}
	}
}