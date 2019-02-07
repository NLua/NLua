using System;

namespace NLua.Method
{
	/*
	 * Parameter information
	 */
	struct MethodArgs
	{
		// Position of parameter
		public int Index;
		// Type-conversion function
		public ExtractValue ExtractValue;
		public bool IsParamsArray;
		public Type ParamsArrayType;
	}
}