using System;
using System.Collections.Generic;

namespace MutableIdeas.Web.Linq.Query.Test
{

	public enum TestEnum
	{
		Maybe = 0,
		Yes = 1,
		No = 2
	}

	public class TestModel
	{
        public TestModel()
        {
            TestItems = new string[0] { };
            TestStrings = new string[0] { };
            TestModels = new List<SubTestModel>();
            TestModelList = new List<SubTestModel>();
            TestModelCol = new List<SubTestModel>();
        }

		public string Name { get; set; }
		public string LastName { get; set; }
		public int Page { get; set; }
		public string[] TestItems { get; set; }
		public SubTestModel SubTest { get; set; }
		public IEnumerable<string> TestStrings { get; set; }
		public IEnumerable<SubTestModel> TestModels { get; set; }
        public ICollection<SubTestModel> TestModelCol { get; set; }
        public IList<SubTestModel> TestModelList { get; set; }
		public decimal Points { get; set; }

		public bool Testing { get; set; }
		public int? TestingNullable { get; set; }
		public TestEnum TestStatus { get; set; }

		public DateTime ApplyDate { get; set; }
	}

	public class SubTestModel
	{
        public SubTestModel()
        {
            OrgTags = new string[0] { };
            Models = new List<AnotherModel>();
        }

		public string Name { get; set; }
		public int Index { get; set; }
		public string[] OrgTags { get; set; }
		public AnotherModel Model { get; set; }
		public IEnumerable<AnotherModel> Models { get; set; }
	}

	public class AnotherModel
	{
        public AnotherModel()
        {
            Values = new string[0] { };
        }

		public string Value { get; set; }
        public IEnumerable<string> Values { get; set; }
	}
}
