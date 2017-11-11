using System.Linq.Expressions;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Moq;
using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;
using MutableIdeas.Web.Linq.Query.Services;

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
		public void TestExpression()
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
    }
}
