﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Helps to enumerate items in a collection asynchronously
    /// </summary>
    /// <example>
    /// <code>
    /// IAsyncEnumerable&lt;int&gt; ProduceNumbers(int start, int end)
    /// {
    ///   return new AsyncEnumerable&lt;int&gt;(async yield => {
    ///     for (int number = start; number &lt;= end; number++)
    ///       await yield.ReturnAsync(number);
    ///   });
    /// }
    /// 
    /// async Task ConsumeAsync()
    /// {
    ///   var asyncEnumerableCollection = ProduceNumbers(start: 1, end: 10);
    ///   await asyncEnumerableCollection.ForEachAsync(async number => {
    ///     await Console.Out.WriteLineAsync(number)
    ///   });
    /// }
    /// 
    /// // It's backward compatible with synchronous enumeration, but gives no benefits
    /// void ConsumeSync()
    /// {
    ///   var enumerableCollection = ProduceNumbers(start: 1, end: 10);
    ///   foreach (var number in enumerableCollection) {
    ///     Console.Out.WriteLine(number)
    ///   };
    /// }
    /// </code>
    /// </example>
    public sealed class AsyncEnumerable<T> : AsyncEnumerable, IAsyncEnumerable<T>
    {
        private Func<AsyncEnumerator<T>.Yield, Task> _enumerationFunction;
        private bool _oneTimeUse;

        /// <summary>
        /// A pre-cached empty collection
        /// </summary>
        public readonly static IAsyncEnumerable<T> Empty = new AsyncEnumerable<T>(yield => CompletedTask);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        public AsyncEnumerable(Func<AsyncEnumerator<T>.Yield, Task> enumerationFunction)
            : this(enumerationFunction, oneTimeUse: false)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        /// <param name="oneTimeUse">When True the enumeration can be performed once only and Reset method is not allowed</param>
        public AsyncEnumerable(Func<AsyncEnumerator<T>.Yield, Task> enumerationFunction, bool oneTimeUse)
        {
            _enumerationFunction = enumerationFunction;
            _oneTimeUse = oneTimeUse;
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        public Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var enumerator = new AsyncEnumerator<T>(_enumerationFunction, _oneTimeUse);
            return Task.FromResult<IAsyncEnumerator<T>>(enumerator);
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken) => GetAsyncEnumeratorAsync(cancellationToken).ContinueWith<IAsyncEnumerator>(task => task.Result);

        /// <summary>
        /// Returns an enumerator that iterates through the collection
        /// </summary>
        /// <returns>An instance of enumerator</returns>
        public IEnumerator<T> GetEnumerator() => GetAsyncEnumeratorAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Returns an enumerator that iterates through the collection
        /// </summary>
        /// <returns>An instance of enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetAsyncEnumeratorAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Base abstract class that implements <see cref="IAsyncEnumerable"/>.
    /// Use concrete implementation <see cref="AsyncEnumerable{T}"/>.
    /// </summary>
    public abstract class AsyncEnumerable : IAsyncEnumerable
    {
        /// <summary>
        /// Returns pre-cached empty collection
        /// </summary>
        public static IAsyncEnumerable<T> Empty<T>() => AsyncEnumerable<T>.Empty;

        internal static readonly Task CompletedTask = Task.FromResult(0);

        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
