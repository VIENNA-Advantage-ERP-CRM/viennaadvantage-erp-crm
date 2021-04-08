﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVAPAEvaluate
 * Purpose        : Performance Measure
 * Class Used     : X_VAPA_Evaluate
 * Chronological    Development
 * Raghunandan     17-Jun-2009
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
using System.Data.SqlClient;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;
namespace VAdvantage.Model
{
    public class MVAPAEvaluate : X_VAPA_Evaluate
    {
        /**
	 * 	Get MVAPAEvaluate from Cache
	 *	@param ctx context
	 *	@param VAPA_Evaluate_ID id
	 *	@return MVAPAEvaluate
	 */
        public static MVAPAEvaluate Get(Ctx ctx, int VAPA_Evaluate_ID)
        {
            int key = VAPA_Evaluate_ID;
            MVAPAEvaluate retValue = (MVAPAEvaluate)_cache[key];
            if (retValue != null)
                return retValue;
            retValue = new MVAPAEvaluate(ctx, VAPA_Evaluate_ID, null);
            if (retValue.Get_ID() != 0)
                _cache.Add(key, retValue);
            return retValue;
        }

        /**	Cache						*/
        private static CCache<int, MVAPAEvaluate> _cache
            = new CCache<int, MVAPAEvaluate>("VAPA_Evaluate", 10);

        /**
         * 	Standard Constructor
         *	@param ctx context
         *	@param VAPA_Evaluate_ID id
         *	@param trxName trx
         */
        public MVAPAEvaluate(Ctx ctx, int VAPA_Evaluate_ID, Trx trxName) :
            base(ctx, VAPA_Evaluate_ID, trxName)
        {
        }

        /**
         * 	Load Constructor
         *	@param ctx context
         *	@param rs result Set
         *	@param trxName trx
         */
        public MVAPAEvaluate(Ctx ctx, DataRow dr, Trx trxName) :
            base(ctx, dr, trxName)
        {
        }


