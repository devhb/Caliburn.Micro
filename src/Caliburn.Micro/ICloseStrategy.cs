// ------------------------------------------------------------------
// © Copyright 2016 Thermo Fisher Scientific Inc. All rights reserved.
// ------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caliburn.Micro.Async {
    /// <summary>
    /// Used to gather the results from multiple child elements which may or may not prevent closing.
    /// </summary>
    /// <typeparam name="T">The type of child element.</typeparam>
    public interface ICloseStrategy<T> where T : class {
        /// <summary>
        /// Executes the strategy.
        /// </summary>
        /// <param name="toClose">Items that are requesting close.</param>
        Task<CloseStrategyResult<T>> Execute(IEnumerable<T> toClose);
    }

    /// <summary />
    public struct CloseStrategyResult<T> where T : class {
        /// <summary>
        /// </summary>
        /// <param name="result"></param>
        /// <param name="enumerable"></param>
        public CloseStrategyResult(bool result, IEnumerable<T> enumerable)
        {
            CanClose = result;
            Items = enumerable.ToArray();
        }

        /// <summary>
        /// Indicates whether close can occur.
        /// </summary>
        public bool CanClose { get; set; }

        /// <summary>
        /// Represents the children which should be closed if the parent cannot.
        /// </summary>
        public T[] Items { get; set; }
    }
}