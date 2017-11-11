using System.Linq;
using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MutableIdeas.Web.Linq.Query.Service;
using MutableIdeas.Web.Linq.Query.Domain.Enums;

namespace MutableIdeas.Web.Linq.Query.Test
{
	[TestClass]
    public class FilterServiceTest
    {
		FilterService<TestModel> _filterService;

		public FilterServiceTest()
		{
			_filterService = new FilterService<TestModel>();
		}

		[TestMethod]
		public void By()
		{
			IQueryable<TestModel> queryable = new[] {
				new TestModel {  LastName = "Mead", Name = "Paul", Page = 1 },
				new TestModel { LastName = "Castanza", Name = "George", Page = 2 },
				new TestModel { LastName = "Collins", Name = "Brian", Page = 3 }
			}.AsQueryable();

			_filterService.By("name", "Paul", FilterType.Equal);
			_filterService.Or();
			_filterService.By("name", "George", FilterType.Equal);

			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			IEnumerable<TestModel> models = queryable.Where(expression).ToArray();
			models.Count().ShouldBeEquivalentTo(2);

			_filterService.By("name", "Paul", FilterType.NotEqual);
			expression = _filterService.Build();
			queryable.Where(expression).ToArray().Count().ShouldBeEquivalentTo(2);

			_filterService.By("page", "1", FilterType.GreaterThan);
			expression = _filterService.Build();
			queryable.Where(expression).ToArray().Count().ShouldBeEquivalentTo(2);

			_filterService.By("page", "1", FilterType.GreaterThanOrEqualTo);
			expression = _filterService.Build();
			queryable.Where(expression).ToArray().Count().ShouldBeEquivalentTo(3);

			_filterService.By("page", "1", FilterType.Equal);
			_filterService.Or();
			_filterService.By("name", "George", FilterType.Equal);
			_filterService.Or();
			_filterService.By("lastName", "Collins", FilterType.Equal);
			expression = _filterService.Build();
			queryable.Where(expression).ToArray().Count().ShouldBeEquivalentTo(3);

			_filterService.By("name", "a", FilterType.Contains);
			expression = _filterService.Build();
			queryable.Where(expression).ToArray().Count().ShouldBeEquivalentTo(2);

			_filterService.By("page", "1", FilterType.GreaterThan);
			_filterService.And();
			_filterService.By("page", "3", FilterType.LessThan);
			expression = _filterService.Build();

			TestModel between = queryable.Where(expression).First();
			between.Page.Should().Be(2);

			_filterService.By("lastname", "co", FilterType.ContainsIgnoreCase);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);

			_filterService.By("lastname", "Co", FilterType.Contains);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);

			_filterService.By("lastname", "Co", FilterType.ContainsIgnoreCase);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);
		}
    }
}