        /**
         * 	String Representation
         *	@return info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MVAPAEvaluate[");
            sb.Append(Get_ID()).Append("-").Append(GetName()).Append("]");
            return sb.ToString();
        }	//	toString

        /**
         * 	Before Save
         *	@param newRecord new
         *	@return true
         */
        protected override Boolean BeforeSave(Boolean newRecord)
        {
            if (MEASURETYPE_Calculated.Equals(GetMeasureType())
                && GetVAPA_EvaluateCalc_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAPA_EvaluateCalc_ID"));
                return false;
            }
            else if (MEASURETYPE_Ratio.Equals(GetMeasureType())
                && GetVAPA_Ratio_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAPA_Ratio_ID"));
                return false;
            }
            else if (MEASURETYPE_UserDefined.Equals(GetMeasureType())
                && (GetCalculationClass() == null || GetCalculationClass().Length == 0))
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "CalculationClass"));
                return false;
            }
            else if (MEASURETYPE_Request.Equals(GetMeasureType())
                && GetVAR_Req_Type_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAR_Req_Type_ID"));
                return false;
            }
            else if (MEASURETYPE_Project.Equals(GetMeasureType())
                && GetVAB_ProjectType_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAB_ProjectType_ID"));
                return false;
            }
            return true;
        }	//	beforeSave

        /**
         * 	After Save
         *	@param newRecord new
         *	@param success success
         *	@return succes
         */
        protected override Boolean AfterSave(Boolean newRecord, Boolean success)
        {
            //	Update Goals with Manual Measure
            if (success && MEASURETYPE_Manual.Equals(GetMeasureType()))
                UpdateManualGoals();

            return success;
        }	//	afterSave

        /**
         * 	Update/save Goals
         * 	@return true if updated
         */
        public Boolean UpdateGoals()
        {
            String mt = GetMeasureType();
            try
            {
                if (MEASURETYPE_Manual.Equals(mt))
                    return UpdateManualGoals();
                else if (MEASURETYPE_Achievements.Equals(mt))
                    return UpdateAchievementGoals();
                else if (MEASURETYPE_Calculated.Equals(mt))
                    return UpdateCalculatedGoals();
                else if (MEASURETYPE_Ratio.Equals(mt))
                    return UpdateRatios();
                else if (MEASURETYPE_Request.Equals(mt))
                    return UpdateRequests();
                else if (MEASURETYPE_Project.Equals(mt))
                    return UpdateProjects();
                //	Projects
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, "MeasureType=" + mt, e);
            }
            return false;
        }	//	updateGoals

        /**
         * 	Update/save Manual Goals
         * 	@return true if updated
         */
        private Boolean UpdateManualGoals()
        {
            if (!MEASURETYPE_Manual.Equals(GetMeasureType()))
                return false;
            MVAPATarget[] goals = MVAPATarget.GetMeasureGoals(GetCtx(), GetVAPA_Evaluate_ID());
            for (int i = 0; i < goals.Length; i++)
            {
                MVAPATarget goal = goals[i];
                goal.SetMeasureActual(GetManualActual());
                goal.Save();
            }
            return true;
        }	//	updateManualGoals

        /**
         * 	Update/save Goals with Achievement
         * 	@return true if updated
         */
        private Boolean UpdateAchievementGoals()
        {
            if (!MEASURETYPE_Achievements.Equals(GetMeasureType()))
                return false;
            DateTime today = DateTime.Now;
            MVAPATarget[] goals = MVAPATarget.GetMeasureGoals(GetCtx(), GetVAPA_Evaluate_ID());
            for (int i = 0; i < goals.Length; i++)
            {
                MVAPATarget goal = goals[i];
                String MeasureScope = goal.GetMeasureScope();
                String trunc = TimeUtil.TRUNC_DAY;
                if (MVAPATarget.MEASUREDISPLAY_Year.Equals(MeasureScope))
                    trunc = TimeUtil.TRUNVAB_YEAR;
                else if (MVAPATarget.MEASUREDISPLAY_Quarter.Equals(MeasureScope))
                    trunc = TimeUtil.TRUNC_QUARTER;
                else if (MVAPATarget.MEASUREDISPLAY_Month.Equals(MeasureScope))
                    trunc = TimeUtil.TRUNC_MONTH;
                else if (MVAPATarget.MEASUREDISPLAY_Week.Equals(MeasureScope))
                    trunc = TimeUtil.TRUNC_WEEK;
                DateTime compare = TimeUtil.Trunc(today, trunc);
                //
                MVAPAAccomplishment[] achievements = MVAPAAccomplishment.GetOfMeasure(GetCtx(), GetVAPA_Evaluate_ID());
                Decimal ManualActual = Env.ZERO;
                for (int j = 0; j < achievements.Length; j++)
                {
                    MVAPAAccomplishment achievement = achievements[j];
                    if (achievement.IsAchieved() && achievement.GetDateDoc() != null)
                    {
                        DateTime ach = TimeUtil.Trunc(achievement.GetDateDoc(), trunc);
                        if (compare.Equals(ach))
                            ManualActual = Decimal.Add(ManualActual, achievement.GetManualActual());
                    }
                }
                goal.SetMeasureActual(ManualActual);
                goal.Save();
            }
            return true;
        }

        /**
         * 	Update/save Goals with Calculation
         * 	@return true if updated
         */
        private Boolean UpdateCalculatedGoals()
        {
            if (!MEASURETYPE_Calculated.Equals(GetMeasureType()))
                return false;
            MVAPATarget[] goals = MVAPATarget.GetMeasureGoals(GetCtx(), GetVAPA_Evaluate_ID());
            for (int i = 0; i < goals.Length; i++)
            {
                MVAPATarget goal = goals[i];
                //	Find Role
                MVAFRole role = null;
                if (goal.GetVAF_Role_ID() != 0)
                    role = MVAFRole.Get(GetCtx(), goal.GetVAF_Role_ID());
                else if (goal.GetVAF_UserContact_ID() != 0)
                {
                    MVAFUserContact user = MVAFUserContact.Get(GetCtx(), goal.GetVAF_UserContact_ID());
                    MVAFRole[] roles = user.GetRoles(goal.GetVAF_Org_ID());
                    if (roles.Length > 0)
                        role = roles[0];
                }
                if (role == null)
                    role = MVAFRole.GetDefault(GetCtx(), false);	//	could result in wrong data
                //
                MVAPAEvaluateCalc mc = MVAPAEvaluateCalc.Get(GetCtx(), GetVAPA_EvaluateCalc_ID());
                if (mc == null || mc.Get_ID() == 0 || mc.Get_ID() != GetVAPA_EvaluateCalc_ID())
                {
                    log.Log(Level.SEVERE, "Not found VAPA_EvaluateCalc_ID=" + GetVAPA_EvaluateCalc_ID());
                    return false;
                }

                Decimal? ManualActual = null;
                String sql = mc.GetSqlPI(goal.GetRestrictions(false),
                    goal.GetMeasureScope(), GetMeasureDataType(), null, role);
                IDataReader idr = null;
                try		//	SQL statement could be wrong
                {
                    idr = DataBase.DB.ExecuteReader(sql, null, null);
                    if (idr.Read())
                        ManualActual = Utility.Util.GetValueOfDecimal(idr[0]);
                    idr.Close();
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Log (Level.SEVERE, sql, e);
                }

                //	SQL may return no rows or null
                if (ManualActual == null)
                {
                    ManualActual = Env.ZERO;
                    log.Fine("No Value = " + sql);
                }
                goal.SetMeasureActual(ManualActual);
                goal.Save();
            }
            return true;
        }

        /**
         * 	Update/save Goals with Ratios
         * 	@return true if updated
         */
        private Boolean UpdateRatios()
        {
            if (!MEASURETYPE_Ratio.Equals(GetMeasureType()))
                return false;
            return false;
        }

        /**
         * 	Update/save Goals with Requests
         * 	@return true if updated
         */
        private Boolean UpdateRequests()
        {
            if (!MEASURETYPE_Request.Equals(GetMeasureType())
                || GetVAR_Req_Type_ID() == 0)
                return false;
            MVAPATarget[] goals = MVAPATarget.GetMeasureGoals(GetCtx(), GetVAPA_Evaluate_ID());
            for (int i = 0; i < goals.Length; i++)
            {
                MVAPATarget goal = goals[i];
                //	Find Role
                MVAFRole role = null;
                if (goal.GetVAF_Role_ID() != 0)
                    role = MVAFRole.Get(GetCtx(), goal.GetVAF_Role_ID());
                else if (goal.GetVAF_UserContact_ID() != 0)
                {
                    MVAFUserContact user = MVAFUserContact.Get(GetCtx(), goal.GetVAF_UserContact_ID());
                    MVAFRole[] roles = user.GetRoles(goal.GetVAF_Org_ID());
                    if (roles.Length > 0)
                        role = roles[0];
                }
                if (role == null)
                    role = MVAFRole.GetDefault(GetCtx(), false);	//	could result in wrong data
                //
                Decimal? ManualActual = null;
                MVARRequestType rt = MVARRequestType.Get(GetCtx(), GetVAR_Req_Type_ID());
                String sql = rt.GetSqlPI(goal.GetRestrictions(false),
                    goal.GetMeasureScope(), GetMeasureDataType(), null, role);
                //PreparedStatement pstmt = null;
                IDataReader idr = null;
                try		//	SQL statement could be wrong
                {
                    idr = DataBase.DB.ExecuteReader(sql, null, null);
                    if (idr.Read())
                    {
                        ManualActual = Utility.Util.GetValueOfDecimal(idr[0]);
                    }
                    idr.Close();
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Log (Level.SEVERE, sql, e);
                }

                //	SQL may return no rows or null
                if (ManualActual == null)
                {
                    ManualActual = Env.ZERO;
                    log.Fine("No Value = " + sql);
                }
                goal.SetMeasureActual(ManualActual);
                goal.Save();
            }
            return true;
        }

        /**
         * 	Update/save Goals with Projects
         * 	@return true if updated
         */
        private Boolean UpdateProjects()
        {
            if (!MEASURETYPE_Project.Equals(GetMeasureType())
                || GetVAB_ProjectType_ID() == 0)
                return false;
            MVAPATarget[] goals = MVAPATarget.GetMeasureGoals(GetCtx(), GetVAPA_Evaluate_ID());
            for (int i = 0; i < goals.Length; i++)
            {
                MVAPATarget goal = goals[i];
                //	Find Role
                MVAFRole role = null;
                if (goal.GetVAF_Role_ID() != 0)
                    role = MVAFRole.Get(GetCtx(), goal.GetVAF_Role_ID());
                else if (goal.GetVAF_UserContact_ID() != 0)
                {
                    MVAFUserContact user = MVAFUserContact.Get(GetCtx(), goal.GetVAF_UserContact_ID());
                    MVAFRole[] roles = user.GetRoles(goal.GetVAF_Org_ID());
                    if (roles.Length > 0)
                        role = roles[0];
                }
                if (role == null)
                    role = MVAFRole.GetDefault(GetCtx(), false);	//	could result in wrong data
                //
                Decimal? ManualActual = null;
                MVABProjectType pt = MVABProjectType.Get(GetCtx(), GetVAB_ProjectType_ID());
                String sql = pt.GetSqlPI(goal.GetRestrictions(false),
                    goal.GetMeasureScope(), GetMeasureDataType(), null, role);
                IDataReader idr = null;
                try		//	SQL statement could be wrong
                {

                    idr = DataBase.DB.ExecuteReader(sql, null, null);
                    if (idr.Read())
                        ManualActual = Utility.Util.GetValueOfDecimal(idr[0]);
                    idr.Close();
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Log (Level.SEVERE, sql, e);
                }
                //	SQL may return no rows or null
                if (ManualActual == null)
                {
                    ManualActual = Env.ZERO;
                    log.Fine("No Value = " + sql);
                }
                goal.SetMeasureActual(ManualActual);
                goal.Save();
            }
            return true;
        }
    }
}