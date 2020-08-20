using System;
using System.Collections.Generic;
using System.Text;

namespace Umbraco.Code.Volatile
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class UmbracoSuppressVolatileAttribute : Attribute
    {
    }
}
