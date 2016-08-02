using System;
using System.Text.RegularExpressions;

namespace NAppUpdate.Framework.Utils
{
    public class CarelessAssemblyDeserializationBinder : System.Runtime.Serialization.SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            var typeToDeserialize = Type.GetType(Regex.Replace(typeName, @"\[\[(.*?),(.*?)\]\]", "[[$1]]"));
            return typeToDeserialize;
        }
    }
}