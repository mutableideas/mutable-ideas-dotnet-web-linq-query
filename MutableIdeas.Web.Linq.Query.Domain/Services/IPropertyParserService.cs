using MutableIdeas.Web.Linq.Query.Domain.Models;
using System.Collections.Generic;

namespace MutableIdeas.Web.Linq.Query.Domain.Services
{
    public interface IPropertyParserService
    {
		IEnumerable<FilteredProperty> GetFilterProperties<T>(string properties)
			where T : class;
    }
}
