using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Provides extension methods for utility purposes, such as shuffling lists.
    /// </summary>
    public static class UtilityExtensions
    {
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Shuffles the elements of the list in place using the Fisher-Yates algorithm.
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}