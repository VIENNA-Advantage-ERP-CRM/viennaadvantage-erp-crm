﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.DataBase;
using VAdvantage.Logging;

namespace VAdvantage.Model
{

    public class MPrivateAccess : X_VAF_Private_Rights
    {
        #region Private Variables
        //private static long serialVersionUID = -5649529789751432279L;
        private static VLogger _log = VLogger.GetVLogger(typeof(MPrivateAccess).FullName);
        #endregion

        /// <summary>
        /// Load Pricate Access
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="VAF_UserContact_ID"></param>
        /// <param name="VAF_TableView_ID"></param>
        /// <param name="Record_ID"></param>
        /// <returns>access or null if not found</returns>
        public static MPrivateAccess Get(Ctx ctx, int VAF_UserContact_ID, int VAF_TableView_ID, int Record_ID)
        {
            MPrivateAccess retValue = null;
            String sql = "SELECT * FROM VAF_Private_Rights WHERE VAF_UserContact_ID=" + VAF_UserContact_ID + " AND VAF_TableView_ID=" + VAF_TableView_ID + " AND Record_ID=" + Record_ID;
            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql);
                if (idr.Read())
                {
                    retValue = new MPrivateAccess(ctx, idr, null);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, "MPrivateAccess", e);
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }

            return retValue;
        }

        /**
         * 	Get Where Clause of Locked Records for Table
         *	@param VAF_TableView_ID table
         *	@param VAF_UserContact_ID user requesting info
         *	@return "<>1" or " NOT IN (1,2)" or null
         */
        public static String GetLockedRecordWhere(int VAF_TableView_ID, int VAF_UserContact_ID)
        {
            //[ 1644094 ] MPrivateAccess.getLockedRecordWhere inefficient
            /*
            ArrayList<Integer> list = new ArrayList<Integer>();
            PreparedStatement pstmt = null;
            String sql = "SELECT Record_ID FROM VAF_Private_Rights WHERE VAF_TableView_ID=? AND VAF_UserContact_ID<>? AND IsActive='Y'";
            try
            {
                pstmt = DB.prepareStatement(sql, null);
                pstmt.setInt(1, VAF_TableView_ID);
                pstmt.setInt(2, VAF_UserContact_ID);
                ResultSet rs = pstmt.executeQuery();
                while (rs.next())
                    list.add(new Integer(rs.getInt(1))); 
                rs.close();
                pstmt.close();
                pstmt = null;
            }
            catch (Exception e)
            {
                s_log.log(Level.SEVERE, sql, e);
            }
            try
            {
                if (pstmt != null)
                    pstmt.close();
                pstmt = null;
            }
            catch (Exception e)
            {
                pstmt = null;
            }
            //
            if (list.size() == 0)
                return null;
            if (list.size() == 1)
                return "<>" + list.get(0);
            //
            StringBuffer sb = new StringBuffer(" NOT IN(");
            for (int i = 0; i < list.size(); i++)
            {
                if (i > 0)
                    sb.append(",");
                sb.append(list.get(i));
            }
            sb.append(")");
            return sb.toString();*/
            String whereClause = " NOT IN ( SELECT Record_ID FROM VAF_Private_Rights WHERE VAF_TableView_ID = "
                + VAF_TableView_ID + " AND VAF_UserContact_ID <> " + VAF_UserContact_ID + " AND IsActive = 'Y' )";
            return whereClause;
        }

        /**
         * 	Persistency Constructor
         *	@param ctx context
         *	@param ignored ignored
         *	@param trxName transaction
         */
        public MPrivateAccess(Ctx ctx, int ignored, Trx trxName)
            : base(ctx, 0, trxName)
        {
            if (ignored != 0)
            {
                throw new ArgumentException("Multi-Key");
            }
        }

        /**
         * 	Load Constructor
         *	@param ctx context
         *	@param rs result set
         *	@param trxName transaction
         */
        public MPrivateAccess(Ctx ctx, IDataReader idr, Trx trxName)
            : base(ctx, idr, trxName)
        {

        }

        /**
         * 	Load Constructor
         *	@param ctx context
         *	@param rs result set
         *	@param trxName transaction
         */
        public MPrivateAccess(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {

        }

        /**
         * 	New Constructor
         *	@param ctx context
         *	@param VAF_UserContact_ID user
         *	@param VAF_TableView_ID table
         *	@param Record_ID record
         */
        public MPrivateAccess(Ctx ctx, int VAF_UserContact_ID, int VAF_TableView_ID, int Record_ID)
            : base(ctx, 0, null)
        {
            SetVAF_UserContact_ID(VAF_UserContact_ID);
            SetVAF_TableView_ID(VAF_TableView_ID);
            SetRecord_ID(Record_ID);
        }

    }	

}
