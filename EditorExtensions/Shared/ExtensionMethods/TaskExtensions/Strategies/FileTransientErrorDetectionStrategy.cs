using System;
using Microsoft.Practices.TransientFaultHandling;

namespace MadsKristensen.EditorExtensions
{
    public class FileTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            return true;
        }
    }
}
