
using System;
using NUnit.Framework;

namespace NLuaTest
{
	[TestFixture]
	public class Core
	{
		[Test]
		public void Pass ()
		{
			Assert.True (true);
		}

		[Test]
		public void Fail ()
		{
			//Assert.False (true);
		}

		[Test]
		[Ignore ("another time")]
		public void Ignore ()
		{
			//Assert.True (false);
		}
	}
}
