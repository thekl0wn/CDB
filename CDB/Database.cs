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

            // create connection if needed
            if (_Connection == null) _Connection = new SqlConnection(this._ConnectionString);

            // open
            _Connection.Open();

            // check state
            if(_Connection.State == System.Data.ConnectionState.Open)
            {
                // success
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
            // connect
            if (!this.Connect()) return false;

            // create / setup the command
            _Command = _Connection.CreateCommand();
            _Command.CommandText = sql;

            // default
            return true;
        }

        public void Disconnect()
        {

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
        }

        public bool Execute(string sql) { return this.Execute(sql, -999); }
        public bool Execute(string sql, int expected_result)
        {
            // validate the sql
            if (!this.ValidateSql(sql)) return false;

            // create the command
            if (!this.CreateCommand(sql)) return false;

            // execute as non-query
            var result = _Command.ExecuteNonQuery();

            // disconnect
            this.Disconnect();

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

    }
}
