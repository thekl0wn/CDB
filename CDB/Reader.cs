using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDB
{

    public class Reader : IDisposable
    {

        internal SqlDataReader InternalReader
        {
            get { return _InternalReader; }
            set
            {
                _InternalReader = value;
                this.CurrentRow = -1;
            }
        }
        private SqlDataReader _InternalReader;

        public bool HasRows { get { return this.InternalReader.HasRows; } }
        public int CurrentRow { get; private set; } = -1;

        public void Dispose()
        {
            if (this.InternalReader != null)
            {
                this.InternalReader.Dispose();
                this.InternalReader = null;
            }
        }

        public string GetString(int field) { return this.InternalReader.GetString(field); }
        public string GetString(string field) { return this.GetString(this.GetFieldId(field)); }
        public bool GetBoolean(int field) { return this.InternalReader.GetBoolean(field); }
        public bool GetBoolean(string field) { return this.GetBoolean(this.GetFieldId(field)); }
        public decimal GetDecimal(int field) { return this.InternalReader.GetDecimal(field); }
        public decimal GetDecimal(string field) { return this.GetDecimal(this.GetFieldId(field)); }
        public int GetInt(int field) { return this.InternalReader.GetInt32(field); }
        public int GetInt(string field) { return this.GetInt(this.GetFieldId(field)); }
        public DateTime GetDateTime(int field) { return this.InternalReader.GetDateTime(field); }
        public DateTime GetDateTime(string field) { return this.GetDateTime(this.GetFieldId(field)); }
        public Guid GetGuid(int field) { return this.InternalReader.GetGuid(field); }
        public Guid GetGuid(string field) { return this.GetGuid(this.GetFieldId(field)); }

        public int GetFieldId(string field)
        {
            // field id variable
            int id = -1;

            // format incoming
            field = field.Trim().ToLower();

            // loop through the fields in the reader
            for (int f = 0; f < this.InternalReader.FieldCount; f++)
            {
                var name = this.InternalReader.GetName(f).Trim().ToLower();
                if (name == field) return f;
            }

            // return the field id
            return id;
        }

        public bool Read()
        {
            this.CurrentRow++;
            var read = this.InternalReader.Read();
            if (read)
            {
                var args = new ReadEventArgs(this.CurrentRow);
                var handler = this.OnRead;
                handler?.Invoke(this, args);
            }
            return read;
        }

        public event EventHandler<ReadEventArgs> OnRead;

    }

}
