using System;
using System.Collections;
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
		bool _isEnumerable = false;
		bool _isNullable = false;

		readonly ParameterExpression _pe;
		readonly List<FilterStatement> _filterStatements;

		readonly MethodInfo arrayContainsMethod = typeof(IList).GetRuntimeMethod("Contains", new[] { typeof(object) });
		readonly MethodInfo stringToLowerMethod = typeof(string).GetRuntimeMethod("ToLower", new Type[0]);
		readonly MethodInfo getArrayConstant = typeof(ExpressionExtension).GetRuntimeMethods().First(p => p.Name == "GetArrayConstantValue");
		readonly MethodInfo getConstantValue = typeof(ExpressionExtension).GetRuntimeMethods().First(p => p.Name == "ConvertValue");

		protected Dictionary<string, PropertyInfo> _propertyInfo;

		public FilterService()
		{
			_filterStatements = new List<FilterStatement>();
			_propertyInfo = new Dictionary<string, PropertyInfo>();
			_pe = Expression.Parameter(typeof(T), "entity");
		}

		public Expression<Func<T, bool>> Build()
		{
			ParameterExpression pe = GetParameter(typeof(T));
			Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(BuildStatements(pe), pe);

			_filterStatements.Clear();
			_operator = null;
			_parameterIndex = 0;

			return lambda;
		}

		public void By(string propertyName, string value, FilterType filterType)
		{
			if (_filterStatements.Count() == 1 && !_operator.HasValue)
				throw new InvalidOperationException("Filter statement must be joined by an \'And\' or \'Or\' operator.");

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
			CheckFilters();
			_operator = FilterOperator.And;
		}

		public void Or()
		{
			CheckFilters();
			_operator = FilterOperator.Or;
		}

		void CheckFilters()
		{
			if (_filterStatements.Count() == 0)
				throw new InvalidOperationException("No filter statements have been supplied to join the statements with an 'and' or 'or'.");
		}

		Expression BuildStatements(ParameterExpression pe)
		{
			Expression lastExpression = null;
			Type filterEntityType = typeof(T);
			List<MemberExpression> properties;
			Expression comparingExpression;

			_filterStatements.Each(statement =>
			{
				_isEnumerable = false;
				_isNullable = false;
				_currentFilterStatement = statement;

				properties = new List<MemberExpression>();
				comparingExpression = ParsePropertyExpression(statement.PropertyName, statement.Value, filterEntityType, pe, properties.Add);

				if (!_isEnumerable && !_isNullable)
				{
					ConstantExpression right = GetConstantExpression(statement.PropertyName, statement.Value);
					comparingExpression = GetComparingExpression(comparingExpression, right, statement.Comparison);
				}

				if (lastExpression != null)
				{
					if (!statement.Operator.HasValue)
						throw new InvalidOperationException("Filter operator must be assigned before adding another expression");

					lastExpression = GetOperatorExpression(lastExpression, comparingExpression, statement.Operator.Value);
				}
				else
				{
					lastExpression = comparingExpression;

					if (properties.Count() > 0)
					{
						Expression expression = NullCheckProperties(properties);
						lastExpression = GetOperatorExpression(expression, lastExpression, FilterOperator.And);
					}
				}
			});

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
					return ExpressionExtension.ContainsExpression(left, right);
				case FilterType.ContainsIgnoreCase:
					Expression leftCase = Expression.Call(left, stringToLowerMethod);
					Expression rightCase = Expression.Call(right, stringToLowerMethod);
					return ExpressionExtension.ContainsExpression(leftCase, rightCase);
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
			PropertyInfo propertyInfo = itemType.GetPropertyInfo(properties[0]);

			foreach (string propertyName in properties)
			{
				if (propertyInfo == null) break;

				if (index > 0)
					propertyInfo = propertyInfo.PropertyType.GetPropertyInfo(propertyName);

				index++;

				if (propertyInfo.PropertyType.IsEnumerable())
				{
					_isEnumerable = true;
					string[] propNames = properties.Skip(index - 1).ToArray();
					return AnyExpression(leftExpression, propNames, value, propertyInfo);
				}

				leftExpression = Expression.Property(leftExpression, propertyInfo);

				if (!propertyInfo.PropertyType.IsNumeric()
					&& !propertyInfo.PropertyType.IsNullable()
					&& propertyInfo.PropertyType != typeof(bool))
					action(leftExpression as MemberExpression);

				if (propertyInfo.PropertyType.IsNullable())
				{
					_isNullable = true;
					leftExpression = GetNullableCheck(leftExpression as MemberExpression, _currentFilterStatement.Value);
				}
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
			object constantValue = value;

			if (value != null && value.StartsWith("[") && value.EndsWith("]"))
			{
				MethodInfo genericMethod = getArrayConstant.MakeGenericMethod(valueType);
				return genericMethod.Invoke(null, new[] { value }) as ConstantExpression;
			}

			if (value != null)
				constantValue = getConstantValue.MakeGenericMethod(valueType).Invoke(null, new[] { value });

			return Expression.Constant(constantValue, valueType);
		}

		Expression EnumerableContains(ConstantExpression constantExpression, MemberExpression memberExpression)
		{
			Type genericType = constantExpression.Type.FirstGenericParameter();
			MethodInfo containsMethod = constantExpression.Type.GetRuntimeMethods()
				.First(p => p.Name == "Contains");

			return Expression.Call(constantExpression, containsMethod, memberExpression);
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

			expressions.Each(exp =>
			{
				Expression notNull = GetNotNullExpression(exp);
				operatorExpression = operatorExpression == null
					? notNull
					: GetOperatorExpression(operatorExpression, notNull, FilterOperator.And);
			});

			return operatorExpression;
		}

		Expression GetNullableCheck(MemberExpression memberExpression, string value)
		{
			Type genericValue = memberExpression.Type.FirstGenericParameter();
			string hasValue = "true";
			FilterOperator filterOperator = FilterOperator.And;

			if (_currentFilterStatement.Comparison == FilterType.NotEqual)
			{
				hasValue = "false";
				filterOperator = FilterOperator.Or;
			}

			MemberExpression hasValueExpression = Expression.Property(memberExpression, "HasValue");
			ConstantExpression boolValueExpression = GetConstantExpression(hasValue, typeof(bool));
			Expression leftComparingExpression = GetComparingExpression(hasValueExpression, boolValueExpression, FilterType.Equal);

			MemberExpression valuePropertyExpression = Expression.Property(memberExpression, "Value");
			ConstantExpression valueExpression = GetConstantExpression(value, genericValue);
			Expression rightComparingExpression = GetComparingExpression(valuePropertyExpression, valueExpression, _currentFilterStatement.Comparison);

			return GetOperatorExpression(leftComparingExpression, rightComparingExpression, filterOperator);
		}
	}
}