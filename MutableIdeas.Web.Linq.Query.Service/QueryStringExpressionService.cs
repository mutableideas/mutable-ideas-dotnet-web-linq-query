using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;

namespace MutableIdeas.Web.DynamicQuery.Services
{
    public class QueryStringExpressionService<T>
        where T : class
    {
        readonly Regex _pattern;
		readonly Dictionary<string, PropertyInfo> _propertyNames;
		readonly IFilterService<T> _filterService;

		public QueryStringExpressionService(IFilterService<T> filterService)
        {
            _pattern = new Regex(@"(?<propName>[_A-Za-z]{1}\w*){1}\s+(?<comparison>eq|lt|lte|ne|ge|gte){1}\s+(?<value>\'[\w+|\s+]+\'|[1-9]\d*)(?<operator>\sand|or\s)?");
            _propertyNames = typeof(T).GetRuntimeProperties().ToDictionary(p => p.Name, p => p);
			_filterService = filterService;
        }

        public Expression<Func<T, bool>> GetExpression(string filter, int? pageSize, int? page)
        {
			Parse(filter);
			return _filterService.Build();
        }

        void Parse(string filterQueryString)
        {
            MatchCollection matches = _pattern.Matches(filterQueryString);

            if (matches.Count == 0)
                throw new FormatException("The querystring provided does not meet the expected format.");

            foreach(Match match in matches)
            {
                string propertyName = match.Groups["propName"].Value;
				string value = match.Groups["value"].Value;
				Group op = match.Groups["operator"];
				FilterType filterType = GetFilterType(match.Groups["comparison"].Value);

				_filterService.By(propertyName, value, filterType);

				if (op != null)
					GetOperator(op.Value);
            }
        }

        void GetOperator(string op)
        {
            switch(op)
            {
                case "and":
					_filterService.And();
					break;
                case "or":
					_filterService.Or();
					break;
            }

            throw new FormatException($"{op} is an invalid operator");
        }

        FilterType GetFilterType(string comparison)
        {
            switch(comparison)
            {
                case "lt":
                    return FilterType.LessThan;
                case "lte":
                    return FilterType.LessThanOrEqualTo;
                case "ne":
                    return FilterType.NotEqual;
                case "ge":
                    return FilterType.GreaterThan;
                case "eq":
                    return FilterType.Equal;
            }

            throw new FormatException($"{comparison} is not a supported comparison type.");
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
            }

            throw new ArgumentException($"The filter type {filterType} does not exist.");
        }
    }
}
