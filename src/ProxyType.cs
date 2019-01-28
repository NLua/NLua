using System;
using System.Reflection;

namespace NLua
{
    /// <summary>
    /// Summary description for ProxyType.
    /// </summary>
    public class ProxyType
    {
        private Type proxy;

        public ProxyType(Type proxy)
        {
            this.proxy = proxy;
        }

        /// <summary>
        /// Provide human readable short hand for this proxy object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "ProxyType(" + UnderlyingSystemType + ")";
        }

        public Type UnderlyingSystemType {
            get { return proxy; }
        }

        public override bool Equals(object obj)
        {
            if (obj is Type)
                return proxy.Equals((Type)obj);
            if (obj is ProxyType)
                return proxy.Equals(((ProxyType)obj).UnderlyingSystemType);
            return proxy.Equals(obj);
        }

        public override int GetHashCode()
        {
            return proxy.GetHashCode();
        }

        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            return proxy.GetMember(name, bindingAttr);
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Type[] signature)
        {
#if NETFX_CORE
            return proxy.GetMethod (name, bindingAttr, signature);
#else
            return proxy.GetMethod(name, bindingAttr, null, signature, null);
#endif
        }
    }
}