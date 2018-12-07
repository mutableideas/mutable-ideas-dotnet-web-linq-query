using System.Linq.Expressions;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Moq;
using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;
using MutableIdeas.Web.Linq.Query.Service;
using System.Linq;

namespace MutableIdeas.Web.Linq.Query.Test
{
	[TestClass]
	public class QueryStringExpressionTest
	{
		QueryStringExpressionService<TestModel> _expressionService;

		public QueryStringExpressionTest()
		{
			var mockService = new Mock<IFilterService<TestModel>>();
			mockService.Setup(p => p.And()).Callback(() => { });
			mockService.Setup(p => p.Or()).Callback(() => { });

			mockService.Setup(p => p.By(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<FilterType>()))
			.Callback(() => { });

			mockService.Setup(p => p.Build())
				.Returns(() => {
					return p => p.Name == "Paul";
				});

			_expressionService = new QueryStringExpressionService<TestModel>(mockService.Object);
		}

		[TestMethod]
		public void TestFilterExpression()
		{
			string qStringFilter = "name eq 'Paul' and lastName ne 'castanza'";
			Expression<Func<TestModel, bool>> expression = _expressionService.GetExpression(qStringFilter);
			expression.Should().NotBeNull();

			// Invalid comparison
			qStringFilter = "name fff 'Hootie'";
			_expressionService.Invoking(p => p.GetExpression(qStringFilter))
				.ShouldThrow<FormatException>()
				.WithMessage("The querystring provided does not meet the expected format.");

			// Invalid Property Name
			qStringFilter = "443 eq 455666";
			_expressionService.Invoking(p => p.GetExpression(qStringFilter))
				.ShouldThrow<FormatException>()
				.WithMessage("The querystring provided does not meet the expected format.");

			// Invalid operator
			qStringFilter = "name eq 'Paul' flub chew eq 'ha'";
			_expressionService.Invoking(p => p.GetExpression(qStringFilter))
				.ShouldThrow<FormatException>()
				.WithMessage("flub is an invalid operator");

			qStringFilter = "name eq 'Paul%20was%20here'";
			expression = _expressionService.GetExpression(qStringFilter);
			expression.Should().NotBeNull();

			// Contains ignore case
			qStringFilter = "name ctic 'CO'";
			expression = _expressionService.GetExpression(qStringFilter);
			expression.Should().NotBeNull();

			//contains dashes in value
			qStringFilter = "userId eq '13ee038a-29a1-4640-bea6-e088a5d6e89b'";
			expression = _expressionService.GetExpression(qStringFilter);
			expression.Should().NotBeNull();

			// in value
			qStringFilter = "userId in [1, 2]";
			expression = _expressionService.GetExpression(qStringFilter);
			expression.Should().NotBeNull();

			// in string value
			qStringFilter = "userId in ['1', '2', '3',  'Paul']";
			expression = _expressionService.GetExpression(qStringFilter);
			expression.Should().NotBeNull();
		}

		[TestMethod]
		public void TestSort()
		{
			IQueryable<TestModel> testModels = new TestModel[] {
				new TestModel { LastName = "Castanza", Name = "George", Page = 3 },
				new TestModel { LastName = "Yzerman", Name = "Steve", Page = 2 },
				new TestModel { LastName = "Federov", Name = "Sergei", Page = 1 }
			}.AsQueryable();

			string qstringFilter = "name";
			var models = _expressionService.Sort(qstringFilter, testModels).ToArray();
			models[0].Name.Should().Be("George");
			models[1].Name.Should().Be("Sergei");
			models[2].Name.Should().Be("Steve");

			qstringFilter = "name desc";
			models = _expressionService.Sort(qstringFilter, testModels).ToArray();
			models[0].Name.Should().Be("Steve");
			models[1].Name.Should().Be("Sergei");
			models[2].Name.Should().Be("George");

			qstringFilter = "page desc";
			models = _expressionService.Sort(qstringFilter, testModels).ToArray();
			models[0].Page.ShouldBeEquivalentTo(3);

			qstringFilter = "page";
			models = _expressionService.Sort(qstringFilter, testModels).ToArray();
			models[0].Page.ShouldBeEquivalentTo(1);
		}

