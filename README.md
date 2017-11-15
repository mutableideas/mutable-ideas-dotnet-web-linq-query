# MutableIdeas.Web.Linq.Query.Service

Parses a formatted string and that provides the user a an expression for filtering.

Parses a formatted string that will sort an entity set by its property be acsending or descending order.

## Filtering

### Format for Filtering

**String values**

propertyName comparison '*stringValue*'

**Numeric values**

*propertyName* *comparison* *numericvalue*

**Joined Filters **

propertyName comparison 'value' **operator** propertyName comparison 'value'

### Definitions

**propertyName** - The property name of the entity which to filter.  Property names are not case sensitive.

**comparison** - The type of comparison on the property needed to filter

Valid **comparison** values are:

* __eq__ - Equal
* __lt__ - Less Than
* __lte__ - Less Than or Equal to
* __ne__ - Not Equal
* __gt__ - Greater Than
* __gte__ - Greater Than or Equal to
* __ct__ - For string values, contains
* __ctic__ - For string values, contains ignore case

**value** - The value of the property you want to filter.  String values need to be enclosed by *single quotes* and need to be *URL ESCAPED*.


**operator**

Valid **operator** values

* __and__
* __or__


### Examples for Filtering

* name eq 'Red%20Wings' - *Entity.Name == "Red Wings"*
* name ne 'Avs' - *Entity.Name != "Avs"*
* name ct 'Red' - *Entity.Name.Contains("Red")*
* name ctic 'Red' -
 *Entity.Name.ToLower().Contains("red")*
* price gt 20 and price lt 50 - *Entity.Price > 20 && Entity.Price < 50*
* price gte 20 and price lte 50 - *Entity.Price => 20 && Entity.Price <= 50*
* name eq 'Red%20Wings' or price eq 5 - *Entity.Name == "Red Wings" || Entity.Price == 5*

## Sorting
###Format for Sorting
*propertyName sortOrder*

### Definitions

**propertyName** - The property name of the entity which to filter.  Property names are not case sensitive.

**sortOrder (optional)** - By default this will be in ascending order

Valid values for **sortOrder**

* **asc** - Ascending order
* **desc** - Descending order

### Examples for sorting
* name - default ascending order
* name desc - sort descending order
* name asc - sort ascending order


## C# Services

### QueryExpressionService<T>

#### Methods

**Expression<Func<T, bool>> GetExpression(string filter)**

* *__filter__* - From the filters listed above, this will parse the string and create and expression that can be used for filtering.

**IQueryable<T> Sort(string sort, IQueryable<T> queryable)**

This will sort the entities of the queryable parameter per the sort string.

* *__sort__* - From the sorting string listed above
* *__queryable__* - A queryable entity set


### FilterService<T>

####Methods

**By(string propertyName, string value, FilterType filtertype)**

Creates a filter expression by the propertyName, value, and the type of comparison.

**And()**

Adds the conditional **And** expression for the next expression.

**Or()**

Adds the conditional **Or** expression for the next expression.

**Expression<Func<T, bool>> Build()**
Builds the Expression after all the filter comparisons and operators were added.


#### Examples

var _filterService = new FilterService<SomeClass>();

_filterService.By("Name", "Red", FilterType.Equals);

_filterService.Or();

_filterService.By("Name", "Avs", FilterType.Equals);

Expression<Func<SomeClass, bool>> expression = _filterService.Build();


** This is incomplete but will be updated **
