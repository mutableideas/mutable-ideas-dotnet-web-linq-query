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
		
	}

	public class SubTestModel
	{
		public string Name { get; set; }
		public int Index { get; set; }
		public string[] OrgTags { get; set; } 
	}
}
