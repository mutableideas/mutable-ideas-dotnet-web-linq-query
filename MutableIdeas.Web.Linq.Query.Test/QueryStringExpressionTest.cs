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
	}
}