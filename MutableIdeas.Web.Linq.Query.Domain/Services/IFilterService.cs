using System;
using System.Linq.Expressions;
using MutableIdeas.Web.Linq.Query.Domain.Enums;
	
namespace MutableIdeas.Web.Linq.Query.Domain.Services
{
    public interface IFilterService<T> where T : class
    {
		Expression<Func<T, bool>> Build();
		void By(string propertyName, string value, FilterType filterType);
		void And();
		void Or();
    }
}
