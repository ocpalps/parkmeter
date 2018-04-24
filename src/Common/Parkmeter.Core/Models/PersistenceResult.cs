using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.Core.Models
{
    public enum ResultStates
    {
        Completed,
        CompletedWithWarnings,
        Error
    }
    public class PersistenceResult
    {
        public PersistenceResult()
        {
            ID = -1;
            Message = "";
            Warnings = new List<string>();
        }

        /// <summary>
        /// The ID created when a new item is added
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// State of the operation result
        /// </summary>
        public ResultStates State { get; set; }

        /// <summary>
        /// Possible error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Collection of possible warning messages
        /// </summary>
        public List<string> Warnings { get; set; }
    }
}
