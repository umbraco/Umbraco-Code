using System;
using System.Collections.Generic;
using System.Text;

namespace Umbraco.Code.Volatile
{
    /// <summary>
    /// Attribute used to supresss a 
    /// VolatileError to a warning
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class UmbracoSuppressVolatileAttribute : Attribute
    {
    }
}
