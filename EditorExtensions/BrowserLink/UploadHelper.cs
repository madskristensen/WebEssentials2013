using System;
using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions.BrowserLink
{
    public class UploadHelper
    {
        private class Operation
        {
            private readonly string[] _chunks;
            private int _numberOfChunksReceived;
            private readonly int _chunkCount;

            public Operation(int chunkCount)
            {
                _chunkCount = chunkCount;
                _chunks = new string[chunkCount];
            }

            public bool SetPart(int chunkNumber, string chunkContents)
            {
                _chunks[chunkNumber] = chunkContents;

                return Interlocked.Increment(ref _numberOfChunksReceived) == _chunkCount;
            }

            public TResult Read<TResult>()
            {
                var str = string.Join("", _chunks);

                return JsonConvert.DeserializeObject<TResult>(str);
            }
        }

        private readonly ConcurrentDictionary<Guid, Operation> _operationSet = new ConcurrentDictionary<Guid, Operation>();

        public bool TryFinishOperation<TResult>(Guid operationId, string chunkContents, int chunkNumber, int chunkCount, out TResult result)
        {
            var operation = _operationSet.GetOrAdd(operationId, id => new Operation(chunkCount));

            if (operation.SetPart(chunkNumber, chunkContents))
            {
                result = operation.Read<TResult>();

                _operationSet.TryRemove(operationId, out operation);

                return true;
            }

            result = default(TResult);

            return false;
        }

        public void CancelOperation(Guid operationId)
        {
            Operation current;

            _operationSet.TryRemove(operationId, out current);
        }

        public void Reset()
        {
            _operationSet.Clear();
        }
    }
}