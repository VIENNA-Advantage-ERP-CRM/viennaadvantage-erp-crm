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
/** Generated Model for VAF_QuickSearchColumn
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_QuickSearchColumn : PO
{
public X_VAF_QuickSearchColumn (Context ctx, int VAF_QuickSearchColumn_ID, Trx trxName) : base (ctx, VAF_QuickSearchColumn_ID, trxName)
{
/** if (VAF_QuickSearchColumn_ID == 0)
{
SetVAF_QuickSearchColumn_ID (0);
SetVAF_QuickSearchWindow_ID (0);
SetVAF_Control_Ref_ID (0);
SetRecordType (null);	// U
SetIsDisplayed (true);	// Y
SetIsIdentifier (false);
SetIsKey (false);
SetIsQueryCriteria (false);
SetIsRange (false);
SetName (null);
SetSelectClause (null);
SetSeqNo (0);	// @SQL=SELECT COALESCE(MAX(SeqNo),0)+10 AS DefaultValue FROM VAF_QuickSearchColumn WHERE VAF_QuickSearchWindow_ID=@VAF_QuickSearchWindow_ID@
}
 */
}
public X_VAF_QuickSearchColumn (Ctx ctx, int VAF_QuickSearchColumn_ID, Trx trxName) : base (ctx, VAF_QuickSearchColumn_ID, trxName)
{
/** if (VAF_QuickSearchColumn_ID == 0)
{
SetVAF_QuickSearchColumn_ID (0);
SetVAF_QuickSearchWindow_ID (0);
SetVAF_Control_Ref_ID (0);
SetRecordType (null);	// U
SetIsDisplayed (true);	// Y
SetIsIdentifier (false);
SetIsKey (false);
SetIsQueryCriteria (false);
SetIsRange (false);
SetName (null);
SetSelectClause (null);
SetSeqNo (0);	// @SQL=SELECT COALESCE(MAX(SeqNo),0)+10 AS DefaultValue FROM VAF_QuickSearchColumn WHERE VAF_QuickSearchWindow_ID=@VAF_QuickSearchWindow_ID@
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_QuickSearchColumn (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_QuickSearchColumn (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_QuickSearchColumn (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_QuickSearchColumn()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID = 27562514361781L;
/** Last Updated Timestamp 7/29/2010 1:07:25 PM */
public static long updatedMS = 1280389044992L;
/** VAF_TableView_ID=897 */
public static int Table_ID;
 // =897;

/** TableName=VAF_QuickSearchColumn */
public static String Table_Name="VAF_QuickSearchColumn";

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
StringBuilder sb = new StringBuilder ("X_VAF_QuickSearchColumn[").Append(Get_ID()).Append("]");
return sb.ToString();
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
/** Set Info Column.
@param VAF_QuickSearchColumn_ID Info Window Column */
public void SetVAF_QuickSearchColumn_ID (int VAF_QuickSearchColumn_ID)
{
if (VAF_QuickSearchColumn_ID < 1) throw new ArgumentException ("VAF_QuickSearchColumn_ID is mandatory.");
Set_ValueNoCheck ("VAF_QuickSearchColumn_ID", VAF_QuickSearchColumn_ID);
}
/** Get Info Column.
@return Info Window Column */
public int GetVAF_QuickSearchColumn_ID() 
{
Object ii = Get_Value("VAF_QuickSearchColumn_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Info Window.
@param VAF_QuickSearchWindow_ID Info and search/select Window */
public void SetVAF_QuickSearchWindow_ID (int VAF_QuickSearchWindow_ID)
{
if (VAF_QuickSearchWindow_ID < 1) throw new ArgumentException ("VAF_QuickSearchWindow_ID is mandatory.");
Set_ValueNoCheck ("VAF_QuickSearchWindow_ID", VAF_QuickSearchWindow_ID);
}
/** Get Info Window.
@return Info and search/select Window */
public int GetVAF_QuickSearchWindow_ID() 
{
Object ii = Get_Value("VAF_QuickSearchWindow_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}

/** VAF_Control_Ref_ID VAF_Control_Ref_ID=1 */
public static int VAF_CONTROL_REF_ID_VAF_Control_Ref_ID=1;
/** Set Reference.
@param VAF_Control_Ref_ID System Reference and Validation */
public void SetVAF_Control_Ref_ID (int VAF_Control_Ref_ID)
{
if (VAF_Control_Ref_ID < 1) throw new ArgumentException ("VAF_Control_Ref_ID is mandatory.");
Set_Value ("VAF_Control_Ref_ID", VAF_Control_Ref_ID);
}
/** Get Reference.
@return System Reference and Validation */
public int GetVAF_Control_Ref_ID() 
{
Object ii = Get_Value("VAF_Control_Ref_ID");
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

/** RecordType VAF_Control_Ref_ID=389 */
public static int RecordType_VAF_Control_Ref_ID=389;
/** Set Entity Type.
@param RecordType Dictionary Entity Type;
 Determines ownership and synchronization */
public void SetRecordType (String RecordType)
{
if (RecordType.Length > 4)
{
log.Warning("Length > 4 - truncated");
RecordType = RecordType.Substring(0,4);
}
Set_Value ("RecordType", RecordType);
}
/** Get Entity Type.
@return Dictionary Entity Type;
 Determines ownership and synchronization */
public String GetRecordType() 
{
return (String)Get_Value("RecordType");
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
/** Set Centrally maintained.
@param IsCentrallyMaintained Information maintained in System Element table */
public void SetIsCentrallyMaintained (Boolean IsCentrallyMaintained)
{
Set_Value ("IsCentrallyMaintained", IsCentrallyMaintained);
}
/** Get Centrally maintained.
@return Information maintained in System Element table */
public Boolean IsCentrallyMaintained() 
{
Object oo = Get_Value("IsCentrallyMaintained");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
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
/** Set Identifier.
@param IsIdentifier This column is part of the record identifier */
public void SetIsIdentifier (Boolean IsIdentifier)
{
Set_Value ("IsIdentifier", IsIdentifier);
}
/** Get Identifier.
@return This column is part of the record identifier */
public Boolean IsIdentifier() 
{
Object oo = Get_Value("IsIdentifier");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Key column.
@param IsKey This column is the key in this table */
public void SetIsKey (Boolean IsKey)
{
Set_Value ("IsKey", IsKey);
}
/** Get Key column.
@return This column is the key in this table */
public Boolean IsKey() 
{
Object oo = Get_Value("IsKey");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Query Criteria.
@param IsQueryCriteria The column is also used as a query criteria */
public void SetIsQueryCriteria (Boolean IsQueryCriteria)
{
Set_Value ("IsQueryCriteria", IsQueryCriteria);
}
/** Get Query Criteria.
@return The column is also used as a query criteria */
public Boolean IsQueryCriteria() 
{
Object oo = Get_Value("IsQueryCriteria");
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
/** Set Sql SELECT.
@param SelectClause SQL SELECT clause */
public void SetSelectClause (String SelectClause)
{
if (SelectClause == null) throw new ArgumentException ("SelectClause is mandatory.");
if (SelectClause.Length > 1000)
{
log.Warning("Length > 1000 - truncated");
SelectClause = SelectClause.Substring(0,1000);
}
Set_Value ("SelectClause", SelectClause);
}
/** Get Sql SELECT.
@return SQL SELECT clause */
public String GetSelectClause() 
{
return (String)Get_Value("SelectClause");
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