        [TestMethod]
        public void TestNaturalSorting()
        {
            IQueryable<TestModel> testModels = new TestModel[] {
                new TestModel { LastName = "C5", Name = "George", Page = 3, SubTest = new SubTestModel { Name = "Paul" }, TestItems = new string[] { } },
                new TestModel { LastName = "C1", Name = "Steve", Page = 2, SubTest = new SubTestModel { Name = "Andrew" }, TestItems = new string[] { } },
                new TestModel { LastName = "C10", Name = "Sergei", Page = 1, SubTest = new SubTestModel { Name = "Zetterberg" }, TestItems = new string[] { } }
            }.AsQueryable();

            var models = _expressionService.Sort("lastname natdesc", testModels);
            models.First().LastName.ShouldAllBeEquivalentTo("C10");

            models = _expressionService.Sort("lastname natasc", testModels);
            models.First().LastName.ShouldBeEquivalentTo("C1");
        }

		[TestMethod]
		public void NumericValues()
		{
			IQueryable<TestModel> testModels = new TestModel[] {
				new TestModel { LastName = "Castanza", Name = "George", Page = 0, Points = 3.2M },
				new TestModel { LastName = "Yzerman", Name = "Steve", Page = 1, Points = 0.5M },
				new TestModel { LastName = "Federov", Name = "Sergei", Page = 2, Points = 3000.888M }
			}.AsQueryable();

			string qstringFilter = "page gt 0.555";
			Expression<Func<TestModel, bool>> expression = _expressionService.GetExpression(qstringFilter);
			expression.Should().NotBeNull();
		}

		[TestMethod]
		public void TestNested()
		{
			IQueryable<TestModel> testModels = new TestModel[] {
				new TestModel { LastName = "Castanza", Name = "George", Page = 0, Points = 3.2M, SubTest = new SubTestModel { Name = "Andrew" } },
				new TestModel { LastName = "Yzerman", Name = "Steve", Page = 1, Points = 0.5M, SubTest = new SubTestModel { Name = "Paul" } },
				new TestModel { LastName = "Federov", Name = "Sergei", Page = 2, Points = 3000.888M, SubTest = new SubTestModel { Name = "Steve" } }
			}.AsQueryable();

			string qStringFilter = "subtest.name eq 'Sub%20Test%201'";
			Expression<Func<TestModel, bool>> expression = _expressionService.GetExpression(qStringFilter);
			expression.Should().NotBeNull();

			qStringFilter = "subtest.name asc";
			_expressionService.Sort(qStringFilter, testModels).ToArray()[0].SubTest.Name.ShouldAllBeEquivalentTo("Andrew");

			qStringFilter = "subtest.name desc";
			_expressionService.Sort(qStringFilter, testModels).ToArray()[0].SubTest.Name.ShouldAllBeEquivalentTo("Steve");

			qStringFilter = "subtest.name";
			_expressionService.Sort(qStringFilter, testModels).ToArray()[0].SubTest.Name.ShouldAllBeEquivalentTo("Andrew");

			Action action = () =>
			{
				qStringFilter = "subtest.name seomthing";
				_expressionService.Sort(qStringFilter, testModels);
			};

			action.ShouldThrow<FormatException>();
		}

		[TestMethod]
		public void TestBooleanQueryString()
		{
			IQueryable<TestModel> testModels = new TestModel[] {
				new TestModel { LastName = "Castanza", Name = "George", Page = 3, Testing = false },
				new TestModel { LastName = "Yzerman", Name = "Steve", Page = 2, Testing = true },
				new TestModel { LastName = "Federov", Name = "Sergei", Page = 1, Testing = false }
			}.AsQueryable();


			string qstringFilter = "testing eq true";
			Expression<Func<TestModel, bool>> expression = _expressionService.GetExpression(qstringFilter);
			expression.Should().NotBeNull();

			qstringFilter = "testing eq false";
			expression = _expressionService.GetExpression(qstringFilter);
			expression.Should().NotBeNull();
		}

		[TestMethod]
		public void TestLengthQueryString()
		{
			string[] expressions = {
				"prop leneq 4", // length == 4
				"prop lengt 4", // length > 4
				"prop lengte 4", // length >= 4
				"prop lenlt 4", // length < 4,
				"prop lenlte 4", // length <= 4
				"prop lenne 4" // length != 4
			};

			foreach (string qString in expressions)
			{
				_expressionService.GetExpression(qString).Should().NotBeNull();
			}
		}

		[TestMethod]
		public void TestISODate()
		{
			string qstringFilter = "applydate gte '2018-12-05T22%3A08%3A13.198Z'";
			Expression<Func<TestModel, bool>> expression = _expressionService.GetExpression(qstringFilter);
			expression.Should().NotBeNull();
		}
	}
}