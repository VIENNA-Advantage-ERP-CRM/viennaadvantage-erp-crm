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
/** Generated Model for K_Synonym
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_K_Synonym : PO
{
public X_K_Synonym (Context ctx, int K_Synonym_ID, Trx trxName) : base (ctx, K_Synonym_ID, trxName)
{
/** if (K_Synonym_ID == 0)
{
SetVAF_Language (null);
SetK_Synonym_ID (0);
SetName (null);
SetSynonymName (null);
}
 */
}
public X_K_Synonym (Ctx ctx, int K_Synonym_ID, Trx trxName) : base (ctx, K_Synonym_ID, trxName)
{
/** if (K_Synonym_ID == 0)
{
SetVAF_Language (null);
SetK_Synonym_ID (0);
SetName (null);
SetSynonymName (null);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_K_Synonym (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_K_Synonym (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_K_Synonym (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_K_Synonym()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514378158L;
/** Last Updated Timestamp 7/29/2010 1:07:41 PM */
public static long updatedMS = 1280389061369L;
/** VAF_TableView_ID=608 */
public static int Table_ID;
 // =608;

/** TableName=K_Synonym */
public static String Table_Name="K_Synonym";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(3);
/** AccessLevel
@return 3 - Client - Org 
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
StringBuilder sb = new StringBuilder ("X_K_Synonym[").Append(Get_ID()).Append("]");
return sb.ToString();
}

/** VAF_Language VAF_Control_Ref_ID=106 */
public static int VAF_LANGUAGE_VAF_Control_Ref_ID=106;
/** Set Language.
@param VAF_Language Language for this entity */
public void SetVAF_Language (String VAF_Language)
{
if (VAF_Language.Length > 5)
{
log.Warning("Length > 5 - truncated");
VAF_Language = VAF_Language.Substring(0,5);
}
Set_Value ("VAF_Language", VAF_Language);
}
/** Get Language.
@return Language for this entity */
public String GetVAF_Language() 
{
return (String)Get_Value("VAF_Language");
}
/** Set Knowledge Synonym.
@param K_Synonym_ID Knowlege Keyword Synonym */
public void SetK_Synonym_ID (int K_Synonym_ID)
{
if (K_Synonym_ID < 1) throw new ArgumentException ("K_Synonym_ID is mandatory.");
Set_ValueNoCheck ("K_Synonym_ID", K_Synonym_ID);
}
/** Get Knowledge Synonym.
@return Knowlege Keyword Synonym */
public int GetK_Synonym_ID() 
{
Object ii = Get_Value("K_Synonym_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
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
/** Set Synonym Name.
@param SynonymName The synonym for the name */
public void SetSynonymName (String SynonymName)
{
if (SynonymName == null) throw new ArgumentException ("SynonymName is mandatory.");
if (SynonymName.Length > 60)
{
log.Warning("Length > 60 - truncated");
SynonymName = SynonymName.Substring(0,60);
}
Set_Value ("SynonymName", SynonymName);
}
/** Get Synonym Name.
@return The synonym for the name */
public String GetSynonymName() 
{
return (String)Get_Value("SynonymName");
}
}

}