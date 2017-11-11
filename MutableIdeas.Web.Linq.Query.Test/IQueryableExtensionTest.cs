using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Service.Extensions;
using FluentAssertions;

namespace MutableIdeas.Web.Linq.Query.Test
{
	[TestClass]
    public class IQueryableExtensionTest
    {
		[TestMethod]
		public void TestSort()
		{
			IQueryable<TestModel> testModels = new TestModel[] {
				new TestModel { LastName = "Castanza", Name = "George", Page = 3 },
				new TestModel { LastName = "Yzerman", Name = "Steve", Page = 2 },
				new TestModel { LastName = "Federov", Name = "Sergei", Page = 1 }
			}.AsQueryable();

			TestModel[] ordered = testModels.OrderBy("lastname", SortDirection.Ascending).ToArray();
			ordered[1].LastName.Should().Be("Federov");

			ordered = testModels.OrderBy("lastName", SortDirection.Ascending).ToArray();
			ordered[0].LastName.Should().Be("Castanza");

			ordered = testModels.OrderBy("lastName", SortDirection.Descending).ToArray();
			ordered[0].LastName.Should().Be("Yzerman");

			ordered = testModels.OrderBy("page", SortDirection.Ascending).ToArray();
			ordered[0].Page.ShouldBeEquivalentTo(1);

			ordered = testModels.OrderBy("Page", SortDirection.Descending).ToArray();
			ordered[0].Page.ShouldBeEquivalentTo(3);
		}
    }
}
