Provides code-level tools for Umbraco

#### MapAll

Helps replacing AutoMapper with static code, without missing properties. Adding the Umbraco.Code NuGet package to a project adds a Roslyn code analyzer and fix.

The following code:

~~~~
// Umbraco.Code.MapAll
public void Map(SomeType source, OtherType target)
{}
~~~~

Raises an error and fails to compile, if not all assignable properties of `target` (from `OtherType` and parents) are assigned a value.

The fix can be invoked (ctrl + ;) to generate the missing properties.

It is possible to ignore some properties, eg:

~~~~
// Umbraco.Code.MapAll -PropertyToIgnore -Another
public void Map(SomeType source, OtherType target)
{}
~~~~

#### Sources and References

Inspired by, and probably stealing code from:

Some pages:
- https://subscription.packtpub.com/book/application_development/9781787286832/1/ch01lvl1sec13/creating-a-method-body-analyzer-to-analyze-whole-method-and-report-issues
- https://www.meziantou.net/2018/12/17/writing-a-roslyn-analyzer
- https://jacobcarpenter.wordpress.com/category/csharp/

Some projects:
- https://github.com/angularsen/roslyn-analyzers
- https://github.com/cezarypiatek/MappingGenerator

