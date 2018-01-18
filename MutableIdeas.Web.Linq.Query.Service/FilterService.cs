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
		FilterStatement<T> _currentFilterStatement = null;
		
		readonly List<FilterStatement<T>> _filterStatements;
		readonly IPropertyParserService _propertyParserService;

		readonly MethodInfo arrayContainsMethod = typeof(IList).GetRuntimeMethod("Contains", new[] { typeof(object) });
		readonly MethodInfo stringToLowerMethod = typeof(string).GetRuntimeMethod("ToLower", new Type[0]);
		readonly MethodInfo getArrayConstant = typeof(ExpressionExtension).GetRuntimeMethods().First(p => p.Name == "GetArrayConstantValue");
		readonly MethodInfo getConstantValue = typeof(ExpressionExtension).GetRuntimeMethods().First(p => p.Name == "ConvertValue");

		protected Dictionary<string, PropertyInfo> _propertyInfo;

		public FilterService(IPropertyParserService propertyParserService)
		{
			_filterStatements = new List<FilterStatement<T>>();
			_propertyInfo = new Dictionary<string, PropertyInfo>();

			_propertyParserService = propertyParserService;
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

			_filterStatements.Add(new FilterStatement<T>
			{
				Comparison = filterType,
				Operator = _operator,
				PropertyName = propertyName.ToLower(),
				Value = value,
				FilteredProperties = _propertyParserService.GetFilterProperties<T>(propertyName.ToLower()),
				IsLengthComparison = IsLengthComparison(filterType),
				IsLessThanComparison = LenIncludeNullComparisons(filterType)
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
			Expression expression = null;

			foreach (var statement in _filterStatements)
			{
				Expression propertyExpression = ParsePropertyExpression(statement.FilteredProperties.GetEnumerator(), pe, statement.Comparison, statement.Value);
				if (expression == null)
				{
					expression = propertyExpression;
					continue;
				}

				expression = GetOperatorExpression(expression, propertyExpression, statement.Operator.Value);
			}

			return expression;
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
				case FilterType.LenEqual:
				case FilterType.LenGreaterThan:
				case FilterType.LenGreaterThanOrEqualTo:
				case FilterType.LenLessThan:
				case FilterType.LenLessThanOrEqualTo:
				case FilterType.LenNotEqual:
					return GetLengthComparingExpression(left, right as ConstantExpression, filterType);
			}

            throw new ArgumentException($"The filter type {filterType} does not exist.");
        }

		Expression GetLengthComparingExpression(Expression left, ConstantExpression right, FilterType filterType)
		{
			FilterType comparingFilterType;
			
			switch (filterType)
			{
				case FilterType.LenGreaterThan:
					comparingFilterType = FilterType.GreaterThan;
					break;
				case FilterType.LenGreaterThanOrEqualTo:
					comparingFilterType = FilterType.GreaterThanOrEqualTo;
					break;
				case FilterType.LenLessThan:
					comparingFilterType = FilterType.LessThan;
					break;
				case FilterType.LenLessThanOrEqualTo:
					comparingFilterType = FilterType.LessThanOrEqualTo;
					break;
				case FilterType.LenNotEqual:
					comparingFilterType = FilterType.NotEqual;
					break;
				default:
					comparingFilterType = FilterType.Equal;
					break;
			}

			return GetComparingExpression(left, right, comparingFilterType);
		}

        Expression GetOperatorExpression(Expression left, Expression right, FilterOperator filterOperator)
        {
            return filterOperator == FilterOperator.And ? Expression.AndAlso(left, right) : Expression.OrElse(left, right);
        }

		Expression ParsePropertyExpression(IEnumerator<FilteredProperty> filterProperties, ParameterExpression pe, FilterType comparison, string value)
		{
			Type lastType = pe.Type;
			Expression leftExpression = null;
			MemberExpression propertyExpression = null;
			bool isLessThanComparison = LenIncludeNullComparisons(comparison);

			Func<Expression, Expression, Expression> operatorExpression = (left, right) =>
			{
				FilterOperator filterOperator = isLessThanComparison ? FilterOperator.Or : FilterOperator.And;
				return left == null ? right : GetOperatorExpression(left, right, filterOperator);
			};

			while(filterProperties.MoveNext())
			{
				FilteredProperty filteredProperty = filterProperties.Current;
				string propertyName = filteredProperty.PropertyName;
				propertyExpression = propertyExpression == null ? Expression.Property(pe, propertyName) : Expression.Property(propertyExpression, filteredProperty.PropertyName);

				lastType = filteredProperty.PropertyType;

				switch (filteredProperty.FilterPropertyInfo)
				{
					case FilterPropertyInfo.Enumerable:
						Expression enumerableExpression = GetEnumerableExperssion(propertyExpression, filterProperties, comparison, value);
						return operatorExpression(leftExpression, enumerableExpression);
					case FilterPropertyInfo.Nullable:
						return GetNullableCheck(propertyExpression, comparison, value);
					case FilterPropertyInfo.ReferenceType:
						FilterType filterType = isLessThanComparison ? FilterType.Equal : FilterType.NotEqual;
						Expression notNullExpression = GetNotNullExpression(propertyExpression, filterType);
						leftExpression = operatorExpression(leftExpression, notNullExpression);
						break;
				}
			}

			// this is starting to feel ugly
			Expression comparingLeftExpression = propertyExpression == null ? pe as Expression : propertyExpression;
			ConstantExpression constant = GetConstantExpression(value, lastType);
			Expression comparingExpression = GetComparingExpression(comparingLeftExpression, constant, comparison);

			if (leftExpression != null)
				comparingExpression = GetOperatorExpression(leftExpression, comparingExpression, FilterOperator.And);

			return Expression.Lambda(comparingExpression, pe).Body;
		}

		Expression GetEnumerableExperssion(MemberExpression expression, IEnumerator<FilteredProperty> filterProperties, FilterType comparison, string value)
		{
			if (IsLengthComparison(comparison))
			{
				Expression lengthExpression = GetLengthExpression(expression as MemberExpression);
				ConstantExpression constant = GetConstantExpression(value, typeof(int));
				Expression binaryExpression = GetComparingExpression(lengthExpression, constant, comparison);

				if (LenIncludeNullComparisons(comparison))
				{
					Expression isNullExpression = GetNotNullExpression(expression, FilterType.Equal);
					binaryExpression = GetOperatorExpression(isNullExpression, binaryExpression, FilterOperator.Or);
				}

				return binaryExpression;
			}

			return AnyExpression(expression, filterProperties, comparison, value);
		}

		ConstantExpression GetConstantExpression(string property, string value)
		{
			bool isLengthComparison = IsLengthComparison(_currentFilterStatement.Comparison);

			Type propType = _propertyInfo[property.ToLower()].PropertyType;
			Type valueType = isLengthComparison ? typeof(int) : propType.FirstGenericParameter() ?? propType;

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

		Expression EnumerableCount(MemberExpression expression)
		{
			if (!expression.Type.IsEnumerable())
				throw new ArgumentException("Member Expression must be of an enumerable type.");

			Type genericParameterType = expression.Type.FirstGenericParameter();

			return Expression.Call(
				typeof(Enumerable),
				"Count",
				new[] { genericParameterType },
				expression
			);
		}

		Expression AnyExpression(Expression propertyExpression, IEnumerator<FilteredProperty> filteredProperties, FilterType filterType, string value)
		{
			// keep state of the current filtered property, may need a null check
			FilteredProperty filteredProperty = filteredProperties.Current;

			// generic parameter for the enumerable
			Type genericParameter = filteredProperties.Current.PropertyType.FirstGenericParameter();
			
			// the parameter in the any clause (pe => pe.IsHere); it's the "pe" part
			ParameterExpression pe = GetParameter(genericParameter);

			// creating lambda expression for the parameter above
			Expression objPropertyExpression = ParsePropertyExpression(filteredProperties, pe, filterType, value);

			// create the any expression
			Expression anyExpression = Expression.Call(
				typeof(Enumerable),
				"Any",
				new[] { genericParameter },
				propertyExpression,
				Expression.Lambda(objPropertyExpression, pe)
			);

			if (filteredProperty.FilterPropertyInfo == FilterPropertyInfo.ValueType)
				return anyExpression;

			Expression nullExpression = GetNotNullExpression(propertyExpression);
			return GetOperatorExpression(nullExpression, anyExpression, FilterOperator.And);
		}

		ParameterExpression GetParameter(Type itemType)
		{
			string parameterName = $"entity{_parameterIndex}";
			_parameterIndex++;

			return Expression.Parameter(itemType, parameterName);
		}

		Expression GetNotNullExpression(Expression property, FilterType filterType = FilterType.NotEqual)
		{
			if (filterType != FilterType.NotEqual && filterType != FilterType.Equal)
				throw new ArgumentException("Filter type can only Equal or NotEqual");

			ConstantExpression nullConstant = GetConstantExpression(null, typeof(object));
			return GetComparingExpression(property, nullConstant, filterType);
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

		Expression GetNullableCheck(MemberExpression memberExpression, FilterType comparison, string value)
		{
			Type genericValue = memberExpression.Type.FirstGenericParameter();
			string hasValue = "true";
			FilterOperator filterOperator = FilterOperator.And;

			if (comparison == FilterType.NotEqual)
			{
				hasValue = "false";
				filterOperator = FilterOperator.Or;
			}

			MemberExpression hasValueExpression = Expression.Property(memberExpression, "HasValue");
			ConstantExpression boolValueExpression = GetConstantExpression(hasValue, typeof(bool));
			Expression leftComparingExpression = GetComparingExpression(hasValueExpression, boolValueExpression, FilterType.Equal);

			MemberExpression valuePropertyExpression = Expression.Property(memberExpression, "Value");
			ConstantExpression valueExpression = GetConstantExpression(value, genericValue);
			Expression rightComparingExpression = GetComparingExpression(valuePropertyExpression, valueExpression, comparison);

			return GetOperatorExpression(leftComparingExpression, rightComparingExpression, filterOperator);
		}

		Expression GetLengthExpression(MemberExpression memberExpression)
		{
			Type memberType = memberExpression.Type;
			if (memberType != typeof(string) && !memberType.IsEnumerable())
				throw new ArgumentException("Expected Member type of string or an enumerable");

			return memberType == typeof(string) ? Expression.Property(memberExpression, "Length") : EnumerableCount(memberExpression);
		}

		bool IsLengthComparison(FilterType filterType)
		{
			switch (filterType)
			{
				case FilterType.LenEqual:
				case FilterType.LenGreaterThan:
				case FilterType.LenGreaterThanOrEqualTo:
				case FilterType.LenLessThan:
				case FilterType.LenLessThanOrEqualTo:
				case FilterType.LenNotEqual:
					return true;
			}

			return false;
		}

		bool LenIncludeNullComparisons(FilterType filterType)
		{
			return filterType == FilterType.LenLessThan || filterType == FilterType.LenLessThanOrEqualTo || filterType == FilterType.LenNotEqual;
		}
	}
}