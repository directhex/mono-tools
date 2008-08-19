//
// Gendarme.Rules.Design.Linq.AvoidExtensionMethodOnSystemObjectRule
//
// Authors:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Cecil;
using Gendarme.Framework;
using Gendarme.Framework.Rocks;

namespace Gendarme.Rules.Design.Linq {

	// ref: http://blogs.msdn.com/mirceat/archive/2008/03/13/linq-framework-design-guidelines.aspx

	[Problem ("This method extends System.Object. This will not work for VB.NET consumer.")]
	[Solution ("Use of more specialized type to extend.")]
	public class AvoidExtensionMethodOnSystemObjectRule : Rule, IMethodRule {

		public override void Initialize (IRunner runner)
		{
			base.Initialize (runner);
			// extension methods are only available in FX3.5
			// check runtime >= NET2_0 (fast) then check if [ExtensionAttribute] is referenced
			Runner.AnalyzeAssembly += delegate (object o, RunnerEventArgs e) {
				Active = (e.CurrentAssembly.Runtime >= TargetRuntime.NET_2_0);
			};
			Runner.AnalyzeModule += delegate (object o, RunnerEventArgs e) {
				Active &= e.CurrentModule.TypeReferences.ContainsType ("System.Runtime.CompilerServices.ExtensionAttribute");
			};
		}

		// rock-ify
		// not 100% bullet-proof against buggy compilers (or IL)
		static bool IsExtension (MethodDefinition method)
		{
			if (!method.IsStatic)
				return false;

			if (method.Parameters.Count < 1)
				return false;

			return method.HasAttribute ("System.Runtime.CompilerServices.ExtensionAttribute");
		}

		public RuleResult CheckMethod (MethodDefinition method)
		{
			if (!IsExtension (method))
				return RuleResult.DoesNotApply;

			if (method.Parameters [0].ParameterType.FullName != "System.Object")
				return RuleResult.Success;

			Runner.Report (method, Severity.High, Confidence.High);
			return RuleResult.Failure;
		}
	}
}