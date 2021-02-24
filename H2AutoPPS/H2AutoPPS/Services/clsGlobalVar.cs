using System;
using System.Data.SqlClient;
using System.Data;



namespace H2AutoPPS.Services
{
    public class clsGlobalVar
    {
        private readonly static string strDBCon = "data source=172.20.168.206;initial catalog=PAL_QSMC_USA;user id=PAL;password=PALSPN;persist security info=True";

        private static string DbConnectString { get { return strDBCon; } }

        public static string setDBconn()
        {
            return strDBCon;
        }

        public DataSet GetSqlDataSet(string strSQLstmt)
        {
            using (SqlConnection conn = new SqlConnection(DbConnectString))
            {
                conn.Open();

                SqlDataAdapter Adap = new SqlDataAdapter(strSQLstmt, conn);
                DataSet ds = new DataSet();
                Adap.Fill(ds);
                return ds;
            }
        }

        public static DataTable GetDataTable(string strSQLstmt)
        {
            using (SqlConnection conn = new SqlConnection(DbConnectString))
            {
                conn.Open();
                DataSet ds = new DataSet();
                using (SqlDataAdapter Adap = new SqlDataAdapter(strSQLstmt, conn))
                {
                    Adap.Fill(ds);
                }
                return ds.Tables[0];
            }
        }

        public static string GetFieldValue(string strSQLstmt)
        {
            using (SqlConnection conn = new SqlConnection(DbConnectString))
            {
                conn.Open();
                string fieldValue = "";
                using (SqlCommand sqlCommand = new SqlCommand(strSQLstmt, conn))
                {
                    object objFieldValue = sqlCommand.ExecuteScalar();
                    if (objFieldValue != null)
                    {
                        fieldValue = objFieldValue.ToString();
                    }

                    return fieldValue;
                }
            }
        }

        public static bool ExecSQL(string strSQLstmt)
        {
            using (SqlConnection conn = new SqlConnection(DbConnectString))
            {
                conn.Open();
                bool result = false;
                using (SqlCommand sqlcommand = new SqlCommand(strSQLstmt, conn))
                {
                    sqlcommand.ExecuteNonQuery();
                    result = true;
                    return result;
                }
            }

        }

    }
}

