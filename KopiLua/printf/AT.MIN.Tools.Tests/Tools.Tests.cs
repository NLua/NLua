#region Usinges
using System;
using NUnit.Framework;
#endregion

namespace AT.MIN
{
	[TestFixture]
	public class ToolTests
	{
		private string sepDecimal = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
		private string sep1000 = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;

		#region Setup Tests
		[TestFixtureSetUp]
		public void SetUp()
		{
		}
		#endregion

		#region Tests
		#region Special Formats
		[Category( "Special" )]
		[Test( Description = "Special formats %% / %n" )]
		public void SpecialFormats()
		{
			Console.WriteLine( "Test special formats %% / %n" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%%]", "[%]" ) );
			Assert.IsTrue( RunTest( "[%n]", "[1]" ) );
			Assert.IsTrue( RunTest( "[%%n shows the number of processed chars so far (%010n)]",
				"[%n shows the number of processed chars so far (0000000048)]" ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region PositiveInteger
		[Category( "Integer" )]
		[Test( Description = "Test positive signed integer format %d / %i" )]
		public void PositiveInteger()
		{
			Console.WriteLine( "Test positive signed integer format %d / %i" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%d]", "[42]", 42 ) );
			Assert.IsTrue( RunTest( "[%10d]", "[        42]", 42 ) );
			Assert.IsTrue( RunTest( "[%-10d]", "[42        ]", 42 ) );
			Assert.IsTrue( RunTest( "[%010d]", "[0000000042]", 42 ) );
			Assert.IsTrue( RunTest( "[%-010d]", "[42        ]", 42 ) );
			Assert.IsTrue( RunTest( "[%+d]", "[+42]", 42 ) );
			Assert.IsTrue( RunTest( "[%+10d]", "[       +42]", 42 ) );
			Assert.IsTrue( RunTest( "[%-+10d]", "[+42       ]", 42 ) );
			Assert.IsTrue( RunTest( "[%+010d]", "[+000000042]", 42 ) );
			Assert.IsTrue( RunTest( "[%-+010d]", "[+42       ]", 42 ) );

			Assert.IsTrue( RunTest( "[%d]", "[65537]", 65537 ) );
			Assert.IsTrue( RunTest( "[%'d]", String.Format( "[65{0}537]", sep1000 ), 65537 ) );
			Assert.IsTrue( RunTest( "[%'d]", String.Format( "[10{0}065{0}537]", sep1000 ), 10065537 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region NegativeInteger
		[Category( "Integer" )]
		[Test( Description = "Test negative signed integer format %d / %i" )]
		public void NegativeInteger()
		{
			Console.WriteLine( "Test negative signed integer format %d / %i" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%d]", "[-42]", -42 ) );
			Assert.IsTrue( RunTest( "[%10d]", "[       -42]", -42 ) );
			Assert.IsTrue( RunTest( "[%-10d]", "[-42       ]", -42 ) );
			Assert.IsTrue( RunTest( "[%010d]", "[-000000042]", -42 ) );
			Assert.IsTrue( RunTest( "[%-010d]", "[-42       ]", -42 ) );
			Assert.IsTrue( RunTest( "[%+d]", "[-42]", -42 ) );
			Assert.IsTrue( RunTest( "[%+10d]", "[       -42]", -42 ) );
			Assert.IsTrue( RunTest( "[%-+10d]", "[-42       ]", -42 ) );
			Assert.IsTrue( RunTest( "[%+010d]", "[-000000042]", -42 ) );
			Assert.IsTrue( RunTest( "[%-+010d]", "[-42       ]", -42 ) );

			Assert.IsTrue( RunTest( "[%d]", "[-65537]", -65537 ) );
			Assert.IsTrue( RunTest( "[%'d]", String.Format( "[-65{0}537]", sep1000 ), -65537 ) );
			Assert.IsTrue( RunTest( "[%'d]", String.Format( "[-10{0}065{0}537]", sep1000 ), -10065537 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region UnsignedInteger
		[Category( "Integer" )]
		[Test( Description = "Test unsigned integer format %u" )]
		public void UnsignedInteger()
		{
			Console.WriteLine( "Test unsigned integer format %u" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%u]", "[42]", 42 ) );
			Assert.IsTrue( RunTest( "[%10u]", "[        42]", 42 ) );
			Assert.IsTrue( RunTest( "[%-10u]", "[42        ]", 42 ) );
			Assert.IsTrue( RunTest( "[%010u]", "[0000000042]", 42 ) );
			Assert.IsTrue( RunTest( "[%-010u]", "[42        ]", 42 ) );

			Assert.IsTrue( RunTest( "[%u]", "[4294967254]", -42 ) );
			Assert.IsTrue( RunTest( "[%20u]", "[          4294967254]", -42 ) );
			Assert.IsTrue( RunTest( "[%-20u]", "[4294967254          ]", -42 ) );
			Assert.IsTrue( RunTest( "[%020u]", "[00000000004294967254]", -42 ) );
			Assert.IsTrue( RunTest( "[%-020u]", "[4294967254          ]", -42 ) );

			Assert.IsTrue( RunTest( "[%u]", "[65537]", 65537 ) );
			Assert.IsTrue( RunTest( "[%'u]", String.Format( "[65{0}537]", sep1000 ), 65537 ) );
			Assert.IsTrue( RunTest( "[%'u]", String.Format( "[10{0}065{0}537]", sep1000 ), 10065537 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region Float
		[Category( "Float" )]
		[Test( Description = "Test float format %f" )]
		public void Floats()
		{
			Console.WriteLine( "Test float format %f" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%f]", String.Format( "[42{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%f]", String.Format("[42{0}500000]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%10f]", String.Format("[ 42{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%10f]", String.Format("[ 42{0}500000]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-10f]", String.Format("[42{0}000000 ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-10f]", String.Format("[42{0}500000 ]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%010f]", String.Format("[042{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%010f]", String.Format("[042{0}500000]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-010f]", String.Format("[42{0}000000 ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-010f]", String.Format("[42{0}500000 ]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%+f]", String.Format("[+42{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+f]", String.Format("[+42{0}500000]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+10f]", String.Format("[+42{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+10f]", String.Format("[+42{0}500000]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-10f]", String.Format("[+42{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-10f]", String.Format("[+42{0}500000]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+010f]", String.Format("[+42{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+010f]", String.Format("[+42{0}500000]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-010f]", String.Format("[+42{0}000000]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-010f]", String.Format("[+42{0}500000]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%10f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%10f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-10f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-10f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%010f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%010f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-010f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-010f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );

			Assert.IsTrue( RunTest( "[%+f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+10f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+10f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+-10f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-10f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+010f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+010f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+-010f]", String.Format("[-42{0}000000]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-010f]", String.Format("[-42{0}500000]", sepDecimal ), -42.5 ) );

			// -----

			Assert.IsTrue( RunTest( "[%.2f]", String.Format("[42{0}00]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%.2f]", String.Format("[42{0}50]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%10.2f]", String.Format("[     42{0}00]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%10.2f]", String.Format("[     42{0}50]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-10.2f]", String.Format("[42{0}00     ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-10.2f]", String.Format("[42{0}50     ]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%010.2f]", String.Format("[0000042{0}00]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%010.2f]", String.Format("[0000042{0}50]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-010.2f]", String.Format("[42{0}00     ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-010.2f]", String.Format("[42{0}50     ]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%+.2f]", String.Format("[+42{0}00]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+.2f]", String.Format("[+42{0}50]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+10.2f]", String.Format("[    +42{0}00]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+10.2f]", String.Format("[    +42{0}50]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-10.2f]", String.Format("[+42{0}00    ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-10.2f]", String.Format("[+42{0}50    ]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+010.2f]", String.Format("[+000042{0}00]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+010.2f]", String.Format("[+000042{0}50]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-010.2f]", String.Format("[+42{0}00    ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-010.2f]", String.Format("[+42{0}50    ]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%.2f]", String.Format("[-42{0}00]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%.2f]", String.Format("[-42{0}50]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%10.2f]", String.Format("[    -42{0}00]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%10.2f]", String.Format("[    -42{0}50]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-10.2f]", String.Format("[-42{0}00    ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-10.2f]", String.Format("[-42{0}50    ]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%010.2f]", String.Format("[-000042{0}00]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%010.2f]", String.Format("[-000042{0}50]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-010.2f]", String.Format("[-42{0}00    ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-010.2f]", String.Format("[-42{0}50    ]", sepDecimal ), -42.5 ) );

			Assert.IsTrue( RunTest( "[%+.2f]", String.Format("[-42{0}00]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+.2f]", String.Format("[-42{0}50]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+10.2f]", String.Format("[    -42{0}00]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+10.2f]", String.Format("[    -42{0}50]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+-10.2f]", String.Format("[-42{0}00    ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-10.2f]", String.Format("[-42{0}50    ]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+010.2f]", String.Format("[-000042{0}00]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+010.2f]", String.Format("[-000042{0}50]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+-010.2f]", String.Format("[-42{0}00    ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-010.2f]", String.Format( "[-42{0}50    ]", sepDecimal ), -42.5 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region Exponent
		[Category( "Exponent" )]
		[Test( Description = "Test exponent format %f" )]
		public void Exponents()
		{
			Console.WriteLine( "Test exponent format %f" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%e]", String.Format( "[4{0}200000e+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%e]", String.Format("[4{0}250000e+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%20e]", String.Format("[       4{0}200000e+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%20e]", String.Format("[       4{0}250000e+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-20e]", String.Format("[4{0}200000e+001       ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-20e]", String.Format("[4{0}250000e+001       ]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%020e]", String.Format("[00000004{0}200000e+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%020e]", String.Format("[00000004{0}250000e+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-020e]", String.Format("[4{0}200000e+001       ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-020e]", String.Format("[4{0}250000e+001       ]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%+E]", String.Format("[+4{0}200000E+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+E]", String.Format("[+4{0}250000E+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+20E]", String.Format("[      +4{0}200000E+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+20E]", String.Format("[      +4{0}250000E+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-20E]", String.Format("[+4{0}200000E+001      ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-20E]", String.Format("[+4{0}250000E+001      ]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+020E]", String.Format("[+0000004{0}200000E+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+020E]", String.Format("[+0000004{0}250000E+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-020E]", String.Format("[+4{0}200000E+001      ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-020E]", String.Format("[+4{0}250000E+001      ]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%e]", String.Format("[-4{0}200000e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%e]", String.Format("[-4{0}250000e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%20e]", String.Format("[      -4{0}200000e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%20e]", String.Format("[      -4{0}250000e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-20e]", String.Format("[-4{0}200000e+001      ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-20e]", String.Format("[-4{0}250000e+001      ]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%020e]", String.Format("[-0000004{0}200000e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%020e]", String.Format("[-0000004{0}250000e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-020e]", String.Format("[-4{0}200000e+001      ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-020e]", String.Format("[-4{0}250000e+001      ]", sepDecimal ), -42.5 ) );

			Assert.IsTrue( RunTest( "[%+e]", String.Format("[-4{0}200000e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+e]", String.Format("[-4{0}250000e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+20e]", String.Format("[      -4{0}200000e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+20e]", String.Format("[      -4{0}250000e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+-20e]", String.Format("[-4{0}200000e+001      ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-20e]", String.Format("[-4{0}250000e+001      ]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+020e]", String.Format("[-0000004{0}200000e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+020e]", String.Format("[-0000004{0}250000e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+-020e]", String.Format("[-4{0}200000e+001      ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-020e]", String.Format("[-4{0}250000e+001      ]", sepDecimal ), -42.5 ) );

			// -----

			Assert.IsTrue( RunTest( "[%.2e]", String.Format("[4{0}20e+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%.2e]", String.Format("[4{0}25e+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%20.2e]", String.Format("[           4{0}20e+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%20.2e]", String.Format("[           4{0}25e+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-20.2e]", String.Format("[4{0}20e+001           ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-20.2e]", String.Format("[4{0}25e+001           ]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%020.2e]", String.Format("[000000000004{0}20e+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%020.2e]", String.Format("[000000000004{0}25e+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%-020.2e]", String.Format("[4{0}20e+001           ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%-020.2e]", String.Format("[4{0}25e+001           ]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%+.2E]", String.Format("[+4{0}20E+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+.2E]", String.Format("[+4{0}25E+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+20.2E]", String.Format("[          +4{0}20E+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+20.2E]", String.Format("[          +4{0}25E+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-20.2E]", String.Format("[+4{0}20E+001          ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-20.2E]", String.Format("[+4{0}25E+001          ]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+020.2E]", String.Format("[+00000000004{0}20E+001]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+020.2E]", String.Format("[+00000000004{0}25E+001]", sepDecimal ), 42.5 ) );
			Assert.IsTrue( RunTest( "[%+-020.2E]", String.Format("[+4{0}20E+001          ]", sepDecimal ), 42 ) );
			Assert.IsTrue( RunTest( "[%+-020.2E]", String.Format("[+4{0}25E+001          ]", sepDecimal ), 42.5 ) );

			Assert.IsTrue( RunTest( "[%.2e]", String.Format("[-4{0}20e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%.2e]", String.Format("[-4{0}25e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%20.2e]", String.Format("[          -4{0}20e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%20.2e]", String.Format("[          -4{0}25e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-20.2e]", String.Format("[-4{0}20e+001          ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-20.2e]", String.Format("[-4{0}25e+001          ]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%020.2e]", String.Format("[-00000000004{0}20e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%020.2e]", String.Format("[-00000000004{0}25e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%-020.2e]", String.Format("[-4{0}20e+001          ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%-020.2e]", String.Format("[-4{0}25e+001          ]", sepDecimal ), -42.5 ) );

			Assert.IsTrue( RunTest( "[%+.2e]", String.Format("[-4{0}20e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+.2e]", String.Format("[-4{0}25e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+20.2e]", String.Format("[          -4{0}20e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+20.2e]", String.Format("[          -4{0}25e+001]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+-20.2e]", String.Format("[-4{0}20e+001          ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-20.2e]", String.Format("[-4{0}25e+001          ]", sepDecimal ), -42.5 ) );
			Assert.IsTrue( RunTest( "[%+020.2e]", String.Format("[-00000000004{0}20e+001]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+020.2e]", String.Format("[-00000000004{0}25e+001]", sepDecimal ), -42.545 ) );
			Assert.IsTrue( RunTest( "[%+-020.2e]", String.Format("[-4{0}20e+001          ]", sepDecimal ), -42 ) );
			Assert.IsTrue( RunTest( "[%+-020.2e]", String.Format( "[-4{0}26e+001          ]", sepDecimal ), -42.555 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region Character
		[Category( "Character" )]
		[Test( Description = "Character format %c" )]
		public void CharacterFormat()
		{
			Console.WriteLine( "Test character formats %c" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%c]", "[]", null ) );
			Assert.IsTrue( RunTest( "[%c]", "[A]", 'A' ) );
			Assert.IsTrue( RunTest( "[%c]", "[A]", "A Test" ) );
			Assert.IsTrue( RunTest( "[%c]", "[A]", 65 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region Strings
		[Category( "String" )]
		[Test( Description = "Test string format %s" )]
		public void Strings()
		{
			Console.WriteLine( "Test string format %s" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%s]", "[This is a test]", "This is a test" ) );
			Assert.IsTrue( RunTest( "[%s]", "[A test with %]", "A test with %" ) );
			Assert.IsTrue( RunTest( "[%s]", "[A test with %s inside]", "A test with %s inside" ) );
			Assert.IsTrue( RunTest( "[%% %s %%]", "[% % Another test % %]", "% Another test %" ) );
			Assert.IsTrue( RunTest( "[%20s]", "[       a long string]", "a long string" ) );
			Assert.IsTrue( RunTest( "[%-20s]", "[a long string       ]", "a long string" ) );
			Assert.IsTrue( RunTest( "[%020s]", "[0000000a long string]", "a long string" ) );
			Assert.IsTrue( RunTest( "[%-020s]", "[a long string       ]", "a long string" ) );

			Assert.IsTrue( RunTest( "[%.10s]", "[This is a ]", "This is a shortened string" ) );
			Assert.IsTrue( RunTest( "[%20.10s]", "[          This is a ]", "This is a shortened string" ) );
			Assert.IsTrue( RunTest( "[%-20.10s]", "[This is a           ]", "This is a shortened string" ) );
			Assert.IsTrue( RunTest( "[%020.10s]", "[0000000000This is a ]", "This is a shortened string" ) );
			Assert.IsTrue( RunTest( "[%-020.10s]", "[This is a           ]", "This is a shortened string" ) );

			Tools.printf( "Account balance: %'+20.2f\n", 12345678 );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region Hex
		[Category( "HEX" )]
		[Test( Description = "Test hex format %x / %X" )]
		public void Hex()
		{
			Console.WriteLine( "Test hex format %x / %X" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%x]", "[2a]", 42 ) );
			Assert.IsTrue( RunTest( "[%X]", "[2A]", 42 ) );
			Assert.IsTrue( RunTest( "[%5x]", "[   2a]", 42 ) );
			Assert.IsTrue( RunTest( "[%5X]", "[   2A]", 42 ) );
			Assert.IsTrue( RunTest( "[%05x]", "[0002a]", 42 ) );
			Assert.IsTrue( RunTest( "[%05X]", "[0002A]", 42 ) );
			Assert.IsTrue( RunTest( "[%-05x]", "[2a   ]", 42 ) );
			Assert.IsTrue( RunTest( "[%-05X]", "[2A   ]", 42 ) );

			Assert.IsTrue( RunTest( "[%#x]", "[0x2a]", 42 ) );
			Assert.IsTrue( RunTest( "[%#X]", "[0X2A]", 42 ) );
			Assert.IsTrue( RunTest( "[%#5x]", "[ 0x2a]", 42 ) );
			Assert.IsTrue( RunTest( "[%#5X]", "[ 0X2A]", 42 ) );
			Assert.IsTrue( RunTest( "[%#05x]", "[0x02a]", 42 ) );
			Assert.IsTrue( RunTest( "[%#05X]", "[0X02A]", 42 ) );
			Assert.IsTrue( RunTest( "[%#-05x]", "[0x2a ]", 42 ) );
			Assert.IsTrue( RunTest( "[%#-05X]", "[0X2A ]", 42 ) );

			Assert.IsTrue( RunTest( "[%.2x]", "[05]", 5 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region Octal
		[Category( "Octal" )]
		[Test( Description = "Test octal format %o" )]
		public void Octal()
		{
			Console.WriteLine( "Test octal format %o" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%o]", "[52]", 42 ) );
			Assert.IsTrue( RunTest( "[%o]", "[52]", 42 ) );
			Assert.IsTrue( RunTest( "[%5o]", "[   52]", 42 ) );
			Assert.IsTrue( RunTest( "[%5o]", "[   52]", 42 ) );
			Assert.IsTrue( RunTest( "[%05o]", "[00052]", 42 ) );
			Assert.IsTrue( RunTest( "[%05o]", "[00052]", 42 ) );
			Assert.IsTrue( RunTest( "[%-05o]", "[52   ]", 42 ) );
			Assert.IsTrue( RunTest( "[%-05o]", "[52   ]", 42 ) );

			Assert.IsTrue( RunTest( "[%#o]", "[052]", 42 ) );
			Assert.IsTrue( RunTest( "[%#o]", "[052]", 42 ) );
			Assert.IsTrue( RunTest( "[%#5o]", "[  052]", 42 ) );
			Assert.IsTrue( RunTest( "[%#5o]", "[  052]", 42 ) );
			Assert.IsTrue( RunTest( "[%#05o]", "[00052]", 42 ) );
			Assert.IsTrue( RunTest( "[%#05o]", "[00052]", 42 ) );
			Assert.IsTrue( RunTest( "[%#-05o]", "[052  ]", 42 ) );
			Assert.IsTrue( RunTest( "[%#-05o]", "[052  ]", 42 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#region PositionIndex
		[Category( "PositionIndex" )]
		[Test( Description = "Test position index (n$)" )]
		public void PositionIndex()
		{
			Console.WriteLine( "Test position index (n$)" );
			Console.WriteLine( "--------------------------------------------------------------------------------" );

			Assert.IsTrue( RunTest( "[%2$d %1$#x %1$d]", "[17 0x10 16]", 16, 17 ) );

			Console.WriteLine( "\n\n" );
		}
		#endregion
		#endregion

		#region Destroy Tests
		[TestFixtureTearDown]
		public void TearDown()
		{
		}
		#endregion

		#region Private Methods
		#region RunTest
		[Ignore( "" )]
		private bool RunTest( string Format, string Wanted, params object[] Parameters )
		{
			string result = Tools.sprintf( Format, Parameters );
			Console.WriteLine( "Format:\t{0,-30}Parameters: {1}\nWanted:\t{2}\nResult:\t{3}",
				Format, ShowParameters( Parameters ), Wanted, result );
			if ( Wanted == null || Wanted == result )
			{
				Console.WriteLine();
				return true;
			}
			else
			{
				Console.WriteLine( "*** ERROR ***\n" );
				return false;
			}
		}
		#endregion
		#region ShowParameters
		private string ShowParameters( params object[] Parameters )
		{
			string w = String.Empty;

			if ( Parameters == null )
				return "(null)";

			foreach ( object o in Parameters )
				w += ( w.Length > 0 ? ", " : "" ) + ( o == null ? "(null)" : o.ToString() );
			return w;
		}
		#endregion
		#endregion
	}
}
