using System;
using System.Data.SqlClient;
using System.Data;



namespace H2AutoPPS.Utils
{
    public class SqlConnectHelper
    {
        private readonly static string strDBCon = "data source=172.20.168.206;initial catalog=PAL_QSMC_USA;user id=PAL;password=PALSPN;persist security info=True";

        private SqlConnection conn = null;

        private string DbConnectString { get { return strDBCon; } }

        public SqlConnectHelper()
        {
            conn = new SqlConnection(DbConnectString);
            conn.Open();
        }

        public DataSet GetSqlDataSet(string strSQLstmt)
        {
            if (conn == null || conn.State != ConnectionState.Open)
            {
                conn = new SqlConnection(DbConnectString);
                conn.Open();
            }

            SqlDataAdapter Adap = new SqlDataAdapter(strSQLstmt, conn);
            DataSet ds = new DataSet();
            Adap.Fill(ds);
            return ds;
        }

        public DataTable GetDataTable(string strSQLstmt)
        {
            try
            {
                if (conn == null || conn.State != ConnectionState.Open)
                {
                    conn = new SqlConnection(DbConnectString);
                    conn.Open();
                }
                DataSet ds = new DataSet();
                using (SqlDataAdapter Adap = new SqlDataAdapter(strSQLstmt, conn))
                {
                    Adap.Fill(ds);
                }
                return ds.Tables[0];
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string GetFieldValue(string strSQLstmt)
        {
            if (conn == null || conn.State != ConnectionState.Open)
            {
                conn = new SqlConnection(DbConnectString);
                conn.Open();
            }
            string fieldValue = "";
            using (SqlCommand sqlCommand = new SqlCommand(strSQLstmt, conn))
            {
                try
                {
                    object objFieldValue = sqlCommand.ExecuteScalar();
                    if (objFieldValue != null)
                    {
                        fieldValue = objFieldValue.ToString();
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                return fieldValue;
            }
        }

        public bool ExecSQL(string strSQLstmt)
        {
            if (conn == null || conn.State != ConnectionState.Open)
            {
                conn = new SqlConnection(DbConnectString);
                conn.Open();
            }
            bool result = false;
            using (SqlCommand sqlcommand = new SqlCommand(strSQLstmt, conn))
            {
                try
                {
                    sqlcommand.ExecuteNonQuery();
                    result = true;
                }
                catch (Exception e)
                {
                    throw e;
                }
                return result;
            }
        }

        public void CloseConnect()
        {
            if (conn != null || conn.State != ConnectionState.Closed)
            {
                conn.Close();
            }
        }

    }
}

