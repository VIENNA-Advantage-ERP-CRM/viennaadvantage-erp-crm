﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MAcctProcessor
 * Purpose        : Accounting Processor Model
 * Class Used     : X_VAB_AccountHanlder and ViennaProcessor
 * Chronological    Development
 * Raghunandan     07-Jan-2010
  ******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.Classes;
using System.Data.SqlClient;
using System.Data;
using VAdvantage.Logging;
using VAdvantage.Process;
using VAdvantage.Utility;

namespace VAdvantage.Model
{
    public class MVABAccountHanlder : X_VAB_AccountHanlder, ViennaProcessor
    {
        //Static Logger	
        private static VLogger _log = VLogger.GetVLogger(typeof(MVABAccountHanlder).FullName);

        /// <summary>
        /// Get Active
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>active processors</returns>
        public static MVABAccountHanlder[] GetActive(Ctx ctx)
        {
            List<MVABAccountHanlder> list = new List<MVABAccountHanlder>();
            String sql = "SELECT * FROM VAB_AccountHanlder WHERE IsActive='Y'";
            IDataReader idr = null;

            //Changed By Karan.....
            //DataSet pstmt = null;
            string scheduleIP = null;
            try
            {

                string machineIP = System.Net.Dns.GetHostEntry(Environment.MachineName).AddressList[0].ToString();


               idr = DataBase.DB.ExecuteReader(sql, null, null);
                while (idr.Read())
                {
                    scheduleIP = Util.GetValueOfString(DB.ExecuteScalar(@"SELECT RunOnlyOnIP FROM VAF_Plan WHERE 
                                                       VAF_Plan_ID = (SELECT VAF_Plan_ID FROM VAB_AccountHanlder WHERE VAB_AccountHanlder_ID =" + idr["VAB_AccountHanlder_ID"] + " )"));

                    //list.Add(new MAcctProcessor(ctx, idr, null));

                      if (string.IsNullOrEmpty(scheduleIP) || machineIP.Contains(scheduleIP))
                      {
                          list.Add(new MVABAccountHanlder(new Ctx(), idr, null));
                      }

                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null) { idr.Close(); }
                _log.Log(Level.SEVERE, "GetActive", e);
            }

            MVABAccountHanlder[] retValue = new MVABAccountHanlder[list.Count];
            retValue = list.ToArray();
            return retValue;
        }




        /// <summary>
        /// Standard Construvtor
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="VAB_AccountHanlder_ID"></param>
        /// <param name="trxName"></param>
        public MVABAccountHanlder(Ctx ctx, int VAB_AccountHanlder_ID, Trx trxName)
            : base(ctx, VAB_AccountHanlder_ID, trxName)
        {
            if (VAB_AccountHanlder_ID == 0)
            {
                //	setName (null);
                //	setSupervisor_ID (0);
                SetFrequencyType(FREQUENCYTYPE_Hour);
                SetFrequency(1);
                SetKeepLogDays(7);	// 7
            }
        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="idr"></param>
        /// <param name="trxName"></param>
        public MVABAccountHanlder(Ctx ctx, IDataReader idr, Trx trxName)
            : base(ctx, idr, trxName)
        {

        }

        /// <summary>
        /// Parent Constructor
        /// </summary>
        /// <param name="client">parent</param>
        /// <param name="Supervisor_ID">admin</param>
        public MVABAccountHanlder(MVAFClient client, int Supervisor_ID)
            : this(client.GetCtx(), 0, client.Get_TrxName())
        {
            SetClientOrg(client);
            SetName(client.GetName() + " - "
                + Msg.Translate(GetCtx(), "VAB_AccountHanlder_ID"));
            SetSupervisor_ID(Supervisor_ID);
        }



        /// <summary>
        /// Get Server ID
        /// </summary>
        /// <returns>id</returns>
        public String GetServerID()
        {
            return "AcctProcessor" + Get_ID();
        }

        /// <summary>
        /// Get Date Next Run
        /// </summary>
        /// <param name="requery">requery</param>
        /// <returns>date next run</returns>
        public DateTime? GetDateNextRun(bool requery)
        {
            if (requery)
            {
                Load(Get_TrxName());
            }
            return GetDateNextRun();
        }

        /// <summary>
        /// Get Logs
        /// </summary>
        /// <returns>logs</returns>
        public ViennaProcessorLog[] GetLogs()
        {
            List<MVABAccountHanlderLog> list = new List<MVABAccountHanlderLog>();
            String sql = "SELECT * "
                + "FROM VAB_AccountHanlderLog "
                + "WHERE VAB_AccountHanlder_ID=@VAB_AccountHanlder_ID"
                + "ORDER BY Created DESC";
            IDataReader idr = null;
            try
            {
                SqlParameter[] param = new SqlParameter[1];
                param[0] = new SqlParameter("@VAB_AccountHanlder_ID", GetVAB_AccountHanlder_ID());
                while (idr.Read())
                {
                    list.Add(new MVABAccountHanlderLog(GetCtx(), idr, Get_TrxName()));
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }

            MVABAccountHanlderLog[] retValue = new MVABAccountHanlderLog[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Delete old Request Log
        /// </summary>
        /// <returns>number of records</returns>
        public int DeleteLog()
        {
            if (GetKeepLogDays() < 1)
            {
                return 0;
            }
            String sql = "DELETE FROM VAB_AccountHanlderLog "
                + "WHERE VAB_AccountHanlder_ID=" + GetVAB_AccountHanlder_ID()
                //jz + " AND (Created+" + getKeepLogDays() + ") < SysDate";
                + " AND addDays(Created," + GetKeepLogDays() + ") < SysDate";
            int no = DataBase.DB.ExecuteQuery(sql, null, Get_TrxName());
            return no;
        }
    }
}