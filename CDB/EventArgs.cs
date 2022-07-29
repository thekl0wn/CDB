using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDB
{

    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; } = "";
    }

    public class ExecuteEventArgs : EventArgs
    {
        public ExecuteEventArgs(string sql, int result, int expected)
        {
            this.Sql = sql;
            this.Result = result;
            this.ExpectedResult = expected;
            if (this.Result == this.ExpectedResult)
            {
                this.Success = true;
            }
            else
            {
                this.Success = false;
            }
        }

        public string Sql { get; } = "";
        public int Result { get; } = 0;
        public int ExpectedResult { get; } = 0;
        public bool Success { get; } = true;
    }

    public class ReadEventArgs
    {
        public ReadEventArgs(int row)
        {
            this.Row = row;
        }
        public int Row { get; } = -1;
    }

    public class ScalarEventArgs
    {
        public ScalarEventArgs(string sql, string value)
        {
            Sql = sql;
            Value = value;
        }

        public string Sql { get; } = "";
        public string Value { get; } = "";
    }

}
