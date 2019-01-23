using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MutableIdeas.Web.Linq.Query.Service;

namespace MutableIdeas.Web.Linq.Query.Test
{
	[TestClass]
	public class ExpressionExtensionTest
	{
		[TestMethod]
		public void ConvertGuid()
		{
			string value = "99f0931e-f82e-431c-bde2-8f276f3f32c8";
			Guid guidValue = ExpressionExtension.ConvertValue<Guid>(value);

			Assert.IsNotNull(guidValue);
		}
	}
}
