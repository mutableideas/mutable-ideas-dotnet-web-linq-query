using System;
using System.Collections.Generic;
using System.Text;

namespace MutableIdeas.Web.Linq.Query.Test
{
	public class TestModel
	{
		public string Name { get; set; }
		public string LastName { get; set; }
		public int Page { get; set; }
		public string[] TestItems { get; set; }
		public SubTestModel SubTest { get; set; }
		public IEnumerable<string> TestStrings { get; set; }
		public IEnumerable<SubTestModel> TestModels { get; set; }
		
	}

	public class SubTestModel
	{
		public string Name { get; set; }
		public int Index { get; set; }
		public string[] OrgTags { get; set; }
		public AnotherModel Model { get; set; }
		public IEnumerable<AnotherModel> Models { get; set; }
	}

	public class AnotherModel
	{
		public string Value { get; set; }
	}
}
