using System;

namespace NLua.Method
{
	/*
	 * Parameter information
	 */
	struct MethodArgs
	{
		// Position of parameter
		public int index;
		// Type-conversion function
		public ExtractValue extractValue;
		public bool isParamsArray;
		public Type paramsArrayType;
	}
}