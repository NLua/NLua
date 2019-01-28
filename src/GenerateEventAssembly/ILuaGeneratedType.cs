namespace NLua
{
    /*
     * Common interface for types generated from tables. The method
     * returns the table that overrides some or all of the type's methods.
     */
    public interface ILuaGeneratedType
    {
        LuaTable LuaInterfaceGetLuaTable();
    }
}