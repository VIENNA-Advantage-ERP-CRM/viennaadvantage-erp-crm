namespace VAdvantage.Model{
/** Generated Model - DO NOT CHANGE */
using System;using System.Text;using VAdvantage.DataBase;using VAdvantage.Common;using VAdvantage.Classes;using VAdvantage.Process;using VAdvantage.Model;using VAdvantage.Utility;using System.Data;/** Generated Model for VAB_DunningExeLine
 *  @author Raghu (Updated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAB_DunningExeLine : PO{public X_VAB_DunningExeLine (Context ctx, int VAB_DunningExeLine_ID, Trx trxName) : base (ctx, VAB_DunningExeLine_ID, trxName){/** if (VAB_DunningExeLine_ID == 0){SetAmt (0.0);SetVAB_DunningExeEntry_ID (0);SetVAB_DunningExeLine_ID (0);SetConvertedAmt (0.0);SetDaysDue (0);SetFeeAmt (0.0);SetInterestAmt (0.0);SetIsInDispute (false);SetOpenAmt (0.0);SetProcessed (false);// N
SetTimesDunned (0);SetTotalAmt (0.0);} */
}public X_VAB_DunningExeLine (Ctx ctx, int VAB_DunningExeLine_ID, Trx trxName) : base (ctx, VAB_DunningExeLine_ID, trxName){/** if (VAB_DunningExeLine_ID == 0){SetAmt (0.0);SetVAB_DunningExeEntry_ID (0);SetVAB_DunningExeLine_ID (0);SetConvertedAmt (0.0);SetDaysDue (0);SetFeeAmt (0.0);SetInterestAmt (0.0);SetIsInDispute (false);SetOpenAmt (0.0);SetProcessed (false);// N
SetTimesDunned (0);SetTotalAmt (0.0);} */
}/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAB_DunningExeLine (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName){}/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAB_DunningExeLine (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName){}/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAB_DunningExeLine (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName){}/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAB_DunningExeLine(){ Table_ID = Get_Table_ID(Table_Name); model = new KeyNamePair(Table_ID,Table_Name);}/** Serial Version No */
static long serialVersionUID = 27837534353617L;/** Last Updated Timestamp 4/16/2019 3:33:56 PM */
public static long updatedMS = 1555409036828L;/** VAF_TableView_ID=524 */
public static int Table_ID; // =524;
/** TableName=VAB_DunningExeLine */
public static String Table_Name="VAB_DunningExeLine";
protected static KeyNamePair model;protected Decimal accessLevel = new Decimal(3);/** AccessLevel
@return 3 - Client - Org 
*/
protected override int Get_AccessLevel(){return Convert.ToInt32(accessLevel.ToString());}/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Context ctx){POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);return poi;}/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Ctx ctx){POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);return poi;}/** Info
@return info
*/
public override String ToString(){StringBuilder sb = new StringBuilder ("X_VAB_DunningExeLine[").Append(Get_ID()).Append("]");return sb.ToString();}/** Set Amount.
@param Amt Amount */
public void SetAmt (Decimal? Amt){if (Amt == null) throw new ArgumentException ("Amt is mandatory.");Set_Value ("Amt", (Decimal?)Amt);}/** Get Amount.
@return Amount */
public Decimal GetAmt() {Object bd =Get_Value("Amt");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set Dunning Run Entry.
@param VAB_DunningExeEntry_ID Dunning Run Entry */
public void SetVAB_DunningExeEntry_ID (int VAB_DunningExeEntry_ID){if (VAB_DunningExeEntry_ID < 1) throw new ArgumentException ("VAB_DunningExeEntry_ID is mandatory.");Set_ValueNoCheck ("VAB_DunningExeEntry_ID", VAB_DunningExeEntry_ID);}/** Get Dunning Run Entry.
@return Dunning Run Entry */
public int GetVAB_DunningExeEntry_ID() {Object ii = Get_Value("VAB_DunningExeEntry_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Dunning Run Line.
@param VAB_DunningExeLine_ID Dunning Run Line */
public void SetVAB_DunningExeLine_ID (int VAB_DunningExeLine_ID){if (VAB_DunningExeLine_ID < 1) throw new ArgumentException ("VAB_DunningExeLine_ID is mandatory.");Set_ValueNoCheck ("VAB_DunningExeLine_ID", VAB_DunningExeLine_ID);}/** Get Dunning Run Line.
@return Dunning Run Line */
public int GetVAB_DunningExeLine_ID() {Object ii = Get_Value("VAB_DunningExeLine_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Invoice Payment Schedule.
@param VAB_sched_InvoicePayment_ID Invoice Payment Schedule */
public void SetVAB_sched_InvoicePayment_ID (int VAB_sched_InvoicePayment_ID){if (VAB_sched_InvoicePayment_ID <= 0) Set_Value ("VAB_sched_InvoicePayment_ID", null);else
Set_Value ("VAB_sched_InvoicePayment_ID", VAB_sched_InvoicePayment_ID);}/** Get Invoice Payment Schedule.
@return Invoice Payment Schedule */
public int GetVAB_sched_InvoicePayment_ID() {Object ii = Get_Value("VAB_sched_InvoicePayment_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Invoice.
@param VAB_Invoice_ID Invoice Identifier */
public void SetVAB_Invoice_ID (int VAB_Invoice_ID){if (VAB_Invoice_ID <= 0) Set_Value ("VAB_Invoice_ID", null);else
Set_Value ("VAB_Invoice_ID", VAB_Invoice_ID);}/** Get Invoice.
@return Invoice Identifier */
public int GetVAB_Invoice_ID() {Object ii = Get_Value("VAB_Invoice_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() {return new KeyNamePair(Get_ID(), GetVAB_Invoice_ID().ToString());}/** Set Payment.
@param VAB_Payment_ID Payment identifier */
public void SetVAB_Payment_ID (int VAB_Payment_ID){if (VAB_Payment_ID <= 0) Set_Value ("VAB_Payment_ID", null);else
Set_Value ("VAB_Payment_ID", VAB_Payment_ID);}/** Get Payment.
@return Payment identifier */
public int GetVAB_Payment_ID() {Object ii = Get_Value("VAB_Payment_ID");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Converted Amount.
@param ConvertedAmt Converted Amount */
public void SetConvertedAmt (Decimal? ConvertedAmt){if (ConvertedAmt == null) throw new ArgumentException ("ConvertedAmt is mandatory.");Set_Value ("ConvertedAmt", (Decimal?)ConvertedAmt);}/** Get Converted Amount.
@return Converted Amount */
public Decimal GetConvertedAmt() {Object bd =Get_Value("ConvertedAmt");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set Days due.
@param DaysDue Number of days due (negative: due in number of days) */
public void SetDaysDue (int DaysDue){Set_Value ("DaysDue", DaysDue);}/** Get Days due.
@return Number of days due (negative: due in number of days) */
public int GetDaysDue() {Object ii = Get_Value("DaysDue");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Export.
@param Export_ID Export */
public void SetExport_ID (String Export_ID){if (Export_ID != null && Export_ID.Length > 50){log.Warning("Length > 50 - truncated");Export_ID = Export_ID.Substring(0,50);}Set_ValueNoCheck ("Export_ID", Export_ID);}/** Get Export.
@return Export */
public String GetExport_ID() {return (String)Get_Value("Export_ID");}/** Set Fee Amount.
@param FeeAmt Fee amount in invoice currency */
public void SetFeeAmt (Decimal? FeeAmt){if (FeeAmt == null) throw new ArgumentException ("FeeAmt is mandatory.");Set_Value ("FeeAmt", (Decimal?)FeeAmt);}/** Get Fee Amount.
@return Fee amount in invoice currency */
public Decimal GetFeeAmt() {Object bd =Get_Value("FeeAmt");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set Interest Amount.
@param InterestAmt Interest Amount */
public void SetInterestAmt (Decimal? InterestAmt){if (InterestAmt == null) throw new ArgumentException ("InterestAmt is mandatory.");Set_Value ("InterestAmt", (Decimal?)InterestAmt);}/** Get Interest Amount.
@return Interest Amount */
public Decimal GetInterestAmt() {Object bd =Get_Value("InterestAmt");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set In Dispute.
@param IsInDispute Document is in dispute */
public void SetIsInDispute (Boolean IsInDispute){Set_Value ("IsInDispute", IsInDispute);}/** Get In Dispute.
@return Document is in dispute */
public Boolean IsInDispute() {Object oo = Get_Value("IsInDispute");if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo);}return false;}/** Set Open Amount.
@param OpenAmt Open item amount */
public void SetOpenAmt (Decimal? OpenAmt){if (OpenAmt == null) throw new ArgumentException ("OpenAmt is mandatory.");Set_Value ("OpenAmt", (Decimal?)OpenAmt);}/** Get Open Amount.
@return Open item amount */
public Decimal GetOpenAmt() {Object bd =Get_Value("OpenAmt");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}/** Set Processed.
@param Processed The document has been processed */
public void SetProcessed (Boolean Processed){Set_Value ("Processed", Processed);}/** Get Processed.
@return The document has been processed */
public Boolean IsProcessed() {Object oo = Get_Value("Processed");if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo);}return false;}/** Set Times Dunned.
@param TimesDunned Number of times dunned previously */
public void SetTimesDunned (int TimesDunned){Set_Value ("TimesDunned", TimesDunned);}/** Get Times Dunned.
@return Number of times dunned previously */
public int GetTimesDunned() {Object ii = Get_Value("TimesDunned");if (ii == null) return 0;return Convert.ToInt32(ii);}/** Set Total Amount.
@param TotalAmt Total Amount */
public void SetTotalAmt (Decimal? TotalAmt){if (TotalAmt == null) throw new ArgumentException ("TotalAmt is mandatory.");Set_Value ("TotalAmt", (Decimal?)TotalAmt);}/** Get Total Amount.
@return Total Amount */
public Decimal GetTotalAmt() {Object bd =Get_Value("TotalAmt");if (bd == null) return Env.ZERO;return  Convert.ToDecimal(bd);}}
}