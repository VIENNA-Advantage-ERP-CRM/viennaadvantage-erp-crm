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
    /** Generated Model for VAB_PaymentOptionCheck
     *  @author Jagmohan Bhatt (generated) 
     *  @version Vienna Framework 1.1.1 - $Id$ */
    public class X_VAB_PaymentOptionCheck : PO
    {
        public X_VAB_PaymentOptionCheck(Context ctx, int VAB_PaymentOptionCheck_ID, Trx trxName)
            : base(ctx, VAB_PaymentOptionCheck_ID, trxName)
        {
            /** if (VAB_PaymentOptionCheck_ID == 0)
            {
            SetVAB_BusinessPartner_ID (0);
            SetVAB_PaymentOptionCheck_ID (0);
            SetVAB_PaymentOption_ID (0);
            SetDiscountAmt (0.0);
            SetIsPrinted (false);
            SetIsReceipt (false);
            SetPayAmt (0.0);
            SetPaymentRule (null);
            SetProcessed (false);	// N
            SetQty (0);
            }
             */
        }
        public X_VAB_PaymentOptionCheck(Ctx ctx, int VAB_PaymentOptionCheck_ID, Trx trxName)
            : base(ctx, VAB_PaymentOptionCheck_ID, trxName)
        {
            /** if (VAB_PaymentOptionCheck_ID == 0)
            {
            SetVAB_BusinessPartner_ID (0);
            SetVAB_PaymentOptionCheck_ID (0);
            SetVAB_PaymentOption_ID (0);
            SetDiscountAmt (0.0);
            SetIsPrinted (false);
            SetIsReceipt (false);
            SetPayAmt (0.0);
            SetPaymentRule (null);
            SetProcessed (false);	// N
            SetQty (0);
            }
             */
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAB_PaymentOptionCheck(Context ctx, DataRow rs, Trx trxName)
            : base(ctx, rs, trxName)
        {
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAB_PaymentOptionCheck(Ctx ctx, DataRow rs, Trx trxName)
            : base(ctx, rs, trxName)
        {
        }
        /** Load Constructor 
        @param ctx context
        @param rs result set 
        @param trxName transaction
        */
        public X_VAB_PaymentOptionCheck(Ctx ctx, IDataReader dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }
        /** Static Constructor 
         Set Table ID By Table Name
         added by ->Harwinder */
        static X_VAB_PaymentOptionCheck()
        {
            Table_ID = Get_Table_ID(Table_Name);
            model = new KeyNamePair(Table_ID, Table_Name);
        }
        /** Serial Version No */
        //static long serialVersionUID 27562514373629L;
        /** Last Updated Timestamp 7/29/2010 1:07:36 PM */
        public static long updatedMS = 1280389056840L;
        /** VAF_TableView_ID=525 */
        public static int Table_ID;
        // =525;

        /** TableName=VAB_PaymentOptionCheck */
        public static String Table_Name = "VAB_PaymentOptionCheck";

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
        protected override POInfo InitPO(Ctx ctx)
        {
            POInfo poi = POInfo.GetPOInfo(ctx, Table_ID);
            return poi;
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
        /** Info
        @return info
        */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("X_VAB_PaymentOptionCheck[").Append(Get_ID()).Append("]");
            return sb.ToString();
        }
        /** Set Partner Bank Account.
        @param VAB_BPart_Bank_Acct_ID Bank Account of the Business Partner */
        public void SetVAB_BPart_Bank_Acct_ID(int VAB_BPart_Bank_Acct_ID)
        {
            if (VAB_BPart_Bank_Acct_ID <= 0) Set_Value("VAB_BPart_Bank_Acct_ID", null);
            else
                Set_Value("VAB_BPart_Bank_Acct_ID", VAB_BPart_Bank_Acct_ID);
        }
        /** Get Partner Bank Account.
        @return Bank Account of the Business Partner */
        public int GetVAB_BPart_Bank_Acct_ID()
        {
            Object ii = Get_Value("VAB_BPart_Bank_Acct_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Business Partner.
        @param VAB_BusinessPartner_ID Identifies a Business Partner */
        public void SetVAB_BusinessPartner_ID(int VAB_BusinessPartner_ID)
        {
            if (VAB_BusinessPartner_ID < 1) throw new ArgumentException("VAB_BusinessPartner_ID is mandatory.");
            Set_Value("VAB_BusinessPartner_ID", VAB_BusinessPartner_ID);
        }
        /** Get Business Partner.
        @return Identifies a Business Partner */
        public int GetVAB_BusinessPartner_ID()
        {
            Object ii = Get_Value("VAB_BusinessPartner_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Pay Selection Check.
        @param VAB_PaymentOptionCheck_ID Payment Selection Check */
        public void SetVAB_PaymentOptionCheck_ID(int VAB_PaymentOptionCheck_ID)
        {
            if (VAB_PaymentOptionCheck_ID < 1) throw new ArgumentException("VAB_PaymentOptionCheck_ID is mandatory.");
            Set_ValueNoCheck("VAB_PaymentOptionCheck_ID", VAB_PaymentOptionCheck_ID);
        }
        /** Get Pay Selection Check.
        @return Payment Selection Check */
        public int GetVAB_PaymentOptionCheck_ID()
        {
            Object ii = Get_Value("VAB_PaymentOptionCheck_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Payment Selection.
        @param VAB_PaymentOption_ID Payment Selection */
        public void SetVAB_PaymentOption_ID(int VAB_PaymentOption_ID)
        {
            if (VAB_PaymentOption_ID < 1) throw new ArgumentException("VAB_PaymentOption_ID is mandatory.");
            Set_ValueNoCheck("VAB_PaymentOption_ID", VAB_PaymentOption_ID);
        }
        /** Get Payment Selection.
        @return Payment Selection */
        public int GetVAB_PaymentOption_ID()
        {
            Object ii = Get_Value("VAB_PaymentOption_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Payment.
        @param VAB_Payment_ID Payment identifier */
        public void SetVAB_Payment_ID(int VAB_Payment_ID)
        {
            if (VAB_Payment_ID <= 0) Set_Value("VAB_Payment_ID", null);
            else
                Set_Value("VAB_Payment_ID", VAB_Payment_ID);
        }
        /** Get Payment.
        @return Payment identifier */
        public int GetVAB_Payment_ID()
        {
            Object ii = Get_Value("VAB_Payment_ID");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
        /** Set Discount Amount.
        @param DiscountAmt Calculated amount of discount */
        public void SetDiscountAmt(Decimal? DiscountAmt)
        {
            if (DiscountAmt == null) throw new ArgumentException("DiscountAmt is mandatory.");
            Set_Value("DiscountAmt", (Decimal?)DiscountAmt);
        }
        /** Get Discount Amount.
        @return Calculated amount of discount */
        public Decimal GetDiscountAmt()
        {
            Object bd = Get_Value("DiscountAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }
        /** Set Document No.
        @param DocumentNo Document sequence number of the document */
        public void SetDocumentNo(String DocumentNo)
        {
            if (DocumentNo != null && DocumentNo.Length > 30)
            {
                log.Warning("Length > 30 - truncated");
                DocumentNo = DocumentNo.Substring(0, 30);
            }
            Set_Value("DocumentNo", DocumentNo);
        }
        /** Get Document No.
        @return Document sequence number of the document */
        public String GetDocumentNo()
        {
            return (String)Get_Value("DocumentNo");
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
        /** Set Receipt.
        @param IsReceipt This is a sales transaction (receipt) */
        public void SetIsReceipt(Boolean IsReceipt)
        {
            Set_Value("IsReceipt", IsReceipt);
        }
        /** Get Receipt.
        @return This is a sales transaction (receipt) */
        public Boolean IsReceipt()
        {
            Object oo = Get_Value("IsReceipt");
            if (oo != null)
            {
                if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
                return "Y".Equals(oo);
            }
            return false;
        }
        /** Set Payment amount.
        @param PayAmt Amount being paid */
        public void SetPayAmt(Decimal? PayAmt)
        {
            if (PayAmt == null) throw new ArgumentException("PayAmt is mandatory.");
            Set_Value("PayAmt", (Decimal?)PayAmt);
        }
        /** Get Payment amount.
        @return Amount being paid */
        public Decimal GetPayAmt()
        {
            Object bd = Get_Value("PayAmt");
            if (bd == null) return Env.ZERO;
            return Convert.ToDecimal(bd);
        }

        /** PaymentRule VAF_Control_Ref_ID=195 */
        public static int PAYMENTRULE_VAF_Control_Ref_ID = 195;
        /** Cash = B */
        public static String PAYMENTRULE_Cash = "B";
        /** Direct Debit = D */
        public static String PAYMENTRULE_DirectDebit = "D";
        /** Credit Card = K */
        public static String PAYMENTRULE_CreditCard = "K";
        /** On Credit = P */
        public static String PAYMENTRULE_OnCredit = "P";
        /** Check = S */
        public static String PAYMENTRULE_Check = "S";
        /** Direct Deposit = T */
        public static String PAYMENTRULE_DirectDeposit = "T";
        /** Is test a valid value.
        @param test testvalue
        @returns true if valid **/
        public bool IsPaymentRuleValid(String test)
        {
            return test.Equals("B") || test.Equals("D") || test.Equals("K") || test.Equals("P") || test.Equals("S") || test.Equals("T");
        }
        /** Set Payment Method.
        @param PaymentRule How you pay the invoice */
        public void SetPaymentRule(String PaymentRule)
        {
            if (PaymentRule == null) throw new ArgumentException("PaymentRule is mandatory");
            //if (!IsPaymentRuleValid(PaymentRule))
            //throw new ArgumentException ("PaymentRule Invalid value - " + PaymentRule + " - Reference_ID=195 - B - D - K - P - S - T");
            if (PaymentRule.Length > 1)
            {
                log.Warning("Length > 1 - truncated");
                PaymentRule = PaymentRule.Substring(0, 1);
            }
            Set_Value("PaymentRule", PaymentRule);
        }
        /** Get Payment Method.
        @return How you pay the invoice */
        public String GetPaymentRule()
        {
            return (String)Get_Value("PaymentRule");
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
        @param Qty Quantity */
        public void SetQty(int Qty)
        {
            Set_Value("Qty", Qty);
        }
        /** Get Quantity.
        @return Quantity */
        public int GetQty()
        {
            Object ii = Get_Value("Qty");
            if (ii == null) return 0;
            return Convert.ToInt32(ii);
        }
    }

}