﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVAPATarget
 * Purpose        : Performance Goal
 * Class Used     : X_VAPA_Target
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
    public class MVAPATarget : X_VAPA_Target
    {
        /**
         * 	Get User Goals
         *	@param ctx context
         *	@param VAF_UserContact_ID user
         *	@return array of goals
         */
        public static MVAPATarget[] GetUserGoals(Ctx ctx, int VAF_UserContact_ID)
        {
            if (VAF_UserContact_ID < 0)
                return GetTestGoals(ctx);
            List<MVAPATarget> list = new List<MVAPATarget>();
            String sql = "SELECT * FROM VAPA_Target g "
                + "WHERE IsActive='Y'"
                + " AND VAF_Client_ID=@ADClientID"		//	#1
                + " AND ((VAF_UserContact_ID IS NULL AND VAF_Role_ID IS NULL)"
                    + " OR VAF_UserContact_ID=@ADUserID"	//	#2
                    + " OR EXISTS (SELECT * FROM VAF_UserContact_Roles ur "
                        + "WHERE g.VAF_UserContact_ID=ur.VAF_UserContact_ID AND g.VAF_Role_ID=ur.VAF_Role_ID AND ur.IsActive='Y')) "
                + "ORDER BY SeqNo";
            DataTable dt;
            IDataReader idr = null;
            try
            {
                SqlParameter[] param = new SqlParameter[2];
                param[0] = new SqlParameter("@ADClientID", ctx.GetVAF_Client_ID());
                param[1] = new SqlParameter("@ADUserID", VAF_UserContact_ID);

                idr = DataBase.DB.ExecuteReader(sql, null, null);

                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    MVAPATarget goal = new MVAPATarget(ctx, dr, null);
                    goal.UpdateGoal(false);
                    list.Add(goal);
                }


            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                _log.Log (Level.SEVERE, sql, e);
            }
            finally { dt = null; }

            MVAPATarget[] retValue = new MVAPATarget[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /**
         * 	Get Accessible Goals
         *	@param ctx context
         *	@return array of goals
         */
        public static MVAPATarget[] GetGoals(Ctx ctx)
        {
            List<MVAPATarget> list = new List<MVAPATarget>();
            String sql = "SELECT * FROM VAPA_Target WHERE IsActive='Y' "
                + "ORDER BY SeqNo";
            sql = MVAFRole.GetDefault(ctx, false).AddAccessSQL(sql, "VAPA_Target",
                false, true);	//	RW to restrict Access
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    MVAPATarget goal = new MVAPATarget(ctx, dr, null);
                    goal.UpdateGoal(false);
                    list.Add(goal);

                }


            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                _log.Log (Level.SEVERE, sql, e);
            }
            finally
            {
                dt = null;
            }
            MVAPATarget[] retValue = new MVAPATarget[list.Count];
            retValue = list.ToArray();
            return retValue;
        }


        /**
         * 	Create Test Goals
         *	@param ctx context
         *	@return array of goals
         */
        public static MVAPATarget[] GetTestGoals(Ctx ctx)
        {
            MVAPATarget[] retValue = new MVAPATarget[4];
            retValue[0] = new MVAPATarget(ctx, "Test 1", "Description 1", new Decimal(1000), null);
            retValue[0].SetMeasureActual(new Decimal(200));
            retValue[1] = new MVAPATarget(ctx, "Test 2", "Description 2", new Decimal(1000), null);
            retValue[1].SetMeasureActual(new Decimal(900));
            retValue[2] = new MVAPATarget(ctx, "Test 3", "Description 3", new Decimal(1000), null);
            retValue[2].SetMeasureActual(new Decimal(1200));
            retValue[3] = new MVAPATarget(ctx, "Test 4", "Description 4", new Decimal(1000), null);
            retValue[3].SetMeasureActual(new Decimal(3200));
            return retValue;
        }

        /**
         * 	Get Goals with Measure
         *	@param ctx context
         *	@param VAPA_Evaluate_ID measure
         *	@return goals
         */
        public static MVAPATarget[] GetMeasureGoals(Ctx ctx, int VAPA_Evaluate_ID)
        {
            List<MVAPATarget> list = new List<MVAPATarget>();
            String sql = "SELECT * FROM VAPA_Target WHERE IsActive='Y' AND VAPA_Evaluate_ID= " + VAPA_Evaluate_ID
                + " ORDER BY SeqNo";
            DataTable dt;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVAPATarget(ctx, dr, null));
                }

            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                _log.Log (Level.SEVERE, sql, e);
            }
            finally { dt = null; }

            MVAPATarget[] retValue = new MVAPATarget[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /**
         * 	Get Multiplier from Scope to Display
         *	@param goal goal
         *	@return null if error or multiplier
         */
        public static Decimal? GetMultiplier(MVAPATarget goal)
        {
            String MeasureScope = goal.GetMeasureScope();
            String MeasureDisplay = goal.GetMeasureDisplay();
            if (MeasureDisplay == null
                || MeasureScope.Equals(MeasureDisplay))
                return Env.ONE;		//	1:1

            if (MeasureScope.Equals(MEASURESCOPE_Total)
                || MeasureDisplay.Equals(MEASUREDISPLAY_Total))
                return null;		//	Error

            Decimal? Multiplier = null;
            if (MeasureScope.Equals(MEASURESCOPE_Year))
            {
                if (MeasureDisplay.Equals(MEASUREDISPLAY_Quarter))
                    Multiplier = new Decimal(1.0 / 4.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Month))
                    Multiplier = new Decimal(1.0 / 12.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Week))
                    Multiplier = new Decimal(1.0 / 52.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Day))
                    Multiplier = new Decimal(1.0 / 364.0);
            }
            else if (MeasureScope.Equals(MEASURESCOPE_Quarter))
            {
                if (MeasureDisplay.Equals(MEASUREDISPLAY_Year))
                    Multiplier = new Decimal(4.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Month))
                    Multiplier = new Decimal(1.0 / 3.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Week))
                    Multiplier = new Decimal(1.0 / 13.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Day))
                    Multiplier = new Decimal(1.0 / 91.0);
            }
            else if (MeasureScope.Equals(MEASURESCOPE_Month))
            {
                if (MeasureDisplay.Equals(MEASUREDISPLAY_Year))
                    Multiplier = new Decimal(12.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Quarter))
                    Multiplier = new Decimal(3.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Week))
                    Multiplier = new Decimal(1.0 / 4.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Day))
                    Multiplier = new Decimal(1.0 / 30.0);
            }
            else if (MeasureScope.Equals(MEASURESCOPE_Week))
            {
                if (MeasureDisplay.Equals(MEASUREDISPLAY_Year))
                    Multiplier = new Decimal(52.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Quarter))
                    Multiplier = new Decimal(13.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Month))
                    Multiplier = new Decimal(4.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Day))
                    Multiplier = new Decimal(1.0 / 7.0);
            }
            else if (MeasureScope.Equals(MEASURESCOPE_Day))
            {
                if (MeasureDisplay.Equals(MEASUREDISPLAY_Year))
                    Multiplier = new Decimal(364.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Quarter))
                    Multiplier = new Decimal(91.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Month))
                    Multiplier = new Decimal(30.0);
                else if (MeasureDisplay.Equals(MEASUREDISPLAY_Week))
                    Multiplier = new Decimal(7.0);
            }
            return Multiplier;
        }

        /**	Logger	*/
        private static VLogger _log = VLogger.GetVLogger(typeof(MVAPATarget).FullName);

        /**************************************************************************
         * 	Standard Constructor
         *	@param ctx context
         *	@param VAPA_Target_ID id
         *	@param trxName trx
         */
        public MVAPATarget(Ctx ctx, int VAPA_Target_ID, Trx trxName) :
            base(ctx, VAPA_Target_ID, trxName)
        {
            //super ();
            if (VAPA_Target_ID == 0)
            {
                //	SetName (null);
                //	SetVAF_UserContact_ID (0);
                //	SetVAPA_Color_ID (0);
                SetSeqNo(0);
                SetIsSummary(false);
                SetMeasureScope(MEASUREDISPLAY_Year);
                SetGoalPerformance(Env.ZERO);
                SetRelativeWeight(Env.ONE);
                SetMeasureTarget(Env.ZERO);
                SetMeasureActual(Env.ZERO);
            }
        }

        /**
         * 	Load Constructor
         *	@param ctx context
         *	@param dr result Set
         *	@param trxName trx
         */
        public MVAPATarget(Ctx ctx, DataRow dr, Trx trxName) :
            base(ctx, dr, trxName)
        {

        }

        /**
         * 	Base Constructor
         *	@param ctx context
         *	@param Name Name
         *	@param Description Decsription
         *	@param MeasureTarGet tarGet
         *	@param trxName trx
         */
        public MVAPATarget(Ctx ctx, String Name, String Description,
            Decimal MeasureTarGet, Trx trxName) :
            base(ctx, 0, trxName)
        {

            SetName(Name);
            SetDescription(Description);
            SetMeasureTarget(MeasureTarGet);
        }


        /** Restrictions					*/
        private MVAPATargetRestriction[] _restrictions = null;

        /**
         * 	Get Restriction Lines
         *	@param reload reload data
         *	@return array of lines
         */
        public MVAPATargetRestriction[] GetRestrictions(Boolean reload)
        {
            if (_restrictions != null && !reload)
                return _restrictions;
            List<MVAPATargetRestriction> list = new List<MVAPATargetRestriction>();
            //
            String sql = "SELECT * FROM VAPA_TargetRestriction "
                + "WHERE VAPA_Target_ID=@VAPA_Target_ID AND IsActive='Y' "
                + "ORDER BY Org_ID, VAB_BusinessPartner_ID, VAM_Product_ID";
            DataTable dt;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVAPATargetRestriction(GetCtx(), dr, Get_TrxName()));
                }

            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
               log.Log (Level.SEVERE, sql, e);
            }
            finally
            {
                dt = null;
            }
            //
            _restrictions = new MVAPATargetRestriction[list.Count];
            _restrictions = list.ToArray();
            return _restrictions;
        }

        /**
         * 	Get Measure
         *	@return measure or null
         */
        public MVAPAEvaluate GetMeasure()
        {
            if (GetVAPA_Evaluate_ID() != 0)
                return MVAPAEvaluate.Get(GetCtx(), GetVAPA_Evaluate_ID());
            return null;
        }


        /**************************************************************************
         * 	Update/save Goals for the same measure
         * 	@param force force to update goal (default once per day)
         * 	@return true if updated
         */
        public Boolean UpdateGoal(Boolean force)
        {
           log.Config("Force=" + force);
            MVAPAEvaluate measure = MVAPAEvaluate.Get(GetCtx(), GetVAPA_Evaluate_ID());
            if (force
                || GetDateLastRun() == null
                || !TimeUtil.IsSameHour(GetDateLastRun(), null))
            {
                if (measure.UpdateGoals())		//	saves
                {
                    Load(Get_ID(), Get_TrxName());
                    return true;
                }
            }
            return false;
        }

        /**
         * 	Set Measure Actual
         *	@param MeasureActual actual
         */
        public new void SetMeasureActual(Decimal? MeasureActual)
        {
            if (MeasureActual == null)
                return;
            base.SetMeasureActual((Decimal)MeasureActual);
            SetDateLastRun(DateTime.Now);
            SetGoalPerformance();
        }

        /**
         * 	Calculate Performance Goal as multiplier
         */
        public void SetGoalPerformance()
        {
            Decimal MeasureTarGet = GetMeasureTarget();
            Decimal MeasureActual = GetMeasureActual();
            Decimal GoalPerformance = Env.ZERO;
            if (Env.Signum(MeasureTarGet) != 0)
            {
                //GoalPerformance
                GoalPerformance = Decimal.Round(GoalPerformance, 6, MidpointRounding.AwayFromZero);
            }

            base.SetGoalPerformance(GoalPerformance);
        }

        /**
         * 	Get Goal Performance as Double
         *	@return performance as multipier
         */
        public double GetGoalPerformanceDouble()
        {
            Decimal bd = GetGoalPerformance();
            return Decimal.ToDouble(bd);
        }

        /**
         * 	Get Goal Performance in Percent
         *	@return performance in percent
         */
        public int GetPercent()
        {
            Decimal bd = Decimal.Multiply(GetGoalPerformance(), Env.ONEHUNDRED);
            return Decimal.ToInt32(bd);
        }

        /**
         * 	Get Color
         *	@return color - white if no tarGet
         */
        //public Color GetColor()
        //{
        //    if (Env.Signum(GetMeasureTarGet()) == 0)
        //        return Color.white;
        //    else
        //        return MColorSchema.GetColor(GetCtx(), GetVAPA_Color_ID(), GetPercent());
        //}

        /**
         * Get the color schema for this goal.
         * 
         * @return the color schema, or null if the measure targer is 0
         */
        //public MColorSchema GetColorSchema()
        //{
        //    return (Env.Signum(GetMeasureTarget()) == 0) ?
        //        null : MColorSchema.Get(GetCtx(), GetVAPA_Color_ID());
        //}

        /**
         * 	Get Measure Display
         *	@return Measure Display
         */
        public new String GetMeasureDisplay()
        {
            String s = base.GetMeasureDisplay();
            if (s == null)
            {
                if (MEASURESCOPE_Week.Equals(GetMeasureScope()))
                    s = MEASUREDISPLAY_Week;
                else if (MEASURESCOPE_Day.Equals(GetMeasureScope()))
                    s = MEASUREDISPLAY_Day;
                else
                    s = MEASUREDISPLAY_Month;
            }
            return s;
        }

        /**
         * 	Get Measure Display Text
         *	@return Measure Display Text
         */
        public String GetXAxisText()
        {
            MVAPAEvaluate measure = GetMeasure();
            if (measure != null
                && MVAPAEvaluate.MEASUREDATATYPE_StatusQtyAmount.Equals(measure.GetMeasureDataType()))
            {
                if (MVAPAEvaluate.MEASURETYPE_Request.Equals(measure.GetMeasureType()))
                    return Msg.GetElement(GetCtx(), "VAR_Req_Status_ID");
                if (MVAPAEvaluate.MEASURETYPE_Project.Equals(measure.GetMeasureType()))
                    return Msg.GetElement(GetCtx(), "VAB_Std_Stage_ID");
            }
            String value = GetMeasureDisplay();
            String display = MVAFCtrlRefList.GetListName(GetCtx(), MEASUREDISPLAY_VAF_Control_Ref_ID, value);
            return display == null ? value : display;
        }

        /**
         * 	Goal has TarGet
         *	@return true if tarGet
         */
        public Boolean IsTarGet()
        {
            return Env.Signum(GetMeasureTarget()) != 0;
        }

        /**
         * 	String Representation
         *	@return info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MVAPATarget[");
            sb.Append(Get_ID())
                .Append("-").Append(GetName())
                .Append(",").Append(GetGoalPerformance())
                .Append("]");
            return sb.ToString();
        }

        /**
         * 	Before Save
         *	@param newRecord new
         *	@return true
         */
        protected override Boolean BeforeSave(Boolean newRecord)
        {
            //	Measure required if nor Summary
            if (!IsSummary() && GetVAPA_Evaluate_ID() == 0)
            {
               log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "VAPA_Evaluate_ID"));
                return false;
            }
            if (IsSummary() && GetVAPA_Evaluate_ID() != 0)
                SetVAPA_Evaluate_ID(0);

            //	User/Role Check
            if ((newRecord || Is_ValueChanged("VAF_UserContact_ID") || Is_ValueChanged("VAF_Role_ID"))
                && GetVAF_UserContact_ID() != 0)
            {
                MVAFUserContact user = MVAFUserContact.Get(GetCtx(), GetVAF_UserContact_ID());
                MVAFRole[] roles = user.GetRoles(GetVAF_Org_ID());
                if (roles.Length == 0)		//	No Role
                    SetVAF_Role_ID(0);
                else if (roles.Length == 1)	//	One
                    SetVAF_Role_ID(roles[0].GetVAF_Role_ID());
                else
                {
                    int VAF_Role_ID = GetVAF_Role_ID();
                    if (VAF_Role_ID != 0)	//	validate
                    {
                        Boolean found = false;
                        for (int i = 0; i < roles.Length; i++)
                        {
                            if (VAF_Role_ID == roles[i].GetVAF_Role_ID())
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            VAF_Role_ID = 0;
                    }
                    if (VAF_Role_ID == 0)		//	Set to first one
                        SetVAF_Role_ID(roles[0].GetVAF_Role_ID());
                }	//	multiple roles
            }

            return true;
        }

        /**
         * 	After Save
         *	@param newRecord new
         *	@param success success
         *	@return true
         */
        protected override Boolean AfterSave(Boolean newRecord, Boolean success)
        {
            if (!success)
                return success;

            //	Update Goal if TarGet / Scope Changed
            if (newRecord
                || Is_ValueChanged("MeasureTarGet")
                || Is_ValueChanged("MeasureScope"))
                UpdateGoal(true);

            return success;
        }
    }
}