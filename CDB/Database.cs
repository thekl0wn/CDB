using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDB
{

    public class Database : IDisposable
    {

        public Database()
        {
            this.ServerName = Properties.Settings.Default.DefaultServer;
            this.DatabaseName = Properties.Settings.Default.DefaultDatabase;
            this.IntegratedSecurity = Properties.Settings.Default.DefaultSecurity;
        }
        public Database(string server_name, string database_name, bool integrated_security = false)
        {
            this.DatabaseName = database_name;
            this.ServerName = server_name;
            this.IntegratedSecurity = integrated_security;
        }

        public string DatabaseName { get; set; } = "";
        public bool IntegratedSecurity { get; set; } = false;
        public string ServerName { get; set; } = "";
        public CDB.Reader Reader { get; private set; }

        private SqlCommand _Command;
        private SqlConnection _Connection;
        private string _ConnectionString
        {
            get
            {
                var conn = new SqlConnectionStringBuilder();
                conn.InitialCatalog = this.DatabaseName;
                conn.DataSource = this.ServerName;
                conn.IntegratedSecurity = this.IntegratedSecurity;
                return conn.ToString();
            }
        }

        private bool Connect()
        {
            // disconnect anything connected
            this.Disconnect();

            // ensure connection is allowed
            if (!this.ValidateConnection()) return false;

            try
            {
                // create connection if needed
                if (_Connection == null) _Connection = new SqlConnection(this._ConnectionString);

                // open
                _Connection.Open();
            }
            catch(Exception e)
            {
                this.Disconnect();
                this.Error(e);
                return false;
            }

            // check state
            if(_Connection.State == System.Data.ConnectionState.Open)
            {
                // success
                var args = EventArgs.Empty;
                var handler = this.OnConnect;
                handler?.Invoke(this, args);
                return true;
            }
            else
            {
                // issue
                return false;
            }
        }

        private bool CreateCommand(string sql)
        {
            // validate the sql
            if (!this.ValidateSql(sql)) return false;

            // connect
            if (!this.Connect()) return false;

            try
            {
                // create / setup the command
                _Command = _Connection.CreateCommand();
                _Command.CommandText = sql;
            }
            catch(Exception e)
            {
                this.Disconnect();
                this.Error(e);
                return false;
            }

            // default
            return true;
        }

        public void Disconnect()
        {
            // check reader
            if (this.Reader != null) this.Reader.Dispose();

            // check command
            if(_Command != null)_Command.Dispose();

            // check connection
            if(_Connection != null)
            {
                if(_Connection.State != System.Data.ConnectionState.Closed)
                {
                    _Connection.Close();
                    var args = EventArgs.Empty;
                    var handler = this.OnDisconnect;
                    handler?.Invoke(this, args);
                }
            }
        }

        public void Dispose()
        {
            // disconnect everyting
            this.Disconnect();

            // dispose of connection
            if(_Connection != null)
            {
                _Connection.Dispose();
                _Connection = null;
            }

            // event
            var args = EventArgs.Empty;
            var handler = this.OnDispose;
            handler?.Invoke(this, args);
        }

        public bool Execute(string sql) { return this.Execute(sql, -999); }
        public bool Execute(string sql, int expected_result)
        {
            // create the command
            if (!this.CreateCommand(sql)) return false;

            var result = -888;
            try
            {
                // execute as non-query
                result = _Command.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                this.Disconnect();
                this.Error(e);
                return false;
            }

            // disconnect
            this.Disconnect();

            // execute event
            this.ExecuteEvent(sql, result, expected_result);

            // check result
            if(expected_result != -999)
            {
                if(expected_result != result)
                {
                    // error event
                    return false;
                }
            }

            // default
            return true;
        }
        public bool Execute(StringBuilder sql) { return this.Execute(sql.ToString()); }
        public bool Execute(List<string> list) { return this.Execute(list, -999); }
        public bool Execute(List<string> list, int expected_result)
        {
            foreach(string sql in list)
            {
                // execute each item individually & bail on first error
                if (!this.Execute(sql, expected_result)) return false;
            }

            // default
            return true;
        }
        public bool Execute(Queue<string> queue) { return this.Execute(queue, -999); }
        public bool Execute(Queue<string> queue, int expected_result)
        {
            while (queue.Count > 0)
            {
                // pull sql out of queue and execute it
                string sql = queue.Dequeue();
                if (!this.Execute(sql, expected_result)) return false;
            }

            // default
            return true;
        }

        public bool Scalar(string sql, out object obj)
        {
            // set object to null as default
            obj = null;

            // create command
            if (!this.CreateCommand(sql)) return false;

            try
            {
                // execute the scalar into object
                obj = _Command.ExecuteScalar();
            }
            catch(Exception e)
            {
                this.Disconnect();
                this.Error(e);
                return false;
            }

            // disconnect
            this.Disconnect();

            // check/return
            if(obj == null)
            {
                return false;
            }
            else
            {
                var args = new ScalarEventArgs(sql, obj.ToString());
                var handler = this.OnScalar;
                handler?.Invoke(this, args);
                return true;
            }
        }
        public bool Scalar(string sql, out string value)
        {
            // set string to blank as default
            value = "";

            // get the scalar object
            object obj;
            if (!this.Scalar(sql, out obj)) return false;

            // convert object to string for output value
            value = obj.ToString();

            // default
            return true;
        }
        public bool Scalar(string sql, out int value)
        {
            // set default output
            value = 0;

            // get string value
            string str;
            if(!this.Scalar(sql, out str)) return false;

            // try to convert to integer
            if (!int.TryParse(str, out value)) return false;

            // default
            return true;
        }
        public bool Scalar(string sql, out decimal value)
        {
            // set default output
            value = 0;

            // get string value
            string str;
            if (!this.Scalar(sql, out str)) return false;

            // try to convert to integer
            if (!decimal.TryParse(str, out value)) return false;

            // default
            return true;
        }
        public bool Scalar(string sql, out DateTime value)
        {
            // set default output
            value = DateTime.MinValue;

            // get string value
            string str;
            if (!this.Scalar(sql, out str)) return false;

            // try to convert to integer
            if (!DateTime.TryParse(str, out value)) return false;

            // default
            return true;
        }
        public bool Scalar(string sql, out Guid value)
        {
            // set default output
            value = Guid.Empty;

            // get string value
            string str;
            if (!this.Scalar(sql, out str)) return false;

            // try to convert to integer
            if (!Guid.TryParse(str, out value)) return false;

            // default
            return true;
        }
        public bool Scalar(string sql, out bool value)
        {
            // set default output
            value = false;

            // get integer value
            int i;
            if (!this.Scalar(sql, out i)) return false;

            // convert to boolean
            if(i == 1)
            {
                value = true;
            }
            else
            {
                value = false;
            }

            // default
            return true;
        }

        public bool Scalar(StringBuilder sql, out object obj) { return this.Scalar(sql.ToString(), out obj); }
        public bool Scalar(StringBuilder sql, out string value) { return this.Scalar(sql.ToString(), out value); }
        public bool Scalar(StringBuilder sql, out int value) { return this.Scalar(sql.ToString(), out value); }
        public bool Scalar(StringBuilder sql, out decimal value) { return this.Scalar(sql.ToString(), out value); }
        public bool Scalar(StringBuilder sql, out DateTime value) { return this.Scalar(sql.ToString(), out value); }
        public bool Scalar(StringBuilder sql, out Guid value) { return this.Scalar(sql.ToString(), out value); }
        public bool Scalar(StringBuilder sql, out bool value) { return this.Scalar(sql.ToString(), out value); }

        public bool StartReader(string sql)
        {
            // create the command
            if (!this.CreateCommand(sql)) return false;

            try
            {
                // set the reader object
                this.Reader = new Reader();
                this.Reader.InternalReader = _Command.ExecuteReader();
            }
            catch(Exception e)
            {
                this.Disconnect();
                this.Error(e);
                return false;
            }

            // do not disconnect... that is calling method's responsibility

            // default
            return true;
        }
        public bool StartReader(StringBuilder sql) { return this.StartReader(sql.ToString()); }

        private bool ValidateConnection()
        {
            if (string.IsNullOrEmpty(this.DatabaseName))
            {
                // error event
                return false;
            }

            if (string.IsNullOrEmpty(this.ServerName))
            {
                // error event
                return false;
            }

            // default
            return true;
        }

        public bool ValidateSql(string sql)
        {
            // format & test
            sql = sql.Trim();
            if (string.IsNullOrEmpty(sql))
            {
                // error event
                return false;
            }

            // default
            return true;
        }

        private void Error(Exception e) { this.Error(e.Message); }
        private void Error(string error)
        {
            var args = new ErrorEventArgs(error);
            var handler = this.OnError;
            handler?.Invoke(this, args);
        }
        private void ExecuteEvent(string sql, int result, int expected)
        {
            var args = new ExecuteEventArgs(sql, result, expected);
            var handler = this.OnExecute;
            handler?.Invoke(this, args);
        }

        public event EventHandler<EventArgs> OnConnect;
        public event EventHandler<EventArgs> OnDisconnect;
        public event EventHandler<EventArgs> OnDispose;
        public event EventHandler<ErrorEventArgs> OnError;
        public event EventHandler<ExecuteEventArgs> OnExecute;
        public event EventHandler<ScalarEventArgs> OnScalar;

    }

}
