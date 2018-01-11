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
					TestStrings = new[] { "Org1", "OrgTag12" },
					TestModels = new[] {
						new SubTestModel {
							Name = "Sub Test 1",
							Model = new AnotherModel { Value = "Howdy!1" },
							Models = new[]
							{
								new AnotherModel { Value = "Hootie Hoo!" }
							}
						}
					},
					Testing = true,
					TestingNullable = 5
				},
				new TestModel {
					LastName = "Castanza",
					Name = "George",
					Page = 2,
					TestItems = new[] { "Tes12t", "Test13", "Test23" },
					Testing = false,
					SubTest = new SubTestModel {
						Index = 2,
						Name = "Sub Test 2",
						OrgTags = new[] { "OrgTag1", "OrgTag22" }
					},
					TestModels = new[] {
						new SubTestModel {
							Name = "Sub Test 3",
							Model = new AnotherModel { Value = "Howdy!2" },
							Models = new[]
							{
								new AnotherModel { Value = "Hootie Hoo!" }
							}
						}
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
					},
					TestModels = new[] {
						new SubTestModel {
							Name = "Sub Test 5",
							Model = new AnotherModel { Value = "Howdy!" },
							Models = new[]
							{
								new AnotherModel { Value = "Hootie Hoo!" }
							}
						}
					}
				},
				new TestModel
				{
					LastName = "Anderson",
					Name = "Cooper",
					ApplyDate = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc)
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
			queryable.Where(expression).ToArray().Count().ShouldBeEquivalentTo(3);

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

		[TestMethod]
		public void TestContains()
		{
			_filterService.By("testItems", "Test31", FilterType.Contains);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);

			_filterService.By("teststrings", "Org1", FilterType.Contains);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);

			_filterService.By("subtest.orgtags", "org", FilterType.ContainsIgnoreCase);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(3);
		}

		[TestMethod]
		public void MultipleProperties()
		{
			_filterService.By("subtest.name", "Test", FilterType.Contains);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(3);

			_filterService.By("subtest.name", "Sub Test 1", FilterType.Equal);
			expression = _filterService.Build();
			queryable.Where(expression).Count().Should().Be(1);

			_filterService.By("subtest.orgtags", "OrgTag1", FilterType.Contains);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(3);
		}

		[TestMethod]
		public void NestedEnumerable()
		{
			Expression<Func<TestModel, bool>> expression = _filterService.For("testmodels.name", "Sub Test 3", FilterType.Equal).Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(1);

			expression = _filterService.For("testmodels.name", "Sub Test 1", FilterType.NotEqual).Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(2);

			_filterService.For("testmodels.model.value", "Howdy!", FilterType.Equal);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(1);

			expression = _filterService.For("testmodels.models.value", "Hootie Hoo!", FilterType.Equal).Build();
			queryable.Where(expression).Count().Should().Be(3);
		}

		[TestMethod]
		public void NestEnumerableContains()
		{
			Expression<Func<TestModel, bool>> expression = _filterService.For("testmodels.name", "Sub", FilterType.Contains).Build();
			queryable.Where(expression).Count().Should().Be(3);
		}

		[TestMethod]
		public void InEnumerable()
		{
			_filterService.By("page", "[1]", FilterType.In);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(1);

			_filterService.By("page", "[1, 2, 3]", FilterType.In);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(3);

			_filterService.By("name", "['George', 'Paul']", FilterType.In);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(2);

			_filterService.By("subtest.name", "['Sub Test 2', 'Sub Test 1']", FilterType.In);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(2);

			_filterService.By("testmodels.name", "['Sub Test 5', 'Sub Test 3']", FilterType.In);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(2);
		}

		[TestMethod]
		public void BooleanTest()
		{
			_filterService.By("testing", "true", FilterType.Equal);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(1);

			_filterService.By("testing", "false", FilterType.Equal);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(3);
		}

		[TestMethod]
		public void NullableTest()
		{
			_filterService.By("testingNullable", "5", FilterType.Equal);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(1);

			// this should find the other 3 items
			_filterService.By("testingNullable", "5", FilterType.NotEqual);
			expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(3);
		}

		[TestMethod]
		public void TestEnum()
		{
			IQueryable<TestModel> testModels = new TestModel[] {
				new TestModel { LastName = "Castanza", Name = "George", Page = 0, Points = 3.2M, SubTest = new SubTestModel { Name = "Andrew" }, TestStatus = Test.TestEnum.Maybe },
				new TestModel { LastName = "Yzerman", Name = "Steve", Page = 1, Points = 0.5M, SubTest = new SubTestModel { Name = "Paul" }, TestStatus = Test.TestEnum.Yes },
				new TestModel { LastName = "Federov", Name = "Sergei", Page = 2, Points = 3000.888M, SubTest = new SubTestModel { Name = "Steve" }, TestStatus = Test.TestEnum.No }
			}.AsQueryable();

			_filterService.By("teststatus", "0", FilterType.Equal);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			testModels.Where(expression).Count().ShouldBeEquivalentTo(1);

			_filterService.By("teststatus", "[0,1]", FilterType.In);
			expression = _filterService.Build();
			testModels.Where(expression).Count().ShouldBeEquivalentTo(2);

			_filterService.By("teststatus", "1", FilterType.NotEqual);
			expression = _filterService.Build();
			testModels.Where(expression).Count().ShouldBeEquivalentTo(2);

			_filterService.By("teststatus", "'Maybe'", FilterType.Equal);
			testModels.Where(_filterService.Build()).Count().ShouldBeEquivalentTo(1);

			_filterService.By("teststatus", "['Maybe', 'No']", FilterType.In);
			testModels.Where(_filterService.Build()).Count().ShouldBeEquivalentTo(2);
		}

		[TestMethod]
		public void DateCompare()
		{
			_filterService.By("applydate", "2017-12-31T00:00:00.000Z", FilterType.GreaterThanOrEqualTo);
			Expression<Func<TestModel, bool>> expression = _filterService.Build();
			queryable.Where(expression).Count().ShouldBeEquivalentTo(1);
		}
	}
}