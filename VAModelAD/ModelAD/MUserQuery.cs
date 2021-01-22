﻿/********************************************************
 * Module Name    : Search/HighVolume
 * Purpose        : Contains functions used to save user query of Advanced tab of "Find" window
 * Class Used     : Inherited from class X_VAF_UserSearch.cs
 * Chronological Development
 * Veena Pandey     9-Feb-2009
  ******************************************************/

using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using VAdvantage.Classes;
using System.Data;
using VAdvantage.SqlExec;
using VAdvantage.DataBase;
using VAdvantage.Logging;
using VAdvantage.Utility;

namespace VAdvantage.Model
{
    /// <summary>
    /// Contains functions used to save user query of Advanced tab of "Find" window
    /// </summary>
    public class MUserQuery : X_VAF_UserSearch
    {
        //	The Lines
        //private MUserQueryLine[] _lines = null;
        
        //	Logger	
        private static VLogger _log = VLogger.GetVLogger(typeof(MUserQuery).FullName);

        /// <summary>
        /// Load Constrctor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">data row</param>
        /// <param name="trxName">transaction</param>
        public MUserQuery(Context ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

        public MUserQuery(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAF_UserSearch_ID">VAF_UserSearch_ID</param>
        /// <param name="trxName">transaction name</param>
        public MUserQuery(Context ctx, int VAF_UserSearch_ID, Trx trxName)
            : base(ctx, VAF_UserSearch_ID, trxName)
        {
        }

        public MUserQuery(Ctx ctx, int VAF_UserSearch_ID, Trx trxName)
            : base(ctx, VAF_UserSearch_ID, trxName)
        {
        }

        /// <summary>
        /// Get query names stored in database
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAF_Tab_ID">tab id</param>
        /// <param name="VAF_TableView_ID">table id</param>
        /// <param name="valueColumnName">column name</param>
        /// <returns>DataSet</returns>
        public static DataSet GetDataSet(Context ctx, int VAF_Tab_ID, int VAF_TableView_ID, string valueColumnName)
        {
            int VAF_Client_ID = ctx.GetVAF_Client_ID();
            DataSet ds = null;
            string sql = "SELECT NAME," + valueColumnName + " FROM VAF_UserSearch WHERE"
                + " VAF_Client_ID=" + VAF_Client_ID + " AND IsActive='Y'"
                + " AND (VAF_Tab_ID=" + VAF_Tab_ID + " OR VAF_TableView_ID=" + VAF_TableView_ID + ")"
                + " ORDER BY VAF_UserSearch_ID";
            try
            {
                ds = DataBase.DB.ExecuteDataset(sql, null, null);
            }
            catch (Exception ex)
            {
                if (ds != null)
                {
                    ds = null;
                }

                _log.Log(Level.SEVERE, sql, ex);
            }
            return ds;
        }

        #region "commented area"

        //public static MUserQuery[] Get(Context ctx, int VAF_Tab_ID, int VAF_TableView_ID)
        //{
        //    int VAF_Client_ID = ctx.GetVAF_Client_ID();

        //    string sqlQry = "SELECT * FROM VAF_UserSearch WHERE"
        //        + " VAF_Client_ID=" + VAF_Client_ID + " AND IsActive='Y'"
        //        + " AND (VAF_Tab_ID=" + VAF_Tab_ID + " OR VAF_TableView_ID=" + VAF_TableView_ID + ")"
        //        + " ORDER BY Name";
        //    List<MUserQuery> list = new List<MUserQuery>();
        //    try
        //    {
        //        DataSet ds = ExecuteQuery.ExecuteDataset(sqlQry);
        //        int iTot = ds.Tables[0].Rows.Count;
        //        for (int iRow = 0; iRow < iTot; iRow++)
        //        {
        //            list.Add(new MUserQuery(ctx, ds.Tables[0].Rows[iRow], null));
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        _log.Log(Level.SEVERE, sql, e);
        //    }
        //    MUserQuery[] retValue = new MUserQuery[list.Count];
        //    //list.ToArray(retValue);
        //    retValue = list.ToArray();
        //    return retValue;
        //}


        //public static MUserQuery Get(Context ctx, int VAF_Tab_ID, string name)
        //{
        //    int VAF_Client_ID = ctx.GetVAF_Client_ID();
        //    if (name == null)
        //        name = "%";
        //    string sqlQry = "SELECT * FROM VAF_UserSearch WHERE"
        //        + " VAF_Client_ID=" + VAF_Client_ID + " AND VAF_Tab_ID=" + VAF_Tab_ID + " AND"
        //        + " UPPER(Name) LIKE '" + name.ToUpper() + "' AND IsActive='Y'"
        //        + " ORDER BY Name";
        //    MUserQuery retValue = null;
        //    try
        //    {
        //        DataSet ds = ExecuteQuery.ExecuteDataset(sqlQry);
        //        int iTot = ds.Tables[0].Rows.Count;
        //        for (int iRow = 0; iRow < iTot; iRow++)
        //        {
        //            retValue = new MUserQuery(ctx, ds.Tables[0].Rows[iRow], null);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        _log.Log(Level.SEVERE, sql, e);
        //    }
        //    return retValue;
        //}


        //public static List<string> GetSavedQueryNames(int VAF_Client_ID, int VAF_Tab_ID)
        //{
        //    string sqlQry = "SELECT Name FROM VAF_UserSearch WHERE"
        //        + " VAF_Client_ID=" + VAF_Client_ID + " AND VAF_Tab_ID=" + VAF_Tab_ID + " AND IsActive='Y'"
        //        + " ORDER BY Name";
        //    List<string> retValue = new List<string>();
        //    try
        //    {
        //        IDataReader dr = ExecuteQuery.ExecuteReader(sqlQry);
        //        while (dr.Read())
        //        {
        //            string name = dr[0].ToString();
        //            retValue.Add(name);
        //        }
        //        dr.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        _log.Log(Level.SEVERE, sql, e);
        //    }
        //    return retValue;
        //}


        //public static List<string> GetSavedQueryNamesForUser(int VAF_UserContact_ID, int VAF_Tab_ID)
        //{
        //    string sqlQry = "SELECT Name FROM VAF_UserSearch WHERE"
        //        + " VAF_UserContact_ID=" + VAF_UserContact_ID + " AND VAF_Tab_ID=" + VAF_Tab_ID + " AND IsActive='Y'"
        //        + " ORDER BY Name";
        //    List<string> retValue = new List<string>();
        //    try
        //    {
        //        IDataReader dr = ExecuteQuery.ExecuteReader(sqlQry);
        //        while (dr.Read())
        //        {
        //            string name = dr[0].ToString();
        //            retValue.Add(name);
        //        }
        //        dr.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        _log.Log(Level.SEVERE, sql, e);
        //    }
        //    return retValue;
        //}


        //public static MUserQuery GetForUser(Context ctx, int VAF_Tab_ID, string name)
        //{
        //    int VAF_UserContact_ID = ctx.GetVAF_UserContact_ID();
        //    if (name == null)
        //        name = "%";
        //    string sqlQry = "SELECT * FROM VAF_UserSearch WHERE"
        //        + " VAF_UserContact_ID=" + VAF_UserContact_ID + " AND VAF_Tab_ID=" + VAF_Tab_ID + " AND"
        //        + " UPPER(Name) LIKE '" + name.ToUpper() + "' AND IsActive='Y'"
        //        + " ORDER BY Name";
        //    MUserQuery retValue = null;
        //    try
        //    {
        //        DataSet ds = ExecuteQuery.ExecuteDataset(sqlQry);
        //        int iTot = ds.Tables[0].Rows.Count;
        //        for (int iRow = 0; iRow < iTot; iRow++)
        //        {
        //            retValue = new MUserQuery(ctx, ds.Tables[0].Rows[iRow], null);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        _log.Log(Level.SEVERE, sql, e);
        //    }
        //    return retValue;
        //}

        //public MUserQueryLine[] GetLines(bool isReload)
        //{
        //    if (_lines != null && !isReload)
        //        return _lines;
        //    List<MUserQueryLine> list = new List<MUserQueryLine>();
        //    string sqlQry = "SELECT * FROM VAF_UserSearchLine WHERE VAF_UserSearch_ID=" + GetVAF_UserSearch_ID() + " ORDER BY SeqNo";

        //    try
        //    {
        //        //pstmt = DataBase.prepareStatement(sqlQry, get_TrxName());
        //        //pstmt.setInt(1, getVAF_UserSearch_ID());
        //        //ResultSet rs = pstmt.executeQuery();
        //        //while (rs.next())
        //        //    list.add(new MUserQueryLine(getCtx(), rs, get_TrxName()));
        //        //rs.close();
        //        //pstmt.close();
        //        //pstmt = null;

        //        DataSet ds = ExecuteQuery.ExecuteDataset(sqlQry);
        //        int iTot = ds.Tables[0].Rows.Count;
        //        for (int iRow = 0; iRow < iTot; iRow++)
        //        {
        //            list.Add(new MUserQueryLine(GetCtx(), ds.Tables[0].Rows[iRow], Get_Trx()));
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        log.Log(Level.SEVERE, sql, e);
        //    }
        //    //
        //    _lines = new MUserQueryLine[list.Count];
        //    _lines = list.ToArray();
        //    return _lines;
        //}

        #endregion

        /// <summary>
        /// Get query lines of a query
        /// </summary>
        /// <returns>Dataset</returns>
        public DataSet GetQueryLines()
        {
            DataSet ds = null;
            string sql = "SELECT KEYNAME,KEYVALUE,OPERATOR AS OPERATORNAME,VALUE1NAME," +
                "VALUE1VALUE,VALUE2NAME,VALUE2VALUE,VAF_USERSEARCHLINE_ID FROM VAF_UserSearchLine WHERE VAF_UserSearch_ID=" +
                GetVAF_UserSearch_ID() + " ORDER BY SeqNo";
            try
            {
                ds = DataBase.DB.ExecuteDataset(sql, null, null);
                if (ds != null)
                {
                    ds.Tables[0].Columns.Add("OPERATOR");
                    DataRow dr;
                    string optrName = "";
                    string optr = "";
                    for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
                    {
                        dr = ds.Tables[0].Rows[iRow];
                        optrName = ds.Tables[0].Rows[iRow]["OPERATORNAME"].ToString();
                        for (int i = 0; i < Query.OPERATORS.Length; i++)
                        {
                            VAdvantage.Model.ValueNamePair pp = Query.OPERATORS[i];
                            if (pp.GetValue().Equals(optrName))
                            {
                                optr = pp.GetName();
                                break;
                            }

                        }
                        dr[8] = optr;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ds != null)
                {
                    ds = null;
                }
                _log.Log(Level.SEVERE, sql, ex);
            }
            return ds;
        }

        /// <summary>
        /// Delete all Lines
        /// </summary>
        /// <returns>bool type true if deleted</returns>
        public bool DeleteLines()
        {
            string sqlQry = "DELETE FROM VAF_UserSearchLine WHERE VAF_UserSearch_ID=" + GetVAF_UserSearch_ID();
            int no = DataBase.DB.ExecuteQuery(sqlQry, null, null);
            log.Info("#" + no);
            //_lines = null;
            return no >= 0;
        }

        /// <summary>
        /// String Representation
        /// </summary>
        /// <returns>info</returns>
        public String ToStringX()
        {
            StringBuilder sb = new StringBuilder("MUserQuery[");
            sb.Append(Get_ID()).Append("-").Append(GetName()).Append("]");
            return sb.ToString();
        }
    }
}
