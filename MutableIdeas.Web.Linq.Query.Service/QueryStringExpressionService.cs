using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;
using MutableIdeas.Web.Linq.Query.Service.Extensions;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public class QueryStringExpressionService<T> : IQueryStringExpressionService<T>
        where T : class
    {
        readonly Regex _pattern;
		readonly Regex _sortPattern;
		readonly Dictionary<string, PropertyInfo> _propertyNames;
		readonly IFilterService<T> _filterService;

		public QueryStringExpressionService(IFilterService<T> filterService)
        {
            _pattern = new Regex(@"(?<entity>([_A-Za-z]{1}\w*){1}(\.[_A-Za-z]{1}\w*)*){1}\s+(?<comparison>eq|lt|lte|ne|gt|gte|ct|ctic|in|leneq|lengt|lengte|lenlt|lenlte|lenne){1}\s+(?<value>\[(('[\w+\s+.:-]+\'|\d*\.?\d*)\s*,?)*\]|true|false|\'[\w+\s+.:%-]+\'|\d*\.?\d*)(?<operator>\s\w+\s)?", RegexOptions.Compiled);
			_sortPattern = new Regex(@"^(?<propName>([_A-Za-z]{1}\w*){1}(\.[_A-Za-z]{1}\w*)*){1}(\s+(?<order>desc|asc|natdesc|natasc))?$", RegexOptions.Compiled);

			_propertyNames = typeof(T).GetRuntimeProperties().ToDictionary(p => p.Name, p => p);
			_filterService = filterService;
        }

        public Expression<Func<T, bool>> GetExpression(string filter)
        {
			Parse(filter);
			return _filterService.Build();
        }

		public IQueryable<T> Sort(string sort, IQueryable<T> queryable)
		{
			Match match = _sortPattern.Match(sort);

			if (!match.Success)
				ThrowFormatException();

			Group orderDirection = match.Groups["order"];
			string propertyName = match.Groups["propName"].Value;
			SortDirection sortDirection = SortDirection.Ascending;

            if (orderDirection != null)
            {
                switch (orderDirection.Value)
                {
                    case "desc":
                        sortDirection = SortDirection.Descending;
                        break;
                    case "asc":
                        sortDirection = SortDirection.Ascending;
                        break;
                    case "natdesc":
                        sortDirection = SortDirection.NatrualDesc;
                        break;
                    case "natasc":
                        sortDirection = SortDirection.NaturalAsc;
                        break;
                }
            }

			return queryable.OrderBy(propertyName, sortDirection);
		}
	
        void Parse(string filterQueryString)
        {
            MatchCollection matches = _pattern.Matches(filterQueryString);

			if (matches.Count == 0)
				ThrowFormatException();

			foreach (Match match in matches)
            {
                string propertyName = match.Groups["entity"].Value;
				string value = match.Groups["value"].Value.UnescapeUrlValue();
				Group op = match.Groups["operator"];
				FilterType filterType = GetFilterType(match.Groups["comparison"].Value);

				if (string.IsNullOrEmpty(value))
					ThrowFormatException();

				_filterService.By(propertyName, value, filterType);

				if (op != null && !string.IsNullOrEmpty(op.Value))
					GetOperator(op.Value);
            }
        }
	
        void GetOperator(string op)
        {
			string filterOperator = op.Trim();

            switch(filterOperator)
            {
                case "and":
					_filterService.And();
					return;
                case "or":
					_filterService.Or();
					return;
            }

            throw new FormatException($"{filterOperator} is an invalid operator");
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
                case "gt":
                    return FilterType.GreaterThan;
                case "eq":
                    return FilterType.Equal;
				case "gte":
					return FilterType.GreaterThanOrEqualTo;
				case "ct":
					return FilterType.Contains;
				case "ctic":
					return FilterType.ContainsIgnoreCase;
				case "in":
					return FilterType.In;
				case "leneq":
					return FilterType.LenEqual;
				case "lengt":
					return FilterType.LenGreaterThan;
				case "lengte":
					return FilterType.LenGreaterThanOrEqualTo;
				case "lenlt":
					return FilterType.LenLessThan;
				case "lenlte":
					return FilterType.LenLessThanOrEqualTo;
				case "lenne":
					return FilterType.LenNotEqual;
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

		void ThrowFormatException()
		{
			throw new FormatException("The querystring provided does not meet the expected format.");
		}
    }
}
