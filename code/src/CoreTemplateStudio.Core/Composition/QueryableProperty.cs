﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.Templates.Core.Composition
{
    public class QueryableProperty
    {
        public static QueryableProperty Empty => new QueryableProperty(string.Empty, string.Empty);

        public string Name { get; }

        public string Value { get; }

        public QueryableProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public bool Compare(QueryNode query)
        {
            if (string.IsNullOrEmpty(Value) || string.IsNullOrEmpty(query.Value))
            {
                return false;
            }

            var comparitionResult = Compare(query.Value);

            if (query.Operator == QueryOperator.Equals)
            {
                return comparitionResult;
            }
            else
            {
                return !comparitionResult;
            }
        }

        private bool Compare(string toCompare)
        {
            if (Value.IsMultiValue() && toCompare.IsMultiValue())
            {
                return Value.GetMultiValue().Any(v => toCompare.GetMultiValue().Contains(v));
            }
            else if (Value.IsMultiValue() && !toCompare.IsMultiValue())
            {
                return Value.GetMultiValue().Any(v => v.Equals(toCompare));
            }
            else if (!Value.IsMultiValue() && toCompare.IsMultiValue())
            {
                return toCompare.GetMultiValue().Any(v => v.Equals(Value));
            }
            else
            {
                return Value.Equals(toCompare);
            }
        }
    }
}
