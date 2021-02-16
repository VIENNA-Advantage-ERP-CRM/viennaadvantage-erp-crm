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
/** Generated Model for VAM_PFeature_Value
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAM_PFeature_Value : PO
{
public X_VAM_PFeature_Value (Context ctx, int VAM_PFeature_Value_ID, Trx trxName) : base (ctx, VAM_PFeature_Value_ID, trxName)
{
/** if (VAM_PFeature_Value_ID == 0)
{
SetVAM_PFeature_Value_ID (0);
SetVAM_ProductFeature_ID (0);
SetName (null);
SetValue (null);
}
 */
}
public X_VAM_PFeature_Value (Ctx ctx, int VAM_PFeature_Value_ID, Trx trxName) : base (ctx, VAM_PFeature_Value_ID, trxName)
{
/** if (VAM_PFeature_Value_ID == 0)
{
SetVAM_PFeature_Value_ID (0);
SetVAM_ProductFeature_ID (0);
SetName (null);
SetValue (null);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_PFeature_Value (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_PFeature_Value (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAM_PFeature_Value (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAM_PFeature_Value()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514378425L;
/** Last Updated Timestamp 7/29/2010 1:07:41 PM */
public static long updatedMS = 1280389061636L;
/** VAF_TableView_ID=558 */
public static int Table_ID;
 // =558;

/** TableName=VAM_PFeature_Value */
public static String Table_Name="VAM_PFeature_Value";

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
StringBuilder sb = new StringBuilder ("X_VAM_PFeature_Value[").Append(Get_ID()).Append("]");
return sb.ToString();
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
/** Set Attribute Value.
@param VAM_PFeature_Value_ID Product Attribute Value */
public void SetVAM_PFeature_Value_ID (int VAM_PFeature_Value_ID)
{
if (VAM_PFeature_Value_ID < 1) throw new ArgumentException ("VAM_PFeature_Value_ID is mandatory.");
Set_ValueNoCheck ("VAM_PFeature_Value_ID", VAM_PFeature_Value_ID);
}
/** Get Attribute Value.
@return Product Attribute Value */
public int GetVAM_PFeature_Value_ID() 
{
Object ii = Get_Value("VAM_PFeature_Value_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Attribute.
@param VAM_ProductFeature_ID Product Attribute */
public void SetVAM_ProductFeature_ID (int VAM_ProductFeature_ID)
{
if (VAM_ProductFeature_ID < 1) throw new ArgumentException ("VAM_ProductFeature_ID is mandatory.");
Set_ValueNoCheck ("VAM_ProductFeature_ID", VAM_ProductFeature_ID);
}
/** Get Attribute.
@return Product Attribute */
public int GetVAM_ProductFeature_ID() 
{
Object ii = Get_Value("VAM_ProductFeature_ID");
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
/** Set Search Key.
@param Value Search key for the record in the format required - must be unique */
public void SetValue (String Value)
{
if (Value == null) throw new ArgumentException ("Value is mandatory.");
if (Value.Length > 40)
{
log.Warning("Length > 40 - truncated");
Value = Value.Substring(0,40);
}
Set_Value ("Value", Value);
}
/** Get Search Key.
@return Search key for the record in the format required - must be unique */
public String GetValue() 
{
return (String)Get_Value("Value");
}
}

}