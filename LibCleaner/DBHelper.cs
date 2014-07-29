using System;
using System.Data;

namespace LibCleaner
{
    public class DBHelper
    {
        public static string GetString(IDataReader dr, string fieldName)
        {
            return GetString(dr, fieldName, string.Empty);
        }

        public static string GetString(IDataReader dr, string fieldName, string defaultValue)
        {
            return GetString(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static string GetString(IDataReader dr, int columnIndex, string defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetString(columnIndex);
        }

        public static string GetString(IDataReader dr, int columnIndex)
        {
            return dr.IsDBNull(columnIndex) ? string.Empty : dr.GetString(columnIndex);
        }

        public static byte GetByte(IDataReader dr, int columnIndex)
        {
            return GetByte(dr, columnIndex, 0);
        }

        public static byte GetByte(IDataReader dr, string fieldName)
        {
            return GetByte(dr, dr.GetOrdinal(fieldName), 0);
        }

        public static byte GetByte(IDataReader dr, string fieldName, byte defaultValue)
        {
            return GetByte(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static byte GetByte(IDataReader dr, int columnIndex, byte defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetByte(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : (byte) dr.GetDecimal(columnIndex);
        }

        public static int GetInt(IDataReader dr, string fieldName)
        {
            return GetInt(dr, fieldName, 0);
        }

        public static int GetInt(IDataReader dr, string fieldName, int defaultValue)
        {
            return GetInt(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static int GetInt(IDataReader dr, int columnIndex, int defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetInt32(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : (int) dr.GetDecimal(columnIndex);
        }

        public static short GetShort(IDataReader dr, string fieldName)
        {
            var columnIndex = dr.GetOrdinal(fieldName);
            return dr.IsDBNull(columnIndex) ? (short) 0 : dr.GetInt16(columnIndex);
        }

        public static long GetBigint(IDataReader dr, string fieldName)
        {
            return GetBigint(dr, fieldName, 0);
        }

        public static double GetDouble(IDataReader dr, int columnIndex)
        {
            return dr.IsDBNull(columnIndex) ? 0 : dr.GetDouble(columnIndex);
//            return dr.IsDBNull(columnIndex) ? 0 : (double) dr.GetDecimal(columnIndex);
        }

        public static long GetBigint(IDataReader dr, string fieldName, long defaultValue)
        {
            return GetBigint(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static long GetBigint(IDataReader dr, int columnIndex, long defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetInt32(columnIndex);
        }

        public static byte[] GetBytes(IDataReader dr, string fieldName)
        {
            var columnIndex = dr.GetOrdinal(fieldName);
            var myBytes = new byte[5000];
            dr.GetBytes(columnIndex, 0, myBytes, 0, 5000);
            return dr.IsDBNull(columnIndex) ? null : myBytes;
        }

        public static double GetDouble(IDataReader dr, string fieldName)
        {
            return GetDouble(dr, dr.GetOrdinal(fieldName), 0);
        }

        public static double GetDouble(IDataReader dr, string fieldName, double defaultValue)
        {
            return GetDouble(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static double GetDouble(IDataReader dr, int columnIndex, double defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetDouble(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : (double) dr.GetDecimal(columnIndex);
        }

        public static bool GetBoolean(IDataReader dr, string fieldName)
        {
            return GetBoolean(dr, fieldName, false);
        }

        public static bool GetBoolean(IDataReader dr, string fieldName, bool defaultValue)
        {
            return GetBoolean(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static bool GetBoolean(IDataReader dr, int columnIndex, bool defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetBoolean(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetDecimal(columnIndex) == 1;
        }

        public static Guid GetGuid(IDataReader dr, string fieldName)
        {
            var columnIndex = dr.GetOrdinal(fieldName);
            return dr.IsDBNull(columnIndex) ? Guid.Empty : dr.GetGuid(columnIndex);
        }

        public static DateTime GetDateTime(IDataReader dr, int columnIndex)
        {
            return GetDateTime(dr, columnIndex, DateTime.MinValue);
        }

        public static DateTime GetDateTime(IDataReader dr, string fieldName)
        {
            return GetDateTime(dr, fieldName, DateTime.MinValue);
        }

        public static DateTime GetDateTime(IDataReader dr, string fieldName, DateTime defaultValue)
        {
            return GetDateTime(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static DateTime GetDateTime(IDataReader dr, int columnIndex, DateTime defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetDateTime(columnIndex);
        }

        public static decimal GetDecimal(IDataReader dr, string fieldName)
        {
            return GetDecimal(dr, fieldName, 0);
        }

        public static decimal GetDecimal(IDataReader dr, string fieldName, decimal defaultValue)
        {
            return GetDecimal(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static decimal GetDecimal(IDataReader dr, int columnIndex)
        {
            return GetDecimal(dr, columnIndex, 0);
        }

        public static decimal GetDecimal(IDataReader dr, int columnIndex, decimal defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetDecimal(columnIndex);
        }

        public static string GetXml(IDataReader sdr, int columnIndex, string defaultValue)
        {
            return sdr.IsDBNull(columnIndex) ? defaultValue : sdr.GetString(columnIndex);
        }
    }
}