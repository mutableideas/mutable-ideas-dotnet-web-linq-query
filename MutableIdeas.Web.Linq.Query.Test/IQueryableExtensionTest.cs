using System;
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
				new TestModel { LastName = "Castanza", Name = "George", Page = 3, SubTest = new SubTestModel { Name = "Paul" }, TestItems = new string[] { } },
				new TestModel { LastName = "Yzerman", Name = "Steve", Page = 2, SubTest = new SubTestModel { Name = "Andrew" }, TestItems = new string[] { } },
				new TestModel { LastName = "Federov", Name = "Sergei", Page = 1, SubTest = new SubTestModel { Name = "Zetterberg" }, TestItems = new string[] { } }
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

			ordered = testModels.OrderBy("subtest.name", SortDirection.Ascending).ToArray();
			ordered[0].Page.ShouldBeEquivalentTo(2);

			Action act = () => {
				testModels.OrderBy("testitems", SortDirection.Ascending).ToArray();
			};

			act.ShouldThrow<InvalidOperationException>()
				.WithMessage("Cannot sort enumerable properties.");
		}

        [TestMethod]
        public void CheckNaturalSort()
        {
            IQueryable<TestModel> testModels = new TestModel[] {
                new TestModel { LastName = "C5", Name = "George", Page = 3, SubTest = new SubTestModel { Name = "Paul" }, TestItems = new string[] { } },
                new TestModel { LastName = "C1", Name = "Steve", Page = 2, SubTest = new SubTestModel { Name = "Andrew" }, TestItems = new string[] { } },
                new TestModel { LastName = "C10", Name = "Sergei", Page = 1, SubTest = new SubTestModel { Name = "Zetterberg" }, TestItems = new string[] { } }
            }.AsQueryable();

            var orderedModels = testModels.OrderBy("lastname", SortDirection.NaturalAsc);
            orderedModels.First().LastName.ShouldBeEquivalentTo("C1");

            orderedModels = testModels.OrderBy("lastname", SortDirection.NatrualDesc);
            orderedModels.First().LastName.ShouldAllBeEquivalentTo("C10");
        }
    }
}
