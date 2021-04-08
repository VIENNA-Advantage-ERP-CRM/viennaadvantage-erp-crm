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
    /** Generated Model for VAB_InvoiceLine
     *  @author Jagmohan Bhatt (generated) 
     *  @version Vienna Framework 1.1.1 - $Id$ */
    public class X_VAB_InvoiceLine : PO
    {
        public X_VAB_InvoiceLine(Context ctx, int VAB_InvoiceLine_ID, Trx trxName)
            : base(ctx, VAB_InvoiceLine_ID, trxName)
        {
            /** if (VAB_InvoiceLine_ID == 0)
            { 
            SetVAB_InvoiceLine_ID (0);
            SetVAB_Invoice_ID (0);
            SetIsDescription (false);	// N
            SetIsPrinted (true);	// Y
            SetLine (0);	// @SQL=SELECT NVL(MAX(Line),0)+10 AS DefaultValue FROM VAB_InvoiceLine WHERE VAB_Invoice_ID=@VAB_Invoice_ID@
            SetLineNetAmt (0.0);
            SetPriceActual (0.0);
            SetPriceEntered (0.0);
            SetPriceLimit (0.0);
            SetPriceList (0.0);
            SetProcessed (false);	// N
            SetQtyEntered (0.0);	// 1
            SetQtyInvoiced (0.0);	// 1
            }
             */
        }
        public X_VAB_InvoiceLine(Ctx ctx, int VAB_InvoiceLine_ID, Trx trxName)
            : base(ctx, VAB_InvoiceLine_ID, trxName)
        {
            /** if (VAB_InvoiceLine_ID == 0)
            {
            SetVAB_InvoiceLine_ID (0);
            SetVAB_Invoice_ID (0);
            SetIsDescription (false);	// N
            SetIsPrinted (true);	// Y
            SetLine (0);	// @SQL=SELECT NVL(MAX(Line),0)+10 AS DefaultValue FROM VAB_InvoiceLine WHERE VAB_Invoice_ID=@VAB_Invoice_ID@
            SetLineNetAmt (0.0);
            SetPriceActual (0.0);
            SetPriceEntered (0.0);
            SetPriceLimit (0.0);
            SetPriceList (0.0);
            SetProcessed (false);	// N
            SetQtyEntered (0.0);	// 1
            SetQtyInvoiced (0.0);	// 1
            }
             */
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAB_InvoiceLine(Context ctx, DataRow rs, Trx trxName)
            : base(ctx, rs, trxName)
        {
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAB_InvoiceLine(Ctx ctx, DataRow rs, Trx trxName)
            : base(ctx, rs, trxName)
        {
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAB_InvoiceLine(Ctx ctx, IDataReader dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }
        /** Static Constructor 
         Set Table ID By Table Name
         added by ->Harwinder */
        static X_VAB_InvoiceLine()
        {
            Table_ID = Get_Table_ID(Table_Name);
            model = new KeyNamePair(Table_ID, Table_Name);
        }
        /** Serial Version No */
        static long serialVersionUID = 27562514372485L;
        /** Last Updated Timestamp 7/29/2010 1:07:35 PM */
        public static long updatedMS = 1280389055696L;
        /** VAF_TableView_ID=333 */
        public static int Table_ID;
        // =333;

        /** TableName=VAB_InvoiceLine */
        public static String Table_Name = "VAB_InvoiceLine";

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
        protected override POInfo InitPO(Context ctx)
        {
            POInfo poi = POInfo.GetPOInfo(ctx, Table_ID);
            return poi;
        }
        /** Load Meta Data
        @param ctx context
        @return PO Info
        */
        protected override POInfo InitPO(Ctx ctx)
        {
            POInfo poi = POInfo.GetPOInfo(ctx, Table_ID);
            return poi;
        }
        /** Info
        @return info
        */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("X_VAB_InvoiceLine[").Append(Get_ID()).Append("]");
            return sb.ToString();
        }

        /** VAF_OrgTrx_ID VAF_Control_Ref_ID=130 */
        public static int VAF_ORGTRX_ID_VAF_Control_Ref_ID = 130;
        /** Set Trx Organization.
        @param VAF_OrgTrx_ID Performing or initiating organization */
        public void SetVAF_OrgTrx_ID(int VAF_OrgTrx_ID)
        {
            if (VAF_OrgTrx_ID <= 0) Set_Value("VAF_OrgTrx_ID", null);
            else
                Set_Value("VAF_OrgTrx_ID", VAF_OrgTrx_ID);
        }
        /** Get Trx Organization.
        @return Performing or initiating organization */
        public int GetVAF_OrgTrx_ID()
        {
            Object ii = Get_Value("VAF_OrgTrx_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Asset.
        @param VAA_Asset_ID Asset used internally or by customers */
        public void SetA_Asset_ID(int VAA_Asset_ID)
        {
            if (VAA_Asset_ID <= 0) Set_Value("VAA_Asset_ID", null);
            else
                Set_Value("VAA_Asset_ID", VAA_Asset_ID);
        }
        /** Get Asset.
        @return Asset used internally or by customers */
        public int GetA_Asset_ID()
        {
            Object ii = Get_Value("VAA_Asset_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Activity.
        @param VAB_BillingCode_ID Business Activity */
        public void SetVAB_BillingCode_ID(int VAB_BillingCode_ID)
        {
            if (VAB_BillingCode_ID <= 0) Set_Value("VAB_BillingCode_ID", null);
            else
                Set_Value("VAB_BillingCode_ID", VAB_BillingCode_ID);
        }
        /** Get Activity.
        @return Business Activity */
        public int GetVAB_BillingCode_ID()
        {
            Object ii = Get_Value("VAB_BillingCode_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Campaign.
        @param VAB_Promotion_ID Marketing Campaign */
        public void SetVAB_Promotion_ID(int VAB_Promotion_ID)
        {
            if (VAB_Promotion_ID <= 0) Set_Value("VAB_Promotion_ID", null);
            else
                Set_Value("VAB_Promotion_ID", VAB_Promotion_ID);
        }
        /** Get Campaign.
        @return Marketing Campaign */
        public int GetVAB_Promotion_ID()
        {
            Object ii = Get_Value("VAB_Promotion_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Charge.
        @param VAB_Charge_ID Additional document charges */
        public void SetVAB_Charge_ID(int VAB_Charge_ID)
        {
            if (VAB_Charge_ID <= 0) Set_Value("VAB_Charge_ID", null);
            else
                Set_Value("VAB_Charge_ID", VAB_Charge_ID);
        }
        /** Get Charge.
        @return Additional document charges */
        public int GetVAB_Charge_ID()
        {
            Object ii = Get_Value("VAB_Charge_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Invoice Line.
        @param VAB_InvoiceLine_ID Invoice Detail Line */
        public void SetVAB_InvoiceLine_ID(int VAB_InvoiceLine_ID)
        {
            if (VAB_InvoiceLine_ID < 1) throw new ArgumentException("VAB_InvoiceLine_ID is mandatory.");
            Set_ValueNoCheck("VAB_InvoiceLine_ID", VAB_InvoiceLine_ID);
        }
        /** Get Invoice Line.
        @return Invoice Detail Line */
        public int GetVAB_InvoiceLine_ID()
        {
            Object ii = Get_Value("VAB_InvoiceLine_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Invoice.
        @param VAB_Invoice_ID Invoice Identifier */
        public void SetVAB_Invoice_ID(int VAB_Invoice_ID)
        {
            if (VAB_Invoice_ID < 1) throw new ArgumentException("VAB_Invoice_ID is mandatory.");
            Set_ValueNoCheck("VAB_Invoice_ID", VAB_Invoice_ID);
        }
        /** Get Invoice.
        @return Invoice Identifier */
        public int GetVAB_Invoice_ID()
        {
            Object ii = Get_Value("VAB_Invoice_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Get Record ID/ColumnName
        @return ID/ColumnName pair */
        public KeyNamePair GetKeyNamePair()
        {
            return new KeyNamePair(Get_ID(), GetVAB_Invoice_ID().ToString());
        }
        /** Set Order Line.
        @param VAB_OrderLine_ID Order Line */
        public void SetVAB_OrderLine_ID(int VAB_OrderLine_ID)
        {
            if (VAB_OrderLine_ID <= 0) Set_ValueNoCheck("VAB_OrderLine_ID", null);
            else
                Set_ValueNoCheck("VAB_OrderLine_ID", VAB_OrderLine_ID);
        }
        /** Get Order Line.
        @return Order Line */
        public int GetVAB_OrderLine_ID()
        {
            Object ii = Get_Value("VAB_OrderLine_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Project Phase.
        @param VAB_ProjectStage_ID Phase of a Project */
        public void SetVAB_ProjectStage_ID(int VAB_ProjectStage_ID)
        {
            if (VAB_ProjectStage_ID <= 0) Set_ValueNoCheck("VAB_ProjectStage_ID", null);
            else
                Set_ValueNoCheck("VAB_ProjectStage_ID", VAB_ProjectStage_ID);
        }
        /** Get Project Phase.
        @return Phase of a Project */
        public int GetVAB_ProjectStage_ID()
        {
            Object ii = Get_Value("VAB_ProjectStage_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Project Task.
        @param VAB_ProjectJob_ID Actual Project Task in a Phase */
        public void SetVAB_ProjectJob_ID(int VAB_ProjectJob_ID)
        {
            if (VAB_ProjectJob_ID <= 0) Set_ValueNoCheck("VAB_ProjectJob_ID", null);
            else
                Set_ValueNoCheck("VAB_ProjectJob_ID", VAB_ProjectJob_ID);
        }
        /** Get Project Task.
        @return Actual Project Task in a Phase */
        public int GetVAB_ProjectJob_ID()
        {
            Object ii = Get_Value("VAB_ProjectJob_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Project.
        @param VAB_Project_ID Financial Project */
        public void SetVAB_Project_ID(int VAB_Project_ID)
        {
            if (VAB_Project_ID <= 0) Set_Value("VAB_Project_ID", null);
            else
                Set_Value("VAB_Project_ID", VAB_Project_ID);
        }
        /** Get Project.
        @return Financial Project */
        public int GetVAB_Project_ID()
        {
            Object ii = Get_Value("VAB_Project_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Tax.
        @param VAB_TaxRate_ID Tax identifier */
        public void SetVAB_TaxRate_ID(int VAB_TaxRate_ID)
        {
            if (VAB_TaxRate_ID <= 0) Set_Value("VAB_TaxRate_ID", null);
            else
                Set_Value("VAB_TaxRate_ID", VAB_TaxRate_ID);
        }
        /** Get Tax.
        @return Tax identifier */
        public int GetVAB_TaxRate_ID()
        {
            Object ii = Get_Value("VAB_TaxRate_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set UOM.
        @param VAB_UOM_ID Unit of Measure */
        public void SetVAB_UOM_ID(int VAB_UOM_ID)
        {
            if (VAB_UOM_ID <= 0) Set_ValueNoCheck("VAB_UOM_ID", null);
            else
                Set_ValueNoCheck("VAB_UOM_ID", VAB_UOM_ID);
        }
        /** Get UOM.
        @return Unit of Measure */
        public int GetVAB_UOM_ID()
        {
            Object ii = Get_Value("VAB_UOM_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Discount per Unit.
        @param Discount per Unit */
        public void SetED007_DiscountPerUnit(Decimal? ED007_DiscountPerUnit)
        {
            Set_Value("ED007_DiscountPerUnit", (Decimal?)ED007_DiscountPerUnit);
        }
        /** Get Discount per Unit.
        @return Discount per Unit */
        public Decimal GetED007_DiscountPerUnit()
        {
            Object bd = Get_Value("ED007_DiscountPerUnit");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set ED007_DiscountPercent.
        @param ED007_DiscountPercent */
        public void SetED007_DiscountPercent(Decimal? ED007_DiscountPercent)
        {
            Set_Value("ED007_DiscountPercent", (Decimal?)ED007_DiscountPercent);
        }
        /** Get Discount Percentage 5.
        @return Discount Percentage 5 */
        public Decimal GetED007_DiscountPercent()
        {
            Object bd = Get_Value("ED007_DiscountPercent");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Discount Percentage 1.
        @param ED007_DiscountPercentage1 */
        public void SetED007_DiscountPercentage1(Decimal? ED007_DiscountPercentage1)
        {
            Set_Value("ED007_DiscountPercentage1", (Decimal?)ED007_DiscountPercentage1);
        }
        /** Get Discount Percentage 1.
        @return Discount Percentage 1 */
        public Decimal GetED007_DiscountPercentage1()
        {
            Object bd = Get_Value("ED007_DiscountPercentage1");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** Set Discount Percentage 2.
        @param ED007_DiscountPercentage2*/
        public void SetED007_DiscountPercentage2(Decimal? ED007_DiscountPercentage2)
        {
            Set_Value("ED007_DiscountPercentage2", (Decimal?)ED007_DiscountPercentage2);
        }
        /** Get Discount Percentage 2.
        @return Discount Percentage 2 */
        public Decimal GetED007_DiscountPercentage2()
        {
            Object bd = Get_Value("ED007_DiscountPercentage2");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** Set Discount Percentage 3.
        @param ED007_DiscountPercentage3 */
        public void SetED007_DiscountPercentage3(Decimal? ED007_DiscountPercentage3)
        {
            Set_Value("ED007_DiscountPercentage3", (Decimal?)ED007_DiscountPercentage3);
        }
        /** Get Discount Percentage 3.
        @return Discount Percentage 3 */
        public Decimal GetED007_DiscountPercentage3()
        {
            Object bd = Get_Value("ED007_DiscountPercentage3");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** Set Discount Percentage 4.
        @param ED007_DiscountPercentage4 */
        public void SetED007_DiscountPercentage4(Decimal? ED007_DiscountPercentage4)
        {
            Set_Value("ED007_DiscountPercentage4", (Decimal?)ED007_DiscountPercentage4);
        }
        /** Get Discount Percentage 4.
        @return Discount Percentage 4 */
        public Decimal GetED007_DiscountPercentage4()
        {
            Object bd = Get_Value("ED007_DiscountPercentage4");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** Set Discount Percentage 5.
        @param ED007_DiscountPercentage5 */
        public void SetED007_DiscountPercentage5(Decimal? ED007_DiscountPercentage5)
        {
            Set_Value("ED007_DiscountPercentage5", (Decimal?)ED007_DiscountPercentage5);
        }
        /** Get Discount Percentage 5.
        @return Discount Percentage 5 */
        public Decimal GetED007_DiscountPercentage5()
        {
            Object bd = Get_Value("ED007_DiscountPercentage5");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Discount Percent%.
        @param ED004_DiscntPrcnt Discount in percent */
        public void SetED004_DiscntPrcnt(Decimal? ED004_DiscntPrcnt)
        {
            Set_Value("Discount", (Decimal?)ED004_DiscntPrcnt);
        }


        /** Set Discount Percent%.
       @param VA025_DiscntPrcnt Discount in percent */
        public void SetVA025_DiscntPrcnt(Decimal? VA025_DiscntPrcnt)
        {
            Set_Value("VA025_DiscntPrcnt", (Decimal?)VA025_DiscntPrcnt);
        }

        /** Set ED007_DiscountAmount.
        @param ED007_DiscountAmount */
        public void SetED007_DiscountAmount(Decimal? ED007_DiscountAmount)
        {
            Set_Value("ED007_DiscountAmount", (Decimal?)ED007_DiscountAmount);
        }
        /** Get ED007_DiscountAmount.
        @return ED007_DiscountAmount */
        public Decimal GetED007_DiscountAmount()
        {
            Object bd = Get_Value("ED007_DiscountAmount");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** Set ED007_DscuntlineAmt.
        @param ED007_DscuntlineAmt */
        public void SetED007_DscuntlineAmt(Decimal? ED007_DscuntlineAmt)
        {
            Set_Value("ED007_DscuntlineAmt", (Decimal?)ED007_DscuntlineAmt);
        }
        /** Get ED007_DscuntlineAmt.
        @return ED007_DscuntlineAmt */
        public Decimal GetED007_DscuntlineAmt()
        {
            Object bd = Get_Value("ED007_DscuntlineAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Value Base Discount.
        @param Value Base Discount */
        public void SetED007_ValueBaseDiscount(Decimal? ED007_ValueBaseDiscount)
        {
            Set_Value("ED007_ValueBaseDiscount", (Decimal?)ED007_ValueBaseDiscount);
        }
        /** Get Value Base Discount.
        @return Value Base Discount */
        public Decimal GetED007_ValueBaseDiscount()
        {
            Object bd = Get_Value("ED007_ValueBaseDiscount");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Description.
        @param Description Optional short description of the record */
        public void SetDescription(String Description)
        {
            if (Description != null && Description.Length > 255)
            {
                log.Warning("Length > 255 - truncated");
                Description = Description.Substring(0, 255);
            }
            Set_Value("Description", Description);
        }
        /** Get Description.
        @return Optional short description of the record */
        public String GetDescription()
        {
            return (String)Get_Value("Description");
        }
        /** Set Description Only.
        @param IsDescription if true, the line is just description and no transaction */
        public void SetIsDescription(Boolean IsDescription)
        {
            Set_Value("IsDescription", IsDescription);
        }
        /** Get Description Only.
        @return if true, the line is just description and no transaction */
        public Boolean IsDescription()
        {
            Object oo = Get_Value("IsDescription");
            if (oo != null)
            {
                if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
                return "Y".Equals(oo);
            }
            return false;
        }
        /** Set Printed.
        @param IsPrinted Indicates if this document / line is printed */
        public void SetIsPrinted(Boolean IsPrinted)
        {
            Set_Value("IsPrinted", IsPrinted);
        }
        /** Get Printed.
        @return Indicates if this document / line is printed */
        public Boolean IsPrinted()
        {
            Object oo = Get_Value("IsPrinted");
            if (oo != null)
            {
                if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
                return "Y".Equals(oo);
            }
            return false;
        }
        /** Set Line No.
        @param Line Unique line for this document */
        public void SetLine(int Line)
        {
            Set_Value("Line", Line);
        }
        /** Get Line No.
        @return Unique line for this document */
        public int GetLine()
        {
            Object ii = Get_Value("Line");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }

        /** LineDocStatus VAF_Control_Ref_ID=131 */
        public static int LINEDOCSTATUS_VAF_Control_Ref_ID = 131;
        /** Unknown = ?? */
        public static String LINEDOCSTATUS_Unknown = "??";
        /** Approved = AP */
        public static String LINEDOCSTATUS_Approved = "AP";
        /** Closed = CL */
        public static String LINEDOCSTATUS_Closed = "CL";
        /** Completed = CO */
        public static String LINEDOCSTATUS_Completed = "CO";
        /** Drafted = DR */
        public static String LINEDOCSTATUS_Drafted = "DR";
        /** Invalid = IN */
        public static String LINEDOCSTATUS_Invalid = "IN";
        /** In Progress = IP */
        public static String LINEDOCSTATUS_InProgress = "IP";
        /** Not Approved = NA */
        public static String LINEDOCSTATUS_NotApproved = "NA";
        /** Reversed = RE */
        public static String LINEDOCSTATUS_Reversed = "RE";
        /** Voided = VO */
        public static String LINEDOCSTATUS_Voided = "VO";
        /** Waiting Confirmation = WC */
        public static String LINEDOCSTATUS_WaitingConfirmation = "WC";
        /** Waiting Payment = WP */
        public static String LINEDOCSTATUS_WaitingPayment = "WP";
        /** Is test a valid value.
        @param test testvalue
        @returns true if valid **/
        public bool IsLineDocStatusValid(String test)
        {
            return test == null || test.Equals("??") || test.Equals("AP") || test.Equals("CL") || test.Equals("CO") || test.Equals("DR") || test.Equals("IN") || test.Equals("IP") || test.Equals("NA") || test.Equals("RE") || test.Equals("VO") || test.Equals("WC") || test.Equals("WP");
        }
        /** Set Line Document Status.
        @param LineDocStatus The current status of the document line */
        public void SetLineDocStatus(String LineDocStatus)
        {
            if (!IsLineDocStatusValid(LineDocStatus))
                throw new ArgumentException("LineDocStatus Invalid value - " + LineDocStatus + " - Reference_ID=131 - ?? - AP - CL - CO - DR - IN - IP - NA - RE - VO - WC - WP");
            if (LineDocStatus != null && LineDocStatus.Length > 2)
            {
                log.Warning("Length > 2 - truncated");
                LineDocStatus = LineDocStatus.Substring(0, 2);
            }
            Set_Value("LineDocStatus", LineDocStatus);
        }
        /** Get Line Document Status.
        @return The current status of the document line */
        public String GetLineDocStatus()
        {
            return (String)Get_Value("LineDocStatus");
        }
        /** Set Line Amount.
        @param LineNetAmt Line Extended Amount (Quantity * Actual Price) without Freight and Charges */
        public void SetLineNetAmt(Decimal? LineNetAmt)
        {
            if (LineNetAmt == null) throw new ArgumentException("LineNetAmt is mandatory.");
            Set_ValueNoCheck("LineNetAmt", (Decimal?)LineNetAmt);
        }
        /** Get Line Amount.
        @return Line Extended Amount (Quantity * Actual Price) without Freight and Charges */
        public Decimal GetLineNetAmt()
        {
            Object bd = Get_Value("LineNetAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Line Total.
        @param LineTotalAmt Total line amount incl. Tax */
        public void SetLineTotalAmt(Decimal? LineTotalAmt)
        {
            Set_Value("LineTotalAmt", (Decimal?)LineTotalAmt);
        }
        /** Get Line Total.
        @return Total line amount incl. Tax */
        public Decimal GetLineTotalAmt()
        {
            Object bd = Get_Value("LineTotalAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Attribute Set Instance.
        @param VAM_PFeature_SetInstance_ID Product Attribute Set Instance */
        public void SetVAM_PFeature_SetInstance_ID(int VAM_PFeature_SetInstance_ID)
        {
            if (VAM_PFeature_SetInstance_ID <= 0) Set_Value("VAM_PFeature_SetInstance_ID", null);
            else
                Set_Value("VAM_PFeature_SetInstance_ID", VAM_PFeature_SetInstance_ID);
        }
        /** Get Attribute Set Instance.
        @return Product Attribute Set Instance */
        public int GetVAM_PFeature_SetInstance_ID()
        {
            Object ii = Get_Value("VAM_PFeature_SetInstance_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Shipment/Receipt Line.
        @param VAM_Inv_InOutLine_ID Line on Shipment or Receipt document */
        public void SetVAM_Inv_InOutLine_ID(int VAM_Inv_InOutLine_ID)
        {
            if (VAM_Inv_InOutLine_ID <= 0) Set_ValueNoCheck("VAM_Inv_InOutLine_ID", null);
            else
                Set_ValueNoCheck("VAM_Inv_InOutLine_ID", VAM_Inv_InOutLine_ID);
        }
        /** Get Shipment/Receipt Line.
        @return Line on Shipment or Receipt document */
        public int GetVAM_Inv_InOutLine_ID()
        {
            Object ii = Get_Value("VAM_Inv_InOutLine_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Product.
        @param VAM_Product_ID Product, Service, Item */
        public void SetVAM_Product_ID(int VAM_Product_ID)
        {
            if (VAM_Product_ID <= 0) Set_Value("VAM_Product_ID", null);
            else
                Set_Value("VAM_Product_ID", VAM_Product_ID);
        }
        /** Get Product.
        @return Product, Service, Item */
        public int GetVAM_Product_ID()
        {
            Object ii = Get_Value("VAM_Product_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Unit Price.
        @param PriceActual Actual Price */
        public void SetPriceActual(Decimal? PriceActual)
        {
            if (PriceActual == null) throw new ArgumentException("PriceActual is mandatory.");
            Set_ValueNoCheck("PriceActual", (Decimal?)PriceActual);
        }
        /** Get Unit Price.
        @return Actual Price */
        public Decimal GetPriceActual()
        {
            Object bd = Get_Value("PriceActual");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Price.
        @param PriceEntered Price Entered - the price based on the selected/base UoM */
        public void SetPriceEntered(Decimal? PriceEntered)
        {
            if (PriceEntered == null) throw new ArgumentException("PriceEntered is mandatory.");
            Set_Value("PriceEntered", (Decimal?)PriceEntered);
        }
        /** Get Price.
        @return Price Entered - the price based on the selected/base UoM */
        public Decimal GetPriceEntered()
        {
            Object bd = Get_Value("PriceEntered");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Limit Price.
        @param PriceLimit Lowest price for a product */
        public void SetPriceLimit(Decimal? PriceLimit)
        {
            if (PriceLimit == null) throw new ArgumentException("PriceLimit is mandatory.");
            Set_Value("PriceLimit", (Decimal?)PriceLimit);
        }
        /** Get Limit Price.
        @return Lowest price for a product */
        public Decimal GetPriceLimit()
        {
            Object bd = Get_Value("PriceLimit");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set List Price.
        @param PriceList List Price */
        public void SetPriceList(Decimal? PriceList)
        {
            if (PriceList == null) throw new ArgumentException("PriceList is mandatory.");
            Set_Value("PriceList", (Decimal?)PriceList);
        }
        /** Get List Price.
        @return List Price */
        public Decimal GetPriceList()
        {
            Object bd = Get_Value("PriceList");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Processed.
        @param Processed The document has been processed */
        public void SetProcessed(Boolean Processed)
        {
            Set_Value("Processed", Processed);
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
        /** Set Quantity.
        @param QtyEntered The Quantity Entered is based on the selected UoM */
        public void SetQtyEntered(Decimal? QtyEntered)
        {
            if (QtyEntered == null) throw new ArgumentException("QtyEntered is mandatory.");
            Set_Value("QtyEntered", (Decimal?)QtyEntered);
        }
        /** Get Quantity.
        @return The Quantity Entered is based on the selected UoM */
        public Decimal GetQtyEntered()
        {
            Object bd = Get_Value("QtyEntered");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Quantity Invoiced.
        @param QtyInvoiced Invoiced Quantity */
        public void SetQtyInvoiced(Decimal? QtyInvoiced)
        {
            if (QtyInvoiced == null) throw new ArgumentException("QtyInvoiced is mandatory.");
            Set_Value("QtyInvoiced", (Decimal?)QtyInvoiced);
        }
        /** Get Quantity Invoiced.
        @return Invoiced Quantity */
        public Decimal GetQtyInvoiced()
        {
            Object bd = Get_Value("QtyInvoiced");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Revenue Recognition Amt.
        @param RRAmt Revenue Recognition Amount */
        public void SetRRAmt(Decimal? RRAmt)
        {
            Set_Value("RRAmt", (Decimal?)RRAmt);
        }
        /** Get Revenue Recognition Amt.
        @return Revenue Recognition Amount */
        public Decimal GetRRAmt()
        {
            Object bd = Get_Value("RRAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Revenue Recognition Start.
        @param RRStartDate Revenue Recognition Start Date */
        public void SetRRStartDate(DateTime? RRStartDate)
        {
            Set_Value("RRStartDate", (DateTime?)RRStartDate);
        }
        /** Get Revenue Recognition Start.
        @return Revenue Recognition Start Date */
        public DateTime? GetRRStartDate()
        {
            return (DateTime?)Get_Value("RRStartDate");
        }
        /** Set Referenced Invoice Line.
        @param Ref_InvoiceLine_ID Referenced Invoice Line */
        public void SetRef_InvoiceLine_ID(int Ref_InvoiceLine_ID)
        {
            if (Ref_InvoiceLine_ID <= 0) Set_Value("Ref_InvoiceLine_ID", null);
            else
                Set_Value("Ref_InvoiceLine_ID", Ref_InvoiceLine_ID);
        }
        /** Get Referenced Invoice Line.
        @return Referenced Invoice Line */
        public int GetRef_InvoiceLine_ID()
        {
            Object ii = Get_Value("Ref_InvoiceLine_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Assigned Resource.
        @param VAS_Res_Assignment_ID Assigned Resource */
        public void SetVAS_Res_Assignment_ID(int VAS_Res_Assignment_ID)
        {
            if (VAS_Res_Assignment_ID <= 0) Set_ValueNoCheck("VAS_Res_Assignment_ID", null);
            else
                Set_ValueNoCheck("VAS_Res_Assignment_ID", VAS_Res_Assignment_ID);
        }
        /** Get Assigned Resource.
        @return Assigned Resource */
        public int GetVAS_Res_Assignment_ID()
        {
            Object ii = Get_Value("VAS_Res_Assignment_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /// <summary>
        /// Set Surcharge Amount.
        /// </summary>
        /// <param name="SurchargeAmt">Surcharge Amount for a document</param>
        public void SetSurchargeAmt(Decimal? SurchargeAmt)
        {
            Set_Value("SurchargeAmt", (Decimal?)SurchargeAmt);
        }
        /// <summary>
        /// Get Surcharge Amount.
        /// </summary>
        /// <returns>Surcharge Amount for a document</returns>
        public Decimal GetSurchargeAmt()
        {
            Object bd = Get_Value("SurchargeAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Tax Amount.
        @param TaxAmt Tax Amount for a document */
        public void SetTaxAmt(Decimal? TaxAmt)
        {
            Set_Value("TaxAmt", (Decimal?)TaxAmt);
        }
        /** Get Tax Amount.
        @return Tax Amount for a document */
        public Decimal GetTaxAmt()
        {
            Object bd = Get_Value("TaxAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** User1_ID VAF_Control_Ref_ID=134 */
        public static int USER1_ID_VAF_Control_Ref_ID = 134;
        /** Set User List 1.
        @param User1_ID User defined list element #1 */
        public void SetUser1_ID(int User1_ID)
        {
            if (User1_ID <= 0) Set_Value("User1_ID", null);
            else
                Set_Value("User1_ID", User1_ID);
        }
        /** Get User List 1.
        @return User defined list element #1 */
        public int GetUser1_ID()
        {
            Object ii = Get_Value("User1_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }

        /** User2_ID VAF_Control_Ref_ID=137 */
        public static int USER2_ID_VAF_Control_Ref_ID = 137;
        /** Set User List 2.
        @param User2_ID User defined list element #2 */
        public void SetUser2_ID(int User2_ID)
        {
            if (User2_ID <= 0) Set_Value("User2_ID", null);
            else
                Set_Value("User2_ID", User2_ID);
        }
        /** Get User List 2.
        @return User defined list element #2 */
        public int GetUser2_ID()
        {
            Object ii = Get_Value("User2_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }

        // Change by mohit asked by ravikant 21/03/2016
        /** Set BasePrice.
        @param BasePrice This column contains the original price of product from pricelist . */
        public void SetBasePrice(Decimal? BasePrice)
        {
            Set_Value("BasePrice", (Decimal?)BasePrice);
        }
        /** Get BasePrice.
        @return This column contains the original price of product from pricelist . */
        public Decimal GetBasePrice()
        {
            Object bd = Get_Value("BasePrice");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        //------------------------------------
        /** Set Discount Per Unit.
@param VA025_DiscountPerUnit Discount Per Unit */
        public void SetVA025_DiscountPerUnit(Decimal? VA025_DiscountPerUnit) { Set_Value("VA025_DiscountPerUnit", (Decimal?)VA025_DiscountPerUnit); }/** Get Discount Per Unit.
@return Discount Per Unit */
        public Decimal GetVA025_DiscountPerUnit() { Object bd = Get_Value("VA025_DiscountPerUnit"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Discount Percentage 1.
@param VA025_DiscountPercentage1 Discount Percentage 1 */
        public void SetVA025_DiscountPercentage1(Decimal? VA025_DiscountPercentage1) { Set_Value("VA025_DiscountPercentage1", (Decimal?)VA025_DiscountPercentage1); }/** Get Discount Percentage 1.
@return Discount Percentage 1 */
        public Decimal GetVA025_DiscountPercentage1() { Object bd = Get_Value("VA025_DiscountPercentage1"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Discount Percentage 2.
@param VA025_DiscountPercentage2 Discount Percentage 2 */
        public void SetVA025_DiscountPercentage2(Decimal? VA025_DiscountPercentage2) { Set_Value("VA025_DiscountPercentage2", (Decimal?)VA025_DiscountPercentage2); }/** Get Discount Percentage 2.
@return Discount Percentage 2 */
        public Decimal GetVA025_DiscountPercentage2() { Object bd = Get_Value("VA025_DiscountPercentage2"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Discount Percentage 3.
@param VA025_DiscountPercentage3 Discount Percentage 3 */
        public void SetVA025_DiscountPercentage3(Decimal? VA025_DiscountPercentage3) { Set_Value("VA025_DiscountPercentage3", (Decimal?)VA025_DiscountPercentage3); }/** Get Discount Percentage 3.
@return Discount Percentage 3 */
        public Decimal GetVA025_DiscountPercentage3() { Object bd = Get_Value("VA025_DiscountPercentage3"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Discount Percentage 4.
@param VA025_DiscountPercentage4 Discount Percentage 4 */
        public void SetVA025_DiscountPercentage4(Decimal? VA025_DiscountPercentage4) { Set_Value("VA025_DiscountPercentage4", (Decimal?)VA025_DiscountPercentage4); }/** Get Discount Percentage 4.
@return Discount Percentage 4 */
        public Decimal GetVA025_DiscountPercentage4() { Object bd = Get_Value("VA025_DiscountPercentage4"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Discount Percentage 5.
@param VA025_DiscountPercentage5 Discount Percentage 5 */
        public void SetVA025_DiscountPercentage5(Decimal? VA025_DiscountPercentage5) { Set_Value("VA025_DiscountPercentage5", (Decimal?)VA025_DiscountPercentage5); }/** Get Discount Percentage 5.
@return Discount Percentage 5 */
        public Decimal GetVA025_DiscountPercentage5() { Object bd = Get_Value("VA025_DiscountPercentage5"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Discount Line Amount.
@param VA025_DscuntLineAmt Discount Line Amount */
        public void SetVA025_DscuntLineAmt(Decimal? VA025_DscuntLineAmt) { Set_Value("VA025_DscuntLineAmt", (Decimal?)VA025_DscuntLineAmt); }/** Get Discount Line Amount.
@return Discount Line Amount */
        public Decimal GetVA025_DscuntLineAmt() { Object bd = Get_Value("VA025_DscuntLineAmt"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Promotional Amount.
@param VA025_PromotionalAmount Promotional Amount */
        public void SetVA025_PromotionalAmount(Decimal? VA025_PromotionalAmount) { Set_Value("VA025_PromotionalAmount", (Decimal?)VA025_PromotionalAmount); }/** Get Promotional Amount.
@return Promotional Amount */
        public Decimal GetVA025_PromotionalAmount() { Object bd = Get_Value("VA025_PromotionalAmount"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }/** Set Value Base Discount.
@param VA025_ValueBaseDiscount Value Base Discount */
        public void SetVA025_ValueBaseDiscount(Decimal? VA025_ValueBaseDiscount) { Set_Value("VA025_ValueBaseDiscount", (Decimal?)VA025_ValueBaseDiscount); }/** Get Value Base Discount.
@return Value Base Discount */
        public Decimal GetVA025_ValueBaseDiscount() { Object bd = Get_Value("VA025_ValueBaseDiscount"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }

        /** Set Cost Calculated.@param IsCostCalculated Cost Calculated */
        public void SetIsCostCalculated(Boolean IsCostCalculated) { Set_Value("IsCostCalculated", IsCostCalculated); }
        /** Get Cost Calculated. @return Cost Calculated */
        public Boolean IsCostCalculated() { Object oo = Get_Value("IsCostCalculated"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }

        /** Set Reversed Cost Calculated.@param IsReversedCostCalculated Reversed Cost Calculated */
        public void SetIsReversedCostCalculated(Boolean IsReversedCostCalculated) { Set_Value("IsReversedCostCalculated", IsReversedCostCalculated); }
        /** Get Reversed Cost Calculated. @return Reversed Cost Calculated */
        public Boolean IsReversedCostCalculated() { Object oo = Get_Value("IsReversedCostCalculated"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }

        /** Set Cost Immediately.@param IsCostImmediate Update Costs immediately for testing */
        public void SetIsCostImmediate(Boolean IsCostImmediate) { Set_Value("IsCostImmediate", IsCostImmediate); }
        /** Get Cost Immediately.@return Update Costs immediately for testing */
        public Boolean IsCostImmediate() { Object oo = Get_Value("IsCostImmediate"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }

        /** Set Future Cost Calculated.@param IsFutureCostCalculated Future Cost Calculated */
        public void SetIsFutureCostCalculated(Boolean IsFutureCostCalculated) { Set_Value("IsFutureCostCalculated", IsFutureCostCalculated); }
        /** Get Future Cost Calculated.@return Future Cost Calculated */
        public Boolean IsFutureCostCalculated() { Object oo = Get_Value("IsFutureCostCalculated"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }

        /** Set IsCommissionCalculated.
@param IsCommissionCalculated IsCommissionCalculated */
        public void SetIsCommissionCalculated(Boolean IsCommissionCalculated)
        {
            Set_Value("IsCommissionCalculated", IsCommissionCalculated);
        }
        /** Get IsCommissionCalculated.
        @return IsCommissionCalculated */
        public Boolean IsCommissionCalculated()
        {
            Object oo = Get_Value("IsCommissionCalculated");
            if (oo != null)
            {
                if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
                return "Y".Equals(oo);
            }
            return false;
        }

        //Added by Bharat
        /** Set Discrepancy Amount .
        @param Discrepancy Amount */
        public void SetDiscrepancyAmt(Decimal? DiscrepancyAmt)
        {
            Set_Value("DiscrepancyAmt", (Decimal?)DiscrepancyAmt);
        }
        /** Get Discrepancy Amount (Base).
        @return Discrepancy Amount (Base) */
        public Decimal GetDiscrepancyAmt()
        {
            Object bd = Get_Value("DiscrepancyAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** Set VA023_Reference.
        @param VA023_Reference VA023_Reference */
        public void SetVA023_Reference(String VA023_Reference)
        {
            if (VA023_Reference != null && VA023_Reference.Length > 100)
            {
                log.Warning("Length > 100 - truncated");
                VA023_Reference = VA023_Reference.Substring(0, 100);
            }
            Set_Value("VA023_Reference", VA023_Reference);
        }
        /** Get VA023_Reference.
        @return VA023_Reference */
        public String GetVA023_Reference()
        {
            return (String)Get_Value("VA023_Reference");
        }

        /** Set Current Cost.@param CurrentCostPrice The currently used cost price */
        public void SetCurrentCostPrice(Decimal? CurrentCostPrice) { Set_Value("CurrentCostPrice", (Decimal?)CurrentCostPrice); }
        /** Get Current Cost.@return The currently used cost price */
        public Decimal GetCurrentCostPrice() { Object bd = Get_Value("CurrentCostPrice"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }
        /** Set Post Current Cost Price.@param PostCurrentCostPrice It indicate the cost after cost calculation of current record */
        public void SetPostCurrentCostPrice(Decimal? PostCurrentCostPrice) { Set_Value("PostCurrentCostPrice", (Decimal?)PostCurrentCostPrice); }
        /** Get Post Current Cost Price. @return It indicate the cost after cost calculation of current record */
        public Decimal GetPostCurrentCostPrice() { Object bd = Get_Value("PostCurrentCostPrice"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }

        //Edited By Arpit Rai 20th Sept,2017
        /** Set Drop Shipment.
@param IsDropShip Drop Shipments are sent from the Vendor directly to the Customer */
        public void SetIsDropShip(Boolean IsDropShip) { Set_Value("IsDropShip", IsDropShip); }/** Get Drop Shipment.
@return Drop Shipments are sent from the Vendor directly to the Customer */
        public Boolean IsDropShip() { Object oo = Get_Value("IsDropShip"); if (oo != null) { if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo); return "Y".Equals(oo); } return false; }
        //Arpit

        /** Set Tax Amount (Base Currency).
@param TaxBaseCurrencyAmt Tax Amount (Base Currency) Indicate amount in Base Currency */
        public void SetTaxBaseCurrencyAmt(Decimal? TaxBaseCurrencyAmt) { Set_Value("TaxBaseCurrencyAmt", (Decimal?)TaxBaseCurrencyAmt); }
        /** Get Tax Amount (Base Currency).@return Tax Amount (Base Currency) Indicate amount in Base Currency */
        public Decimal GetTaxBaseCurrencyAmt() { Object bd = Get_Value("TaxBaseCurrencyAmt"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }
        /** Set Taxable Amount.@param TaxBaseAmt Base for calculating the tax amount */
        public void SetTaxBaseAmt(Decimal? TaxBaseAmt) { Set_Value("TaxBaseAmt", (Decimal?)TaxBaseAmt); }
        /** Get Taxable Amount.@return Base for calculating the tax amount */
        public Decimal GetTaxBaseAmt() { Object bd = Get_Value("TaxBaseAmt"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }

        /** Set Discount Amount after Total Discount.@param AmountAfterApplyDiscount It is the Total amount deducted with respect to Line amount based on the calculation of Overall Discount on the Grand Total. */
        public void SetAmountAfterApplyDiscount(Decimal? AmountAfterApplyDiscount) { Set_Value("AmountAfterApplyDiscount", (Decimal?)AmountAfterApplyDiscount); }
        /** Get Discount Amount after Total Discount.@return It is the Total amount deducted with respect to Line amount based on the calculation of Overall Discount on the Grand Total. */
        public Decimal GetAmountAfterApplyDiscount() { Object bd = Get_Value("AmountAfterApplyDiscount"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }

        /** Set Withholding Tax.
@param VAB_Withholding_ID Withholding type defined */
        public void SetVAB_Withholding_ID(int VAB_Withholding_ID)
        {
            if (VAB_Withholding_ID <= 0) Set_Value("VAB_Withholding_ID", null);
            else
                Set_Value("VAB_Withholding_ID", VAB_Withholding_ID);
        }/** Get Withholding Tax.
@return Withholding type defined */
        public int GetVAB_Withholding_ID() { Object ii = Get_Value("VAB_Withholding_ID"); if (ii == null) return 0; return Convert.ToInt32(ii); }

        /** Set Withholding Amount.
@param WithholdingAmt This field represents the calculated withholding amount */
        public void SetWithholdingAmt(Decimal? WithholdingAmt) { Set_Value("WithholdingAmt", (Decimal?)WithholdingAmt); }/** Get Withholding Amount.
@return This field represents the calculated withholding amount */
        public Decimal GetWithholdingAmt() { Object bd = Get_Value("WithholdingAmt"); if (bd == null) return Env.ZERO; return Convert.ToDecimal(bd); }


    }

}