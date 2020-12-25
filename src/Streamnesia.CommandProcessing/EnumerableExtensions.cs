using System;
using System.Collections.Generic;
using System.Linq;

namespace Streamnesia.CommandProcessing
{
    public static class EnumerableExtensions
    {
        public static T Random<T>(this IEnumerable<T> input, Random rng)
        {
            return input.ElementAt(rng.Next(input.Count()));
        }
    }
}
