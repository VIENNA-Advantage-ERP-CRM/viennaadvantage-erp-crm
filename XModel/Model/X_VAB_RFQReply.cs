namespace VAdvantage.Model
{

/** Generated Model - DO NOT CHANGE */
using System;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Common;
using VAdvantage.Classes;
using VAdvantage.Process;
using VAdvantage.Model;
using VAdvantage.Utility;
using System.Data;
/** Generated Model for VAB_RFQReply
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAB_RFQReply : PO
{
public X_VAB_RFQReply (Context ctx, int VAB_RFQReply_ID, Trx trxName) : base (ctx, VAB_RFQReply_ID, trxName)
{
/** if (VAB_RFQReply_ID == 0)
{
SetVAB_BusinessPartner_ID (0);
SetVAB_BPart_Location_ID (0);
SetVAB_Currency_ID (0);	// @VAB_Currency_ID@
SetVAB_RFQReply_ID (0);
SetVAB_RFQ_ID (0);
SetIsComplete (false);
SetIsSelectedWinner (false);
SetIsSelfService (false);
SetName (null);
SetPrice (0.0);
SetProcessed (false);	// N
}
 */
}
public X_VAB_RFQReply (Ctx ctx, int VAB_RFQReply_ID, Trx trxName) : base (ctx, VAB_RFQReply_ID, trxName)
{
/** if (VAB_RFQReply_ID == 0)
{
SetVAB_BusinessPartner_ID (0);
SetVAB_BPart_Location_ID (0);
SetVAB_Currency_ID (0);	// @VAB_Currency_ID@
SetVAB_RFQReply_ID (0);
SetVAB_RFQ_ID (0);
SetIsComplete (false);
SetIsSelectedWinner (false);
SetIsSelfService (false);
SetName (null);
SetPrice (0.0);
SetProcessed (false);	// N
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAB_RFQReply (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAB_RFQReply (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAB_RFQReply (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAB_RFQReply()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514374961L;
/** Last Updated Timestamp 7/29/2010 1:07:38 PM */
public static long updatedMS = 1280389058172L;
/** VAF_TableView_ID=674 */
public static int Table_ID;
 // =674;

/** TableName=VAB_RFQReply */
public static String Table_Name="VAB_RFQReply";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(1);
/** AccessLevel
@return 1 - Org 
*/
protected override int Get_AccessLevel()
{
return Convert.ToInt32(accessLevel.ToString());
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Ctx ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO(Context ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Info
@return info
*/
public override String ToString()
{
StringBuilder sb = new StringBuilder ("X_VAB_RFQReply[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set User/Contact.
@param VAF_UserContact_ID User within the system - Internal or Business Partner Contact */
public void SetVAF_UserContact_ID (int VAF_UserContact_ID)
{
if (VAF_UserContact_ID <= 0) Set_ValueNoCheck ("VAF_UserContact_ID", null);
else
Set_ValueNoCheck ("VAF_UserContact_ID", VAF_UserContact_ID);
}
/** Get User/Contact.
@return User within the system - Internal or Business Partner Contact */
public int GetVAF_UserContact_ID() 
{
Object ii = Get_Value("VAF_UserContact_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Business Partner.
@param VAB_BusinessPartner_ID Identifies a Business Partner */
public void SetVAB_BusinessPartner_ID (int VAB_BusinessPartner_ID)
{
if (VAB_BusinessPartner_ID < 1) throw new ArgumentException ("VAB_BusinessPartner_ID is mandatory.");
Set_Value ("VAB_BusinessPartner_ID", VAB_BusinessPartner_ID);
}
/** Get Business Partner.
@return Identifies a Business Partner */
public int GetVAB_BusinessPartner_ID() 
{
Object ii = Get_Value("VAB_BusinessPartner_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Partner Location.
@param VAB_BPart_Location_ID Identifies the (ship to) address for this Business Partner */
public void SetVAB_BPart_Location_ID (int VAB_BPart_Location_ID)
{
if (VAB_BPart_Location_ID < 1) throw new ArgumentException ("VAB_BPart_Location_ID is mandatory.");
Set_Value ("VAB_BPart_Location_ID", VAB_BPart_Location_ID);
}
/** Get Partner Location.
@return Identifies the (ship to) address for this Business Partner */
public int GetVAB_BPart_Location_ID() 
{
Object ii = Get_Value("VAB_BPart_Location_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Currency.
@param VAB_Currency_ID The Currency for this record */
public void SetVAB_Currency_ID (int VAB_Currency_ID)
{
if (VAB_Currency_ID < 1) throw new ArgumentException ("VAB_Currency_ID is mandatory.");
Set_Value ("VAB_Currency_ID", VAB_Currency_ID);
}
/** Get Currency.
@return The Currency for this record */
public int GetVAB_Currency_ID() 
{
Object ii = Get_Value("VAB_Currency_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Order.
@param VAB_Order_ID Order */
public void SetVAB_Order_ID (int VAB_Order_ID)
{
if (VAB_Order_ID <= 0) Set_Value ("VAB_Order_ID", null);
else
Set_Value ("VAB_Order_ID", VAB_Order_ID);
}
/** Get Order.
@return Order */
public int GetVAB_Order_ID() 
{
Object ii = Get_Value("VAB_Order_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set RfQ Response.
@param VAB_RFQReply_ID Request for Quotation Response from a potential Vendor */
public void SetVAB_RFQReply_ID (int VAB_RFQReply_ID)
{
if (VAB_RFQReply_ID < 1) throw new ArgumentException ("VAB_RFQReply_ID is mandatory.");
Set_ValueNoCheck ("VAB_RFQReply_ID", VAB_RFQReply_ID);
}
/** Get RfQ Response.
@return Request for Quotation Response from a potential Vendor */
public int GetVAB_RFQReply_ID() 
{
Object ii = Get_Value("VAB_RFQReply_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set RfQ.
@param VAB_RFQ_ID Request for Quotation */
public void SetVAB_RFQ_ID (int VAB_RFQ_ID)
{
if (VAB_RFQ_ID < 1) throw new ArgumentException ("VAB_RFQ_ID is mandatory.");
Set_ValueNoCheck ("VAB_RFQ_ID", VAB_RFQ_ID);
}
/** Get RfQ.
@return Request for Quotation */
public int GetVAB_RFQ_ID() 
{
Object ii = Get_Value("VAB_RFQ_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Check Complete.
@param CheckComplete Check Complete */
public void SetCheckComplete (String CheckComplete)
{
if (CheckComplete != null && CheckComplete.Length > 1)
{
log.Warning("Length > 1 - truncated");
CheckComplete = CheckComplete.Substring(0,1);
}
Set_Value ("CheckComplete", CheckComplete);
}
/** Get Check Complete.
@return Check Complete */
public String GetCheckComplete() 
{
return (String)Get_Value("CheckComplete");
}
/** Set Invited.
@param DateInvited Date when (last) invitation was sent */
public void SetDateInvited (DateTime? DateInvited)
{
Set_Value ("DateInvited", (DateTime?)DateInvited);
}
/** Get Invited.
@return Date when (last) invitation was sent */
public DateTime? GetDateInvited() 
{
return (DateTime?)Get_Value("DateInvited");
}
/** Set Response Date.
@param DateResponse Date of the Response */
public void SetDateResponse (DateTime? DateResponse)
{
Set_Value ("DateResponse", (DateTime?)DateResponse);
}
/** Get Response Date.
@return Date of the Response */
public DateTime? GetDateResponse() 
{
return (DateTime?)Get_Value("DateResponse");
}
/** Set Work Complete.
@param DateWorkComplete Date when work is (planned to be) complete */
public void SetDateWorkComplete (DateTime? DateWorkComplete)
{
Set_Value ("DateWorkComplete", (DateTime?)DateWorkComplete);
}
/** Get Work Complete.
@return Date when work is (planned to be) complete */
public DateTime? GetDateWorkComplete() 
{
return (DateTime?)Get_Value("DateWorkComplete");
}
/** Set Work Start.
@param DateWorkStart Date when work is (planned to be) started */
public void SetDateWorkStart (DateTime? DateWorkStart)
{
Set_Value ("DateWorkStart", (DateTime?)DateWorkStart);
}
/** Get Work Start.
@return Date when work is (planned to be) started */
public DateTime? GetDateWorkStart() 
{
return (DateTime?)Get_Value("DateWorkStart");
}
/** Set Delivery Days.
@param DeliveryDays Number of Days (planned) until Delivery */
public void SetDeliveryDays (int DeliveryDays)
{
Set_Value ("DeliveryDays", DeliveryDays);
}
/** Get Delivery Days.
@return Number of Days (planned) until Delivery */
public int GetDeliveryDays() 
{
Object ii = Get_Value("DeliveryDays");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Description.
@param Description Optional short description of the record */
public void SetDescription (String Description)
{
if (Description != null && Description.Length > 255)
{
log.Warning("Length > 255 - truncated");
Description = Description.Substring(0,255);
}
Set_Value ("Description", Description);
}
/** Get Description.
@return Optional short description of the record */
public String GetDescription() 
{
return (String)Get_Value("Description");
}
/** Set Comment.
@param Help Comment, Help or Hint */
public void SetHelp (String Help)
{
if (Help != null && Help.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
Help = Help.Substring(0,2000);
}
Set_Value ("Help", Help);
}
/** Get Comment.
@return Comment, Help or Hint */
public String GetHelp() 
{
return (String)Get_Value("Help");
}
/** Set Complete.
@param IsComplete It is complete */
public void SetIsComplete (Boolean IsComplete)
{
Set_Value ("IsComplete", IsComplete);
}
/** Get Complete.
@return It is complete */
public Boolean IsComplete() 
{
Object oo = Get_Value("IsComplete");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Selected Winner.
@param IsSelectedWinner The resonse is the selected winner */
public void SetIsSelectedWinner (Boolean IsSelectedWinner)
{
Set_Value ("IsSelectedWinner", IsSelectedWinner);
}
/** Get Selected Winner.
@return The resonse is the selected winner */
public Boolean IsSelectedWinner() 
{
Object oo = Get_Value("IsSelectedWinner");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Self-Service.
@param IsSelfService This is a Self-Service entry or this entry can be changed via Self-Service */
public void SetIsSelfService (Boolean IsSelfService)
{
Set_Value ("IsSelfService", IsSelfService);
}
/** Get Self-Service.
@return This is a Self-Service entry or this entry can be changed via Self-Service */
public Boolean IsSelfService() 
{
Object oo = Get_Value("IsSelfService");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Name.
@param Name Alphanumeric identifier of the entity */
public void SetName (String Name)
{
if (Name == null) throw new ArgumentException ("Name is mandatory.");
if (Name.Length > 60)
{
log.Warning("Length > 60 - truncated");
Name = Name.Substring(0,60);
}
Set_Value ("Name", Name);
}
/** Get Name.
@return Alphanumeric identifier of the entity */
public String GetName() 
{
return (String)Get_Value("Name");
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetName());
}
/** Set Price.
@param Price Price */
public void SetPrice (Decimal? Price)
{
if (Price == null) throw new ArgumentException ("Price is mandatory.");
Set_Value ("Price", (Decimal?)Price);
}
/** Get Price.
@return Price */
public Decimal GetPrice() 
{
Object bd =Get_Value("Price");
if (bd == null) return Env.ZERO;
return  Convert.ToDecimal(bd);
}
/** Set Processed.
@param Processed The document has been processed */
public void SetProcessed (Boolean Processed)
{
Set_Value ("Processed", Processed);
}
/** Get Processed.
@return The document has been processed */
public Boolean IsProcessed() 
{
Object oo = Get_Value("Processed");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Process Now.
@param Processing Process Now */
public void SetProcessing (Boolean Processing)
{
Set_Value ("Processing", Processing);
}
/** Get Process Now.
@return Process Now */
public Boolean IsProcessing() 
{
Object oo = Get_Value("Processing");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Ranking.
@param Ranking Relative Rank Number */
public void SetRanking (int Ranking)
{
Set_Value ("Ranking", Ranking);
}
/** Get Ranking.
@return Relative Rank Number */
public int GetRanking() 
{
Object ii = Get_Value("Ranking");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
}

}