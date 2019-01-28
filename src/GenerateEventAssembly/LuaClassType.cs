using System;

namespace NLua
{
    /*
     * Structure to store a type and the return types of
     * its methods (the type of the returned value and out/ref
     * parameters).
     */
    struct LuaClassType
    {
        public Type klass;
        public Type[][] returnTypes;
    }
}