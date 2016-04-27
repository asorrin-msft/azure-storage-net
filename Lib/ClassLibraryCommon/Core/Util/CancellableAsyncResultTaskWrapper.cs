﻿//-----------------------------------------------------------------------
// <copyright file="CancellableAsyncResultTaskWrapper.cs" company="Microsoft">
//    Copyright 2016 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class is designed to bridge the gap between async and APM.
    /// Specifically, if you have a Task-based async method and you want to wrap Begin() and End() methods around it, that's
    /// what this class is for.
    /// Usually, this is trivial with normal Tasks, but because we use our custom 'ICancellableAsyncResult' rather than 'IAsyncResult', 
    /// we need this custom class.
    /// 
    /// 
    /// Sample usage, assuming we already have an "DoThingAsync(CancellationToken token)" method that returns a Task:
    /// 
    /// public virtual ICancellableAsyncResult BeginDoThing(AsyncCallback callback, object state)
    /// {
    ///     return new CancellableAsyncResultTaskWrapper(token => DoThingAsync(token), callback, state);
    /// }
    /// 
    /// public virtual void EndDoThing(IAsyncResult asyncResult)
    /// {
    ///     ((CancellableAsyncResultTaskWrapper)asyncResult).Wait();
    /// }
    /// </summary>
    internal class CancellableAsyncResultTaskWrapper : ICancellableAsyncResult
    {
        internal IAsyncResult internalAsyncResult;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Creates a new ICancellableAsyncResult task wrapper object.
        /// </summary>
        /// <param name="generateTask">This is essentially the async method that does the actual work we want to wrap.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        public CancellableAsyncResultTaskWrapper(Func<CancellationToken, Task> generateTask, AsyncCallback callback, Object state)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;
            this.internalAsyncResult = generateTask(token).AsApm(callback, state);
        }

        public object AsyncState
        {
            get
            {
                return this.internalAsyncResult.AsyncState;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                return this.internalAsyncResult.AsyncWaitHandle;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return this.internalAsyncResult.CompletedSynchronously;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return this.internalAsyncResult.IsCompleted;
            }
        }

        public void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }

        internal void Wait()
        {
            ((Task)this.internalAsyncResult).Wait();
        }
    }

    /// <summary>
    /// This class is the same as CancellableAsyncResultTaskWrapper, except it's used to wrap operations that return a Task&lt;TResult&gt; (instead of just a Task).
    /// </summary>
    /// <typeparam name="TResult">The return type of the operation to wrap</typeparam>
    internal class CancellableAsyncResultTaskWrapper<TResult> : CancellableAsyncResultTaskWrapper
    {
        /// <summary>
        /// Creates a new ICancellableAsyncResult Task&lt;TResult&gt; wrapper object.
        /// </summary>
        /// <param name="generateTask">This is essentially the async method that does the actual work we want to wrap.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        public CancellableAsyncResultTaskWrapper(Func<CancellationToken, Task<TResult>> generateTask, AsyncCallback callback, Object state)
            : base(generateTask, callback, state)
        {
        }

        internal TResult Result
        {
            get
            {
                return ((Task<TResult>)this.internalAsyncResult).Result;
            }
        }
    }
}
