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
/** Generated Model for VAF_CrystalReport_Para
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_CrystalReport_Para : PO
{
public X_VAF_CrystalReport_Para (Context ctx, int VAF_CrystalReport_Para_ID, Trx trxName) : base (ctx, VAF_CrystalReport_Para_ID, trxName)
{
/** if (VAF_CrystalReport_Para_ID == 0)
{
SetVAF_CrystalReport_Para_ID (0);
SetVAF_Page_ID (0);
}
 */
}
public X_VAF_CrystalReport_Para (Ctx ctx, int VAF_CrystalReport_Para_ID, Trx trxName) : base (ctx, VAF_CrystalReport_Para_ID, trxName)
{
/** if (VAF_CrystalReport_Para_ID == 0)
{
SetVAF_CrystalReport_Para_ID (0);
SetVAF_Page_ID (0);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_CrystalReport_Para (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_CrystalReport_Para (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_CrystalReport_Para (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_CrystalReport_Para()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27642972314450L;
/** Last Updated Timestamp 2/14/2013 6:33:18 PM */
public static long updatedMS = 1360846997661L;
/** VAF_TableView_ID=1000175 */
public static int Table_ID;
 // =1000175;

/** TableName=VAF_CrystalReport_Para */
public static String Table_Name="VAF_CrystalReport_Para";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(4);
/** AccessLevel
@return 4 - System 
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
StringBuilder sb = new StringBuilder ("X_VAF_CrystalReport_Para[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set VAF_CrystalReport_Para_ID.
@param VAF_CrystalReport_Para_ID VAF_CrystalReport_Para_ID */
public void SetVAF_CrystalReport_Para_ID (int VAF_CrystalReport_Para_ID)
{
if (VAF_CrystalReport_Para_ID < 1) throw new ArgumentException ("VAF_CrystalReport_Para_ID is mandatory.");
Set_ValueNoCheck ("VAF_CrystalReport_Para_ID", VAF_CrystalReport_Para_ID);
}
/** Get VAF_CrystalReport_Para_ID.
@return VAF_CrystalReport_Para_ID */
public int GetVAF_CrystalReport_Para_ID() 
{
Object ii = Get_Value("VAF_CrystalReport_Para_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set System Element.
@param VAF_ColumnDic_ID System Element enables the central maintenance of column description and help. */
public void SetVAF_ColumnDic_ID (int VAF_ColumnDic_ID)
{
if (VAF_ColumnDic_ID <= 0) Set_Value ("VAF_ColumnDic_ID", null);
else
Set_Value ("VAF_ColumnDic_ID", VAF_ColumnDic_ID);
}
/** Get System Element.
@return System Element enables the central maintenance of column description and help. */
public int GetVAF_ColumnDic_ID() 
{
Object ii = Get_Value("VAF_ColumnDic_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Special Form.
@param VAF_Page_ID Special Form */
public void SetVAF_Page_ID (int VAF_Page_ID)
{
if (VAF_Page_ID < 1) throw new ArgumentException ("VAF_Page_ID is mandatory.");
Set_ValueNoCheck ("VAF_Page_ID", VAF_Page_ID);
}
/** Get Special Form.
@return Special Form */
public int GetVAF_Page_ID() 
{
Object ii = Get_Value("VAF_Page_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Reference.
@param AD_Reference_ID System Reference and Validation */
public void SetAD_Reference_ID (int AD_Reference_ID)
{
if (AD_Reference_ID <= 0) Set_Value ("AD_Reference_ID", null);
else
Set_Value ("AD_Reference_ID", AD_Reference_ID);
}
/** Get Reference.
@return System Reference and Validation */
public int GetAD_Reference_ID() 
{
Object ii = Get_Value("AD_Reference_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}

/** AD_Reference_Value_ID AD_Reference_ID=4 */
public static int AD_REFERENCE_VALUE_ID_AD_Reference_ID=4;
/** Set Reference Key.
@param AD_Reference_Value_ID Required to specify, if data type is Table or List */
public void SetAD_Reference_Value_ID (int AD_Reference_Value_ID)
{
if (AD_Reference_Value_ID <= 0) Set_Value ("AD_Reference_Value_ID", null);
else
Set_Value ("AD_Reference_Value_ID", AD_Reference_Value_ID);
}
/** Get Reference Key.
@return Required to specify, if data type is Table or List */
public int GetAD_Reference_Value_ID() 
{
Object ii = Get_Value("AD_Reference_Value_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Dynamic Validation.
@param VAF_DataVal_Rule_ID Dynamic Validation Rule */
public void SetVAF_DataVal_Rule_ID (int VAF_DataVal_Rule_ID)
{
if (VAF_DataVal_Rule_ID <= 0) Set_Value ("VAF_DataVal_Rule_ID", null);
else
Set_Value ("VAF_DataVal_Rule_ID", VAF_DataVal_Rule_ID);
}
/** Get Dynamic Validation.
@return Dynamic Validation Rule */
public int GetVAF_DataVal_Rule_ID() 
{
Object ii = Get_Value("VAF_DataVal_Rule_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set DB Column Name.
@param ColumnName Name of the column in the database */
public void SetColumnName (String ColumnName)
{
if (ColumnName != null && ColumnName.Length > 50)
{
log.Warning("Length > 50 - truncated");
ColumnName = ColumnName.Substring(0,50);
}
Set_Value ("ColumnName", ColumnName);
}
/** Get DB Column Name.
@return Name of the column in the database */
public String GetColumnName() 
{
return (String)Get_Value("ColumnName");
}
/** Set Default Logic.
@param DefaultValue Default value hierarchy, separated by ;
 */
public void SetDefaultValue (String DefaultValue)
{
if (DefaultValue != null && DefaultValue.Length > 100)
{
log.Warning("Length > 100 - truncated");
DefaultValue = DefaultValue.Substring(0,100);
}
Set_Value ("DefaultValue", DefaultValue);
}
/** Get Default Logic.
@return Default value hierarchy, separated by ;
 */
public String GetDefaultValue() 
{
return (String)Get_Value("DefaultValue");
}
/** Set Description.
@param Description Optional short description of the record */
public void SetDescription (String Description)
{
if (Description != null && Description.Length > 50)
{
log.Warning("Length > 50 - truncated");
Description = Description.Substring(0,50);
}
Set_Value ("Description", Description);
}
/** Get Description.
@return Optional short description of the record */
public String GetDescription() 
{
return (String)Get_Value("Description");
}
/** Set Export.
@param Export_ID Export */
public void SetExport_ID (String Export_ID)
{
if (Export_ID != null && Export_ID.Length > 50)
{
log.Warning("Length > 50 - truncated");
Export_ID = Export_ID.Substring(0,50);
}
Set_ValueNoCheck ("Export_ID", Export_ID);
}
/** Get Export.
@return Export */
public String GetExport_ID() 
{
return (String)Get_Value("Export_ID");
}
/** Set Length.
@param FieldLength Length of the column in the database */
public void SetFieldLength (int FieldLength)
{
Set_Value ("FieldLength", FieldLength);
}
/** Get Length.
@return Length of the column in the database */
public int GetFieldLength() 
{
Object ii = Get_Value("FieldLength");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Displayed.
@param IsDisplayed Determines, if this field is displayed */
public void SetIsDisplayed (Boolean IsDisplayed)
{
Set_Value ("IsDisplayed", IsDisplayed);
}
/** Get Displayed.
@return Determines, if this field is displayed */
public Boolean IsDisplayed() 
{
Object oo = Get_Value("IsDisplayed");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Mandatory.
@param IsMandatory Data is required in this column */
public void SetIsMandatory (Boolean IsMandatory)
{
Set_Value ("IsMandatory", IsMandatory);
}
/** Get Mandatory.
@return Data is required in this column */
public Boolean IsMandatory() 
{
Object oo = Get_Value("IsMandatory");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Range.
@param IsRange The parameter is a range of values */
public void SetIsRange (Boolean IsRange)
{
Set_Value ("IsRange", IsRange);
}
/** Get Range.
@return The parameter is a range of values */
public Boolean IsRange() 
{
Object oo = Get_Value("IsRange");
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
if (Name != null && Name.Length > 50)
{
log.Warning("Length > 50 - truncated");
Name = Name.Substring(0,50);
}
Set_Value ("Name", Name);
}
/** Get Name.
@return Alphanumeric identifier of the entity */
public String GetName() 
{
return (String)Get_Value("Name");
}
/** Set Sequence.
@param SeqNo Method of ordering elements;
 lowest number comes first */
public void SetSeqNo (int SeqNo)
{
Set_Value ("SeqNo", SeqNo);
}
/** Get Sequence.
@return Method of ordering elements;
 lowest number comes first */
public int GetSeqNo() 
{
Object ii = Get_Value("SeqNo");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
}

}
