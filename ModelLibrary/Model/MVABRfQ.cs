﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MRfQ
 * Purpose        : RfQ Model
 * Class Used     : X_VAB_RFQ
 * Chronological    Development
 * Raghunandan     10-Aug.-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
//////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using System.Data.SqlClient;


namespace VAdvantage.Model
{
    public class MVABRfQ : X_VAB_RFQ
    {

        //Cache	
        private static CCache<int, MVABRfQ> s_cache = new CCache<int, MVABRfQ>("VAB_RFQ", 10);

        /// <summary>
        /// Get MRfQ from Cache
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAB_RFQ_ID">ID</param>
        /// <param name="trxName">transction</param>
        /// <returns>MRFQ</returns>
        public static MVABRfQ Get(Ctx ctx, int VAB_RFQ_ID, Trx trxName)
        {
            int key = VAB_RFQ_ID;
            MVABRfQ retValue = (MVABRfQ)s_cache[key];
            if (retValue != null)
            {
                return retValue;
            }
            retValue = new MVABRfQ(ctx, VAB_RFQ_ID, trxName);
            if (retValue.Get_ID() != 0)
            {
                s_cache.Add(key, retValue);
            }
            return retValue;
        }

        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAB_RFQ_ID">ID</param>
        /// <param name="trxName">transaction</param>
        public MVABRfQ(Ctx ctx, int VAB_RFQ_ID, Trx trxName)
            :base(ctx, VAB_RFQ_ID, trxName)
        {
            if (VAB_RFQ_ID == 0)
            {
                //	setVAB_RFQ_Subject_ID (0);
                //	setName (null);
                //	setVAB_Currency_ID (0);	// @$VAB_Currency_ID @
                //	setSalesRep_ID (0);
                //
                SetDateResponse(DateTime.Now);// Commented by Bharat on 15 Jan 2019 for point given by Puneet
                SetDateWorkStart(DateTime.Now);// Convert.ToDateTime(System.currentTimeMillis()));
                SetIsInvitedVendorsOnly(false);
                SetQuoteType(QUOTETYPE_QuoteSelectedLines);
                SetIsQuoteAllQty(false);
                SetIsQuoteTotalAmt(false);
                SetIsRfQResponseAccepted(true);
                SetIsSelfService(true);
                SetProcessed(false);
            }
        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">Ctx</param>
        /// <param name="dr">dataroe</param>
        /// <param name="trxName">transaction</param>
        public MVABRfQ(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {

        }

        /// <summary>
        /// Get active Lines
        /// </summary>
        /// <returns>array of lines</returns>
        public MVABRfQLine[] GetLines()
        {
            List<MVABRfQLine> list = new List<MVABRfQLine>();
            String sql = "SELECT * FROM VAB_RFQLine "
                + "WHERE VAB_RFQ_ID=@param1 AND IsActive='Y' "
                + "ORDER BY Line";
            DataTable dt = null;
            IDataReader idr = null;
            SqlParameter[] param = null;
            try
            {
                param = new SqlParameter[1];
                param[0] = new SqlParameter("@param1", GetVAB_RFQ_ID());
                idr = DataBase.DB.ExecuteReader(sql, param, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)// while (dr.next())
                {
                    list.Add(new MVABRfQLine(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch (Exception e)
            {
                log.Log(VAdvantage.Logging.Level.SEVERE, sql, e);
            }
            finally
            {
                dt = null;
                idr.Close();
            }

            MVABRfQLine[] retValue = new MVABRfQLine[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Get RfQ Responses
        /// </summary>
        /// <param name="activeOnly">active responses only</param>
        /// <param name="completedOnly">complete responses only</param>
        /// <returns>array of lines</returns>
        public MVABRFQReply[] GetResponses(bool activeOnly, bool completedOnly)
        {
            List<MVABRFQReply> list = new List<MVABRFQReply>();
            String sql = "SELECT * FROM VAB_RFQReply "
                + "WHERE VAB_RFQ_ID=@param1";
            if (activeOnly)
            {
                sql += " AND IsActive='Y'";
            }
            if (completedOnly)
            {
                sql += " AND IsComplete='Y'";
            }
            sql += " ORDER BY Price";
            DataTable dt = null;
            IDataReader idr = null;
            SqlParameter[] param = null;
            try
            {
                param = new SqlParameter[1];
                param[0] = new SqlParameter("@param1", GetVAB_RFQ_ID());
                idr = DataBase.DB.ExecuteReader(sql, param, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)// while (dr.next())
                {
                    list.Add(new MVABRFQReply(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch 
            {
                //log.log(Level.SEVERE, sql, e);
            }
            finally
            {
                dt = null;
                idr.Close();
            }

            MVABRFQReply[] retValue = new MVABRFQReply[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// String Representation
        /// </summary>
        /// <returns>info</returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MRfQ[");
            sb.Append(Get_ID()).Append(",Name=").Append(GetName())
                .Append(",QuoteType=").Append(GetQuoteType())
                .Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Is Quote Total Amt Only
        /// </summary>
        /// <returns>true if total amout only</returns>
        public bool IsQuoteTotalAmtOnly()
        {
            return QUOTETYPE_QuoteTotalOnly.Equals(GetQuoteType());
        }

        /// <summary>
        /// Is Quote Selected Lines
        /// </summary>
        /// <returns>true if quote selected lines</returns>
        public bool IsQuoteSelectedLines()
        {
            return QUOTETYPE_QuoteSelectedLines.Equals(GetQuoteType());
        }

        /// <summary>
        /// Is Quote All Lines
        /// </summary>
        /// <returns>true if quote selected lines</returns>
        public bool IsQuoteAllLines()
        {
            return QUOTETYPE_QuoteAllLines.Equals(GetQuoteType());
        }

        /// <summary>
        /// Is "Quote Total Amt Only" Valid
        /// </summary>
        /// <returns>null or error message</returns>
        public String CheckQuoteTotalAmtOnly()
        {
            if (!IsQuoteTotalAmtOnly())
            {
                return null;
            }
            //	Need to check Line Qty
            MVABRfQLine[] lines = GetLines();
            for (int i = 0; i < lines.Length; i++)
            {
                MVABRfQLine line = lines[i];
                MVABRfQLineQty[] qtys = line.GetQtys();
                if (qtys.Length > 1)
                {
                    log.Warning("isQuoteTotalAmtOnlyValid - #" + qtys.Length + " - " + line);
                   
                    String msg = "@Line@ " + line.GetLine()
                        + ": #@VAB_RFQLine_Qty@=" + qtys.Length + " - @IsQuoteTotalAmt@";
                    return msg;
                }
            }
            return null;
        }

        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">newRecord new</param>
        /// <returns>true</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            //	Calculate Complete Date (also used to verify)
            if (GetDateWorkStart() != null && GetDeliveryDays() != 0)
            {
                SetDateWorkComplete(TimeUtil.AddDays(GetDateWorkStart(), GetDeliveryDays()));
            }
            //	Calculate Delivery Days
            else if (GetDateWorkStart() != null && GetDeliveryDays() == 0 && GetDateWorkComplete() != null)
            {
                SetDeliveryDays(TimeUtil.GetDaysBetween(GetDateWorkStart(), GetDateWorkComplete()));
            }
            //	Calculate Start Date
            else if (GetDateWorkStart() == null && GetDeliveryDays() != 0 && GetDateWorkComplete() != null)
            {
                SetDateWorkStart(TimeUtil.AddDays(GetDateWorkComplete(), GetDeliveryDays() * -1));
            }
            return true;
        }

        //Added by Vivek 23-12-2015
        public new void SetProcessed(bool processed)
        {
            base.SetProcessed(processed);
            //if (Get_ID() == 0)
            //    return;
            String setline = "SET Processed='"
                + (processed ? "Y" : "N")
                + "' WHERE VAB_RFQ_ID =" + GetVAB_RFQ_ID();
            int noLine = DataBase.DB.ExecuteQuery("UPDATE VAB_RFQLine " + setline, null, Get_Trx());

            string setQty = @"UPDATE VAB_RFQLINE_QTYVAB_SalesRegionState
 SET PROCESSED ='Y' WHERE VAB_RFQLINE_QTY_ID IN (SELECT LQTY.VAB_RFQLINE_QTY_ID FROM VAB_RFQLINE_QTY LQTY INNER JOIN VAB_RFQLINE LINE
                    ON LINE.VAB_RFQLINE_ID=LQTY.VAB_RFQLINE_ID INNER JOIN VAB_RFQ RFQ ON RFQ.VAB_RFQ_ID = LINE.VAB_RFQ_ID WHERE RFQ.VAB_RFQ_ID =" + GetVAB_RFQ_ID() + ")";
            int noQty = DataBase.DB.ExecuteQuery(setQty, null, Get_Trx());

            log.Fine(processed + " - Lines=" + noLine + ", Qty=" + noQty);
        }
    }
}