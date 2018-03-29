using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;

namespace OvenController {
    class TempSettings {

        public TempSettings() {
            // Get database file path.
            string databaseFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            databaseFile += @"\OvenController\Database";
            System.IO.Directory.CreateDirectory(databaseFile);
            databaseFile += @"\settings.db3";
            string connectionSt = "Data Source=\"" + databaseFile + "\"";

            dataConnection = new SQLiteConnection(connectionSt);
            dataConnection.Open();
            // Check that defult settings exist.
            DataTable tableSchema = dataConnection.GetSchema("Tables");

            if (tableSchema.Rows.Count < 1) {
                // Database creation code.
                DbCommand sqlCommand = new SQLiteCommand((SQLiteConnection)dataConnection);
                sqlCommand.CommandText = "CREATE TABLE Profile (" +
                    "id INTEGER PRIMARY KEY," +
                    "Name TEXT UNIQUE);";
                int rowsAffected = sqlCommand.ExecuteNonQuery();
                sqlCommand.CommandText = "CREATE TABLE Steps (" +
                    "ProfileID INTEGER," +
                    "StartTemp INTEGER," +
                    "TargetTemp INTEGER," +
                    "MinDuration INTEGER," +
                    "MaxDuration INTEGER);";
                rowsAffected = sqlCommand.ExecuteNonQuery();
            }
        }

        public int AddProfile(String profileName) {
            int newId = 1;
            DbCommand sqlCommand = new SQLiteCommand((SQLiteConnection)dataConnection);
            sqlCommand.CommandText = "SELECT MAX(id) FROM Profile";
            DbDataReader queryResult = sqlCommand.ExecuteReader();

            if (queryResult.HasRows && (queryResult[0].GetType() !=
                typeof(System.DBNull))) {
                newId = (int)(Int64)queryResult[0] + 1;
            }
            queryResult.Close();
            sqlCommand.CommandText = "INSERT INTO Profile VALUES (" +
                newId.ToString() + ", \"" + profileName + "\");";
            int rowsAffected = sqlCommand.ExecuteNonQuery();
            sqlCommand.CommandText = "SELECT id FROM Profile WHERE Name = \"" +
                profileName + "\";";
            queryResult = sqlCommand.ExecuteReader();
            return (int)(Int64)queryResult["id"];
        }

        public int GetProfileId(String profileName) {
            DbCommand sqlCommand = new SQLiteCommand((SQLiteConnection)dataConnection);
            sqlCommand.CommandText = "SELECT id FROM Profile WHERE Name = \"" +
                profileName + "\";";
            DbDataReader queryResult = sqlCommand.ExecuteReader();
            return (int)(Int64)queryResult["id"];
        }

        public void AddStep(int profileId, int startTemp, int targetTemp, int minDuration, int maxDuration) {
            DbCommand sqlCommand = new SQLiteCommand((SQLiteConnection)dataConnection);
            sqlCommand.CommandText = "INSERT INTO Steps VALUES ("
                + profileId.ToString() + ", " + startTemp.ToString() + ", " + targetTemp.ToString()
                + ", " + minDuration.ToString() + ", "
                + maxDuration.ToString() + ");";
            int rowsAffected = sqlCommand.ExecuteNonQuery();
        }

        public TempStep[] LoadProfile(String profileName) {
            TempStep[] result = null;
            DbCommand sqlCommand = new SQLiteCommand((SQLiteConnection)dataConnection);
            int profileSteps = 0;
            sqlCommand.CommandText = "SELECT COUNT(Time) FROM Profile JOIN"
            + " Steps ON Profile.id = Steps.ProfileID WHERE Name = \"" +
                profileName + "\";";

            DbDataReader queryResult = sqlCommand.ExecuteReader();

            if (queryResult.HasRows && (queryResult[0].GetType() !=
               typeof(System.DBNull))) {
                profileSteps = (int)(Int64)queryResult[0];
            } else {
                return null;
            }
            queryResult.Close();

            sqlCommand.CommandText = "SELECT * FROM Profile JOIN"
            + " Steps ON Profile.id = Steps.ProfileID WHERE Name = \"" +
                profileName + "\";";
            queryResult = sqlCommand.ExecuteReader();

            result = new TempStep[profileSteps];
            int step = 0;
            while (queryResult.Read()) {
                result[step++] = new TempStep((int)(Int64)queryResult["StartTemp"], (int)(Int64)queryResult["TargetTemp"], (int)(Int64)queryResult["MinDuration"], (int)(Int64)queryResult["MaxDuration"]);
            }

            return result;
        }

        private DbConnection dataConnection;
    }
}
