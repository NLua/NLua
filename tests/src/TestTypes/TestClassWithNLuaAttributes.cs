using System;
using NLua;

namespace NLuaTest.TestTypes
{
    public class TestClassWithNLuaAttributes
    {
        public int PropWithoutAttribute { get; set; } = 0;

        [LuaMember(Name = "prop_with_attribute")]
        public int PropWithAttribute { get; set; } = 1;

        public int fieldWithoutAttribute = 2;

        [LuaMember(Name = "field_with_attribute")]
        public int fieldWithAttribute = 3;

        public int MethodWithoutAttribute()
        {
            return 4;
        }

        [LuaMember(Name = "method_with_attribute")]
        public int MethodWithAttribute()
        {
            return 5;
        }

        [LuaHide]
        public int HiddenProperty { get; set; } = 6;

        [LuaHide]
        public int hiddenField = 7;

        [LuaHide]
        public int HiddenMethod()
        {
            return 8;
        }
    }
}