using System;
using System.Threading;

namespace NLuaTest.TestTypes
{
    /// <summary>
    /// Use to test threading
    /// </summary>
    class DoWorkClass
    {

        public void DoWork()
        {

            //simulate work by sleeping
            Thread.Sleep(new Random().Next(0, 1000));
        }
    }
}