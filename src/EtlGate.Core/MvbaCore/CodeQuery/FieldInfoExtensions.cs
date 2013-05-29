//  * **************************************************************************
//  * Copyright (c) McCreary, Veselka, Bragg & Allen, P.C.
//  * This source code is subject to terms and conditions of the MIT License.
//  * A copy of the license can be found in the License.txt file
//  * at the root of this distribution.
//  * By using this source code in any fashion, you are agreeing to be bound by
//  * the terms of the MIT License.
//  * You must not remove this notice from this software.
//  * **************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

// ReSharper disable CheckNamespace
namespace CodeQuery
// ReSharper restore CheckNamespace
{
	public static class FieldInfoExtensions
	{
		[NotNull]
		public static IEnumerable<T> CustomAttributesOfType<T>([NotNull] this FieldInfo input) where T : Attribute
		{
			return ((MemberInfo)input).CustomAttributesOfType<T>();
		}

		public static bool HasAttributeOfType<TAttributeType>([NotNull] this FieldInfo input) where TAttributeType : Attribute
		{
			return input.CustomAttributesOfType<TAttributeType>().Any();
		}

		[NotNull]
		public static IEnumerable<FieldInfo> ThatAreStatic([NotNull] this IEnumerable<FieldInfo> items)
		{
			return items.Where(x => x.IsStatic);
		}

		[NotNull]
		public static IEnumerable<FieldInfo> WithAttributeOfType<TAttributeType>([NotNull] this IEnumerable<FieldInfo> input) where TAttributeType : Attribute
		{
			return input.Where(x => x.HasAttributeOfType<TAttributeType>());
		}
	}
}