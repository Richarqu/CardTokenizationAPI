using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardTokenizationAPI.TokenProvider
{
    class FEPConn
    {
        public SqlConnection conn;
        public SqlCommand sql;
        public int num_rows;
        public int returnValue;
        public FEPConn(string query)
        {
            try
            {
                conn = new SqlConnection();
                //conn.ConnectionString = "data source=172.25.31.4;initial catalog=postcard;persist security info=True;User ID=carduser;Password=C@rdusr1";
                conn.ConnectionString = "data source=10.0.41.239;initial catalog=postcard;persist security info=True;User ID=olaniranqr;Password=Password12";
                conn.Open();
                sql = new SqlCommand();
                sql.Connection = conn;
                sql.CommandText = query;
                sql.CommandType = CommandType.Text;
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
        }
        public void addparam(string key, object val)
        {
            sql.Parameters.AddWithValue(key, val);
        }
        public DataSet query(string tblName)
        {
            DataSet ds = new DataSet();
            num_rows = 0;
            try
            {
                SqlDataAdapter res = new SqlDataAdapter();
                res.SelectCommand = sql;
                res.TableMappings.Add("Table", tblName);
                res.Fill(ds);
                num_rows = ds.Tables[tblName].Rows.Count;
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }

            close();
            return ds;
        }
        public int query()
        {
            SqlParameter prm = new SqlParameter();
            prm.SqlDbType = SqlDbType.Int;
            prm.Direction = ParameterDirection.ReturnValue;
            sql.Parameters.Add(prm);
            returnValue = 0;
            int j = 0;
            try
            {
                j = sql.ExecuteNonQuery();
                returnValue = Convert.ToInt32(prm.Value);
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
            close();
            return j;
        }
        public DataSet select()
        {
            DataSet ds = new DataSet();
            num_rows = 0;
            try
            {
                SqlDataAdapter res = new SqlDataAdapter();
                res.SelectCommand = sql;
                res.TableMappings.Add("Table", "recs");
                res.Fill(ds);
                num_rows = ds.Tables["recs"].Rows.Count;
            }
            catch { }
            close();
            return ds;
        }
        public int delete()
        {
            int j = 0;
            try
            {
                j = sql.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
            close();
            return j;
        }
        public int update()
        {
            int j = 0;
            try
            {
                j = sql.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
            close();
            return j;
        }
        public object insert()
        {
            sql.CommandText += "; select @@IDENTITY ";
            object j = 0;
            try
            {
                j = sql.ExecuteScalar();
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
            close();
            return j;
        }
        public string selectScalar()
        {
            string j = "";
            try
            {
                j = Convert.ToString(sql.ExecuteScalar());
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
            close();
            return j;
        }
        public void close()
        {
            conn.Close();
        }
    }
}
