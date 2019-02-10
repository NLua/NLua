using System;

namespace NLua.Method
{
	/*
	 * Parameter information
	 */
	class MethodArgs
	{
		// Position of parameter
		public int Index;
		public Type ParameterType;

		// Type-conversion function
		public ExtractValue ExtractValue;
		public bool IsParamsArray;
	}
}