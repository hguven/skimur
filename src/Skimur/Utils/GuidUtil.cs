﻿using System;
using System.Runtime.InteropServices;

namespace Skimur.Utils
{
    public static class GuidUtil
    {
        [DllImport("rpcrt4.dll", SetLastError = true)]
        static extern int UuidCreateSequential(out Guid guid);

        /// <summary>
        /// Creates a new sequential GUID, ideal for SQL server
        /// </summary>
        /// <returns></returns>
        public static Guid NewSequentialId()
        {
            // http://blogs.msdn.com/b/dbrowne/archive/2012/07/03/how-to-generate-sequential-guids-for-sql-server-in-net.aspx
            Guid guid;
            UuidCreateSequential(out guid);
            var s = guid.ToByteArray();
            var t = new byte[16];
            t[3] = s[0];
            t[2] = s[1];
            t[1] = s[2];
            t[0] = s[3];
            t[5] = s[4];
            t[4] = s[5];
            t[7] = s[6];
            t[6] = s[7];
            t[8] = s[8];
            t[9] = s[9];
            t[10] = s[10];
            t[11] = s[11];
            t[12] = s[12];
            t[13] = s[13];
            t[14] = s[14];
            t[15] = s[15];
            return new Guid(t);
        }

        public static Guid ParseGuid(this string value, bool throwOnInvalidGuid = false)
        {
            if (string.IsNullOrEmpty(value))
                return Guid.Empty;
            Guid result;
            if (Guid.TryParse(value, out result)) return result;
            if(throwOnInvalidGuid)
                throw new Exception("Invalid guid " + value);
            return Guid.Empty;
        }
    }
}
