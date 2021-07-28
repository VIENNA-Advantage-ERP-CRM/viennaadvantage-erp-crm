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
    using System.Data;/** Generated Model for M_MatchPO
 *  @author Raghu (Updated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
    public class X_M_MatchPO : PO
    {
        public X_M_MatchPO(Context ctx, int M_MatchPO_ID, Trx trxName) : base(ctx, M_MatchPO_ID, trxName)
        {/** if (M_MatchPO_ID == 0){SetC_OrderLine_ID (0);SetDateAcct (DateTime.Now);SetDateTrx (DateTime.Now);SetM_MatchPO_ID (0);SetM_Product_ID (0);SetPosted (false);SetProcessed (false);// N
SetProcessing (false);// N
SetQty (0.0);} */
        }
        public X_M_MatchPO(Ctx ctx, int M_MatchPO_ID, Trx trxName) : base(ctx, M_MatchPO_ID, trxName)
        {/** if (M_MatchPO_ID == 0){SetC_OrderLine_ID (0);SetDateAcct (DateTime.Now);SetDateTrx (DateTime.Now);SetM_MatchPO_ID (0);SetM_Product_ID (0);SetPosted (false);SetProcessed (false);// N
SetProcessing (false);// N
SetQty (0.0);} */
        }/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
        public X_M_MatchPO(Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName) { }/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
        public X_M_MatchPO(Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName) { }/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
        public X_M_MatchPO(Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName) { }/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
        static X_M_MatchPO() { Table_ID = Get_Table_ID(Table_Name); model = new KeyNamePair(Table_ID, Table_Name); }/** Serial Version No */
        static long serialVersionUID = 27909152579082L;/** Last Updated Timestamp 7/23/2021 8:01:02 AM */
        public static long updatedMS = 1627027262293L;/** AD_Table_ID=473 */
        public static int Table_ID; // =473;
        /** TableName=M_MatchPO */
        public static String Table_Name = "M_MatchPO";
        protected static KeyNamePair model; protected Decimal accessLevel = new Decimal(3);/** AccessLevel
@return 3 - Client - Org 
*/
        protected override int Get_AccessLevel() { return Convert.ToInt32(accessLevel.ToString()); }/** Load Meta Data
@param ctx context
@return PO Info
*/
        protected override POInfo InitPO(Context ctx) { POInfo poi = POInfo.GetPOInfo(ctx, Table_ID); return poi; }/** Load Meta Data
@param ctx context
@return PO Info
*/
        protected override POInfo InitPO(Ctx ctx) { POInfo poi = POInfo.GetPOInfo(ctx, Table_ID); return poi; }/** Info
@return info
*/
        public override String ToString() { StringBuilder sb = new StringBuilder("X_M_MatchPO[").Append(Get_ID()).Append("]"); return sb.ToString(); }/** Set Available Stock.
@param AvailableStock Available Stock into respective warehouse */
        public void SetAvailableStock(Decimal? AvailableStock) { Set_Value("AvailableStock", (Decimal?)AvailableStock); }/** Get Available Stock.
@return Available Stock into respective warehouse */
        public Decimal GetAvailableStock() { Object bd = Get_Value("AvailableStock"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Business Partner.
@param C_BPartner_ID Identifies a Customer/Prospect */
        public void SetC_BPartner_ID(int C_BPartner_ID)
        {
            if (C_BPartner_ID <= 0) Set_Value("C_BPartner_ID", null);
            else
                Set_Value("C_BPartner_ID", C_BPartner_ID);
        }/** Get Business Partner.
@return Identifies a Customer/Prospect */
        public int GetC_BPartner_ID() { Object ii = Get_Value("C_BPartner_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }/** Set Invoice Line.
@param C_InvoiceLine_ID Invoice Detail Line */
        public void SetC_InvoiceLine_ID(int C_InvoiceLine_ID)
        {
            if (C_InvoiceLine_ID <= 0) Set_ValueNoCheck("C_InvoiceLine_ID", null);
            else
                Set_ValueNoCheck("C_InvoiceLine_ID", C_InvoiceLine_ID);
        }/** Get Invoice Line.
@return Invoice Detail Line */
        public int GetC_InvoiceLine_ID() { Object ii = Get_Value("C_InvoiceLine_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }/** Set Order Line.
@param C_OrderLine_ID Order Line */
        public void SetC_OrderLine_ID(int C_OrderLine_ID) { if (C_OrderLine_ID < 1) throw new ArgumentException("C_OrderLine_ID is mandatory."); Set_ValueNoCheck("C_OrderLine_ID", C_OrderLine_ID); }/** Get Order Line.
@return Order Line */
        public int GetC_OrderLine_ID() { Object ii = Get_Value("C_OrderLine_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }/** Set Current Cost.
@param CurrentCostPrice The currently used cost price */
        public void SetCurrentCostPrice(Decimal? CurrentCostPrice) { Set_Value("CurrentCostPrice", (Decimal?)CurrentCostPrice); }/** Get Current Cost.
@return The currently used cost price */
        public Decimal GetCurrentCostPrice() { Object bd = Get_Value("CurrentCostPrice"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Account Date.
@param DateAcct General Ledger Date */
        public void SetDateAcct(DateTime? DateAcct) { if (DateAcct == null) throw new ArgumentException("DateAcct is mandatory."); Set_Value("DateAcct", (DateTime?)DateAcct); }/** Get Account Date.
@return General Ledger Date */
        public DateTime? GetDateAcct() { return (DateTime?)Get_Value("DateAcct"); }/** Set Transaction Date.
@param DateTrx Transaction Date */
        public void SetDateTrx(DateTime? DateTrx) { if (DateTrx == null) throw new ArgumentException("DateTrx is mandatory."); Set_ValueNoCheck("DateTrx", (DateTime?)DateTrx); }/** Get Transaction Date.
@return Transaction Date */
        public DateTime? GetDateTrx() { return (DateTime?)Get_Value("DateTrx"); }/** Set Description.
@param Description Optional short description of the record */
        public void SetDescription(String Description) { if (Description != null && Description.Length > 255) { log.Warning("Length > 255 - truncated"); Description = Description.Substring(0, 255); } Set_Value("Description", Description); }/** Get Description.
@return Optional short description of the record */
        public String GetDescription() { return (String)Get_Value("Description"); }/** Set Document No..
@param DocumentNo Document sequence number of the document */
        public void SetDocumentNo(String DocumentNo) { if (DocumentNo != null && DocumentNo.Length > 30) { log.Warning("Length > 30 - truncated"); DocumentNo = DocumentNo.Substring(0, 30); } Set_Value("DocumentNo", DocumentNo); }/** Get Document No..
@return Document sequence number of the document */
        public String GetDocumentNo() { return (String)Get_Value("DocumentNo"); }/** Get Record ID/ColumnName
@return ID/ColumnName pair */
        public KeyNamePair GetKeyNamePair() { return new KeyNamePair(Get_ID(), GetDocumentNo()); }/** Set Export.
@param Export_ID Export */
        public void SetExport_ID(String Export_ID) { if (Export_ID != null && Export_ID.Length > 50) { log.Warning("Length > 50 - truncated"); Export_ID = Export_ID.Substring(0, 50); } Set_ValueNoCheck("Export_ID", Export_ID); }/** Get Export.
@return Export */
        public String GetExport_ID() { return (String)Get_Value("Export_ID"); }/** Set Approved.
@param IsApproved Indicates if this document requires approval */
        public void SetIsApproved(Boolean IsApproved) { Set_Value("IsApproved", IsApproved); }/** Get Approved.
@return Indicates if this document requires approval */
        public Boolean IsApproved() { Object oo = Get_Value("IsApproved"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Cost Calculated.
@param IsCostCalculated This checkbox will auto set "True", when the cost is calculated for the document. */
        public void SetIsCostCalculated(Boolean IsCostCalculated) { Set_Value("IsCostCalculated", IsCostCalculated); }/** Get Cost Calculated.
@return This checkbox will auto set "True", when the cost is calculated for the document. */
        public Boolean IsCostCalculated() { Object oo = Get_Value("IsCostCalculated"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Match PO from Form.
@param IsMatchPOForm Match PO from Form */
        public void SetIsMatchPOForm(Boolean IsMatchPOForm) { Set_Value("IsMatchPOForm", IsMatchPOForm); }/** Get Match PO from Form.
@return Match PO from Form */
        public Boolean IsMatchPOForm() { Object oo = Get_Value("IsMatchPOForm"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Return Transaction.
@param IsReturnTrx This is a return transaction */
        public void SetIsReturnTrx(Boolean IsReturnTrx) { Set_Value("IsReturnTrx", IsReturnTrx); }/** Get Return Transaction.
@return This is a return transaction */
        public Boolean IsReturnTrx() { Object oo = Get_Value("IsReturnTrx"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Sales Transaction.
@param IsSOTrx This is a Sales Transaction */
        public void SetIsSOTrx(Boolean IsSOTrx) { Set_Value("IsSOTrx", IsSOTrx); }/** Get Sales Transaction.
@return This is a Sales Transaction */
        public Boolean IsSOTrx() { Object oo = Get_Value("IsSOTrx"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Attribute Set Instance.
@param M_AttributeSetInstance_ID Product Attribute Set Instance */
        public void SetM_AttributeSetInstance_ID(int M_AttributeSetInstance_ID)
        {
            if (M_AttributeSetInstance_ID <= 0) Set_ValueNoCheck("M_AttributeSetInstance_ID", null);
            else
                Set_ValueNoCheck("M_AttributeSetInstance_ID", M_AttributeSetInstance_ID);
        }/** Get Attribute Set Instance.
@return Product Attribute Set Instance */
        public int GetM_AttributeSetInstance_ID() { Object ii = Get_Value("M_AttributeSetInstance_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }/** Set Shipment/Receipt Line.
@param M_InOutLine_ID Line on Shipment or Receipt document */
        public void SetM_InOutLine_ID(int M_InOutLine_ID)
        {
            if (M_InOutLine_ID <= 0) Set_ValueNoCheck("M_InOutLine_ID", null);
            else
                Set_ValueNoCheck("M_InOutLine_ID", M_InOutLine_ID);
        }/** Get Shipment/Receipt Line.
@return Line on Shipment or Receipt document */
        public int GetM_InOutLine_ID() { Object ii = Get_Value("M_InOutLine_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }/** Set Match PO.
@param M_MatchPO_ID Match Purchase Order to Shipment/Receipt and Invoice */
        public void SetM_MatchPO_ID(int M_MatchPO_ID) { if (M_MatchPO_ID < 1) throw new ArgumentException("M_MatchPO_ID is mandatory."); Set_ValueNoCheck("M_MatchPO_ID", M_MatchPO_ID); }/** Get Match PO.
@return Match Purchase Order to Shipment/Receipt and Invoice */
        public int GetM_MatchPO_ID() { Object ii = Get_Value("M_MatchPO_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }/** Set Product.
@param M_Product_ID Product, Service, Item */
        public void SetM_Product_ID(int M_Product_ID) { if (M_Product_ID < 1) throw new ArgumentException("M_Product_ID is mandatory."); Set_ValueNoCheck("M_Product_ID", M_Product_ID); }/** Get Product.
@return Product, Service, Item */
        public int GetM_Product_ID() { Object ii = Get_Value("M_Product_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }/** Set Posted.
@param Posted Posting status */
        public void SetPosted(Boolean Posted) { Set_ValueNoCheck("Posted", Posted); }/** Get Posted.
@return Posting status */
        public Boolean IsPosted() { Object oo = Get_Value("Posted"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Price Difference.
@param PriceDifference Price Difference */
        public void SetPriceDifference(Decimal? PriceDifference) { Set_Value("PriceDifference", (Decimal?)PriceDifference); }/** Get Price Difference.
@return Price Difference */
        public Decimal GetPriceDifference() { Object bd = Get_Value("PriceDifference"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Price Match Difference.
@param PriceMatchDifference Difference between Purchase and Invoice Price per matched line */
        public void SetPriceMatchDifference(Decimal? PriceMatchDifference) { Set_Value("PriceMatchDifference", (Decimal?)PriceMatchDifference); }/** Get Price Match Difference.
@return Difference between Purchase and Invoice Price per matched line */
        public Decimal GetPriceMatchDifference() { Object bd = Get_Value("PriceMatchDifference"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set PO Price.
@param PricePO Price based on a purchase order */
        public void SetPricePO(Decimal? PricePO) { Set_Value("PricePO", (Decimal?)PricePO); }/** Get PO Price.
@return Price based on a purchase order */
        public Decimal GetPricePO() { Object bd = Get_Value("PricePO"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Processed.
@param Processed The document has been processed */
        public void SetProcessed(Boolean Processed) { Set_ValueNoCheck("Processed", Processed); }/** Get Processed.
@return The document has been processed */
        public Boolean IsProcessed() { Object oo = Get_Value("Processed"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Process Now.
@param Processing Process Now */
        public void SetProcessing(Boolean Processing) { Set_Value("Processing", Processing); }/** Get Process Now.
@return Process Now */
        public Boolean IsProcessing() { Object oo = Get_Value("Processing"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }/** Set Quantity.
@param Qty Quantity */
        public void SetQty(Decimal? Qty) { if (Qty == null) throw new ArgumentException("Qty is mandatory."); Set_ValueNoCheck("Qty", (Decimal?)Qty); }/** Get Quantity.
@return Quantity */
        public Decimal GetQty() { Object bd = Get_Value("Qty"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }
    }
}