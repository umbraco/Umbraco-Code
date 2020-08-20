Provides code-level tools for Umbraco

#### MapAll

Helps replacing AutoMapper with static code, without missing properties. Adding the Umbraco.Code NuGet package to a project adds a Roslyn code analyzer and fix.

The following code:

~~~~c#
// Umbraco.Code.MapAll
public void Map(SomeType source, OtherType target)
{}
~~~~

Raises an error and fails to compile, if not all assignable properties of `target` (from `OtherType` and parents) are assigned a value.
The fix can be invoked (ctrl + ;) to generate the missing properties.
When the fix can find a corresponding property in `source`, it generates the assignment (corresponding by name, whatever the type).
When no corresponding property can be found, the fix generates an assignment to `default` with a comment.

~~~~c#
// Umbraco.Code.MapAll
public void Map(SomeType source, OtherType target)
{
	target.Value1 = source.Value1;
	target.Value2 = default; // fixme
}
~~~~

It is possible to ignore some properties, eg:

~~~~c#
// Umbraco.Code.MapAll -PropertyToIgnore -Another
public void Map(SomeType source, OtherType target)
{}
~~~~

#### Volatile

Allows for methods and classes to be marked as volatile with an attribute. Methods marked with volatile, or from a class marked as volatile,
will throw an error and fail to compile. The error can be suppressed to a warning within an assembly by using the UmbracoSuppressVolatileAttribute.

This is intented to be used for objects/methods that were previously marked as internal, typically because the may break in the future, 
but are still useful in some aspect, typically testing where it doesn't matter if a method breaks. 

Marking a method as volatile looks like this: 
~~~c#
    public class DemoClass
    {
        [UmbracoVolatile]
        public void VolatileMethod()
        {
            // Do volatile things here.
        }

    }
~~~

Whenever ```DemoClass.VolatileMethod``` is invoked there'll be raised an UmbracoCodeVolatile error, 
to suppress it to a warning use the assembly level UmbracoSuppressVolatileAttribute: 
~~~c#
[assembly: UmbracoSuppressVolatile]
namespace VolatileDemo
{
    public class DemoClass
    {
	[UmbracoVolatile]
        public void VolatileMethod()
        {
           // Volatile things here...
        }

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            testClass.VolatileMethod();
        }
    }
}
~~~

Now there'll only be raised a warning even though ```DemoClass.VolatileMethod``` is marked as volatile.

The same thing goes for objects, if you do something like this: 
~~~c#
    [UmbracoVolatile]
    public class DemoClass
    {
        public void VolatileMethod()
        {
            // Do volatile things here.
        }

    }
~~~

All of DemoClasses methods will be marked as volatile. 

##### The Attributes
It's worthwile noting that the attributes are compared by name and not by type. 

This means that it's not needed to use the attributes that are included in this project (the namespace of analyzers is not accecible), 
any attribute named UmbracoVolatileAttribute or UmbracoSuppressVolatileAttribute will do the trick. 


#### Sources and References

Inspired by, and probably stealing code from:

Some pages:
- https://subscription.packtpub.com/book/application_development/9781787286832/1/ch01lvl1sec13/creating-a-method-body-analyzer-to-analyze-whole-method-and-report-issues
- https://www.meziantou.net/2018/12/17/writing-a-roslyn-analyzer
- https://jacobcarpenter.wordpress.com/category/csharp/

Some projects:
- https://github.com/angularsen/roslyn-analyzers
- https://github.com/cezarypiatek/MappingGenerator

