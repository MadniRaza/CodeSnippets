using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Configuration;

namespace MMR_AIMS
{
    public class DAL
    {
        private SqlTransaction _Transaction;
        private SqlConnection _Connection;
        private string _ConnectionString;
        private bool _HasTransactionalQueries;
        public DAL(bool has_transactional_queries)
        {
            _Connection = new SqlConnection();
            _ConnectionString = ConfigurationManager.ConnectionStrings["MMR_AIMS_CS"].ConnectionString;
            _HasTransactionalQueries = has_transactional_queries;
            _Connection.ConnectionString = _ConnectionString;
            if (_HasTransactionalQueries)
            {
                _Connection.Open();
                _Transaction = _Connection.BeginTransaction(IsolationLevel.ReadUncommitted); }
        }
        public enum QueryExecutionTypes
        {
            Nothing = 0,
            Data = 1,
            Value = 2
            
        }
        private SqlCommand CreateSQLCommand(SqlQuery query)
        {
            if (!_HasTransactionalQueries)
            {
                _Connection.Open();
            }

            SqlCommand cmd = new SqlCommand(query.QueryText, _Connection);
            cmd.CommandType = query.IsStoreProcedure == true ? CommandType.StoredProcedure : CommandType.Text;
            if (query.Parameters != null)
            {
                foreach (SqlParam p in query.Parameters)
                {
                    if(p.DBType == SqlDbType.Structured)
                    cmd.Parameters.Add(new SqlParameter() { ParameterName = p.Name, Value = p.Value, SqlDbType = p.DBType });
                    else
                        cmd.Parameters.Add(new SqlParameter() { ParameterName = p.Name, Value = p.Value });
                }
            }
            if (_HasTransactionalQueries)
                cmd.Transaction = _Transaction;
            return cmd;
        }
        public object Request(SqlQuery query, QueryExecutionTypes et)
        {
            object ret = null;
            SqlCommand cmd = CreateSQLCommand(query);
            cmd.CommandTimeout = 0;
            try
            {
                switch (et)
                {
                    case QueryExecutionTypes.Data:
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        da.Dispose();
                        cmd.Dispose();
                        ret = ds;
                        break;
                    case QueryExecutionTypes.Value:
                        var scalar = cmd.ExecuteScalar();
                        cmd.Dispose();
                        ret = scalar;
                        break;
                    case QueryExecutionTypes.Nothing:
                        cmd.Connection = _Connection;
                        cmd.ExecuteNonQuery();
                        cmd.Dispose();
                        ret = null;
                        break;
                }
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            if (!_HasTransactionalQueries)
                CloseConnection();
            return ret;
        }
        public void Commit()
        {
            if (_HasTransactionalQueries)
            {
                _Transaction.Commit();
                CloseConnection();
            }
        }
        public void Rollback()
        {
            if (_HasTransactionalQueries)
            {
                _Transaction.Rollback();
                CloseConnection();
            }
        }
        private void CloseConnection()
        {
            _Connection.Close();
            SqlConnection.ClearPool(_Connection);
            _Connection.Dispose();
        }


    }
    public class SqlParam
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public SqlDbType DBType { get; set; }
        public SqlParam(string name, object value, SqlDbType _DBType = SqlDbType.NVarChar)
        {
            Name = name;
            Value = value;
            DBType = _DBType;
        }
    }
    public class SqlQuery
    {
        public string QueryText { get; set; } = "";
        public List<SqlParam> Parameters { get; set; } = new List<SqlParam>();
        public bool IsStoreProcedure = false;

        //public void AddParameter(string key, object value)
        //{
        //    Parameters.Add(new SqlParam(key, value));
        //}

        public SqlQuery(string query)
        {
            QueryText = query;
        }

        public SqlQuery(string query, bool isStoreProcedure)
        {
            QueryText = query;
            IsStoreProcedure = isStoreProcedure;
        }

        public SqlQuery(string query, bool isStoreProcedure, List<SqlParam> Pars)
        {

            QueryText = query;
            IsStoreProcedure = isStoreProcedure;
            Parameters = Pars;
        }
    }
}
