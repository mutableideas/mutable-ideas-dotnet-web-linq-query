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
		IQueryable<TestModel> queryable;

		public FilterServiceTest()
		{
			_filterService = new FilterService<TestModel>();

			queryable = new[] {
				new TestModel {  LastName = "Mead",
					Name = "Paul",
					Page = 1,
					TestItems = new[] { "Test", "Test1", "Test2" },
					SubTest = new SubTestModel {
						Index = 1,
						Name = "Sub Test 1",
						OrgTags = new[] { "OrgTag1", "OrgTag12" }
					},
					TestStrings = new[] { "Org1", "OrgTag12" }
				},
				new TestModel {
					LastName = "Castanza",
					Name = "George",
					Page = 2,
					TestItems = new[] { "Tes12t", "Test13", "Test23" },
					SubTest = new SubTestModel {
						Index = 2,
						Name = "Sub Test 2",
						OrgTags = new[] { "OrgTag1", "OrgTag22" }
					}
				},
				new TestModel {
					LastName = "Collins",
					Name = "Brian",
					Page = 3,
					TestItems = new[] { "Test31", "Test32", "Test33" },
					SubTest = new SubTestModel {
						Index = 3,
						Name = "Sub Test 3",
						OrgTags = new[] { "OrgTag1", "OrgTag32" }
					}
				}
			}.AsQueryable();
		}

		[TestMethod]
		public void By()
		{
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

			_filterService.By("SubTest.index", "1", FilterType.Equal);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(1);
		}

		[TestMethod]
		public void TestContains()
		{
			_filterService.By("testItems", "Test31", FilterType.Contains);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);

			_filterService.By("teststrings", "Org1", FilterType.Contains);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);
		}

		[TestMethod]
		public void MultipleProperties()
		{
			_filterService.By("subtest.name", "Test", FilterType.Contains);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(3);

			_filterService.By("subtest.name", "Sub Test 2", FilterType.Equal);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);

			_filterService.By("subtest.orgtags", "OrgTag1", FilterType.Contains);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(3);
		}
    }
}
