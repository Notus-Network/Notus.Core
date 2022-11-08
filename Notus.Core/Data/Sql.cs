//using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
namespace Notus.Data
{
    public class Sql : IDisposable
    {
        private class BlockRecordStruct
        {
            public string uID { get; set; }
            public string RawData { get; set; }
            public string Prev { get; set; }
            public string ControlHash { get; set; }
        }

        private bool DbOpened = false;
        //private bool TableControlled = false;
        private string ErrorStrInsideObj;
        public string ErrorStr
        {
            get
            {
                return ErrorStrInsideObj;
            }
        }
        private string CurrentTableName;
        private string OpenedDbName;
        private SqliteConnection conObj;

        public bool TableExist(string tableName, string ifTableDoesntExistSql)
        {
            try
            {
                using (SqliteCommand command = conObj.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM sqlite_sequence WHERE name = '" + tableName + "';";
                    SqliteDataReader reader = command.ExecuteReader();
                    int kaySay = 0;
                    while (reader.Read())
                    {
                        kaySay++;
                    }
                    if (kaySay > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                ErrorStrInsideObj = err.Message;
            }

            try
            {
                SqliteCommand createCommand = conObj.CreateCommand();
                createCommand.CommandText = ifTableDoesntExistSql;
                createCommand.ExecuteNonQuery();
            }
            catch (Exception err)
            {
                ErrorStrInsideObj = err.Message;
            }
            return false;
        }

        // select işlemi
        public void Select(string tableName, Action<Dictionary<string, string>> incomeAction, List<string> nameList, Dictionary<string, string> condAndValue)
        {
            CurrentTableName = tableName;
            List<string> fCond = new List<string>();
            int condCount = 0;
            foreach (KeyValuePair<string, string> entry in condAndValue)
            {
                fCond.Add($"`{entry.Key}` = @{entry.Key}");
                condCount++;
            }

            //string selectQuery = "SELECT * FROM '" + tableName+"'";
            string selectQuery = "SELECT * FROM " + tableName;
            if (condCount > 0)
            {
                selectQuery = selectQuery + " WHERE " + String.Join(", ", fCond.ToArray());
            }

            SqliteCommand command = conObj.CreateCommand();
            command.CommandText = selectQuery;
            if (condCount > 0)
            {
                foreach (KeyValuePair<string, string> entry in condAndValue)
                {
                    SqliteParameter uid_p = command.CreateParameter();
                    uid_p.ParameterName = "@" + entry.Key;
                    uid_p.Value = entry.Value;
                    command.Parameters.Add(uid_p);
                }
            }

            try
            {
                using (SqliteDataReader reader = command.ExecuteReader()) {
                    while (reader.Read())
                    {
                        Dictionary<string, string> returnList = new Dictionary<string, string>();
                        foreach (string fieldName in nameList)
                        {
                            string dataValue = reader[fieldName].ToString();
                            returnList.Add(fieldName, dataValue);
                        }
                        incomeAction(returnList);
                    }
                    reader.Close();
                }
            }
            catch (Exception msg)
            {
                ErrorStrInsideObj = msg.Message;
            }
        }

        public bool Clear(string tableName)
        {
            SqliteCommand command = conObj.CreateCommand();
            command.CommandText = "DELETE FROM " + tableName;
            int result=command.ExecuteNonQuery();
            if (result >= 0)
            {
                return true;
            }
            return false;
        }
        // delete işlemi
        public bool Delete(string tableName, Dictionary<string, string> condAndValue)
        {
            CurrentTableName = tableName;
            List<string> fCond = new List<string>();
            foreach (KeyValuePair<string, string> entry in condAndValue)
            {
                fCond.Add($"`{entry.Key}` = @{entry.Key}");
            }

            string deleteQuery = "DELETE FROM " +
                tableName +
                " WHERE " +
                String.Join(", ", fCond.ToArray());

            SqliteCommand command = conObj.CreateCommand();
            command.CommandText = deleteQuery;

            foreach (KeyValuePair<string, string> entry in condAndValue)
            {
                SqliteParameter uid_p = command.CreateParameter();
                uid_p.ParameterName = "@" + entry.Key;
                uid_p.Value = entry.Value;
                command.Parameters.Add(uid_p);
            }
            command.ExecuteNonQuery();
            return true;
        }

        // update işlemi
        public bool Update(string tableName, Dictionary<string, string> fieldAndValue, Dictionary<string, string> condAndValue)
        {
            CurrentTableName = tableName;
            List<string> fUpdate = new List<string>();
            List<string> fCond = new List<string>();
            foreach (KeyValuePair<string, string> entry in fieldAndValue)
            {
                fUpdate.Add($"`{entry.Key}` = @{entry.Key}");
            }
            foreach (KeyValuePair<string, string> entry in condAndValue)
            {
                fCond.Add($"`{entry.Key}` = @{entry.Key}");
            }

            string updateQuery = "UPDATE " + tableName + " SET " +
                String.Join(", ", fUpdate.ToArray()) +
                " WHERE " +
                String.Join(", ", fCond.ToArray());
            SqliteCommand command = conObj.CreateCommand();
            command.CommandText = updateQuery;

            foreach (KeyValuePair<string, string> entry in fieldAndValue)
            {
                SqliteParameter uid_p = command.CreateParameter();
                uid_p.ParameterName = "@" + entry.Key;
                uid_p.Value = entry.Value;
                command.Parameters.Add(uid_p);
            }
            foreach (KeyValuePair<string, string> entry in condAndValue)
            {
                SqliteParameter uid_p = command.CreateParameter();
                uid_p.ParameterName = "@" + entry.Key;
                uid_p.Value = entry.Value;
                command.Parameters.Add(uid_p);
            }
            command.ExecuteNonQuery();
            return true;
        }
        public bool Insert(string tableName, Dictionary<string, string> fieldAndValue)
        {
            CurrentTableName = tableName;

            List<string> fName = new List<string>();
            List<string> fValue = new List<string>();
            foreach (KeyValuePair<string, string> entry in fieldAndValue)
            {
                fName.Add("`" + entry.Key + "`");
                fValue.Add("@" + entry.Key);
            }

            string insertQuery = "INSERT INTO " +
                tableName +
                " ( " +
                String.Join(", ", fName.ToArray()) +
                " ) VALUES ( " +
                String.Join(", ", fValue.ToArray()) +
                " )";
            SqliteCommand command = conObj.CreateCommand();
            command.CommandText = insertQuery;

            foreach (KeyValuePair<string, string> entry in fieldAndValue)
            {
                SqliteParameter uid_p = command.CreateParameter();
                uid_p.ParameterName = "@" + entry.Key;
                uid_p.Value = entry.Value;
                command.Parameters.Add(uid_p);
            }
            command.ExecuteNonQuery();
            return true;
        }
        public bool Open(string dbName)
        {
            if (string.Equals(OpenedDbName, dbName) == true)
            {
                return true;
            }
            try
            {
                conObj = new SqliteConnection("Data Source=" + dbName);
                conObj.Open();
                OpenedDbName = dbName;
                DbOpened = true;
                return true;
            }
            catch (Exception msg)
            {
                ErrorStrInsideObj = msg.Message;
            }
            DbOpened = false;
            return false;
        }
        public void Close()
        {

            OpenedDbName = "";
            if (DbOpened == true)
            {
                try
                {
                    conObj.Close();
                }
                catch (Exception err)
                {
                    ErrorStrInsideObj = err.Message;
                }
                DbOpened = false;
            }
            conObj = null;
        }
        public Sql()
        {

        }
        public void Dispose()
        {
            Close();
        }
        ~Sql() { }
    }

}
