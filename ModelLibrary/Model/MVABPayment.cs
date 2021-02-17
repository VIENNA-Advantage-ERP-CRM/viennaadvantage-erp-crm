﻿/********************************************************
 * Module Name    : 
 * Purpose        : 
 * Class Used     : X_VAB_Payment
 * Chronological Development
 * Veena Pandey     23-June-2009 
 ******************************************************/

using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using VAdvantage.Classes;
using VAdvantage.Utility;
using VAdvantage.DataBase;
using VAdvantage.Common;
//using System.Windows.Forms;
using VAdvantage.Process;
using VAdvantage.Logging;
using VAdvantage.ProcessEngine;

namespace VAdvantage.Model
{
    public class MVABPayment : X_VAB_Payment, DocAction, ProcessCall
    {
        /**	Temporary	Payment Processors		*/
        private MVABPaymentHandler[] _paymentProcessors = null;
        /**	Temporary	Payment Processor		*/
        private MVABPaymentHandler _paymentProcessor = null;
        /** VVC not stored						*/
        private String _creditCardVV = null;
        // Logger	
        private static VLogger _log = VLogger.GetVLogger(typeof(MVABPayment).FullName);
        /** Error Message						*/
        private String _errorMessage = null;

        /** Reversal Indicator			*/
        public const String REVERSE_INDICATOR = "^";

        /**	Process Message 			*/
        private String _processMsg = null;
        /**	Just Prepared Flag			*/
        private bool _justPrepared = false;

        private int VA026_LCDetail_ID = 0;

        /**	Allocation Message 			*/
        private String _AllocMsg = "";

        private bool resetAmtDim = false;

        private bool autoCheck = false;

        public bool _isWithholdingFromPaymentAllocate = false;

        // used for locking query - checking schedule is paid or not on completion
        // when multiple user try to pay agaisnt same schedule from different scenarion at that tym lock record
        static readonly object objLock = new object();

        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAB_Payment_ID">id</param>
        /// <param name="trxName">transaction</param>
        public MVABPayment(Ctx ctx, int VAB_Payment_ID, Trx trxName)
            : base(ctx, VAB_Payment_ID, trxName)
        {
            //  New
            if (VAB_Payment_ID == 0)
            {
                SetDocAction(DOCACTION_Complete);
                SetDocStatus(DOCSTATUS_Drafted);
                SetTrxType(TRXTYPE_Sales);
                //
                SetR_AvsAddr(R_AVSZIP_Unavailable);
                SetR_AvsZip(R_AVSZIP_Unavailable);
                //
                SetIsReceipt(true);
                SetIsApproved(false);
                SetIsReconciled(false);
                SetIsAllocated(false);
                SetIsOnline(false);
                SetIsSelfService(false);
                SetIsDelayedCapture(false);
                SetIsPrepayment(false);
                SetProcessed(false);
                SetProcessing(false);
                SetPosted(false);
                //
                SetPayAmt(Env.ZERO);
                SetDiscountAmt(Env.ZERO);
                SetTaxAmt(Env.ZERO);
                SetWriteOffAmt(Env.ZERO);
                SetIsOverUnderPayment(false);
                SetOverUnderAmt(Env.ZERO);
                //
                SetDateTrx(DateTime.Now);
                SetDateAcct(GetDateTrx());
                SetTenderType(TENDERTYPE_Check);
            }
        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">data row</param>
        /// <param name="trxName">transaction</param>
        public MVABPayment(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

        /**
	     * 	Get Payments Of BPartner
	     *	@param ctx context
	     *	@param VAB_BusinessPartner_ID id
	     *	@param trxName transaction
	     *	@return array
	     */
        public static MVABPayment[] GetOfBPartner(Ctx ctx, int VAB_BusinessPartner_ID, Trx trxName)
        {
            List<MVABPayment> list = new List<MVABPayment>();
            String sql = "SELECT * FROM VAB_Payment WHERE VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID;
            try
            {
                DataSet ds = DataBase.DB.ExecuteDataset(sql, null, trxName);
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        list.Add(new MVABPayment(ctx, dr, trxName));
                    }
                }
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql, e);
            }

            //
            MVABPayment[] retValue = new MVABPayment[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /**
	     *  ReSet Payment to new status
	     */
        public void ReSetNew()
        {
            Set_ValueNoCheck("VAB_Payment_ID", 0);		//	forces new Record
            Set_ValueNoCheck("DocumentNo", null);
            SetDocAction(DOCACTION_Prepare);
            SetDocStatus(DOCSTATUS_Drafted);
            SetProcessed(false);
            SetPosted(false);
            SetIsReconciled(false);
            SetIsAllocated(false);
            SetIsOnline(false);
            SetIsDelayedCapture(false);
            //	SetVAB_BusinessPartner_ID(0);
            SetVAB_Invoice_ID(0);
            SetVAB_Order_ID(0);
            SetVAB_Charge_ID(0);
            SetVAB_Project_ID(0);
            SetIsPrepayment(false);
        }

        /**
         * 	Is Cashbook Transfer Trx
         *	@return true if Cash Trx
         */
        public bool IsCashTrx()
        {
            return "X".Equals(GetTenderType());
        }

        /**
         *  Set Credit Card.
         *  Need to Set PatmentProcessor after Amount/Currency Set
         *
         *  @param TrxType Transaction Type see TRX_
         *  @param creditCardType CC type
         *  @param creditCardNumber CC number
         *  @param creditCardVV CC verification
         *  @param creditCardExpMM CC Exp MM
         *  @param creditCardExpYY CC Exp YY
         *  @return true if valid
         */
        public bool SetCreditCard(String TrxType, String creditCardType, String creditCardNumber,
            String creditCardVV, int creditCardExpMM, int creditCardExpYY)
        {
            SetTenderType(TENDERTYPE_CreditCard);
            SetTrxType(TrxType);
            //
            SetCreditCardType(creditCardType);
            SetCreditCardNumber(creditCardNumber);
            SetCreditCardVV(creditCardVV);
            SetCreditCardExpMM(creditCardExpMM);
            SetCreditCardExpYY(creditCardExpYY);
            //
            int check = MPaymentValidate.ValidateCreditCardNumber(creditCardNumber, creditCardType).Length
                + MPaymentValidate.ValidateCreditCardExp(creditCardExpMM, creditCardExpYY).Length;
            if (creditCardVV.Length > 0)
                check += MPaymentValidate.ValidateCreditCardVV(creditCardVV, creditCardType).Length;
            return check == 0;
        }

        /**
         *  Set Credit Card - Exp.
         *  Need to Set PatmentProcessor after Amount/Currency Set
         *
         *  @param TrxType Transaction Type see TRX_
         *  @param creditCardType CC type
         *  @param creditCardNumber CC number
         *  @param creditCardVV CC verification
         *  @param creditCardExp CC Exp
         *  @return true if valid
         */
        public bool SetCreditCard(String trxType, String creditCardType, String creditCardNumber,
            String creditCardVV, String creditCardExp)
        {
            return SetCreditCard(trxType, creditCardType, creditCardNumber,
                creditCardVV, MPaymentValidate.GetCreditCardExpMM(creditCardExp),
                MPaymentValidate.GetCreditCardExpYY(creditCardExp));
        }

        /**
         *  Set ACH BankAccount Info
         *
         *  @param VAB_Bank_Acct_ID bank account
         *  @param isReceipt true if receipt
         *  @return true if valid
         */
        public bool SetBankACH(MVABPaymentOptionCheck preparedPayment)
        {
            //	Our Bank
            SetVAB_Bank_Acct_ID(preparedPayment.GetParent().GetVAB_Bank_Acct_ID());
            //	TarGet Bank
            int VAB_BPart_Bank_Acct_ID = preparedPayment.GetVAB_BPart_Bank_Acct_ID();
            MVABBPartBankAcct ba = new MVABBPartBankAcct(preparedPayment.GetCtx(), VAB_BPart_Bank_Acct_ID, null);
            SetRoutingNo(ba.GetRoutingNo());
            SetAccountNo(ba.GetAccountNo());
            SetIsReceipt(X_VAB_Order.PAYMENTRULE_DirectDebit.Equals	//	AR only
                    (preparedPayment.GetPaymentRule()));
            //
            int check = MPaymentValidate.ValidateRoutingNo(GetRoutingNo()).Length
                + MPaymentValidate.ValidateAccountNo(GetAccountNo()).Length;
            return check == 0;
        }

        /**
         *  Set ACH BankAccount Info
         *
         *  @param VAB_Bank_Acct_ID bank account
         *  @param isReceipt true if receipt
         * 	@param tenderType - Direct Debit or Direct Deposit
         *  @param routingNo routing
         *  @param accountNo account
         *  @return true if valid
         */
        public bool SetBankACH(int VAB_Bank_Acct_ID, bool isReceipt, String tenderType,
            String routingNo, String accountNo)
        {
            SetTenderType(tenderType);
            SetIsReceipt(isReceipt);
            //
            if (VAB_Bank_Acct_ID > 0
                && (routingNo == null || routingNo.Length == 0 || accountNo == null || accountNo.Length == 0))
                SetBankAccountDetails(VAB_Bank_Acct_ID);
            else
            {
                SetVAB_Bank_Acct_ID(VAB_Bank_Acct_ID);
                SetRoutingNo(routingNo);
                SetAccountNo(accountNo);
            }
            SetCheckNo("");
            //
            int check = MPaymentValidate.ValidateRoutingNo(routingNo).Length
                + MPaymentValidate.ValidateAccountNo(accountNo).Length;
            return check == 0;
        }

        /**
         *  Set Check BankAccount Info
         *
         *  @param VAB_Bank_Acct_ID bank account
         *  @param isReceipt true if receipt
         *  @param checkNo chack no
         *  @return true if valid
         */
        public bool SetBankCheck(int VAB_Bank_Acct_ID, bool isReceipt, String checkNo)
        {
            return SetBankCheck(VAB_Bank_Acct_ID, isReceipt, null, null, checkNo);
        }

        /**
         *  Set Check BankAccount Info
         *
         *  @param VAB_Bank_Acct_ID bank account
         *  @param isReceipt true if receipt
         *  @param routingNo routing no
         *  @param accountNo account no
         *  @param checkNo chack no
         *  @return true if valid
         */
        public bool SetBankCheck(int VAB_Bank_Acct_ID, bool isReceipt,
            String routingNo, String accountNo, String checkNo)
        {
            SetTenderType(TENDERTYPE_Check);
            SetIsReceipt(isReceipt);
            //
            if (VAB_Bank_Acct_ID > 0
                && (routingNo == null || routingNo.Length == 0
                    || accountNo == null || accountNo.Length == 0))
                SetBankAccountDetails(VAB_Bank_Acct_ID);
            else
            {
                SetVAB_Bank_Acct_ID(VAB_Bank_Acct_ID);
                SetRoutingNo(routingNo);
                SetAccountNo(accountNo);
            }
            SetCheckNo(checkNo);
            //
            int check = MPaymentValidate.ValidateRoutingNo(routingNo).Length
                + MPaymentValidate.ValidateAccountNo(accountNo).Length
                + MPaymentValidate.ValidateCheckNo(checkNo).Length;
            return check == 0;       //  no error message
        }

        /**
         * 	Set Bank Account Details.
         * 	Look up Routing No & Bank Acct No
         * 	@param VAB_Bank_Acct_ID bank account
         */
        public void SetBankAccountDetails(int VAB_Bank_Acct_ID)
        {
            if (VAB_Bank_Acct_ID == 0)
                return;
            SetVAB_Bank_Acct_ID(VAB_Bank_Acct_ID);
            //
            String sql = "SELECT b.RoutingNo, ba.AccountNo "
                + "FROM VAB_Bank_Acct ba"
                + " INNER JOIN VAB_Bank b ON (ba.VAB_Bank_ID=b.VAB_Bank_ID) "
                + "WHERE VAB_Bank_Acct_ID=" + VAB_Bank_Acct_ID;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_Trx());
                if (idr.Read())
                {
                    SetRoutingNo(idr.GetString(0));
                    SetAccountNo(idr.GetString(1));
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }
        }

        /**
         *  Set Account Address
         *
         *  @param name name
         *  @param street street
         *  @param city city
         *  @param state state
         *  @param zip zip
         * 	@param country country
         */
        public void SetAccountAddress(String name, String street, String city,
            String state, String zip, String country)
        {
            SetA_Name(name);
            SetA_Street(street);
            SetA_City(city);
            SetA_State(state);
            SetA_Zip(zip);
            SetA_Country(country);
        }


        /**
         *  Process Payment
         *  @return true if approved
         */
        public bool ProcessOnline()
        {
            log.Info("Amt=" + GetPayAmt());
            //
            SetIsOnline(true);
            SetErrorMessage(null);
            //	prevent charging twice
            if (IsApproved())
            {
                log.Info("Already processed - " + GetR_Result() + " - " + GetR_RespMsg());
                SetErrorMessage("Payment already Processed");
                return true;
            }

            if (_paymentProcessor == null)
                SetPaymentProcessor();
            if (_paymentProcessor == null)
            {
                log.Log(Level.WARNING, "No Payment Processor Model");
                SetErrorMessage("No Payment Processor Model");
                return false;
            }

            bool approved = false;
            /**	Process Payment on Server	*/
            //if (DataBase.isRemoteObjects())
            //{
            //    Server server = CConnection.Get().GetServer();
            //    try
            //    {
            //        if (server != null)
            //        {	//	See ServerBean
            //            String trxName = null;	//	unconditionally save
            //            Save(trxName);	//	server reads from disk
            //            approved = server.paymentOnline (GetCtx(), GetVAB_Payment_ID(), 
            //                _paymentProcessor.GetVAB_PaymentHandler_ID(), trxName);
            //            if (CLogMgt.IsLevelFinest())
            //                s_log.Fine("server => " + approved);
            //            Load(trxName);	//	server saves to disk
            //            SetIsApproved(approved);
            //            return approved;
            //        }
            //        log.log(Level.WARNING, "AppsServer not found"); 
            //    }
            //    catch (RemoteException ex)
            //    {
            //        log.log(Level.SEVERE, "AppsServer error", ex);
            //    }
            //}
            /** **/

            //	Try locally
            try
            {
                PaymentProcessor pp = PaymentProcessor.Create(_paymentProcessor, this);
                if (pp == null)
                    SetErrorMessage("No Payment Processor");
                else
                {
                    approved = pp.ProcessCC();
                    if (approved)
                        SetErrorMessage(null);
                    else
                        SetErrorMessage("From " + GetCreditCardName() + ": " + GetR_RespMsg());
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, "processOnline", e);
                SetErrorMessage("Payment Processor Error");
            }
            SetIsApproved(approved);
            return approved;
        }

        /**
         *  Process Online Payment.
         *  implements ProcessEngine.ProcessCall after standard constructor
         *  Called when pressing the Process_Online button in VAB_Payment
         *
         *  @param ctx Ctx
         *  @param pi Process Info
         *  @param trx transaction
         *  @return true if the next process should be performed
         */
        public bool StartProcess(Ctx ctx, ProcessInfo pi, Trx trx)
        {
            log.Info("startProcess - " + pi.GetRecord_ID());
            bool retValue = false;
            //
            if (pi.GetRecord_ID() != Get_ID())
            {
                log.Log(Level.SEVERE, "startProcess - Not same Payment - " + pi.GetRecord_ID());
                return false;
            }
            //  Process it
            retValue = ProcessOnline();
            Save();
            return retValue;    //  Payment processed
        }


        /// <summary>
        ///  Before Save
        /// </summary>
        /// <param name="newRecord">new</param>
        /// <returns>true, on Save</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            MVABOrder order = null;
            MVABInvoice invoice = null;
            MVABDocTypes dt1 = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

            bool hasVA009Module = Env.IsModuleInstalled("VA009_");

            try
            {
                // JID_1259: Check Date should not be greater than Account Date. 
                // When Discounting PDC is true, No need to check Check Date with Account Date.
                if (Env.IsModuleInstalled("VA027_") && IsVA027_DiscountingPDC())
                {

                }
                else
                {
                    if (GetCheckDate() != null)
                    {
                        if (GetCheckDate().Value.Date > GetDateAcct().Value.Date)
                        {
                            log.SaveError("Error", Msg.GetMsg(GetCtx(), "VIS_CheckDateCantbeGreaterSys"));
                            return false;
                        }
                    }
                }

                // Handle PaymentDocTypeInvoiceInconsistent
                if (GetVAB_Order_ID() != 0 || GetVAB_Invoice_ID() != 0)
                {
                    if (GetVAB_Invoice_ID() > 0)
                    {
                        invoice = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), null);
                        if (invoice.IsSOTrx() != dt1.IsSOTrx())
                        {
                            return false;
                        }
                        // set currency type from invoice
                        // SetVAB_CurrencyType_ID(invoice.GetVAB_CurrencyType_ID());
                        // Change By Mohit Asked By Amardeep Sir 02/03/2016
                        SetPOReference(invoice.GetPOReference());
                        // Changes Done by Bharat on 20 June 2017 to not allow more than Schedule Amount.
                        if (GetDescription() != null && GetDescription().Contains("{->"))
                        {

                        }
                        else
                        {
                            /* By Pass for POS terminal Order */
                            if (invoice.GetVAB_Order_ID() > 0 && DB.GetSQLValue(null,
                                            "SELECT VAPOS_POSTerminal_ID FROM VAB_Order WHERE VAB_Order_ID = " + invoice.GetVAB_Order_ID()) > 0)
                            {
                                ; //Intentionlly left blank
                            }
                            else if ((invoice.IsReturnTrx() && GetOverUnderAmt() > 0) || (!invoice.IsReturnTrx() && GetOverUnderAmt() < 0)
                                || (invoice.IsReturnTrx() && GetPayAmt() > 0) || (!invoice.IsReturnTrx() && GetPayAmt() < 0))
                            {
                                log.SaveError("Error", Msg.GetMsg(GetCtx(), "MoreScheduleAmount"));
                                return false;
                            }
                        }
                    }
                    else if (GetVAB_Order_ID() > 0)
                    {
                        order = new MVABOrder(GetCtx(), GetVAB_Order_ID(), null);
                        if (order.IsSOTrx() != dt1.IsSOTrx())
                        {
                            return false;
                        }
                        // Change By Mohit Asked By Amardeep Sir 02/03/2016
                        SetPOReference(order.GetPOReference());
                        // set conversion type from order
                        //SetVAB_CurrencyType_ID(order.GetVAB_CurrencyType_ID());
                    }
                }

                if (GetVAB_Charge_ID() == 0)
                {
                    // when charge is not defined then set Value as Zero
                    SetVAB_TaxRate_ID(0);
                    SetTaxAmount(0);
                    Set_Value("SurchargeAmt", 0);
                }

                //	We have a charge
                if (GetVAB_Charge_ID() != 0)
                {
                    if (newRecord || Is_ValueChanged("VAB_Charge_ID"))
                    {
                        SetVAB_Order_ID(0);
                        SetVAB_Invoice_ID(0);
                        SetWriteOffAmt(Env.ZERO);
                        SetDiscountAmt(Env.ZERO);
                        SetIsOverUnderPayment(false);
                        SetOverUnderAmt(Env.ZERO);
                        //string sql = "SELECT IsAdvanceCharge FROM VAB_Charge WHERE VAB_Charge_ID = " + GetVAB_Charge_ID();
                        //string isAdvCharge = "";
                        //try
                        //{
                        //    isAdvCharge = Util.GetValueOfString(DB.ExecuteScalar(sql, null, null));
                        //}
                        //catch
                        //{

                        //}
                        //if (isAdvCharge.Equals("Y"))
                        //{
                        //    SetIsPrepayment(true);
                        //}
                        //else
                        //{
                        //    SetIsPrepayment(false);
                        //}
                    }
                }
                //	We need a BPartner
                //else if (GetVAB_BusinessPartner_ID() == 0 && !IsCashTrx())
                else if (GetVAB_BusinessPartner_ID() == 0 && GetTenderType() == null)
                {
                    if (GetVAB_Invoice_ID() != 0)
                    {
                    }
                    else if (GetVAB_Order_ID() != 0)
                    {
                    }
                    else
                    {
                        log.SaveError("Error", Msg.ParseTranslation(GetCtx(), "@NotFound@: @VAB_BusinessPartner_ID@"));
                        return false;
                    }
                }

                // check schedule is hold or not, if hold then no to save record
                if (GetVAB_sched_InvoicePayment_ID() > 0)
                {
                    if (IsHoldpaymentSchedule(GetVAB_sched_InvoicePayment_ID()))
                    {
                        log.SaveError("", Msg.GetMsg(GetCtx(), "VIS_PaymentisHold"));
                        return false;
                    }
                }

                // when payment method is "Cash" then no to save payment
                // need to change payment mode rather than Cash
                if (hasVA009Module)
                {
                    // compare payment mode - if cash then return false
                    if (Util.GetValueOfString(DB.ExecuteScalar(@"SELECT VA009_PaymentBaseType FROM VA009_PaymentMethod WHERE 
                          VA009_PaymentMethod_ID=" + GetVA009_PaymentMethod_ID(), null, Get_Trx())) == "B")
                    {
                        log.SaveError("Error", Msg.GetMsg(GetCtx(), "VIS_PaymentMismatch"));
                        return false;
                    }
                }

                //	Prepayment: No charge and order or project (not as acct dimension)
                // new change to set prepayment false when payment is for order schedules
                // done by Vivek Kumar on 05/07/2017 as per discussion with Mandeep sir
                //if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(AD_MODULEINFO_ID) FROM AD_MODULEINFO WHERE PREFIX='VA009_'  AND IsActive = 'Y'")) > 0)
                //JID_1469 prepayment should be true in case of order and independent business partner.
                if ((GetVAB_BusinessPartner_ID() != 0 && GetVAB_Invoice_ID() == 0 && GetVAB_Charge_ID() == 0 && GetVAB_Project_ID() == 0))
                {
                    SetIsPrepayment(true);
                }
                else
                {
                    if (newRecord
                        || Is_ValueChanged("VAB_Charge_ID") || Is_ValueChanged("VAB_Invoice_ID")
                        || Is_ValueChanged("VAB_Order_ID") || Is_ValueChanged("VAB_Project_ID"))
                        SetIsPrepayment(GetVAB_Charge_ID() == 0
                            && GetVAB_BusinessPartner_ID() != 0
                            && (GetVAB_Order_ID() != 0
                                || (GetVAB_Project_ID() != 0 && GetVAB_Invoice_ID() == 0)));
                }
                //if (Env.HasModulePrefix("VA009_", out asmInfo))
                //{
                //    if (GetVA009_OrderPaySchedule_ID() > 0)
                //    {
                //        SetIsPrepayment(false);
                //    }
                //    else
                //    {
                //        if (newRecord
                //        || Is_ValueChanged("VAB_Charge_ID") || Is_ValueChanged("VAB_Invoice_ID")
                //        || Is_ValueChanged("VAB_Order_ID") || Is_ValueChanged("VAB_Project_ID"))
                //            SetIsPrepayment(GetVAB_Charge_ID() == 0
                //                && GetVAB_BusinessPartner_ID() != 0
                //                && (GetVAB_Order_ID() != 0
                //                    || (GetVAB_Project_ID() != 0 && GetVAB_Invoice_ID() == 0)));
                //    }
                //}
                //else
                //{
                //    if (newRecord
                //    || Is_ValueChanged("VAB_Charge_ID") || Is_ValueChanged("VAB_Invoice_ID")
                //    || Is_ValueChanged("VAB_Order_ID") || Is_ValueChanged("VAB_Project_ID"))
                //        SetIsPrepayment(GetVAB_Charge_ID() == 0
                //            && GetVAB_BusinessPartner_ID() != 0
                //            && (GetVAB_Order_ID() != 0
                //                || (GetVAB_Project_ID() != 0 && GetVAB_Invoice_ID() == 0)));
                //}

                if (GetVAB_Charge_ID() != 0)
                {
                    string sqlAdvCharge = "SELECT IsAdvanceCharge FROM VAB_Charge WHERE VAB_Charge_ID = " + GetVAB_Charge_ID();
                    string isAdvCharge = "";
                    try
                    {
                        isAdvCharge = Util.GetValueOfString(DB.ExecuteScalar(sqlAdvCharge, null, null));
                    }
                    catch
                    {

                    }
                    if (isAdvCharge.Equals("Y"))
                    {
                        SetIsPrepayment(true);
                    }
                    else
                    {
                        SetIsPrepayment(false);
                    }
                }

                if (IsPrepayment())
                {
                    if (newRecord
                        || Is_ValueChanged("VAB_Order_ID") || Is_ValueChanged("VAB_Project_ID"))
                    {
                        SetWriteOffAmt(Env.ZERO);
                        SetDiscountAmt(Env.ZERO);
                        SetIsOverUnderPayment(false);
                        SetOverUnderAmt(Env.ZERO);
                    }
                }

                //	Document Type/Receipt
                if (GetVAB_DocTypes_ID() == 0)
                    SetVAB_DocTypes_ID();
                else
                {
                    MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
                    SetIsReceipt(dt.IsSOTrx());
                }
                SetDocumentNo();
                //
                if (GetDateAcct() == null)
                    SetDateAcct(GetDateTrx());
                //
                //if (!IsOverUnderPayment())
                //    SetOverUnderAmt(Env.ZERO);

                //	Organization
                if ((newRecord || Is_ValueChanged("VAB_Bank_Acct_ID"))
                    && GetVAB_Charge_ID() == 0)	//	allow different org for charge
                {
                    MVABBankAcct ba = MVABBankAcct.Get(GetCtx(), GetVAB_Bank_Acct_ID());
                    if (ba.GetVAF_Org_ID() != 0)
                        SetVAF_Org_ID(ba.GetVAF_Org_ID());
                }
            }
            catch (Exception ex)
            {
                log.Severe(ex.ToString());
                //MessageBox.Show("MPayment-Error Payment not saved");
                return false;
            }

            try
            {
                if (hasVA009Module)
                {
                    string tenderType = Util.GetValueOfString(DB.ExecuteScalar(@"select VA009_PAYMENTBASETYPE from VA009_PAYMENTMETHOD where VA009_PAYMENTMETHOD_ID=" + GetVA009_PaymentMethod_ID()));
                    if (tenderType == "K")
                    {
                        SetTenderType("C");
                    }
                    else if (tenderType == "D")
                    {
                        SetTenderType("D");
                    }
                    else if (tenderType == "S")
                    {
                        SetTenderType("K");
                    }
                    else if (tenderType == "T")
                    {
                        SetTenderType("A");
                    }
                    else
                    {
                        SetTenderType("A");
                    }

                    // JID_1676 -- set Payment Execution as "In-Progress if not defined
                    if (String.IsNullOrEmpty(GetVA009_ExecutionStatus()))
                    {
                        SetVA009_ExecutionStatus(MVABPayment.VA009_EXECUTIONSTATUS_In_Progress);
                    }


                    //change by amit // for letter of credit
                    if (Env.IsModuleInstalled("VA026_"))
                    {
                        if (GetTenderType() == "L" && GetVA026_LCDetail_ID() == 0)
                        {
                            return false;
                        }

                        //Should be Select either Invoice and TR Load Application 
                        if (GetVAB_Invoice_ID() > 0 && Get_ValueAsInt("VA026_TRLoanApplication_ID") > 0)
                        {
                            log.SaveError("", Msg.GetMsg(GetCtx(), "VA026_SelectEitherTRLoadORInvoice"));
                            return false;
                        }

                        if (GetVA026_LCDetail_ID() != 0)
                        {
                            if (string.IsNullOrEmpty(tenderType) || tenderType == "L")
                            {
                                SetTenderType("L");
                            }
                            if (Get_ValueAsInt("VA026_TRLoanApplication_ID") > 0)
                            {
                                int TRBankAccount = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_Bank_Acct_ID FROM VA026_TRLoanApplication 
                                        WHERE VA026_TRLoanApplication_ID = " + Get_ValueAsInt("VA026_TRLoanApplication_ID"), null, Get_Trx()));
                                if (TRBankAccount != GetVAB_Bank_Acct_ID())
                                {
                                    log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "VA026_TRBankActNotMatched"));
                                    return false;
                                }
                            }

                            // check exact LC Detail ID bind on payment which link on LC Detail window
                            string sql = string.Empty;
                            if (GetVAB_Order_ID() > 0)
                            {
                                if (order != null && order.IsSOTrx())
                                {
                                    // check Header of LC Detail 
                                    sql = @"SELECT VA026_LCDetail_ID FROM VA026_LCDetail WHERE IsActive = 'Y' AND DocStatus IN ('CO' , 'CL') 
                                         AND VA026_Order_ID = " + GetVAB_Order_ID();
                                    VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));

                                    // Check SO Detail tab of Letter of Credit
                                    if (VA026_LCDetail_ID == 0)
                                    {
                                        VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT MIN(lc.VA026_LCDetail_ID)  FROM VA026_LCDetail lc
                                                        INNER JOIN VA026_SODetail sod ON sod.VA026_LCDetail_ID = lc.VA026_LCDetail_ID 
                                                            WHERE sod.IsActive = 'Y' AND lc.IsActive = 'Y' AND lc.DocStatus IN ('CO' , 'CL')  AND
                                                            sod.VAB_Order_ID =" + order.GetVAB_Order_ID(), null, Get_Trx()));
                                    }
                                }
                                else if (order != null && !order.IsSOTrx())
                                {
                                    // check Header of LC Detail 
                                    sql = @"SELECT VA026_LCDetail_ID FROM VA026_LCDetail WHERE IsActive = 'Y' AND DocStatus IN ('CO' , 'CL') 
                                         AND VAB_Order_ID = " + GetVAB_Order_ID();
                                    VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));

                                    // Check PO Detail tab of Letter of Credit
                                    if (VA026_LCDetail_ID == 0)
                                    {
                                        VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT MIN(lc.VA026_LCDetail_ID)  FROM VA026_LCDetail lc
                                                        INNER JOIN VA026_PODetail sod ON sod.VA026_LCDetail_ID = lc.VA026_LCDetail_ID 
                                                            WHERE sod.IsActive = 'Y' AND lc.IsActive = 'Y' AND lc.DocStatus IN ('CO' , 'CL')  AND
                                                            sod.VAB_Order_ID =" + order.GetVAB_Order_ID(), null, Get_Trx()));
                                    }
                                }
                                if (GetVA026_LCDetail_ID() != VA026_LCDetail_ID)
                                {
                                    log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "VA026_LCDetailNotMatch"));
                                    return false;
                                }
                            }
                            else if (GetVAB_Invoice_ID() > 0)
                            {
                                if (invoice != null && invoice.IsSOTrx() && invoice.GetVAB_Order_ID() > 0)
                                {
                                    // check Header of LC Detail 
                                    sql = @"SELECT VA026_LCDetail_ID FROM VA026_LCDetail WHERE IsActive = 'Y' AND DocStatus IN ('CO' , 'CL') 
                                         AND VA026_Order_ID =  (SELECT VAB_Order_ID FROM VAB_Invoice WHERE VAB_Invoice_ID =  " + GetVAB_Invoice_ID() + " )";
                                    VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));

                                    // Check SO Detail tab of Letter of Credit
                                    if (VA026_LCDetail_ID == 0)
                                    {
                                        VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT MIN(lc.VA026_LCDetail_ID)  FROM VA026_LCDetail lc
                                                        INNER JOIN VA026_SODetail sod ON sod.VA026_LCDetail_ID = lc.VA026_LCDetail_ID 
                                                            WHERE sod.IsActive = 'Y' AND lc.IsActive = 'Y' AND lc.DocStatus IN ('CO' , 'CL')  AND
                                                            sod.VAB_Order_ID =" + invoice.GetVAB_Order_ID(), null, Get_Trx()));
                                    }
                                }
                                else if (invoice != null && !invoice.IsSOTrx() && invoice.GetVAB_Order_ID() > 0)
                                {
                                    // check Header of LC Detail 
                                    sql = @"SELECT VA026_LCDetail_ID FROM VA026_LCDetail WHERE IsActive = 'Y' AND DocStatus IN ('CO' , 'CL') 
                                         AND VAB_Order_ID = (SELECT VAB_Order_ID FROM VAB_Invoice WHERE VAB_Invoice_ID =  " + GetVAB_Invoice_ID() + " )";
                                    VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));

                                    // Check PO Detail tab of Letter of Credit
                                    if (VA026_LCDetail_ID == 0)
                                    {
                                        VA026_LCDetail_ID = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT MIN(lc.VA026_LCDetail_ID)  FROM VA026_LCDetail lc
                                                        INNER JOIN VA026_PODetail sod ON sod.VA026_LCDetail_ID = lc.VA026_LCDetail_ID 
                                                            WHERE sod.IsActive = 'Y' AND lc.IsActive = 'Y' AND lc.DocStatus IN ('CO' , 'CL')  AND
                                                            sod.VAB_Order_ID =" + invoice.GetVAB_Order_ID(), null, Get_Trx()));
                                    }
                                }
                                if (invoice.GetVAB_Order_ID() > 0 && GetVA026_LCDetail_ID() != VA026_LCDetail_ID)
                                {
                                    log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "VA026_LCDetailNotMatch"));
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { }

            string isAutoControl = Util.GetValueOfString(DB.ExecuteScalar("SELECT ChkNoAutoControl FROM VAB_Bank_Acct  WHERE IsActive='Y' AND VAB_Bank_Acct_ID=" + GetVAB_Bank_Acct_ID()));
            if (GetTenderType() == "K")
            {
                if (isAutoControl.Equals("N"))
                {
                    if (string.IsNullOrEmpty(GetCheckNo()))
                    {
                        //"Error" not required
                        //log.SaveError("Error", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        log.SaveError("", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        return false;
                    }
                }
                // when post dated check module is not installed then system check Cheque date can not be greater than Current Date
                if (Env.IsModuleInstalled("VA027_") && !IsReversal())
                {
                    // validate when we giving check (Payable)
                    if (string.IsNullOrEmpty(GetPDCType()) && dt1.GetDocBaseType() != "ARR")
                    {
                        // chcek -- cheque no exist on Post dated record or not
                        // check on first tab
                        if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(*) FROM  VA027_PostDatedCheck pdc 
                                                        INNER JOIN  VAB_Bank_Acct ba ON ba.VAB_Bank_Acct_ID = pdc.VAB_Bank_Acct_ID 
                                                        INNER JOIN VAB_Bank b ON b.VAB_Bank_ID = ba.VAB_Bank_ID 
                                                        INNER JOIN VAB_DocTypes dt ON dt.VAB_DocTypes_ID = pdc.VAB_DocTypes_ID
                                                        WHERE pdc.IsActive = 'Y' AND pdc.DocStatus NOT IN ('RE', 'VO') AND dt.DocBaseType <> 'PDR'
                                                        AND b.VAB_Bank_ID = (SELECT VAB_Bank_ID FROM VAB_Bank_Acct WHERE VAB_Bank_Acct_ID =" + GetVAB_Bank_Acct_ID() +
                                    @" ) AND pdc.VA027_CheckNo = '" + GetCheckNo() + @"'", null, Get_Trx())) > 0)
                        {
                            log.SaveError("Error", Msg.GetMsg(GetCtx(), "VIS_CheckNoAlreadyExistOnPDC"));
                            return false;
                        }

                        // check on Check Detail tab
                        if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(*) FROM VA027_ChequeDetails cd 
                                                    INNER JOIN VA027_PostDatedCheck pdc ON pdc.VA027_PostDatedCheck_ID = cd.VA027_PostDatedCheck_ID
                                                    INNER JOIN  VAB_Bank_Acct ba ON ba.VAB_Bank_Acct_ID = pdc.VAB_Bank_Acct_ID 
                                                    INNER JOIN VAB_Bank b ON b.VAB_Bank_ID = ba.VAB_Bank_ID  
                                                    INNER JOIN VAB_DocTypes dt ON dt.VAB_DocTypes_ID = pdc.VAB_DocTypes_ID 
                                                    WHERE cd.IsActive = 'Y' AND pdc.IsActive = 'Y' AND pdc.VA027_MultiCheque = 'Y' 
                                                    AND pdc.DocStatus NOT IN ('RE', 'VO') AND dt.DocBaseType <> 'PDR' AND
                                                    b.VAB_Bank_ID = (SELECT VAB_Bank_ID FROM VAB_Bank_Acct WHERE VAB_Bank_Acct_ID =" + GetVAB_Bank_Acct_ID() +
                                @" ) AND cd.VA027_CheckNo = '" + GetCheckNo() + @"'", null, Get_Trx())) > 0)
                        {
                            log.SaveError("Error", Msg.GetMsg(GetCtx(), "VIS_CheckNoAlreadyExistOnPDC"));
                            return false;
                        }
                    }
                }

                // make unique record based on Bank and cheque no - thats why on reversing a record - place reverse indicator on it.
                // not checked for reverse record
                if ((!((!string.IsNullOrEmpty(GetDocumentNo()) && GetDocumentNo().Contains("^")) &&
                        (!string.IsNullOrEmpty(GetDescription()) && GetDescription().Contains("{->")))) &&
                        (dt1.GetDocBaseType() != "ARR"))
                {
                    if (newRecord)
                    {
                        if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(*) FROM  VAB_Payment pdc
                        INNER JOIN  VAB_Bank_Acct ba ON ba.VAB_Bank_Acct_ID = pdc.VAB_Bank_Acct_ID 
                        INNER JOIN VAB_Bank b ON b.VAB_Bank_ID = ba.VAB_Bank_ID  
                        INNER JOIN VAB_DocTypes dt ON dt.VAB_DocTypes_ID = pdc.VAB_DocTypes_ID
                        WHERE  pdc.IsActive = 'Y' AND
                        b.VAB_Bank_ID = (SELECT VAB_Bank_ID FROM VAB_Bank_Acct WHERE VAB_Bank_Acct_ID =" + GetVAB_Bank_Acct_ID() +
                                @" ) AND pdc.CheckNo = '" + GetCheckNo() + @"' AND dt.DocBaseType <> 'ARR' AND DocStatus NOT IN ('RE', 'VO')", null, Get_Trx())) > 0)
                        {
                            log.SaveError("Error", Msg.GetMsg(GetCtx(), "VIS_CheckNoAlreadyExist"));
                            return false;
                        }
                    }
                    else
                    {
                        if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(*) FROM  VAB_Payment pdc
                        INNER JOIN  VAB_Bank_Acct ba ON ba.VAB_Bank_Acct_ID = pdc.VAB_Bank_Acct_ID 
                        INNER JOIN VAB_Bank b ON b.VAB_Bank_ID = ba.VAB_Bank_ID 
                        INNER JOIN VAB_DocTypes dt ON dt.VAB_DocTypes_ID = pdc.VAB_DocTypes_ID
                        WHERE  pdc.IsActive = 'Y' AND
                        b.VAB_Bank_ID = (SELECT VAB_Bank_ID FROM VAB_Bank_Acct WHERE VAB_Bank_Acct_ID =" + GetVAB_Bank_Acct_ID() +
                        @" ) AND pdc.CheckNo = '" + GetCheckNo() + @"' AND dt.DocBaseType <> 'ARR'  AND DocStatus NOT IN ('RE', 'VO') 
                        AND pdc.VAB_Payment_ID <> " + GetVAB_Payment_ID(), null, Get_Trx())) > 0)
                        {
                            log.SaveError("Error", Msg.GetMsg(GetCtx(), "VIS_CheckNoAlreadyExist"));
                            return false;
                        }
                    }
                }
            }

            // Reset Amount Dimension if Payment Amount is different
            if (Util.GetValueOfInt(Get_Value("AmtDimPaymentAmount")) > 0)
            {
                string qry = "SELECT Amount FROM VAB_DimAmt WHERE VAB_DimAmt_ID=" + Util.GetValueOfInt(Get_Value("AmtDimPaymentAmount"));
                decimal amtdimAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(qry, null, Get_TrxName()));

                if (amtdimAmt != GetPayAmt())
                {
                    resetAmtDim = true;
                    Set_Value("AmtDimPaymentAmount", null);
                }
            }

            // auto check number work - Mohit - 7 march 2020
            string docBaseType = Util.GetValueOfString(DB.ExecuteScalar("SELECT DocBaseType FROM VAB_DocTypes WHERE IsActive='Y' AND VAB_DocTypes_ID=" + GetVAB_DocTypes_ID()));

            if (GetTenderType() == X_VAB_Payment.TENDERTYPE_Check && !IsReversal())
            {
                // if not auto control.
                if (isAutoControl.Equals("N"))
                {
                    if (docBaseType.Equals(MVABMasterDocType.DOCBASETYPE_APPAYMENT) && GetCheckNo() == null)
                    {
                        //"Error" not required
                        //log.SaveError("Error", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        log.SaveError("", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        return false;
                    }
                    else if (docBaseType.Equals(MVABMasterDocType.DOCBASETYPE_ARRECEIPT) && GetCheckNo() == null)
                    {
                        //"Error" not required
                        //log.SaveError("Error", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        log.SaveError("", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        return false;
                    }

                }
                else
                {
                    autoCheck = false;
                    // if AP Pay and payamt less than 0
                    if (docBaseType.Equals(MVABMasterDocType.DOCBASETYPE_APPAYMENT) && GetPayAmt() < 0 && GetCheckNo() == null)
                    {
                        //"Error" not required
                        //log.SaveError("Error", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        log.SaveError("", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        return false;
                    }
                    // if AR Rec and payamt greater than 0
                    else if (docBaseType.Equals(MVABMasterDocType.DOCBASETYPE_ARRECEIPT) && GetPayAmt() >= 0 && GetCheckNo() == null)
                    {
                        //"Error" not required
                        //log.SaveError("Error", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        log.SaveError("", Msg.GetMsg(GetCtx(), "EnterCheckNo"));
                        return false;
                    }


                }
            }

            // set Payment Amount as PayAmt
            if (GetReversalDoc_ID() <= 0 && Get_ColumnIndex("PaymentAmount") > 0)
            {
                if (Is_ValueChanged("PayAmt"))
                {
                    SetPaymentAmount(GetPayAmt());
                }
                if (!IsProcessing() && !_isWithholdingFromPaymentAllocate && !VerifyAndCalculateWithholding(false))
                {
                    log.SaveError("Error", GetProcessMsg());
                    return false;
                }
                if (Is_ValueChanged("BackupWithholdingAmount") || Is_ValueChanged("WithholdingAmt"))
                {
                    SetPayAmt(Decimal.Subtract(GetPaymentAmount(), Decimal.Add(GetBackupWithholdingAmount(), GetWithholdingAmt())));
                }
            }

            return true;
        }	//	beforeSave

        /// <summary>
        /// Is used to get Invoice payment schedule is Hold payment or not
        /// </summary>
        /// <param name="VAB_sched_InvoicePayment_ID">Invoice payment schedule reference</param>
        /// <returns>TRUE, if hold payment</returns>
        public bool IsHoldpaymentSchedule(int VAB_sched_InvoicePayment_ID)
        {
            try
            {
                String sql = "SELECT IsHoldPayment FROM VAB_sched_InvoicePayment WHERE VAB_sched_InvoicePayment_ID = " + VAB_sched_InvoicePayment_ID;
                String IsHoldPayment = Util.GetValueOfString(DB.ExecuteScalar(sql, null, Get_Trx()));
                return IsHoldPayment.Equals("Y");
            }
            catch
            {
                // when column not found, mean hold payment functionlity not in system
                return false;
            }
        }


        protected override bool AfterSave(bool newRecord, bool success)
        {
            if (!success)
                return success;
            if (Env.IsModuleInstalled("VA027_"))
            {
                if (GetVA027_PostDatedCheck_ID() > 0)
                {
                    if (GetVA009_ExecutionStatus() == "R")
                    {
                        if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VAB_Payment_ID=" + GetVAB_Payment_ID())) > 0)
                        {
                            DB.ExecuteQuery("UPDATE VA027_ChequeDetails Set VA027_PaymentStatus='2' Where VAB_Payment_ID=" + GetVAB_Payment_ID());
                            int count = Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID()));
                            if (count > 0)
                            {
                                if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID() + " And VA027_PAYMENTSTATUS = '2'")) == count)
                                {
                                    DB.ExecuteQuery("UPDATE VA027_PostDatedCheck Set VA027_PaymentStatus='2' where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID());
                                }
                            }
                        }
                        else
                            DB.ExecuteQuery("UPDATE VA027_PostDatedCheck Set VA027_PaymentStatus='2' where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID());
                    }
                }

            }

            if (!IsReceipt())
            {
                bool isRev = false;
                if (Get_ColumnIndex("IsReversal") > 0 && Util.GetValueOfBool(Get_Value("IsReversal")))
                    isRev = true;

                if (!isRev && GetVAB_BusinessPartner_ID() != 0)
                {
                    MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());

                    Decimal payAmt = Decimal.Add(Decimal.Add(GetPayAmt(false), GetDiscountAmt()), GetWriteOffAmt());
                    // If Amount is ZERO then no need to check currency conversion
                    if (!payAmt.Equals(Env.ZERO))
                    {
                        payAmt = MVABExchangeRate.ConvertBase(GetCtx(), payAmt,
                        GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                        if (payAmt == 0)
                        {
                            // JID_0084: On payment window if conversion not found system will give message Correct Message: Could not convert currency to base currency - Conversion type: XXXX
                            MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                            _processMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                                + MVABCurrency.GetISO_Code(GetCtx(), MVAFClient.Get(GetCtx()).GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();
                        }
                    }

                    payAmt = Decimal.Subtract(0, payAmt);

                    string retMsg = "";
                    bool crdAll = bp.IsCreditAllowed(GetVAB_BPart_Location_ID(), payAmt, out retMsg);
                    if (!crdAll)
                        log.SaveWarning("Warning", retMsg);
                    else if (bp.IsCreditWatch(GetVAB_BPart_Location_ID()))
                        log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "VIS_BPCreditWatch"));
                }
            }


            return true;
        }
        /// <summary>
        /// Get checknumber for selected bank account and payment method from GetCheckNo procedure.
        /// </summary>
        /// <param name="payMethod_ID">Payment Mehtod ID</param>
        /// <param name="BankAccount_ID"> Bank Account ID</param>
        /// <param name="trx">transaction object</param>
        /// <returns></returns>
        public string GetChecknumber(int payMethod_ID, int BankAccount_ID, Trx trx)
        {
            SqlParameter[] param = new SqlParameter[3];
            param[0] = new SqlParameter("P_BANKACCOUNTID", BankAccount_ID);
            param[0].SqlDbType = SqlDbType.Int;
            param[0].Direction = ParameterDirection.Input;

            param[1] = new SqlParameter("p_PAYMETHODID", payMethod_ID);
            param[1].SqlDbType = SqlDbType.Int;
            param[1].Direction = ParameterDirection.Input;

            param[2] = new SqlParameter("p_result", 0);
            param[2].SqlDbType = SqlDbType.Int;
            param[2].Direction = ParameterDirection.Output;

            param = DB.ExecuteProcedure("GETCHECKNO", param, trx);

            if (param != null && param.Length > 0) // If Record Found
            {
                return param[0].Value.ToString();
            }
            return string.Empty;
        }

        /**
         * 	Get Allocated Amt in Payment Currency
         *	@return amount or null
         */
        public Decimal? GetAllocatedAmt()
        {
            Decimal? retValue = null;
            if (GetVAB_Charge_ID() != 0)
                return GetPayAmt();
            //
            String sql = "SELECT SUM(currencyConvert(al.Amount,"
                    + "ah.VAB_Currency_ID, p.VAB_Currency_ID,ah.DateTrx,p.VAB_CurrencyType_ID, al.VAF_Client_ID,al.VAF_Org_ID)) "
                + "FROM VAB_DocAllocationLine al"
                + " INNER JOIN VAB_DocAllocation ah ON (al.VAB_DocAllocation_ID=ah.VAB_DocAllocation_ID) "
                + " INNER JOIN VAB_Payment p ON (al.VAB_Payment_ID=p.VAB_Payment_ID) "
                + "WHERE al.VAB_Payment_ID=" + GetVAB_Payment_ID() + ""
                + " AND ah.IsActive='Y' AND al.IsActive='Y'";
            //	+ " AND al.VAB_Invoice_ID IS NOT NULL";
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_Trx());
                if (idr.Read())
                    retValue = Utility.Util.GetValueOfDecimal(idr[0]);
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, "GetAllocatedAmt", e);
            }
            //	log.Fine("GetAllocatedAmt - " + retValue);
            //	? ROUND(NVL(v_AllocatedAmt,0), 2);
            return retValue;
        }

        /**
         * 	Test Allocation (and Set allocated flag)
         *	@return true if updated
         */
        public bool TestAllocation()
        {
            //	Cash Trx always allocated
            if (IsCashTrx())
            {
                if (!IsAllocated())
                {
                    SetIsAllocated(true);
                    return true;
                }
                return false;
            }
            //
            Decimal? alloc = GetAllocatedAmt();
            if (alloc == null)
                alloc = Env.ZERO;
            Decimal total = GetPayAmt() + (Get_ColumnIndex("WithholdingAmt") >= 0 ? (GetBackupWithholdingAmount() + GetWithholdingAmt()) : 0);

            if (!IsReceipt())
                total = Decimal.Negate(total);
            bool test = total.CompareTo((Decimal)alloc) == 0;
            bool change = test != IsAllocated();
            if (change)
                SetIsAllocated(test);
            log.Fine("Allocated=" + test
                + " (" + alloc + "=" + total + ")");
            return change;
        }

        /**
         * 	Set Allocated Flag for payments
         * 	@param ctx context
         *	@param VAB_BusinessPartner_ID if 0 all
         *	@param trxName trx
         */
        public static void SetIsAllocated(Ctx ctx, int VAB_BusinessPartner_ID, Trx trxName)
        {
            int counter = 0;
            String sql = "SELECT * FROM VAB_Payment "
                + "WHERE IsAllocated='N' AND DocStatus IN ('CO','CL')";
            if (VAB_BusinessPartner_ID > 1)
                sql += " AND VAB_BusinessPartner_ID=" + VAB_BusinessPartner_ID;
            else
                sql += " AND VAF_Client_ID=" + ctx.GetVAF_Client_ID();
            try
            {
                DataSet ds = DataBase.DB.ExecuteDataset(sql, null, trxName);
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        MVABPayment pay = new MVABPayment(ctx, dr, trxName);
                        if (pay.TestAllocation())
                            if (pay.Save())
                                counter++;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql, e);
            }
            _log.Config("#" + counter);
        }

        /**
         * 	Set Error Message
         *	@param errorMessage error message
         */
        public void SetErrorMessage(String errorMessage)
        {
            _errorMessage = errorMessage;
        }

        /**
         * 	Get Error Message
         *	@return error message
         */
        public String GetErrorMessage()
        {
            return _errorMessage;
        }


        /**
         *  Set Bank Account for Payment.
         *  @param VAB_Bank_Acct_ID VAB_Bank_Acct_ID
         */
        public new void SetVAB_Bank_Acct_ID(int VAB_Bank_Acct_ID)
        {
            if (VAB_Bank_Acct_ID == 0)
            {
                SetPaymentProcessor();
                if (GetVAB_Bank_Acct_ID() == 0)
                    throw new ArgumentException("Can't find Bank Account");
            }
            else
                base.SetVAB_Bank_Acct_ID(VAB_Bank_Acct_ID);
        }

        /**
         *  Set BankAccount and PaymentProcessor
         *  @return true if found
         */
        public bool SetPaymentProcessor()
        {
            return SetPaymentProcessor(GetTenderType(), GetCreditCardType());
        }

        /**
         *  Set BankAccount and PaymentProcessor
         *  @param tender TenderType see TENDER_
         *  @param CCType CC Type see CC_
         *  @return true if found
         */
        public bool SetPaymentProcessor(String tender, String CCType)
        {
            _paymentProcessor = null;
            //	Get Processor List
            if (_paymentProcessors == null || _paymentProcessors.Length == 0)
                _paymentProcessors = MVABPaymentHandler.Find(GetCtx(), tender, CCType, GetVAF_Client_ID(),
                    GetVAB_Currency_ID(), GetPayAmt(), Get_Trx());
            //	Relax Amount
            if (_paymentProcessors == null || _paymentProcessors.Length == 0)
                _paymentProcessors = MVABPaymentHandler.Find(GetCtx(), tender, CCType, GetVAF_Client_ID(),
                    GetVAB_Currency_ID(), Env.ZERO, Get_Trx());
            if (_paymentProcessors == null || _paymentProcessors.Length == 0)
                return false;

            //	Find the first right one
            for (int i = 0; i < _paymentProcessors.Length; i++)
            {
                if (_paymentProcessors[i].Accepts(tender, CCType))
                {
                    _paymentProcessor = _paymentProcessors[i];
                }
            }
            if (_paymentProcessor != null)
                SetVAB_Bank_Acct_ID(_paymentProcessor.GetVAB_Bank_Acct_ID());
            //
            return _paymentProcessor != null;
        }


        /**
         * 	Get Accepted Credit Cards for PayAmt (default 0)
         *	@return credit cards
         */
        public ValueNamePair[] GetCreditCards()
        {
            return GetCreditCards(GetPayAmt());
        }


        /**
         * 	Get Accepted Credit Cards for amount
         *	@param amt trx amount
         *	@return credit cards
         */
        public ValueNamePair[] GetCreditCards(Decimal amt)
        {
            try
            {
                if (_paymentProcessors == null || _paymentProcessors.Length == 0)
                    _paymentProcessors = MVABPaymentHandler.Find(GetCtx(), null, null,
                        GetVAF_Client_ID(), GetVAB_Currency_ID(), amt, Get_Trx());
                //
                Dictionary<String, ValueNamePair> map = new Dictionary<String, ValueNamePair>(); //	to eliminate duplicates
                for (int i = 0; i < _paymentProcessors.Length; i++)
                {
                    if (_paymentProcessors[i].IsAcceptAMEX())
                        map.Add(CREDITCARDTYPE_Amex, GetCreditCardPair(CREDITCARDTYPE_Amex));
                    if (_paymentProcessors[i].IsAcceptDiners())
                        map.Add(CREDITCARDTYPE_Diners, GetCreditCardPair(CREDITCARDTYPE_Diners));
                    if (_paymentProcessors[i].IsAcceptDiscover())
                        map.Add(CREDITCARDTYPE_Discover, GetCreditCardPair(CREDITCARDTYPE_Discover));
                    if (_paymentProcessors[i].IsAcceptMC())
                        map.Add(CREDITCARDTYPE_MasterCard, GetCreditCardPair(CREDITCARDTYPE_MasterCard));
                    if (_paymentProcessors[i].IsAcceptCorporate())
                        map.Add(CREDITCARDTYPE_PurchaseCard, GetCreditCardPair(CREDITCARDTYPE_PurchaseCard));
                    if (_paymentProcessors[i].IsAcceptVisa())
                        map.Add(CREDITCARDTYPE_Visa, GetCreditCardPair(CREDITCARDTYPE_Visa));
                } //	for all payment processors
                //
                ValueNamePair[] retValue = new ValueNamePair[map.Count];
                map.Values.CopyTo(retValue, 0);
                log.Fine("GetCreditCards - #" + retValue.Length + " - Processors=" + _paymentProcessors.Length);
                return retValue;
            }
            catch (Exception ex)
            {
                //ex.StackTrace;
                log.Severe(ex.ToString());
                return null;
            }
        }

        /**
         * 	Get Type and name pair
         *	@param CreditCardType credit card Type
         *	@return pair
         */
        private ValueNamePair GetCreditCardPair(String creditCardType)
        {
            return new ValueNamePair(creditCardType, GetCreditCardName(creditCardType));
        }

        /**
         *  Credit Card Number
         *  @param CreditCardNumber CreditCard Number
         */
        public new void SetCreditCardNumber(String creditCardNumber)
        {
            base.SetCreditCardNumber(MPaymentValidate.CheckNumeric(creditCardNumber));
        }

        /**
         *  Verification Code
         *  @param newCreditCardVV CC verification
         */
        public void SetCreditCardVV(String newCreditCardVV)
        {
            _creditCardVV = MPaymentValidate.CheckNumeric(newCreditCardVV);
        }

        /**
         *  Verification Code
         *  @return CC verification
         */
        public String GetCreditCardVV()
        {
            return _creditCardVV;
        }

        /**
         *  Two Digit CreditCard MM
         *  @param CreditCardExpMM Exp month
         */
        public new void SetCreditCardExpMM(int creditCardExpMM)
        {
            if (creditCardExpMM < 1 || creditCardExpMM > 12)
            {
                ;
            }
            else
            {
                base.SetCreditCardExpMM(creditCardExpMM);
            }
        }

        /**
         *  Two digit CreditCard YY (til 2020)
         *  @param newCreditCardExpYY 2 or 4 digit year
         */
        public new void SetCreditCardExpYY(int newCreditCardExpYY)
        {
            int creditCardExpYY = newCreditCardExpYY;
            if (newCreditCardExpYY > 1999)
                creditCardExpYY = newCreditCardExpYY - 2000;
            base.SetCreditCardExpYY(creditCardExpYY);
        }

        /**
         *  CreditCard Exp  MMYY
         *  @param mmyy Exp in form of mmyy
         *  @return true if valid
         */
        public bool SetCreditCardExp(String mmyy)
        {
            if (MPaymentValidate.ValidateCreditCardExp(mmyy).Length != 0)
                return false;
            //
            String exp = MPaymentValidate.CheckNumeric(mmyy);
            String mmStr = exp.Substring(0, 2);
            String yyStr = exp.Substring(2, 4);
            SetCreditCardExpMM(int.Parse(mmStr));
            SetCreditCardExpYY(int.Parse(yyStr));
            return true;
        }


        /**
         *  CreditCard Exp  MMYY
         *  @param delimiter / - or null
         *  @return Exp
         */
        public String GetCreditCardExp(String delimiter)
        {
            String mm = GetCreditCardExpMM().ToString();
            String yy = GetCreditCardExpYY().ToString();

            StringBuilder retValue = new StringBuilder();
            if (mm.Length == 1)
                retValue.Append("0");
            retValue.Append(mm);
            //
            if (delimiter != null)
                retValue.Append(delimiter);
            //
            if (yy.Length == 1)
                retValue.Append("0");
            retValue.Append(yy);
            //
            return (retValue.ToString());
        }

        /**
         *  MICR
         *  @param MICR MICR
         */
        public new void SetMicr(String sMICR)
        {
            base.SetMicr(MPaymentValidate.CheckNumeric(sMICR));
        }

        /**
         *  Routing No
         *  @param RoutingNo Routing No
         */
        public new void SetRoutingNo(String routingNo)
        {
            base.SetRoutingNo(MPaymentValidate.CheckNumeric(routingNo));
        }


        /**
         *  Bank Account No
         *  @param AccountNo AccountNo
         */
        public new void SetAccountNo(String accountNo)
        {
            base.SetAccountNo(MPaymentValidate.CheckNumeric(accountNo));
        }

        /**
         *  Check No
         *  @param CheckNo Check No
         */
        public new void SetCheckNo(String checkNo)
        {
            base.SetCheckNo(MPaymentValidate.CheckNumeric(checkNo));
        }

        /**
         *  Set DocumentNo to Payment Info.
         * 	If there is a R_PnRef that is Set automatically 
         */
        private void SetDocumentNo()
        {
            if (IsAdvanceDocControl())
            {
                return;
            }

            // Added By Bharat 17.05.2014
            if (TENDERTYPE_Check.Equals(GetTenderType()))
            {
                return;
            }

            //	Cash Transfer
            if ("X".Equals(GetTenderType()))
                return;
            //	Current Document No
            String documentNo = GetDocumentNo();
            //	Existing reversal
            if (documentNo != null
                && documentNo.IndexOf(REVERSE_INDICATOR) >= 0)
                return;

            //	If external number exists - enforce it 
            if (GetR_PnRef() != null && GetR_PnRef().Length > 0)
            {
                if (!GetR_PnRef().Equals(documentNo))
                    SetDocumentNo(GetR_PnRef());
                return;
            }

            documentNo = "";
            // ------------code comment by Anuj---------------
            //****** Document no generate on bases of Cradit card details. like M 1234 1/4 **********
            //******after comment the code.. Document no generated properly due to requirment**** 
            //	Credit Card
            //if (TENDERTYPE_CreditCard.Equals(GetTenderType()))
            //{
            //    documentNo = GetCreditCardType()
            //        + " " + Obscure.ObscureValue(GetCreditCardNumber())
            //        + " " + GetCreditCardExpMM()
            //        + "/" + GetCreditCardExpYY();
            //}
            ////	Own Check No
            //else 
            if (TENDERTYPE_Check.Equals(GetTenderType())
            && !IsReceipt()
            && GetCheckNo() != null && GetCheckNo().Length > 0)
            {
                documentNo = GetCheckNo();
            }
            //	Customer Check: Routing: Account #Check 
            else if (TENDERTYPE_Check.Equals(GetTenderType())
                && IsReceipt())
            {
                if (GetRoutingNo() != null)
                    documentNo = GetRoutingNo() + ": ";
                if (GetAccountNo() != null)
                    documentNo += GetAccountNo();
                if (GetCheckNo() != null)
                {
                    if (documentNo.Length > 0)
                        documentNo += " ";
                    documentNo += "#" + GetCheckNo();
                }
            }

            //	Set Document No
            documentNo = documentNo.Trim();
            if (documentNo.Length > 0)
                SetDocumentNo(documentNo);
        }

        /// <summary>
        /// Check if Advance Document no Module is downloaded
        /// And Advance Doc Control is set then Return and don't set Document No
        /// </summary>
        /// <returns></returns>
        private bool IsAdvanceDocControl()
        {
            Tuple<String, String, String> aInfo = null;
            if (Env.HasModulePrefix("ED001_", out aInfo))
            {
                int docType = GetVAB_DocTypes_ID();

                string sql = "SELECT ED001_IsAdvancedocControl FROM VAB_DocTypes WHERE VAB_DocTypes_ID = " + docType;
                string chkAdvDocCon = Util.GetValueOfString(DB.ExecuteScalar(sql, null, null));
                if (chkAdvDocCon.Equals("Y"))
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * 	Set Refernce No (and Document No)
         *	@param R_PnRef reference
         */
        public new void SetR_PnRef(String R_PnRef)
        {
            base.SetR_PnRef(R_PnRef);
            if (R_PnRef != null)
                SetDocumentNo(R_PnRef);
        }

        /**
         *  Set Payment Amount
         *  @param PayAmt Pay Amt
         */
        public new void SetPayAmt(Decimal? payAmt)
        {
            base.SetPayAmt(payAmt == null ? Env.ZERO : (Decimal)payAmt);
        }

        /**
         *  Set Payment Amount
         *
         * @param VAB_Currency_ID currency
         * @param payAmt amount
         */
        public void SetAmount(int VAB_Currency_ID, Decimal payAmt)
        {
            if (VAB_Currency_ID == 0)
                VAB_Currency_ID = MVAFClient.Get(GetCtx()).GetVAB_Currency_ID();
            SetVAB_Currency_ID(VAB_Currency_ID);
            SetPayAmt(payAmt);
        }

        /**
         *  Discount Amt
         *  @param DiscountAmt Discount
         */
        public new void SetDiscountAmt(Decimal? discountAmt)
        {
            base.SetDiscountAmt(discountAmt == null ? Env.ZERO : (Decimal)discountAmt);
        }

        /**
         *  WriteOff Amt
         *  @param WriteOffAmt WriteOff
         */
        public new void SetWriteOffAmt(Decimal? writeOffAmt)
        {
            base.SetWriteOffAmt(writeOffAmt == null ? Env.ZERO : (Decimal)writeOffAmt);
        }

        /**
         *  OverUnder Amt
         *  @param OverUnderAmt OverUnder
         */
        public new void SetOverUnderAmt(Decimal? overUnderAmt)
        {
            base.SetOverUnderAmt(overUnderAmt == null ? Env.ZERO : (Decimal)overUnderAmt);
            SetIsOverUnderPayment(GetOverUnderAmt().CompareTo(Env.ZERO) != 0);
        }

        /**
         *  Tax Amt
         *  @param TaxAmt Tax
         */
        public new void SetTaxAmt(Decimal? taxAmt)
        {
            base.SetTaxAmt(taxAmt == null ? Env.ZERO : (Decimal)taxAmt);
        }

        /**
         * 	Set Info from BP Bank Account
         *	@param ba BP bank account
         */
        public void SetBP_BankAccount(MVABBPartBankAcct ba)
        {
            log.Fine("" + ba);
            if (ba == null)
                return;
            SetVAB_BusinessPartner_ID(ba.GetVAB_BusinessPartner_ID());
            SetAccountAddress(ba.GetA_Name(), ba.GetA_Street(), ba.GetA_City(),
                ba.GetA_State(), ba.GetA_Zip(), ba.GetA_Country());
            SetA_EMail(ba.GetA_EMail());
            SetA_Ident_DL(ba.GetA_Ident_DL());
            SetA_Ident_SSN(ba.GetA_Ident_SSN());
            //	CC
            if (ba.GetCreditCardType() != null)
                SetCreditCardType(ba.GetCreditCardType());
            if (ba.GetCreditCardNumber() != null)
                SetCreditCardNumber(ba.GetCreditCardNumber());
            if (ba.GetCreditCardExpMM() != 0)
                SetCreditCardExpMM(ba.GetCreditCardExpMM());
            if (ba.GetCreditCardExpYY() != 0)
                SetCreditCardExpYY(ba.GetCreditCardExpYY());
            //	Bank
            if (ba.GetAccountNo() != null)
                SetAccountNo(ba.GetAccountNo());
            if (ba.GetRoutingNo() != null)
                SetRoutingNo(ba.GetRoutingNo());
        }

        /**
         * 	Save Info from BP Bank Account
         *	@param ba BP bank account
         * 	@return true if saved
         */
        public bool SaveToBP_BankAccount(MVABBPartBankAcct ba)
        {
            if (ba == null)
                return false;
            ba.SetA_Name(GetA_Name());
            ba.SetA_Street(GetA_Street());
            ba.SetA_City(GetA_City());
            ba.SetA_State(GetA_State());
            ba.SetA_Zip(GetA_Zip());
            ba.SetA_Country(GetA_Country());
            ba.SetA_EMail(GetA_EMail());
            ba.SetA_Ident_DL(GetA_Ident_DL());
            ba.SetA_Ident_SSN(GetA_Ident_SSN());
            //	CC
            ba.SetCreditCardType(GetCreditCardType());
            ba.SetCreditCardNumber(GetCreditCardNumber());
            ba.SetCreditCardExpMM(GetCreditCardExpMM());
            ba.SetCreditCardExpYY(GetCreditCardExpYY());
            //	Bank
            if (GetAccountNo() != null)
                ba.SetAccountNo(GetAccountNo());
            if (GetRoutingNo() != null)
                ba.SetRoutingNo(GetRoutingNo());
            //	Trx
            ba.SetR_AvsAddr(GetR_AvsAddr());
            ba.SetR_AvsZip(GetR_AvsZip());
            //
            bool ok = ba.Save(Get_Trx());
            log.Fine(ba.ToString());
            return ok;
        }

        /**
         * 	Set Doc Type bases on IsReceipt
         */
        private void SetVAB_DocTypes_ID()
        {
            SetVAB_DocTypes_ID(IsReceipt());
        }

        /**
         * 	Set Doc Type
         * 	@param isReceipt is receipt
         */
        public void SetVAB_DocTypes_ID(bool isReceipt)
        {
            SetIsReceipt(isReceipt);
            String sql = "SELECT VAB_DocTypes_ID FROM VAB_DocTypes WHERE VAF_Client_ID=@clid AND DocBaseType=@docbs ORDER BY IsDefault DESC";
            IDataReader idr = null;
            try
            {
                SqlParameter[] param = new SqlParameter[2];
                param[0] = new SqlParameter("@clid", GetVAF_Client_ID());
                if (isReceipt)
                {
                    param[1] = new SqlParameter("@docbs", MVABMasterDocType.DOCBASETYPE_ARRECEIPT);
                }
                else
                {
                    param[1] = new SqlParameter("@docbs", MVABMasterDocType.DOCBASETYPE_APPAYMENT);
                }

                idr = DataBase.DB.ExecuteReader(sql, param);
                if (idr.Read())
                {
                    SetVAB_DocTypes_ID(Utility.Util.GetValueOfInt(idr[0].ToString()));
                }
                else
                {
                    log.Warning("SetDocType - NOT found - isReceipt=" + isReceipt);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }
        }

        /**
         * 	Set Document Type
         *	@param VAB_DocTypes_ID doc type
         */
        public new void SetVAB_DocTypes_ID(int VAB_DocTypes_ID)
        {
            //	if (GetDocumentNo() != null && GetVAB_DocTypes_ID() != VAB_DocTypes_ID)
            //		SetDocumentNo(null);
            base.SetVAB_DocTypes_ID(VAB_DocTypes_ID);
        }

        /**
         * 	Verify Document Type with Invoice
         *	@return true if ok
         */
        private bool VerifyDocType()
        {
            if (GetVAB_DocTypes_ID() == 0)
                return false;
            //
            Boolean? invoiceSO = null;
            IDataReader idr = null;
            String sql = "";
            //	Check Invoice First
            if (GetVAB_Invoice_ID() > 0)
            {
                sql = "SELECT idt.IsSOTrx "
                    + "FROM VAB_Invoice i"
                    + " INNER JOIN VAB_DocTypes idt ON (i.VAB_DocTypes_ID=idt.VAB_DocTypes_ID) "
                    + "WHERE i.VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                try
                {
                    idr = DataBase.DB.ExecuteReader(sql, null, Get_Trx());
                    if (idr.Read())
                        invoiceSO = "Y".Equals(idr[0].ToString());
                    idr.Close();
                    idr = null;
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Log(Level.SEVERE, sql, e);
                }
            }	//	Invoice

            //	DocumentType
            Boolean? paymentSO = null;
            sql = "SELECT IsSOTrx "
                + "FROM VAB_DocTypes "
                + "WHERE VAB_DocTypes_ID=" + GetVAB_DocTypes_ID();
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_Trx());
                if (idr.Read())
                    paymentSO = "Y".Equals(idr[0].ToString());
                idr.Close();
                idr = null;
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }

            //	No Payment Info
            if (paymentSO == null)
                return false;
            SetIsReceipt((Boolean)paymentSO);

            //	We have an Invoice .. and it does not match
            if (invoiceSO != null
                    && (Boolean)invoiceSO != (Boolean)paymentSO)
                return false;
            //	OK
            return true;
        }


        /**
         * 	Set Invoice - Callout
         *	@param oldVAB_Invoice_ID old BP
         *	@param newVAB_Invoice_ID new BP
         *	@param windowNo window no
         */
        // @UICallout
        public void SetVAB_Invoice_ID(String oldVAB_Invoice_ID, String newVAB_Invoice_ID, int windowNo)
        {
            if (newVAB_Invoice_ID == null || newVAB_Invoice_ID.Length == 0)
                return;
            int VAB_Invoice_ID = int.Parse(newVAB_Invoice_ID);
            //  reSet as dependent fields Get reSet
            //p_changeVO.SetContext(GetCtx(), windowNo, "VAB_Invoice_ID", VAB_Invoice_ID);
            SetContext(windowNo, "VAB_Invoice_ID", VAB_Invoice_ID.ToString());
            SetVAB_Invoice_ID(VAB_Invoice_ID);
            if (VAB_Invoice_ID == 0)
                return;

            SetVAB_Order_ID(0);
            SetVAB_Charge_ID(0);
            SetVAB_Project_ID(0);
            SetIsPrepayment(false);
            //
            SetDiscountAmt(Env.ZERO);
            SetWriteOffAmt(Env.ZERO);
            SetIsOverUnderPayment(false);
            SetOverUnderAmt(Env.ZERO);

            int VAB_sched_InvoicePayment_ID = 0;
            if (GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_Invoice_ID") == VAB_Invoice_ID
            && GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_sched_InvoicePayment_ID") != 0)
                VAB_sched_InvoicePayment_ID = GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_sched_InvoicePayment_ID");

            //  Payment Date
            DateTime? ts = GetDateTrx();
            if (ts == null)
                ts = DateTime.Now;
            //
            String sql = "SELECT VAB_BusinessPartner_ID,VAB_Currency_ID,"		        //	1..2
                + " invoiceOpen(VAB_Invoice_ID, @paysch),"					//	3		#1
                + " invoiceDiscount(VAB_Invoice_ID,@tsdt,@paysch1), IsSOTrx "	//	4..5	#2/3
                + "FROM VAB_Invoice WHERE VAB_Invoice_ID=@invid";			    //			#4
            IDataReader idr = null;
            try
            {
                SqlParameter[] param = new SqlParameter[4];
                param[0] = new SqlParameter("@paysch", VAB_sched_InvoicePayment_ID);
                param[1] = new SqlParameter("@tsdt", (DateTime?)ts);
                param[2] = new SqlParameter("@paysch1", VAB_sched_InvoicePayment_ID);
                param[3] = new SqlParameter("@invid", VAB_Invoice_ID);

                idr = DataBase.DB.ExecuteReader(sql, null, null);
                if (idr.Read())
                {
                    SetVAB_BusinessPartner_ID(Utility.Util.GetValueOfInt(idr[0].ToString()));
                    int VAB_Currency_ID = Utility.Util.GetValueOfInt(idr[1].ToString());	//	Set Invoice Currency
                    SetVAB_Currency_ID(VAB_Currency_ID);
                    //
                    Decimal? invoiceOpen = Utility.Util.GetValueOfDecimal(idr[2]);		//	Set Invoice OPen Amount
                    if (invoiceOpen == null)
                        invoiceOpen = Env.ZERO;
                    Decimal? discountAmt = Utility.Util.GetValueOfDecimal(idr[3]);		//	Set Discount Amt
                    if (discountAmt == null)
                        discountAmt = Env.ZERO;
                    SetPayAmt(Decimal.Subtract(Utility.Util.GetValueOfDecimal(invoiceOpen), Utility.Util.GetValueOfDecimal(discountAmt)));
                    SetDiscountAmt((Decimal)discountAmt);
                    //IsSOTrx, Project
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }
            CheckDocType(windowNo);
        }

        /**
         * 	Set Order - Callout
         *	@param oldVAB_Order_ID old BP
         *	@param newVAB_Order_ID new BP
         *	@param windowNo window no
         */
        //@UICallout
        public void SetVAB_Order_ID(String oldVAB_Order_ID, String newVAB_Order_ID, int windowNo)
        {
            if (newVAB_Order_ID == null || newVAB_Order_ID.Length == 0)
                return;
            int VAB_Order_ID = int.Parse(newVAB_Order_ID);
            SetVAB_Order_ID(VAB_Order_ID);
            if (VAB_Order_ID == 0)
                return;
            //
            SetVAB_Invoice_ID(0);
            SetVAB_Charge_ID(0);
            SetVAB_Project_ID(0);
            SetIsPrepayment(true);
            //
            SetDiscountAmt(Env.ZERO);
            SetWriteOffAmt(Env.ZERO);
            SetIsOverUnderPayment(false);
            SetOverUnderAmt(Env.ZERO);
            //  Payment Date
            DateTime? ts = GetDateTrx();
            if (ts == null)
                ts = DateTime.Now;
            //
            String sql = "SELECT VAB_BusinessPartner_ID,VAB_Currency_ID, GrandTotal, VAB_Project_ID "
                + "FROM VAB_Order WHERE VAB_Order_ID=" + VAB_Order_ID; 	// #1
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                if (idr.Read())
                {
                    SetVAB_BusinessPartner_ID(Utility.Util.GetValueOfInt(idr[0].ToString()));
                    int VAB_Currency_ID = Utility.Util.GetValueOfInt(idr[1].ToString());	//	Set Order Currency
                    SetVAB_Currency_ID(VAB_Currency_ID);
                    //
                    Decimal? grandTotal = Utility.Util.GetValueOfDecimal(idr[2]);		//	Set Pay Amount
                    if (grandTotal == null)
                        grandTotal = Env.ZERO;
                    SetPayAmt(Utility.Util.GetValueOfDecimal(grandTotal));
                    SetVAB_Project_ID(Utility.Util.GetValueOfInt(idr[3].ToString()));
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }
            CheckDocType(windowNo);
        }

        /**
         * 	Set Charge - Callout
         *	@param oldVAB_Charge_ID old BP
         *	@param newVAB_Charge_ID new BP
         *	@param windowNo window no
         */
        //@UICallout
        public void SetVAB_Charge_ID(String oldVAB_Charge_ID, String newVAB_Charge_ID, int windowNo)
        {
            if (newVAB_Charge_ID == null || newVAB_Charge_ID.Length == 0)
                return;
            int VAB_Charge_ID = int.Parse(newVAB_Charge_ID);
            SetVAB_Charge_ID(VAB_Charge_ID);
            if (VAB_Charge_ID == 0)
                return;
            //
            SetVAB_Order_ID(0);
            SetVAB_Invoice_ID(0);
            SetVAB_Project_ID(0);
            SetIsPrepayment(true);
            SetIsReceipt(false);
            //
            SetDiscountAmt(Env.ZERO);
            SetWriteOffAmt(Env.ZERO);
            SetIsOverUnderPayment(false);
            SetOverUnderAmt(Env.ZERO);
        }

        /**
         * 	Set Order - Callout
         *	@param oldVAB_DocTypes_ID old BP
         *	@param newVAB_DocTypes_ID new BP
         *	@param windowNo window no
         */
        //@UICallout
        public void SetVAB_DocTypes_ID(String oldVAB_DocTypes_ID, String newVAB_DocTypes_ID, int windowNo)
        {
            if (newVAB_DocTypes_ID == null || newVAB_DocTypes_ID.Length == 0)
                return;
            int VAB_DocTypes_ID = int.Parse(newVAB_DocTypes_ID);
            SetVAB_DocTypes_ID(VAB_DocTypes_ID);
            CheckDocType(windowNo);
        }

        /**
         * 	Check Document Type (Callout)
         *	@param windowNo windowNo no
         */
        private void CheckDocType(int windowNo)
        {
            int VAB_Invoice_ID = GetVAB_Invoice_ID();
            int VAB_Order_ID = GetVAB_Order_ID();
            int VAB_DocTypes_ID = GetVAB_DocTypes_ID();
            log.Fine("VAB_Invoice_ID=" + VAB_Invoice_ID + ", VAB_DocTypes_ID=" + VAB_DocTypes_ID);
            MVABDocTypes dt = null;
            if (VAB_DocTypes_ID != 0)
            {
                dt = MVABDocTypes.Get(GetCtx(), VAB_DocTypes_ID);
                SetIsReceipt(dt.IsSOTrx());
                //p_changeVO.SetContext(GetCtx(), windowNo, "IsSOTrx", dt.IsSOTrx());
                SetContext(windowNo, "IsSOTrx", dt.IsSOTrx());
            }
            //	Invoice
            if (VAB_Invoice_ID != 0)
            {
                MVABInvoice inv = new MVABInvoice(GetCtx(), VAB_Invoice_ID, null);
                if (dt != null)
                {
                    if (inv.IsSOTrx() != dt.IsSOTrx())
                    {
                        //p_changeVO.addError(Msg.GetMsg(GetCtx(),"PaymentDocTypeInvoiceInconsistent"));
                    }
                }
            }
            //	Order Waiting Payment (can only be SO)
            if (VAB_Order_ID != 0 && !dt.IsSOTrx())
            {
                //p_changeVO.addError(Msg.GetMsg(GetCtx(),"PaymentDocTypeInvoiceInconsistent"));
            }
        }

        /**
         * 	Set Rate - Callout.
         *	@param oldVAB_CurrencyType_ID old
         *	@param newVAB_CurrencyType_ID new
         *	@param windowNo window no
         */
        //@UICallout
        public void SetVAB_CurrencyType_ID(String oldVAB_CurrencyType_ID,
            String newVAB_CurrencyType_ID, int windowNo)
        {
            if (newVAB_CurrencyType_ID == null || newVAB_CurrencyType_ID.Length == 0)
                return;
            int VAB_CurrencyType_ID = int.Parse(newVAB_CurrencyType_ID);
            SetVAB_CurrencyType_ID(VAB_CurrencyType_ID);
            if (VAB_CurrencyType_ID == 0)
                return;
            CheckAmt(windowNo, "VAB_CurrencyType_ID");
        }

        /**
         * 	Set Currency - Callout.
         *	@param oldVAB_Currency_ID old
         *	@param newVAB_Currency_ID new
         *	@param windowNo window no
         */
        //@UICallout
        public void SetVAB_Currency_ID(String oldVAB_Currency_ID, String newVAB_Currency_ID, int windowNo)
        {
            if (newVAB_Currency_ID == null || newVAB_Currency_ID.Length == 0)
                return;
            int VAB_Currency_ID = int.Parse(newVAB_Currency_ID);
            if (VAB_Currency_ID == 0)
                return;
            SetVAB_Currency_ID(VAB_Currency_ID);
            CheckAmt(windowNo, "VAB_Currency_ID");
        }


        /**
         * 	Set Discount - Callout
         *	@param oldDiscountAmt old value
         *	@param newDiscountAmt new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetDiscountAmt(String oldDiscountAmt, String newDiscountAmt, int windowNo)
        {
            if (newDiscountAmt == null || newDiscountAmt.Length == 0)
                return;
            Decimal? discountAmt = PO.ConvertToBigDecimal(newDiscountAmt);
            SetDiscountAmt(discountAmt);
            CheckAmt(windowNo, "DiscountAmt");
        }

        /**
         * 	Set Is Over Under Payment - Callout
         *	@param oldIsOverUnderPayment old value
         *	@param newIsOverUnderPayment new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetIsOverUnderPayment(String oldIsOverUnderPayment,
                String newIsOverUnderPayment, int windowNo)
        {
            if (newIsOverUnderPayment == null || newIsOverUnderPayment.Length == 0)
                return;
            CheckAmt(windowNo, "IsOverUnderPayment");
            SetIsOverUnderPayment("Y".Equals(newIsOverUnderPayment));

        }

        /**
         * 	Set Over Under Amt - Callout
         *	@param oldOverUnderAmt old value
         *	@param newOverUnderAmt new value
         *	@param windowNo window
         *	@throws Exception
         */
        // @UICallout
        public void SetOverUnderAmt(String oldOverUnderAmt, String newOverUnderAmt, int windowNo)
        {
            if (newOverUnderAmt == null || newOverUnderAmt.Length == 0)
                return;
            Decimal? OverUnderAmt = PO.ConvertToBigDecimal(newOverUnderAmt);
            SetOverUnderAmt(OverUnderAmt);
            CheckAmt(windowNo, "OverUnderAmt");
        }

        /**
         * 	Set Pay Amt - Callout
         *	@param oldPayAmt old value
         *	@param newPayAmt new value
         *	@param windowNo window
         *	@throws Exception
         */
        // @UICallout
        public void SetPayAmt(String oldPayAmt, String newPayAmt, int windowNo)
        {
            if (newPayAmt == null || newPayAmt.Length == 0)
                return;
            Decimal? PayAmt = PO.ConvertToBigDecimal(newPayAmt);
            SetPayAmt(PayAmt);
            CheckAmt(windowNo, "PayAmt");
        }

        /**
         * 	Set WriteOff Amt - Callout
         *	@param oldWriteOffAmt old value
         *	@param newWriteOffAmt new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetWriteOffAmt(String oldWriteOffAmt, String newWriteOffAmt, int windowNo)
        {
            if (newWriteOffAmt == null || newWriteOffAmt.Length == 0)
                return;
            Decimal? WriteOffAmt = PO.ConvertToBigDecimal(newWriteOffAmt);
            SetWriteOffAmt(WriteOffAmt);
            CheckAmt(windowNo, "WriteOffAmt");
        }

        /**
         * 	Set DateTrx - Callout
         *	@param oldDateTrx old
         *	@param newDateTrx new
         *	@param windowNo window no
         */
        //@UICallout
        public void SetDateTrx(String oldDateTrx, String newDateTrx, int windowNo)
        {
            if (newDateTrx == null || newDateTrx.Length == 0)
                return;
            DateTime? dateTrx = PO.ConvertToTimestamp(newDateTrx);
            if (dateTrx == null)
                return;
            SetDateTrx((DateTime?)dateTrx);
            SetDateAcct((DateTime?)dateTrx);
            CheckAmt(windowNo, "DateTrx");
        }

        /**
         * 	Check amount (Callout)
         *	@param windowNo window
         *	@param columnName columnName
         */
        private void CheckAmt(int windowNo, String columnName)
        {
            int VAB_Invoice_ID = GetVAB_Invoice_ID();
            //	New Payment
            if (GetVAB_Payment_ID() == 0
                && GetVAB_BusinessPartner_ID() == 0
                && VAB_Invoice_ID == 0)
                return;
            int VAB_Currency_ID = GetVAB_Currency_ID();
            if (VAB_Currency_ID == 0)
                return;

            //	Changed Column
            if (columnName.Equals("IsOverUnderPayment")	//	Set Over/Under Amt to Zero
                || !IsOverUnderPayment())
                SetOverUnderAmt(Env.ZERO);

            int VAB_sched_InvoicePayment_ID = 0;
            if (GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_Invoice_ID") == VAB_Invoice_ID
                && GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_sched_InvoicePayment_ID") != 0)
                VAB_sched_InvoicePayment_ID = GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAB_sched_InvoicePayment_ID");

            //	Get Open Amount & Invoice Currency
            Decimal? invoiceOpenAmt = Env.ZERO;
            Decimal discountAmt = Env.ZERO;
            int VAB_Currency_Invoice_ID = 0;
            if (VAB_Invoice_ID != 0)
            {
                DateTime? ts = GetDateTrx();
                if (ts == null)
                    ts = DateTime.Now;
                String sql = "SELECT VAB_BusinessPartner_ID,VAB_Currency_ID,"		        //	1..2
                    + " invoiceOpen(VAB_Invoice_ID, @paysch),"					//	3		#1
                    + " invoiceDiscount(VAB_Invoice_ID,@tsdt,@paysch1), IsSOTrx "	//	4..5	#2/3
                    + "FROM VAB_Invoice WHERE VAB_Invoice_ID=@invid";			    //			#4
                IDataReader idr = null;
                try
                {
                    SqlParameter[] param = new SqlParameter[4];
                    param[0] = new SqlParameter("@paysch", VAB_sched_InvoicePayment_ID);
                    param[1] = new SqlParameter("@tsdt", Convert.ToDateTime(ts));
                    param[2] = new SqlParameter("@paysch1", VAB_sched_InvoicePayment_ID);
                    param[3] = new SqlParameter("@invid", VAB_Invoice_ID);
                    idr = DataBase.DB.ExecuteReader(sql, null, null);
                    if (idr.Read())
                    {
                        VAB_Currency_Invoice_ID = Utility.Util.GetValueOfInt(idr[1].ToString());
                        invoiceOpenAmt = idr.GetDecimal(2);		//	Set Invoice Open Amount
                        if (invoiceOpenAmt == null)
                            invoiceOpenAmt = Env.ZERO;
                        discountAmt = idr.GetDecimal(3);
                    }
                    idr.Close();
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Log(Level.SEVERE, sql, e);
                }
            }	//	Get Invoice Info
            log.Fine("Open=" + invoiceOpenAmt + " Discount= " + discountAmt
                + ", VAB_Invoice_ID=" + VAB_Invoice_ID
               + ", VAB_Currency_ID=" + VAB_Currency_Invoice_ID);

            //	Get Info from Tab
            Decimal payAmt = GetPayAmt();
            Decimal writeOffAmt = GetWriteOffAmt();
            Decimal overUnderAmt = GetOverUnderAmt();
            Decimal enteredDiscountAmt = GetDiscountAmt();
            log.Fine("Pay=" + payAmt + ", Discount=" + enteredDiscountAmt
               + ", WriteOff=" + writeOffAmt + ", OverUnderAmt=" + overUnderAmt);
            //	Get Currency Info
            MVABCurrency currency = MVABCurrency.Get(GetCtx(), VAB_Currency_ID);
            DateTime? convDate = GetDateTrx();
            int VAB_CurrencyType_ID = GetVAB_CurrencyType_ID();
            int VAF_Client_ID = GetVAF_Client_ID();
            int VAF_Org_ID = GetVAF_Org_ID();
            //	Get Currency Rate
            Decimal currencyRate = Env.ONE;
            if ((VAB_Currency_ID > 0 && VAB_Currency_Invoice_ID > 0 &&
                VAB_Currency_ID != VAB_Currency_Invoice_ID)
                || columnName.Equals("VAB_Currency_ID") || columnName.Equals("VAB_CurrencyType_ID"))
            {
                log.Fine("InvCurrency=" + VAB_Currency_Invoice_ID
                    + ", PayCurrency=" + VAB_Currency_ID
                    + ", Date=" + convDate + ", Type=" + VAB_CurrencyType_ID);
                currencyRate = MVABExchangeRate.GetRate(VAB_Currency_Invoice_ID, VAB_Currency_ID,
                    convDate, VAB_CurrencyType_ID, VAF_Client_ID, VAF_Org_ID);
                if (currencyRate.CompareTo(Env.ZERO) == 0)
                {
                    //	mTab.SetValue("VAB_Currency_ID", new Integer(VAB_Currency_Invoice_ID));	//	does not work
                    if (VAB_Currency_Invoice_ID != 0)
                    {
                        //p_changeVO.addError(Msg.GetMsg(GetCtx(),"NoCurrencyConversion"));
                    }
                    return;
                }
                //
                invoiceOpenAmt = Decimal.Round(Decimal.Multiply((Decimal)invoiceOpenAmt, currencyRate),
                    currency.GetStdPrecision(), MidpointRounding.AwayFromZero);
                discountAmt = Decimal.Round(Decimal.Multiply(discountAmt, currencyRate),
                    currency.GetStdPrecision(), MidpointRounding.AwayFromZero);
                log.Fine("Rate=" + currencyRate + ", InvoiceOpenAmt=" + invoiceOpenAmt + ", DiscountAmt=" + discountAmt);
            }

            //	Currency Changed - convert all
            if (columnName.Equals("VAB_Currency_ID") || columnName.Equals("VAB_CurrencyType_ID"))
            {

                writeOffAmt = Decimal.Round(Decimal.Multiply(writeOffAmt, currencyRate),
                    currency.GetStdPrecision(), MidpointRounding.AwayFromZero);
                SetWriteOffAmt(writeOffAmt);
                overUnderAmt = Decimal.Round(Decimal.Multiply(overUnderAmt, currencyRate),
                    currency.GetStdPrecision(), MidpointRounding.AwayFromZero);
                SetOverUnderAmt(overUnderAmt);

                // Entered Discount amount should be converted to entered currency 
                enteredDiscountAmt = Decimal.Round(Decimal.Multiply(enteredDiscountAmt, currencyRate),
                    currency.GetStdPrecision(), MidpointRounding.AwayFromZero);
                SetDiscountAmt(enteredDiscountAmt);

                //PayAmt = InvoiceOpenAmt.subtract(DiscountAmt).subtract(WriteOffAmt).subtract(OverUnderAmt);
                payAmt = Decimal.Subtract(Decimal.Subtract(Decimal.Subtract((Decimal)invoiceOpenAmt, discountAmt), writeOffAmt), overUnderAmt);
                SetPayAmt(payAmt);
            }

            //	No Invoice - Set Discount, Witeoff, Under/Over to 0
            else if (VAB_Invoice_ID == 0)
            {
                if (Env.Signum(discountAmt) != 0)
                    SetDiscountAmt(Env.ZERO);
                if (Env.Signum(writeOffAmt) != 0)
                    SetWriteOffAmt(Env.ZERO);
                if (Env.Signum(overUnderAmt) != 0)
                    SetOverUnderAmt(Env.ZERO);
            }
            //  PayAmt - calculate write off
            else if (columnName.Equals("PayAmt"))
            {
                //WriteOffAmt = InvoiceOpenAmt.subtract(PayAmt).subtract(DiscountAmt).subtract(OverUnderAmt);
                writeOffAmt = Decimal.Subtract(Decimal.Subtract(Decimal.Subtract((Decimal)invoiceOpenAmt, payAmt), discountAmt), overUnderAmt);
                if (Env.ZERO.CompareTo(writeOffAmt) > 0)
                {
                    if (Math.Abs(writeOffAmt).CompareTo(discountAmt) <= 0)
                        discountAmt = Decimal.Add(discountAmt, writeOffAmt);
                    else
                        discountAmt = Env.ZERO;
                    //WriteOffAmt = InvoiceOpenAmt.subtract(PayAmt).subtract(DiscountAmt).subtract(OverUnderAmt);
                    writeOffAmt = Decimal.Subtract(Decimal.Subtract(Decimal.Subtract((Decimal)invoiceOpenAmt, payAmt), discountAmt), overUnderAmt);
                }
                SetDiscountAmt(discountAmt);
                SetWriteOffAmt(writeOffAmt);
            }
            else    //  calculate PayAmt
            {
                /* Allow reduction in discount, but not an increase. To give a discount that is higher
                   than the calculated discount, users have to enter a write off */
                if (enteredDiscountAmt.CompareTo(discountAmt) < 0)
                    discountAmt = enteredDiscountAmt;
                //PayAmt = invoiceOpenAmt.subtract(discountAmt).subtract(WriteOffAmt).subtract(OverUnderAmt);
                payAmt = Decimal.Subtract(Decimal.Subtract(Decimal.Subtract((Decimal)invoiceOpenAmt, discountAmt), writeOffAmt), overUnderAmt);
                SetPayAmt(payAmt);
                SetDiscountAmt(discountAmt);
            }
        }

        /**
         *	Get ISO Code of Currency
         *	@return Currency ISO
         */
        public String GetCurrencyISO()
        {
            return MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID());
        }

        /**
         * 	Get Document Status
         *	@return Document Status Clear Text
         */
        public String GetDocStatusName()
        {
            return MVAFCtrlRefList.GetListName(GetCtx(), 131, GetDocStatus());
        }

        /**
         *	Get Name of Credit Card
         *	@return Name
         */
        public String GetCreditCardName()
        {
            return GetCreditCardName(GetCreditCardType());
        }

        /**
         *	Get Name of Credit Card
         * 	@param CreditCardType credit card type
         *	@return Name
         */
        public String GetCreditCardName(String creditCardType)
        {
            if (creditCardType == null)
                return "--";
            else if (CREDITCARDTYPE_MasterCard.Equals(creditCardType))
                return "MasterCard";
            else if (CREDITCARDTYPE_Visa.Equals(creditCardType))
                return "Visa";
            else if (CREDITCARDTYPE_Amex.Equals(creditCardType))
                return "Amex";
            else if (CREDITCARDTYPE_ATM.Equals(creditCardType))
                return "ATM";
            else if (CREDITCARDTYPE_Diners.Equals(creditCardType))
                return "Diners";
            else if (CREDITCARDTYPE_Discover.Equals(creditCardType))
                return "Discover";
            else if (CREDITCARDTYPE_PurchaseCard.Equals(creditCardType))
                return "PurchaseCard";
            return "?" + creditCardType + "?";
        }

        /**
         * 	Add to Description
         *	@param description text
         */
        public void AddDescription(String description)
        {
            String desc = GetDescription();
            if (desc == null)
                SetDescription(description);
            else
                SetDescription(desc + " | " + description);
        }

        /**
         * 	Get Pay Amt
         * 	@param absolute if true the absolute amount (i.e. negative if payment)
         *	@return amount
         */
        public Decimal GetPayAmt(bool absolute)
        {
            if (IsReceipt())
                return base.GetPayAmt();
            return Decimal.Negate(base.GetPayAmt());
        }

        /**
         * 	Get Pay Amt in cents
         *	@return amount in cents
         */
        public int GetPayAmtInCents()
        {
            Decimal bd = Decimal.Multiply(base.GetPayAmt(), Env.ONEHUNDRED);
            return Decimal.ToInt32(bd);
        }

        /**
         * 	Process document
         *	@param processAction document action
         *	@return true if performed
         */
        public bool ProcessIt(String processAction)
        {
            _processMsg = null;
            DocumentEngine engine = new DocumentEngine(this, GetDocStatus());
            return engine.ProcessIt(processAction, GetDocAction());
        }

        /**
         * 	Unlock Document.
         * 	@return true if success 
         */
        public bool UnlockIt()
        {
            log.Info(ToString());
            SetProcessing(false);
            return true;
        }

        /**
         * 	Invalidate Document
         * 	@return true if success 
         */
        public bool InvalidateIt()
        {
            log.Info(ToString());
            SetDocAction(DOCACTION_Prepare);
            return true;
        }
        /**
         *	Prepare Document
         * 	@return new status (In Progress or Invalid) 
         */
        public String PrepareIt()
        {
            log.Info(ToString());
            _processMsg = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_BEFORE_PREPARE);
            if (_processMsg != null)
                return DocActionVariables.STATUS_INVALID;

            //	Std Period open?
            if (!MPeriod.IsOpen(GetCtx(), GetDateAcct(),
                IsReceipt() ? MVABMasterDocType.DOCBASETYPE_ARRECEIPT : MVABMasterDocType.DOCBASETYPE_APPAYMENT, GetVAF_Org_ID()))
            {
                _processMsg = "@PeriodClosed@";
                return DocActionVariables.STATUS_INVALID;
            }

            // is Non Business Day?
            // JID_1205: At the trx, need to check any non business day in that org. if not fund then check * org.
            if (MVABNonBusinessDay.IsNonBusinessDay(GetCtx(), GetDateAcct(), GetVAF_Org_ID()))
            {
                _processMsg = Common.Common.NONBUSINESSDAY;
                return DocActionVariables.STATUS_INVALID;
            }

            //	Unsuccessful Online Payment
            if (IsOnline() && !IsApproved())
            {
                if (GetR_Result() != null)
                    _processMsg = "@OnlinePaymentFailed@";
                else
                    _processMsg = "@PaymentNotProcessed@";
                return DocActionVariables.STATUS_INVALID;
            }

            //	Waiting Payment - Need to create Invoice & Shipment
            if (GetVAB_Order_ID() != 0 && GetVAB_Invoice_ID() == 0)
            {	//	see WebOrder.process
                MVABOrder order = new MVABOrder(GetCtx(), GetVAB_Order_ID(), Get_Trx());
                if (DOCSTATUS_WaitingPayment.Equals(order.GetDocStatus()))
                {
                    order.SetVAB_Payment_ID(GetVAB_Payment_ID());
                    order.SetDocAction(X_VAB_Order.DOCACTION_WaitComplete);
                    order.Set_TrxName(Get_Trx());
                    //	Boolean ok = 
                    if (!order.ProcessIt(X_VAB_Order.DOCACTION_WaitComplete))
                    {
                        // if order not completed - then payment also not completed (Prepay Order)
                        throw new Exception("Failed when processing document - " + order.GetProcessMsg());
                    }

                    //JID_0880 Show message on completion of Payment
                    _processMsg = order.GetProcessMsg();
                    GetCtx().SetContext("prepayOrder", _processMsg);

                    order.Save(Get_Trx());

                    /******************Commented By Lakhwinder
                     * //// Payment was Not Completed Against Prepay Order//////////*
                    //	Set Invoice
                    MVABInvoice[] invoices = order.GetInvoices(true);
                    int length = invoices.Length;
                    if (length > 0)		//	Get last invoice
                        SetVAB_Invoice_ID(invoices[length - 1].GetVAB_Invoice_ID());
                    //
                    if (GetVAB_Invoice_ID() == 0)
                    {
                        _processMsg = "@NotFound@ @VAB_Invoice_ID@";
                        return DocActionVariables.STATUS_INVALID;
                    }
                    */
                    ////////////////////////
                }
                //	WaitingPayment
            }

            //	Consistency of Invoice / Document Type and IsReceipt
            if (!VerifyDocType())
            {
                _processMsg = "@PaymentDocTypeInvoiceInconsistent@";
                return DocActionVariables.STATUS_INVALID;
            }

            //	Do not pay when Credit Stop/Hold
            if (!IsReceipt())
            {
                bool isRev = false;
                if (Get_ColumnIndex("IsReversal") > 0 && Util.GetValueOfBool(Get_Value("IsReversal")))
                    isRev = true;

                if (!isRev && GetVAB_BusinessPartner_ID() != 0)
                {
                    MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());

                    Decimal payAmt = Decimal.Add(Decimal.Add(GetPayAmt(false), GetDiscountAmt()), GetWriteOffAmt());
                    // If Amount is ZERO then no need to check currency conversion
                    if (!payAmt.Equals(Env.ZERO))
                    {
                        payAmt = MVABExchangeRate.ConvertBase(GetCtx(), payAmt,
                        GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                        if (payAmt == 0)
                        {
                            // JID_0084: On payment window if conversion not found system will give message Correct Message: Could not convert currency to base currency - Conversion type: XXXX
                            MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                            _processMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                                + MVABCurrency.GetISO_Code(GetCtx(), MVAFClient.Get(GetCtx()).GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();

                            return DocActionVariables.STATUS_INVALID;
                        }
                    }

                    payAmt = Decimal.Subtract(0, payAmt);

                    string retMsg = "";
                    bool crdAll = bp.IsCreditAllowed(GetVAB_BPart_Location_ID(), payAmt, out retMsg);
                    if (!crdAll)
                    {
                        if (bp.ValidateCreditValidation("D", GetVAB_BPart_Location_ID()))
                        {
                            _processMsg = retMsg;
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }
                }
            }

            /* set withholding tax amount
             * While calculating the withholding amount, the system has to check whether the withholding amount 
             * is zero or not equal to zero. If it's not equal to zero then the system will not calculate the withholding amount.
             * After confirmation with Surya  / Ashish By Amit */
            if (GetReversalDoc_ID() == 0 && GetVAB_BusinessPartner_ID() > 0 && Get_ColumnIndex("VAB_Withholding_ID") > 0)
            {
                if (!VerifyAndCalculateWithholding(false))
                {
                    return DocActionVariables.STATUS_INVALID;
                }
            }

            _justPrepared = true;
            if (!DOCACTION_Complete.Equals(GetDocAction()))
                SetDocAction(DOCACTION_Complete);
            return DocActionVariables.STATUS_INPROGRESS;
        }

        /**
         * 	Approve Document
         * 	@return true if success 
         */
        public bool ApproveIt()
        {
            log.Info(ToString());
            SetIsApproved(true);
            return true;
        }

        /**
         * 	Reject Approval
         * 	@return true if success 
         */
        public bool RejectIt()
        {
            log.Info(ToString());
            SetIsApproved(false);
            return true;
        }

        /**
         * 	Complete Document
         * 	@return new status (Complete, In Progress, Invalid, Waiting ..)
         */
        public String CompleteIt()
        {
            //	Re-Check
            if (!_justPrepared)
            {
                String status = PrepareIt();
                if (!DocActionVariables.STATUS_INPROGRESS.Equals(status))
                    return status;
            }
            // JID_1290: Set the document number from completed document sequence after completed (if needed)
            SetCompletedDocumentNo();

            // Amit for VA009 27-10-2015
            int countPaymentAllocateRecords = 0;
            if (Env.IsModuleInstalled("VA009_"))
            {
                if (GetVAB_sched_InvoicePayment_ID() <= 0)
                {
                    string sql = "SELECT COUNT(*) FROM VAB_PaymentAllotment WHERE IsActive = 'Y' AND  NVL(VAB_sched_InvoicePayment_ID , 0) = 0 AND  NVL(VAB_Invoice_ID , 0) <> 0  AND  VAB_Payment_ID = " + GetVAB_Payment_ID();
                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx())) > 0)
                    {
                        _processMsg = "Select Invoice Payment Schedule";
                        return DocActionVariables.STATUS_INVALID;
                    }
                    else
                    {
                        sql = "SELECT COUNT(*) FROM VAB_PaymentAllotment WHERE IsActive = 'Y' AND  NVL(VAB_Invoice_ID , 0) <> 0 AND VAB_Payment_ID = " + GetVAB_Payment_ID();
                        countPaymentAllocateRecords = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));
                        if (countPaymentAllocateRecords > 0)
                        {
                            if (String.IsNullOrEmpty(GetDescription()) || (GetDescription() != null && !GetDescription().Contains("{->")))
                            {
                                // when payment has invoice scedule ref which is paid - but payment not in completed stage (SI_0519)
                                // used for locking query - checking schedule is paid or not on completion
                                // when multiple user try to pay agaisnt same schedule from different scenarion at that tym lock record
                                lock (objLock)
                                {
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(*) FROM VAB_PaymentAllotment pa 
                                            INNER JOIN VAB_Invoice i ON i.VAB_Invoice_id = pa.VAB_Invoice_id INNER JOIN VAB_sched_InvoicePayment ips 
                                            ON (ips.VAB_Invoice_ID = i.VAB_Invoice_ID AND pa.VAB_sched_InvoicePayment_id = ips.VAB_sched_InvoicePayment_id) 
                                            WHERE pa.IsActive = 'Y' AND ips.IsActive = 'Y' AND ips.DueAmt > 0 AND NVL(pa.VAB_Invoice_ID , 0) <> 0 
                                            AND (nvl(ips.VAB_Payment_id,0) != 0 or nvl(ips.VAB_CashJRNLLine_id , 0) != 0 OR VA009_IsPaid = 'Y') 
                                            AND pa.VAB_Payment_ID = " + GetVAB_Payment_ID(), null, Get_Trx())) > 0)
                                    {
                                        String schedule = string.Empty;
                                        try
                                        {
                                            sql = @"SELECT LTRIM(SYS_CONNECT_BY_PATH( PaidSchedule, ' , '),',') PaidSchedule FROM
                                              (SELECT PaidSchedule, ROW_NUMBER () OVER (ORDER BY PaidSchedule ) RN, COUNT (*) OVER () CNT FROM 
                                                (SELECT duedate || '_' || dueamt AS PaidSchedule FROM VAB_PaymentAllotment pa
                                                INNER JOIN VAB_Invoice i ON i.VAB_Invoice_id = pa.VAB_Invoice_id
                                                INNER JOIN VAB_sched_InvoicePayment ips ON (ips.VAB_Invoice_ID = i.VAB_Invoice_ID AND pa.VAB_sched_InvoicePayment_id = ips.VAB_sched_InvoicePayment_id) 
                                                 WHERE pa.IsActive = 'Y' AND ips.IsActive = 'Y' AND NVL(pa.VAB_Invoice_ID , 0)  <> 0 AND (NVL(ips.VAB_Payment_id,0)  != 0
                                                 OR NVL(ips.VAB_CashJRNLLine_id , 0) != 0 OR ips.VA009_IsPaid = 'Y') AND pa.VAB_Payment_ID = " + GetVAB_Payment_ID() +
                                                     @" AND ROWNUM <= 100 )  )
                                                 WHERE RN = CNT START WITH RN = 1 CONNECT BY RN = PRIOR RN + 1";
                                            schedule = Util.GetValueOfString(DB.ExecuteScalar(sql, null, Get_Trx()));
                                        }
                                        catch (Exception ex)
                                        {
                                            log.Log(Level.SEVERE, sql, ex);
                                        }

                                        _processMsg = Msg.GetMsg(GetCtx(), "VIS_PayAlreadyDoneforInvoiceSchedule") + schedule;
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (String.IsNullOrEmpty(GetDescription()) || (GetDescription() != null && !GetDescription().Contains("{->")))
                {
                    // when payment has invoice scedule ref which is paid - but payment not in completed stage (SI_0519)
                    // used for locking query - checking schedule is paid or not on completion
                    // when multiple user try to pay agaisnt same schedule from different scenarion at that tym lock record
                    lock (objLock)
                    {
                        if (Util.GetValueOfInt(DB.ExecuteScalar(@"Select Count(*) from VAB_sched_InvoicePayment Where VAB_Invoice_ID = " + GetVAB_Invoice_ID() +
                            @" AND VAB_sched_InvoicePayment_ID =" + GetVAB_sched_InvoicePayment_ID() +
                            @" AND DueAmt > 0  AND (nvl(VAB_Payment_id,0) != 0 or nvl(VAB_CashJRNLLine_id , 0) != 0 OR VA009_IsPaid = 'Y' )", null, Get_Trx())) > 0)
                        {
                            _processMsg = Msg.GetMsg(GetCtx(), "VIS_PayAlreadyDoneforInvoiceSchedule");
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }
                }
                if (GetVAB_Order_ID() != 0)
                {
                    if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) from VA009_OrderPaySchedule Where VAB_Order_ID=" + GetVAB_Order_ID() + "AND VA009_OrderPaySchedule_ID=" + GetVA009_OrderPaySchedule_ID() + " AND VA009_IsPaid='Y'")) > 0)
                    {
                        _processMsg = "Payment is already done for selected order Schedule";
                        return DocActionVariables.STATUS_INVALID;
                    }
                }
            }
            //end


            //	Implicit Approval
            if (!IsApproved())
                ApproveIt();
            log.Info(ToString());

            //	Charge Handling
            if (GetVAB_Charge_ID() != 0)
            {
                if (!IsPrepayment())
                    SetIsAllocated(true);
            }
            else
            {
                AllocateIt();	//	Create Allocation Records
                if (_AllocMsg != "")
                {
                    _processMsg = _AllocMsg;
                    return DocActionVariables.STATUS_INVALID;
                }
                TestAllocation();
            }

            //	Project update
            if (GetVAB_Project_ID() != 0)
            {
                //	MProject project = new MProject(GetCtx(), GetVAB_Project_ID());
            }

            // Code Commented for updating open amount when payment is independent against  only business partner by Vivek on 24/11/2017
            //	Update BP for Prepayments
            //if (GetVAB_BusinessPartner_ID() != 0 && GetVAB_Invoice_ID() == 0)
            //{
            //    MBPartner bp1 = new MBPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());
            //    //Credit Limit by vivek on 30/9/2016
            //    if (bp1.GetCreditStatusSettingOn() == "CH")
            //    {
            //        bp1.SetTotalOpenBalance();
            //        bp1.Save();
            //    }
            //}

            //	Counter Doc
            MVABPayment counter = CreateCounterDoc();
            if (counter != null)
                _processMsg += " @CounterDoc@: @VAB_Payment_ID@=" + counter.GetDocumentNo();

            //	User Validation
            String valid = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_AFTER_COMPLETE);
            if (valid != null)
            {
                _processMsg = valid;
                return DocActionVariables.STATUS_INVALID;
            }
            // When Tender Type Is Check. Account Date Must Be Same As Check Date  // Commented By Anuj 21-11-2015
            //if (GetTenderType() == "K")
            //{
            //    if (GetDateAcct() != GetCheckDate())
            //    {
            //        _processMsg = "Account Date and Check Date must be same";
            //        return DocActionVariables.STATUS_INVALID;
            //    }
            //}

            // change by Amit 27-5-2016 // Letter Of Credit module
            if (Env.IsModuleInstalled("VA026_"))
            {
                MVABDocTypes docType = new MVABDocTypes(GetCtx(), GetVAB_DocTypes_ID(), Get_Trx());

                if (Get_ValueAsInt("VA026_TRLoanApplication_ID") <= 0)
                {
                    if (!UpdateRealizedAmount(GetVAB_Order_ID(), GetVAB_Invoice_ID()))
                    {
                        _processMsg = Msg.GetMsg(GetCtx(), "VA026_RealizedAmountNotSet");
                        return DocActionVariables.STATUS_INVALID;
                    }
                }
                if (docType.GetDocBaseType() == "APP")
                {
                    if (Get_ValueAsInt("VA026_TRLoanApplication_ID") > 0)
                    {
                        string sql = @"SELECT vaf_tableview_ID  FROM vaf_tableview WHERE tablename LIKE 'VA026_TRLoanApplication' AND IsActive = 'Y'";
                        int tableId = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                        MVAFTableView tbl = new MVAFTableView(GetCtx(), tableId, null);
                        PO po = tbl.GetPO(GetCtx(), Get_ValueAsInt("VA026_TRLoanApplication_ID"), Get_Trx());
                        decimal conversionAmt = 0;
                        conversionAmt = Util.GetValueOfDecimal(Get_Value("PayAmt"));
                        sql = "SELECT " +
                              " CASE     WHEN " + Util.GetValueOfInt(po.Get_Value("VAB_Currency_ID")) + " != " + Util.GetValueOfInt(GetVAB_Currency_ID()) +
                              " THEN CURRENCYCONVERT(" + conversionAmt + ", " + Util.GetValueOfInt(GetVAB_Currency_ID()) +
                              " , " + Util.GetValueOfInt(po.Get_Value("VAB_Currency_ID")) + " , " + GlobalVariable.TO_DATE(GetDateAcct(), true) + " , "
                              + Util.GetValueOfInt(GetVAB_CurrencyType_ID()) + " , "
                              + Util.GetValueOfInt(GetVAF_Client_ID()) + " , "
                              + Util.GetValueOfInt(GetVAF_Org_ID()) + ")    ELSE "
                              + Util.GetValueOfDecimal(conversionAmt) + "   END AS ConvertedAmt";
                        //handle for PostGreSQL
                        if (!DB.IsPostgreSQL())
                        {
                            sql += " FROM DUAL";
                        }
                        conversionAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(sql, null, Get_Trx()));
                        po.Set_Value("VA026_LoanPaidAmount", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_LoanPaidAmount")), conversionAmt));
                        if (!po.Save(Get_Trx()))
                        {
                            Get_Trx().Rollback();
                            ValueNamePair pp = VLogger.RetrieveError();
                            log.Info("Error Occured when try to update record on TR Loan Application. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                            return DocActionVariables.STATUS_INVALID;
                        }
                    }
                }
            }

            SetProcessed(true);
            SetDocAction(DOCACTION_Close);

            // nnayak - update BP open balance and credit used
            //Added by Vivek for Credit Limit on 25/08/2016
            if (GetVAB_BusinessPartner_ID() != 0)
            {
                MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());

                // JID_0556 :: // Change by Lokesh Chauhan to validate watch % from BP Group, 
                // if it is 0 on BP Group then default to 90 // 12 July 2019
                MVABBPartCategory bpg = new MVABBPartCategory(GetCtx(), bp.GetVAB_BPart_Category_ID(), Get_Trx());
                Decimal? watchPer = bpg.GetCreditWatchPercent();
                if (watchPer == 0)
                    watchPer = 90;

                Decimal? newCreditAmt = 0;

                Decimal payAmt = Decimal.Add(Decimal.Add(GetPayAmt(false), GetDiscountAmt()), GetWriteOffAmt());
                // If Amount is ZERO then no need to check currency conversion
                if (!payAmt.Equals(Env.ZERO))
                {
                    payAmt = MVABExchangeRate.ConvertBase(GetCtx(), payAmt,
                    GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                    if (payAmt == 0)
                    {
                        // JID_0084: On payment window if conversin not found system will give message Correct Message: Could not convert currency to base currency - Conversion type: XXXX
                        MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                        _processMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                            + MVABCurrency.GetISO_Code(GetCtx(), MVAFClient.Get(GetCtx()).GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();
                        return DocActionVariables.STATUS_INVALID;
                    }
                }

                if (bp.GetCreditStatusSettingOn() == "CH")
                {
                    //	Total Balance
                    Decimal? newBalance = bp.GetTotalOpenBalance(false);
                    if (newBalance == null)
                        newBalance = Env.ZERO;

                    newBalance = Decimal.Subtract((Decimal)newBalance, (Decimal)payAmt);

                    if (IsReceipt())
                    {
                        newCreditAmt = bp.GetSO_CreditUsed();
                        if (newCreditAmt == null)
                            newCreditAmt = Decimal.Negate((Decimal)payAmt);
                        else
                            newCreditAmt = Decimal.Subtract((Decimal)newCreditAmt, (Decimal)payAmt);
                        //
                        log.Fine("TotalOpenBalance=" + bp.GetTotalOpenBalance(false) + "(" + payAmt
                            + ", Credit=" + bp.GetSO_CreditUsed() + "->" + newCreditAmt
                            + ", Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);
                        bp.SetSO_CreditUsed((Decimal)newCreditAmt);
                    }	//	SO
                    else
                    {
                        log.Fine("Payment Amount =" + GetPayAmt(false) + "(" + payAmt
                            + ") Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);
                    }
                    bp.SetTotalOpenBalance(Convert.ToDecimal(newBalance));
                    // commented this as we will use existing function on MBPartner to calculate logic for setting credit status
                    //if (bp.GetSO_CreditLimit() > 0)
                    //{
                    //    if (((newCreditAmt / bp.GetSO_CreditLimit()) * 100) <= watchPer)
                    //    {
                    //        bp.SetSOCreditStatus("O");
                    //    }
                    //}
                    bp.SetSOCreditStatus();
                    if (!bp.Save(Get_Trx()))
                    {
                        _processMsg = "Could not update Business Partner";
                        return DocActionVariables.STATUS_INVALID;
                    }
                }
                // JID_0161 // change here now will check credit settings on field only on Business Partner Header // Lokesh Chauhan 15 July 2019
                else if (bp.GetCreditStatusSettingOn() == X_VAB_BusinessPartner.CREDITSTATUSSETTINGON_CustomerLocation)
                {
                    MVABBPartLocation loc;
                    //Based on Location need to Update the Open Balance--> JID_1148
                    //when PaymentAllocate tab have records at that time this condition will execute 
                    //becoz Invoice may have diff. Location apart from Payment(Parent Tab) BP Location.
                    if (countPaymentAllocateRecords > 0)
                    {
                        DataSet ds = DB.ExecuteDataset(@"SELECT VAB_PaymentAllotment.VAB_Invoice_ID, inv.VAB_BusinessPartner_ID, inv.VAB_BPart_Location_ID, 
                            VAB_PaymentAllotment.overunderamt , VAB_PaymentAllotment.writeoffamt , VAB_PaymentAllotment.discountamt,
                            VAB_PaymentAllotment.amount 
                            FROM VAB_PaymentAllotment INNER JOIN VAB_Payment ON VAB_Payment.VAB_Payment_ID =VAB_PaymentAllotment.VAB_Payment_ID 
                            INNER JOIN VAB_Invoice inv ON inv.VAB_Invoice_ID=VAB_PaymentAllotment.VAB_Invoice_ID
                            WHERE VAB_PaymentAllotment.IsActive = 'Y' AND  VAB_PaymentAllotment.VAB_Payment_ID =" + GetVAB_Payment_ID(), null, Get_Trx());
                        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                            {
                                loc = new MVABBPartLocation(GetCtx(), Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAB_BPart_Location_ID"]), Get_Trx());

                                payAmt = Decimal.Add(Decimal.Add(Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["amount"]),
                                    Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["discountamt"])), Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["writeoffamt"]));
                                // If Amount is ZERO then no need to check currency conversion
                                if (!payAmt.Equals(Env.ZERO))
                                {
                                    payAmt = MVABExchangeRate.ConvertBase(GetCtx(), payAmt,
                                    GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                                    if (payAmt == 0)
                                    {
                                        // JID_0084: On payment window if conversin not found system will give message Correct Message: Could not convert currency to base currency - Conversion type: XXXX
                                        MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                                        _processMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                                            + MVABCurrency.GetISO_Code(GetCtx(), MVAFClient.Get(GetCtx()).GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                }
                                //	Total Balance
                                Decimal? newBalance = loc.GetTotalOpenBalance();
                                if (newBalance == null)
                                    newBalance = Env.ZERO;
                                newBalance = Decimal.Subtract((Decimal)newBalance, (Decimal)payAmt);
                                if (IsReceipt())
                                {
                                    newCreditAmt = loc.GetSO_CreditUsed();
                                    if (newCreditAmt == null)
                                        newCreditAmt = Decimal.Negate((Decimal)payAmt);
                                    else
                                        newCreditAmt = Decimal.Subtract((Decimal)newCreditAmt, (Decimal)payAmt);
                                    //
                                    log.Fine("TotalOpenBalance=" + loc.GetTotalOpenBalance() + "(" + payAmt
                                        + ", Credit=" + loc.GetSO_CreditUsed() + "->" + newCreditAmt
                                        + ", Balance=" + loc.GetTotalOpenBalance() + " -> " + newBalance);
                                    loc.SetSO_CreditUsed((Decimal)newCreditAmt);

                                    Decimal? bpSOcreditUsed = Decimal.Negate((Decimal)payAmt) + bp.GetSO_CreditUsed();
                                    bp.SetSO_CreditUsed(bpSOcreditUsed);
                                }   //	SO
                                else
                                {
                                    log.Fine("Payment Amount =" + GetPayAmt(false) + "(" + payAmt
                                        + ") Balance=" + loc.GetTotalOpenBalance() + " -> " + newBalance);
                                }
                                loc.SetTotalOpenBalance(Convert.ToDecimal(newBalance));
                                Decimal? bptotalopenbal = Decimal.Negate((Decimal)payAmt) + bp.GetTotalOpenBalance();
                                bp.SetTotalOpenBalance(bptotalopenbal);
                                loc.SetSOCreditStatus();
                                if (bp.Save())
                                {
                                    if (!loc.Save(Get_Trx()))
                                    {
                                        _processMsg = "Could not update Business Partner Location";
                                        return DocActionVariables.STATUS_INVALID;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        loc = new MVABBPartLocation(GetCtx(), GetVAB_BPart_Location_ID(), Get_Trx());

                        //	Total Balance
                        Decimal? newBalance = loc.GetTotalOpenBalance();
                        if (newBalance == null)
                            newBalance = Env.ZERO;
                        newBalance = Decimal.Subtract((Decimal)newBalance, (Decimal)payAmt);
                        if (IsReceipt())
                        {
                            newCreditAmt = loc.GetSO_CreditUsed();
                            if (newCreditAmt == null)
                                newCreditAmt = Decimal.Negate((Decimal)payAmt);
                            else
                                newCreditAmt = Decimal.Subtract((Decimal)newCreditAmt, (Decimal)payAmt);
                            //
                            log.Fine("TotalOpenBalance=" + loc.GetTotalOpenBalance() + "(" + payAmt
                                + ", Credit=" + loc.GetSO_CreditUsed() + "->" + newCreditAmt
                                + ", Balance=" + loc.GetTotalOpenBalance() + " -> " + newBalance);
                            loc.SetSO_CreditUsed((Decimal)newCreditAmt);

                            Decimal? bpSOcreditUsed = Decimal.Negate((Decimal)payAmt) + bp.GetSO_CreditUsed();
                            bp.SetSO_CreditUsed(bpSOcreditUsed);
                        }   //	SO
                        else
                        {
                            log.Fine("Payment Amount =" + GetPayAmt(false) + "(" + payAmt
                                + ") Balance=" + loc.GetTotalOpenBalance() + " -> " + newBalance);
                        }
                        loc.SetTotalOpenBalance(Convert.ToDecimal(newBalance));
                        Decimal? bptotalopenbal = Decimal.Negate((Decimal)payAmt) + bp.GetTotalOpenBalance();
                        bp.SetTotalOpenBalance(bptotalopenbal);
                        // commented this as we will use existing function on MBPartner to calculate logic for setting credit status
                        //if (bp.GetSO_CreditLimit() > 0)
                        //{
                        //    if (((bpSOcreditUsed / bp.GetSO_CreditLimit()) * 100) <= watchPer)
                        //    {
                        //        bp.SetSOCreditStatus("O");
                        //    }
                        //}
                        //if (loc.GetSO_CreditLimit() > 0)
                        //{
                        //    if (((newCreditAmt / loc.GetSO_CreditLimit()) * 100) <= watchPer)
                        //    {
                        //        loc.SetSOCreditStatus("O");
                        //    }
                        //}
                        loc.SetSOCreditStatus();
                        if (bp.Save())
                        {
                            if (!loc.Save(Get_Trx()))
                            {
                                _processMsg = "Could not update Business Partner Location";
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                    }
                }
            }
            //Credit Limit
            if (GetVAB_sched_InvoicePayment_ID() != 0)
            {
                // commeted - bcz of rework -- already handled in View Allocation Completion
                //MVABInvoicePaySchedule paySch = new MVABInvoicePaySchedule(GetCtx(), GetVAB_sched_InvoicePayment_ID(), Get_Trx());
                //paySch.SetVAB_Payment_ID(GetVAB_Payment_ID());
                if (Env.IsModuleInstalled("VA009_"))
                {
                    //MVABInvoice invoice = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), null);
                    //MVABDocTypes doctype = new MVABDocTypes(GetCtx(), invoice.GetVAB_DocTypes_ID(), null);
                    //paySch.SetVA009_ExecutionStatus(GetVA009_ExecutionStatus());
                    //if (doctype.GetDocBaseType() == "ARC" || doctype.GetDocBaseType() == "APC")
                    //{
                    //    if (((-1 * (Get_ColumnIndex("PaymentAmount") > 0 ? GetPaymentAmount() : GetPayAmt())) + (-1 * GetDiscountAmt()) + (-1 * GetWriteOffAmt())) < paySch.GetDueAmt())
                    //    {
                    //        paySch.SetVA009_IsPaid(false);
                    //    }
                    //    else
                    //    {
                    //        paySch.SetVA009_IsPaid(true);
                    //    }
                    //}
                    //else
                    //{
                    //    if (((Get_ColumnIndex("PaymentAmount") > 0 ? GetPaymentAmount() : GetPayAmt()) + GetDiscountAmt() + GetWriteOffAmt()) < paySch.GetDueAmt())
                    //    {
                    //        paySch.SetVA009_IsPaid(false);
                    //    }
                    //    else
                    //    {
                    //        paySch.SetVA009_IsPaid(true);
                    //    }
                    //}
                    //paySch.Save(Get_Trx());

                    if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAB_sched_InvoicePayment WHERE va009_ispaid = 'N' AND VAB_Invoice_ID = " + Util.GetValueOfInt(GetVAB_Invoice_ID()), null, Get_Trx())) == 0)
                    {
                        DB.ExecuteQuery("UPDATE VAB_Invoice SET IsPaid = 'Y' WHERE VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx());
                    }
                    else
                    {
                        DB.ExecuteQuery("UPDATE VAB_Invoice SET IsPaid = 'N' WHERE VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx());
                    }
                }
                //paySch.Save(Get_Trx());

            }
            else if (Env.IsModuleInstalled("VA009_") && GetVA009_OrderPaySchedule_ID() != 0)
            {

                if (GetVA009_OrderPaySchedule_ID() != 0 && GetDescription() != null && GetDescription().Contains("{->"))
                {
                    // also need to set Execution Status As Awaited
                    DB.ExecuteQuery(@"Update VA009_OrderPaySchedule set VA009_ISPAID='N', VAB_Payment_ID=0, 
                                       VA009_PaidAmntInvce = 0 , VA009_PaidAmnt= 0 , VA009_Variance = 0  , VA009_ExecutionStatus = 'A'  
                                       where VA009_OrderPaySchedule_ID=" + GetVA009_OrderPaySchedule_ID(), null, Get_Trx());
                }
                else
                {
                    if (GetVA009_OrderPaySchedule_ID() != 0)
                    {
                        Decimal basePaidAmt = GetPayAmt() + GetDiscountAmt() + GetWriteOffAmt() +
                            (Get_ColumnIndex("WithholdingAmt") >= 0 ? (GetWithholdingAmt() + GetBackupWithholdingAmount()) : 0);
                        Decimal orderPaidAmt = GetPayAmt() + GetDiscountAmt() + GetWriteOffAmt() +
                            (Get_ColumnIndex("WithholdingAmt") >= 0 ? (GetWithholdingAmt() + GetBackupWithholdingAmount()) : 0);
                        MVABOrder order = new MVABOrder(GetCtx(), GetVAB_Order_ID(), Get_Trx());
                        MVAFClientDetail client = MVAFClientDetail.Get(GetCtx(), GetVAF_Client_ID());
                        MVABAccountBook asch = MVABAccountBook.Get(GetCtx(), client.GetVAB_AccountBook1_ID());

                        if (order.GetVAB_Currency_ID() != GetVAB_Currency_ID())
                        {
                            orderPaidAmt = MVABExchangeRate.Convert(GetCtx(), orderPaidAmt, GetVAB_Currency_ID(), order.GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                        }
                        if (asch.GetVAB_Currency_ID() != GetVAB_Currency_ID())
                        {
                            basePaidAmt = MVABExchangeRate.Convert(GetCtx(), basePaidAmt, GetVAB_Currency_ID(), asch.GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                        }
                        // update variance amount as ( Due amount - paid amount )
                        DB.ExecuteQuery("Update VA009_OrderPaySchedule set VA009_ISPAID='Y',VAB_Payment_ID=" + GetVAB_Payment_ID() +
                                        @" , DueAmt = " + (GetOverUnderAmt() != 0 ? orderPaidAmt.ToString() : " DueAmt ") +
                                        @" , VA009_OpnAmntInvce = " + (GetOverUnderAmt() != 0 ? orderPaidAmt.ToString() : " VA009_OpnAmntInvce ") +
                                        @" , VA009_OpenAmnt = " + (GetOverUnderAmt() != 0 ? basePaidAmt.ToString() : " VA009_OpenAmnt ") +
                                        @" , VA009_PaidAmntInvce = " + orderPaidAmt +
                                        @" , VA009_PaidAmnt = " + basePaidAmt +
                                        @" , VA009_Variance = -( " + (GetOverUnderAmt() != 0 ? orderPaidAmt.ToString() : " DueAmt ") + " - " + orderPaidAmt + " ) " +
                                        @" , VA009_ExecutionStatus = 'I' " +
                                        @" , VA009_PaymentMethod_ID =" + GetVA009_PaymentMethod_ID() + //by shubham (JID_0519_2) to update payment method in order schedule
                                        @" WHERE VA009_OrderPaySchedule_ID=" + GetVA009_OrderPaySchedule_ID(), null, Get_Trx());

                        // when underr amount contain value then we need to split order schedule
                        if (GetOverUnderAmt() != 0)
                        {
                            if (!SpiltOrderSchedule(GetCtx(), this, GetVA009_OrderPaySchedule_ID(), asch.GetVAB_Currency_ID(), GetOverUnderAmt(), Get_Trx()))
                            {
                                return DocActionVariables.STATUS_INVALID;
                            }
                        }
                    }
                }
            }
            else if (countPaymentAllocateRecords > 0)
            {
                DataSet ds = DB.ExecuteDataset(@"SELECT VAB_PaymentAllotment.VAB_sched_InvoicePayment_ID , VAB_PaymentAllotment.VAB_Invoice_ID ,
                            VAB_PaymentAllotment.overunderamt , VAB_PaymentAllotment.writeoffamt , VAB_PaymentAllotment.discountamt ,
                            VAB_PaymentAllotment.amount  
                            FROM VAB_PaymentAllotment INNER JOIN VAB_Payment ON VAB_Payment.VAB_Payment_ID =VAB_PaymentAllotment.VAB_Payment_ID 
                            WHERE VAB_PaymentAllotment.IsActive = 'Y' AND  VAB_PaymentAllotment.VAB_Payment_ID =" + GetVAB_Payment_ID(), null, Get_Trx());
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        // commeted - bcz of rework -- already handled in View Allocation Completion
                        //MVABInvoicePaySchedule paySch = new MVABInvoicePaySchedule(GetCtx(), Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAB_sched_InvoicePayment_ID"]), Get_Trx());
                        //paySch.SetVAB_Payment_ID(GetVAB_Payment_ID());
                        //MVABInvoice invoice = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), null);
                        //MVABDocTypes doctype = new MVABDocTypes(GetCtx(), invoice.GetVAB_DocTypes_ID(), null);
                        //if (doctype.GetDocBaseType() == "ARC" || doctype.GetDocBaseType() == "APC")
                        //{
                        //    if (((-1 * Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["writeoffamt"])) + (-1 * Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["discountamt"])) +
                        //        (-1 * Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["amount"]))) < Util.GetValueOfDecimal(paySch.GetDueAmt()))
                        //    {
                        //        paySch.SetVA009_IsPaid(false);
                        //    }
                        //    else
                        //    {
                        //        paySch.SetVA009_IsPaid(true);
                        //    }
                        //}
                        //else
                        //{
                        //    if ((Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["writeoffamt"]) + Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["discountamt"]) +
                        //        Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["amount"])) < Util.GetValueOfDecimal(paySch.GetDueAmt()))
                        //    {
                        //        paySch.SetVA009_IsPaid(false);
                        //    }
                        //    else
                        //    {
                        //        paySch.SetVA009_IsPaid(true);
                        //    }
                        //}
                        //paySch.SetVA009_ExecutionStatus(GetVA009_ExecutionStatus());
                        //paySch.Save(Get_Trx());
                        if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAB_sched_InvoicePayment WHERE va009_ispaid = 'N' AND VAB_Invoice_ID = " + Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAB_Invoice_ID"]), null, Get_Trx())) == 0)
                        {
                            DB.ExecuteQuery("UPDATE VAB_Invoice SET IsPaid = 'Y' WHERE VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx());
                        }
                        else
                        {
                            DB.ExecuteQuery("UPDATE VAB_Invoice SET IsPaid = 'N' WHERE VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx());
                        }
                    }
                }
            }
            else
            {
                int[] InvoicePaySchedule_ID = MVABInvoicePaySchedule.GetAllIDs("VAB_sched_InvoicePayment", "VAB_Invoice_ID = " + GetVAB_Invoice_ID() + @" AND VAB_sched_InvoicePayment_ID NOT IN 
                    (SELECT NVL(VAB_sched_InvoicePayment_ID,0) FROM VAB_sched_InvoicePayment WHERE VAB_Payment_ID IN (SELECT NVL(VAB_Payment_ID,0) FROM VAB_sched_InvoicePayment) UNION 
                    SELECT NVL(VAB_sched_InvoicePayment_ID,0) FROM VAB_sched_InvoicePayment  WHERE VAB_CashJRNLLine_ID IN (SELECT NVL(VAB_CashJRNLLine_ID,0) FROM VAB_sched_InvoicePayment))", Get_Trx());
                foreach (int invocePay in InvoicePaySchedule_ID)
                {
                    MVABInvoicePaySchedule paySch = new MVABInvoicePaySchedule(GetCtx(), invocePay, Get_Trx());
                    paySch.SetVAB_Payment_ID(GetVAB_Payment_ID());
                    paySch.Save();
                }
            }
            //string AllocationMsg = "";
            //if (!GenerateCostAllocation(GetDocumentNo(), GetVAF_Client_ID(), Get_Trx(), GetVAF_Org_ID(), out AllocationMsg))
            //{
            //    _processMsg = AllocationMsg;
            //    return DocActionVariables.STATUS_INVALID;
            //}

            ////Arpit 18 Dec,2017 Asked BY Ashish Sir
            ////if (!UpdateUnMatchedBalanceForAccount(false))
            // Auto check work-Mohit-7 March 2020

            MVABBankAcct bnkAct = MVABBankAcct.Get(GetCtx(), GetVAB_Bank_Acct_ID());
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());



            if (GetTenderType().Equals(X_VAB_Payment.TENDERTYPE_Check) && bnkAct.IsChkNoAutoControl() && !IsReversal())
            {
                if ((dt.GetDocBaseType().Equals(MVABMasterDocType.DOCBASETYPE_APPAYMENT) && GetPayAmt() >= 0) || (dt.GetDocBaseType().Equals(MVABMasterDocType.DOCBASETYPE_ARRECEIPT) && GetPayAmt() < 0))
                {
                    // get Check number
                    string checkNo = GetChecknumber(GetVA009_PaymentMethod_ID(), GetVAB_Bank_Acct_ID(), Get_Trx());
                    if (!string.IsNullOrEmpty(checkNo))
                    {
                        DB.ExecuteQuery("UPDATE VAB_Payment SET CheckNo='" + checkNo + "' WHERE VAB_Payment_ID=" + GetVAB_Payment_ID(), null, Get_Trx());
                    }
                    else
                    {
                        _processMsg = Msg.GetMsg(GetCtx(), "NoCheckNum");
                        log.Info("" + _processMsg + ": Payment Document No " + GetDocumentNo());
                        return DocActionVariables.STATUS_INVALID;
                    }

                }
            }

            if (!UpdateUnMatchedBalanceForAccount())
            {
                return DocActionVariables.STATUS_INVALID;
            }

            //JID_0880 Show message on completion of Payment
            if (GetCtx().GetContext("prepayOrder") != null)
            {
                _processMsg += "," + GetCtx().GetContext("prepayOrder");
                GetCtx().SetContext("prepayOrder", "");
            }


            return DocActionVariables.STATUS_COMPLETED;
        }

        /// <summary>
        /// Set the document number from Completed Document Sequence after completed
        /// </summary>
        private void SetCompletedDocumentNo()
        {
            // if Reversal document then no need to get Document no from Completed sequence
            if (Get_ColumnIndex("IsReversal") > 0 && IsReversal())
            {
                return;
            }

            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());

            // if Overwrite Date on Complete checkbox is true.
            if (dt.IsOverwriteDateOnComplete())
            {
                SetDateTrx(DateTime.Now.Date);
                if (GetDateAcct().Value.Date < GetDateTrx().Value.Date)
                {
                    SetDateAcct(GetDateTrx());

                    //	Std Period open?
                    if (!MPeriod.IsOpen(GetCtx(), GetDateAcct(), dt.GetDocBaseType(), GetVAF_Org_ID()))
                    {
                        throw new Exception("@PeriodClosed@");
                    }
                }
            }

            // if Overwrite Sequence on Complete checkbox is true.
            if (dt.IsOverwriteSeqOnComplete())
            {
                // Set Drafted Document No into Temp Document No.
                if (Get_ColumnIndex("TempDocumentNo") > 0)
                {
                    SetTempDocumentNo(GetDocumentNo());
                }

                // Get current next from Completed document sequence defined on Document type
                String value = MVAFRecordSeq.GetDocumentNo(GetVAB_DocTypes_ID(), Get_TrxName(), GetCtx(), true, this);
                if (value != null)
                {
                    SetDocumentNo(value);
                }
            }
        }

        /// <summary>
        /// This function is used to verify and calculate withholdng
        /// <param name="IsPaymentAllocate">withholding calculate from payment allocate or not</param>
        /// </summary>
        /// <returns>false,when not calculated</returns>
        public bool VerifyAndCalculateWithholding(bool IsPaymentAllocate)
        {
            string sql = @"SELECT VAB_BusinessPartner.IsApplicableonARReceipt, VAB_BusinessPartner.IsApplicableonAPPayment, VAB_BusinessPartner.VAB_Withholding_ID,
                            VAB_BusinessPartner.AP_WithholdingTax_ID,  VAB_Address.VAB_Country_ID , VAB_Address.VAB_RegionState_ID   "
                             + @" FROM VAB_BusinessPartner INNER JOIN VAB_BPart_Location ON 
                                 VAB_BusinessPartner.VAB_BusinessPartner_ID = VAB_BPart_Location.VAB_BusinessPartner_ID
                                 INNER JOIN VAB_Address ON VAB_BPart_Location.VAB_Address_ID = VAB_Address.VAB_Address_ID  WHERE
                                 VAB_BusinessPartner.VAB_BusinessPartner_ID = " + GetVAB_BusinessPartner_ID() + @" AND VAB_BPart_Location.VAB_BPart_Location_ID = " + GetVAB_BPart_Location_ID();
            DataSet ds = DB.ExecuteDataset(sql, null, Get_Trx());
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                // Applicable, but withholding not selected on Business partner record
                if (((IsReceipt() && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonARReceipt"]).Equals("Y") &&
                                   Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Withholding_ID"]) == 0) ||
                    (!IsReceipt() && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonAPPayment"]).Equals("Y") &&
                                   Util.GetValueOfInt(ds.Tables[0].Rows[0]["AP_WithholdingTax_ID"]) == 0)) && GetVAB_Withholding_ID() == 0)
                {
                    SetProcessMsg(Msg.GetMsg(GetCtx(), "WithHoldingNotDefine"));
                    return false;
                }
                else if ((IsReceipt() && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonARReceipt"]).Equals("Y")) ||
                    (!IsReceipt() && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonAPPayment"]).Equals("Y")))
                {
                    // calculate withholding amount
                    //if (GetWithholdingAmt() == 0)
                    //{
                    if (!CalculateWithholdingAmount(IsReceipt() ? Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Withholding_ID"])
                        : Util.GetValueOfInt(ds.Tables[0].Rows[0]["AP_WithholdingTax_ID"]), false,
                        Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_RegionState_ID"]),
                        Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Country_ID"]), IsPaymentAllocate))
                    {
                        SetProcessMsg(Msg.GetMsg(GetCtx(), "WrongWithholdingTax"));
                        return false;
                    }
                    //}

                    // calculate backup withholding amount
                    if (GetBackupWithholding_ID() > 0)//&& GetBackupWithholdingAmount() == 0
                    {
                        if (!CalculateWithholdingAmount(GetBackupWithholding_ID(), true,
                            Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_RegionState_ID"]),
                            Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Country_ID"]), IsPaymentAllocate))
                        {
                            SetProcessMsg(Msg.GetMsg(GetCtx(), "WrongWithholdingTax"));
                            return false;
                        }
                    }
                    else
                    {
                        SetBackupWithholdingAmount(0);
                    }
                }
                else
                {
                    // when not applicable then set reference and amount as ZERO
                    SetBackupWithholding_ID(0);
                    SetVAB_Withholding_ID(0);
                    SetWithholdingAmt(0);
                    SetBackupWithholdingAmount(0);
                }
            }
            return true;
        }

        /// <summary>
        /// Set Withholding Tax Amount / Backup withholding Tax Amount
        /// </summary>
        /// <param name="withholding_ID">withholding reference</param>
        /// <param name="isBackupWithholding">is calculation going on for backup withholding</param>
        /// <param name="Region_ID">busines partnet region</param>
        /// <param name="Country_ID">business partner country</param>
        /// <param name="IsPaymentAllocate">withholding calculate from Payment Allocate or not</param>
        /// <returns></returns>
        private bool CalculateWithholdingAmount(int withholding_ID, bool isBackupWithholding, int Region_ID, int Country_ID, bool IsPaymentAllocate)
        {
            Decimal withholdingAmt = 0;
            String sql = @"SELECT IsApplicableonPay, PayCalculation , PayPercentage 
                                        FROM VAB_Withholding WHERE IsActive = 'Y' AND IsApplicableonPay = 'Y' AND VAB_Withholding_ID = "
                                         + (!isBackupWithholding && GetVAB_Withholding_ID() > 0 ? GetVAB_Withholding_ID() : withholding_ID)
                                         + " AND TransactionType = '" + (IsReceipt() ? X_VAB_Withholding.TRANSACTIONTYPE_Sale
                                         : X_VAB_Withholding.TRANSACTIONTYPE_Purchase) + "'";
            if (Region_ID > 0)
            {
                sql += " AND NVL(VAB_RegionState_ID, 0) IN (0 ,  " + Region_ID + ")";
            }
            else
            {
                sql += " AND NVL(VAB_RegionState_ID, 0) IN (0) ";
            }
            if (Country_ID > 0)
            {
                sql += " AND NVL(VAB_Country_ID , 0) IN (0 ,  " + Country_ID + ")";
            }
            DataSet dsWithholding = DB.ExecuteDataset(sql, null, Get_Trx());
            if (dsWithholding != null && dsWithholding.Tables.Count > 0 && dsWithholding.Tables[0].Rows.Count > 0)
            {
                // check on withholding - "Applicable on Payment" or not
                if (Util.GetValueOfString(dsWithholding.Tables[0].Rows[0]["IsApplicableonPay"]).Equals("Y"))
                {
                    // get amount on which we have to derive withholding tax amount
                    if (Util.GetValueOfString(dsWithholding.Tables[0].Rows[0]["PayCalculation"]).Equals(X_VAB_Withholding.INVCALCULATION_PaymentAmount))
                    {
                        if (IsPaymentAllocate)
                        {
                            withholdingAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(@"SELECT COALESCE(SUM(PaymentAmount),0) - COALESCE(SUM(TaxAmount),0) 
                                               - COALESCE(SUM(SurchargeAmt),0) FROM VAB_Payment WHERE VAB_Payment_ID = " + GetVAB_Payment_ID(), null, Get_Trx()));
                            SetPaymentAmount(withholdingAmt);
                        }
                        else
                        {
                            withholdingAmt = GetPaymentAmount() - GetTaxAmount() - Util.GetValueOfDecimal(Get_Value("SurchargeAmt"));
                        }
                    }

                    _log.Info("Payment withholding detail, Payment Document No = " + GetDocumentNo() + " , Amount on distribute = " + withholdingAmt +
                           " , Payment Withhold Percentage " + Util.GetValueOfDecimal(dsWithholding.Tables[0].Rows[0]["PayPercentage"]));

                    // derive formula
                    withholdingAmt = Decimal.Divide(
                                     Decimal.Multiply(withholdingAmt, Util.GetValueOfDecimal(dsWithholding.Tables[0].Rows[0]["PayPercentage"]))
                                     , 100);
                    if (isBackupWithholding)
                    {
                        SetBackupWithholdingAmount(Decimal.Round(withholdingAmt, MVABCurrency.GetStdPrecision(GetCtx(), GetVAB_Currency_ID())));
                    }
                    else
                    {
                        SetWithholdingAmt(Decimal.Round(withholdingAmt, MVABCurrency.GetStdPrecision(GetCtx(), GetVAB_Currency_ID())));
                        if (GetVAB_Withholding_ID() <= 0)
                        {
                            SetVAB_Withholding_ID(withholding_ID);
                        }
                    }
                }
            }
            else
            {
                // when the selected withholding not applicable for payment then select refernce as ZERO
                if (isBackupWithholding)
                {
                    SetBackupWithholding_ID(0);
                }
                else
                {
                    SetVAB_Withholding_ID(0);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Is used to create new order schedule with given Amount
        /// </summary>
        /// <param name="ctx">Context</param>
        /// <param name="payment">Payment refernce for picking records detail </param>
        /// <param name="VA009_OrderPaySchedule_ID">Order Schedule whose copy to be created with remaing amount</param>
        /// <param name="BaseCurrency">Represent Base currency - for updation of base amount</param>
        /// <param name="Amt">Amount : Due Amt of new created schedule</param>
        /// <param name="TrxName">current transaction</param>
        /// <returns>True when record created successfully</returns>
        private bool SpiltOrderSchedule(Ctx ctx, MVABPayment payment, int VA009_OrderPaySchedule_ID, int BaseCurrency, Decimal Amt, Trx TrxName)
        {
            PO poOrignal = MVAFTableView.GetPO(ctx, "VA009_OrderPaySchedule", VA009_OrderPaySchedule_ID, TrxName);
            if (poOrignal != null && poOrignal.Get_ID() > 0)
            {
                PO poNew = MVAFTableView.GetPO(ctx, "VA009_OrderPaySchedule", 0, TrxName);
                poNew.Set_TrxName(TrxName);
                PO.CopyValues(poOrignal, poNew, poOrignal.GetVAF_Client_ID(), poOrignal.GetVAF_Org_ID());
                poNew.Set_Value("VA009_Variance", 0);
                poNew.Set_Value("VAB_Payment_ID", 0);
                poNew.Set_Value("VAB_CashJRNLLine_ID", 0);
                poNew.Set_Value("VA009_ExecutionStatus", "A");
                poNew.Set_Value("VA009_IsPaid", false);
                poNew.Set_Value("VA009_PaidAmnt", 0);
                poNew.Set_Value("VA009_PaidAmntInvce", 0);
                // convert amount into Order Currency
                Decimal InfoAmount = MVABExchangeRate.Convert(ctx, Amt, payment.GetVAB_Currency_ID(), Util.GetValueOfInt(poOrignal.Get_Value("VAB_Currency_ID")),
                    payment.GetDateAcct(), payment.GetVAB_CurrencyType_ID(), payment.GetVAF_Client_ID(), payment.GetVAF_Org_ID());
                poNew.Set_Value("DueAmt", InfoAmount);
                poNew.Set_Value("VA009_OpnAmntInvce", InfoAmount);
                // convert amount into Base Currency
                InfoAmount = MVABExchangeRate.Convert(ctx, Amt, payment.GetVAB_Currency_ID(), BaseCurrency,
                    payment.GetDateAcct(), payment.GetVAB_CurrencyType_ID(), payment.GetVAF_Client_ID(), payment.GetVAF_Org_ID());
                poNew.Set_Value("VA009_OpenAmnt", InfoAmount);
                if (!poNew.Save(TrxName))
                {
                    TrxName.Rollback();
                    ValueNamePair pp = VLogger.RetrieveError();
                    log.Info("Error Occured when try to Split Order Schedule. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                    SetProcessMsg(Msg.GetMsg(ctx, "VIS_OrderNotSplitted") + ", " + (pp != null ? pp.GetName() : ""));
                    return false;
                }
            }
            return true;
        }

        //public int GetCostAllocationID()
        //{
        //    return Convert.ToInt32(DB.ExecuteScalar("select nvl(cl.VAM_ProductCostAllocation_ID,0) as CostAllocationID from VAB_Payment cp inner join VAB_DocAllocationLine cl on cp.VAB_Payment_ID=cl.VAB_Payment_ID where cp.VAB_Payment_ID='" + GetVAB_Payment_ID() + "'"));
        //}
        //public string GetCostAlloactionDocStatus()
        //{
        //    return Convert.ToString(DB.ExecuteScalar("select DocStatus from VAM_ProductCostAllocation where VAM_ProductCostAllocation_ID=" + GetCostAllocationID()));
        //}
        //public bool GenerateCostAllocation(string DocumentNo, int VAF_Client_ID, Trx trx, int VAF_Org_ID, out String Msg)
        //{
        //    Msg = "";
        //    bool CheckExecution = true;
        //    #region Get Allocation Record Query
        //    string sql = "SELECT DocumentType,PaymentNo,InvoiceNo,BaseCurrency,TotalAllocationAmount,DocStatus,InvoiceLineID,AllocationType,ProductID,partialType,AllocationLineID FROM " +
        //    " (SELECT cs.VAB_DocTypes_id AS DocumentType,pay.paymentID AS PaymentNo,pay.Payamt,pay.Amount,ci.grandTotal AS InvoiceAmount,pay.invoiceID AS InvoiceNo, " +
        //    " pay.DocumentNo,pay.PaymentDoc,pay.partialType,ci.VAB_DocTypes_ID  AS InvoiceDoc,AC.VAB_CURRENCY_ID AS BaseCurrency,ci.VAB_Currency_id,pc.iso_code,icr.multiplyrate AS InvoiceCurrencyRate," +
        //    "  pcr.multiplyrate AS PayCurrencyRate,cl.lineTotalAmt AS LineAmount,CS.DOCSTATUS,cl.VAB_InvoiceLine_id AS InvoiceLineID,CS.ALLOCATIONTYPE,al.VAB_DocAllocationLine_ID as AllocationLineID,mp.VAM_Product_id as ProductID," +
        //    "   CASE WHEN pay.partialType='CD' THEN CASE WHEN ((icr.multiplyrate=pcr.multiplyrate)or(ci.VAB_Currency_ID=AC.VAB_CURRENCY_ID)) THEN 0 ELSE (round(((cl.lineTotalAmt/ci.grandTotal*100)/100*pay.Amount)*(pcr.multiplyrate),12)) END " +
        //    "  ELSE case when (ci.VAB_Currency_ID=AC.VAB_CURRENCY_ID) then (round(((cl.lineTotalAmt/ci.grandTotal*100)/100*pay.Amount),12)) else (round(((cl.lineTotalAmt/ci.grandTotal*100)/100*pay.Amount)*(pcr.multiplyrate),12)) END END AS TotalAllocationAmount " +
        //    "   FROM " +
        //    "  (SELECT PaymentID,Payamt,Amount,InvoiceID,DocumentNo,PaymentDoc,dateTrx,Partialtype,payCurrID,conversiontype_ID FROM " +
        //     "  (SELECT VAB_Payment_id AS PaymentID,Payamt,writeoffamt  AS Amount,VAB_Invoice_id AS InvoiceID,documentNo,VAB_DocTypes_ID AS PaymentDoc,dateTrx,'WO' AS Partialtype,VAB_Currency_id AS payCurrID,VAB_CurrencyType_ID as conversiontype_ID " +
        //     " FROM VAB_Payment UNION ALL " +
        //     "  SELECT VAB_Payment_id AS PaymentID,Payamt,discountamt  AS Amount,VAB_Invoice_id AS InvoiceID,documentNo,VAB_DocTypes_ID AS PaymentDoc,dateTrx,'DI' AS Partialtype,VAB_Currency_id AS payCurrID,VAB_CurrencyType_ID as conversiontype_ID " +
        //     "  FROM VAB_Payment UNION ALL " +
        //      " SELECT VAB_Payment_id AS PaymentID,payamt,payamt AS Amount,VAB_Invoice_ID AS InvoiceID,documentno,VAB_DocTypes_ID AS PAymentDoc,dateTrx,'CD' AS Partialtype,VAB_Currency_id AS payCurrID,VAB_CurrencyType_ID as conversiontype_ID " +
        //      "   FROM VAB_PAYMENT) Payment) pay " +
        //      "  INNER JOIN VAB_Invoice ci " +
        //       " ON pay.invoiceID=ci.VAB_Invoice_ID " +
        //       "  INNER JOIN VAM_ProductCostAllocationSetting cs " +
        //        " ON pay.paymentDoc  =cs.VAB_DocTypes_ID " +
        //         " AND ci.VAB_DocTypes_ID=cs.InvRef_DocType_ID and cs.isactive='Y'" +
        //         "  inner join VAF_ClientDetail adc on adc.VAF_CLIENT_ID=" + VAF_Client_ID + "" +
        //          "  INNER JOIN VAB_ACCOUNTBOOK AC " +
        //           "   ON AC.VAB_ACCOUNTBOOK_ID=adc.VAB_ACCOUNTBOOK1_ID " +
        //             " INNER JOIN VAB_CURRENCY PC " +
        //              " ON PC.VAB_CURRENCY_ID=AC.VAB_CURRENCY_ID " +
        //               " INNER JOIN VAB_DocAllocationLine al " +
        //               " ON pay.paymentID=al.VAB_Payment_ID " +
        //                " INNER JOIN (SELECT SUM(lineTotalAmt) AS lineTotalAmt,VAB_Invoice_id,VAM_Product_ID,VAB_InvoiceLine_id FROM VAB_InvoiceLine GROUP BY VAB_Invoice_id, VAM_Product_ID,VAB_InvoiceLine_id) cl " +
        //                " ON cl.VAB_Invoice_id=ci.VAB_Invoice_id " +
        //                " INNER JOIN VAM_Product mp " +
        //                "  ON cl.VAM_Product_id=mp.VAM_Product_id " +
        //                 "  LEFT JOIN VAB_EXCHANGERATE ICR " +
        //                 "  ON icr.VAB_CURRENCY_ID    =ci.VAB_CURRENCY_ID AND ICR.VAB_CURRENCY_To_ID=AC.VAB_CURRENCY_ID AND (ci.dateinvoiced>= ICR.VALIDFROM AND ci.dateinvoiced<= ICR.VALIDTO) AND ICR.isactive='Y' and ICR.vaf_client_ID=" + VAF_Client_ID + "  and icr.VAB_CurrencyType_id=ci.VAB_CurrencyType_id " +
        //                 "  LEFT JOIN VAB_EXCHANGERATE PCR " +
        //                  " ON PCR.VAB_CURRENCY_ID    =ci.VAB_CURRENCY_ID AND PCR.VAB_CURRENCY_To_ID=AC.VAB_CURRENCY_ID AND (Pay.dateTrx >= PCR.VALIDFROM AND pay.dateTrx<= PCR.VALIDTO) AND PCR.isactive='Y' and pcr.vaf_client_ID=" + VAF_Client_ID + "  and pcr.VAB_CurrencyType_id=pay.conversiontype_ID " +
        //                  " WHERE pay.documentNo ='" + DocumentNo + "' AND mp.productType ='I' AND ((icr.multiplyrate IS NOT NULL AND pcr.multiplyrate IS NOT NULL) OR (ci.VAB_Currency_ID=AC.VAB_CURRENCY_ID))) WHERE TotalAllocationAmount!=0";  //"AND icr.multiplyrate IS NOT NULL " Check After ProductType...
        //    #endregion
        //    try
        //    {
        //        log.SaveError("", sql);

        //        DataSet DsAllocationRecord = DB.ExecuteDataset(sql, null, trx);
        //        decimal CostAllocationAmount = 0;
        //        if (DsAllocationRecord.Tables.Count > 0)
        //        {
        //            if (DsAllocationRecord.Tables[0].Rows.Count > 0)
        //            {
        //                string AllocationType = Convert.ToString(DsAllocationRecord.Tables[0].Rows[0]["AllocationType"]);
        //                string DocStatus = Convert.ToString(DsAllocationRecord.Tables[0].Rows[0]["DocStatus"]);
        //                DataTable DtCurrency = DsAllocationRecord.Tables[0].DefaultView.ToTable(true, "BaseCurrency");
        //                for (int i = 0; i < DtCurrency.Rows.Count; i++)
        //                {
        //                    CostAllocationAmount = 0;
        //                    DataRow[] DrCostAllocationLine;

        //                    // AllocationType Value from CostAllocationSetting according to Payment and Invoice Document Type....
        //                    if (AllocationType.Equals("CD"))
        //                    {
        //                        DrCostAllocationLine = DsAllocationRecord.Tables[0].Select("BaseCurrency=" + Convert.ToString(DtCurrency.Rows[i]["BaseCurrency"]) + " and partialType='CD'");
        //                    }
        //                    else if (AllocationType.Equals("DI"))
        //                    {
        //                        DrCostAllocationLine = DsAllocationRecord.Tables[0].Select("BaseCurrency=" + Convert.ToString(DtCurrency.Rows[i]["BaseCurrency"]) + " and partialType='DI'");
        //                    }
        //                    else if (AllocationType.Equals("WO"))
        //                    {
        //                        DrCostAllocationLine = DsAllocationRecord.Tables[0].Select("BaseCurrency=" + Convert.ToString(DtCurrency.Rows[i]["BaseCurrency"]) + " and partialType='WO'");
        //                    }
        //                    else if (AllocationType.Equals("CA"))
        //                    {
        //                        DrCostAllocationLine = DsAllocationRecord.Tables[0].Select("BaseCurrency=" + Convert.ToString(DtCurrency.Rows[i]["BaseCurrency"]) + " and (partialType='CD' or partialType='DI')");
        //                    }
        //                    else if (AllocationType.Equals("CW"))
        //                    {
        //                        DrCostAllocationLine = DsAllocationRecord.Tables[0].Select("BaseCurrency=" + Convert.ToString(DtCurrency.Rows[i]["BaseCurrency"]) + " and (partialType='CD' or partialType='WO')");
        //                    }
        //                    else if (AllocationType.Equals("DW"))
        //                    {
        //                        DrCostAllocationLine = DsAllocationRecord.Tables[0].Select("BaseCurrency=" + Convert.ToString(DtCurrency.Rows[i]["BaseCurrency"]) + " and (partialType='WO' or partialType='DI')");
        //                    }
        //                    else
        //                    {
        //                        DrCostAllocationLine = DsAllocationRecord.Tables[0].Select("BaseCurrency=" + Convert.ToString(DtCurrency.Rows[i]["BaseCurrency"]));
        //                    }
        //                    MVAMProductCostAllocation ObjMVAMProductCostAllocation = new MVAMProductCostAllocation(GetCtx(), 0, trx);
        //                    ObjMVAMProductCostAllocation.SetVAB_DocTypes_ID(Convert.ToInt32(DrCostAllocationLine[0]["DocumentType"]));
        //                    ObjMVAMProductCostAllocation.SetVAB_Invoice_ID(Convert.ToInt32(DrCostAllocationLine[0]["InvoiceNo"]));
        //                    ObjMVAMProductCostAllocation.SetVAB_Payment_ID(Convert.ToInt32(DrCostAllocationLine[0]["PaymentNo"]));
        //                    ObjMVAMProductCostAllocation.SetVAB_Currency_ID(Convert.ToInt32(DtCurrency.Rows[i]["BaseCurrency"]));
        //                    ObjMVAMProductCostAllocation.SetDocStatus(Convert.ToString(DrCostAllocationLine[0]["DocStatus"]));
        //                    ObjMVAMProductCostAllocation.SetVAF_Org_ID(VAF_Org_ID);
        //                    if (DocStatus.Equals("CO"))
        //                    {
        //                        ObjMVAMProductCostAllocation.CompleteIt();
        //                        ObjMVAMProductCostAllocation.SetIsProcessed(true);
        //                        ObjMVAMProductCostAllocation.SetDocAction(DOCACTION_Close);
        //                        ObjMVAMProductCostAllocation.SetDocStatus(DOCACTION_Complete);
        //                    }

        //                    if (ObjMVAMProductCostAllocation.Save(trx))
        //                    {
        //                        if (DrCostAllocationLine.Length > 0)
        //                        {
        //                            for (int j = 0; j < DrCostAllocationLine.Length; j++)
        //                            {
        //                                CostAllocationAmount += Convert.ToDecimal(DrCostAllocationLine[j]["TotalAllocationAmount"]);
        //                                MVAMProductCostAllocationLine ObjMVAMProductCostAllocationLine = new MVAMProductCostAllocationLine(GetCtx(), 0, trx);
        //                                ObjMVAMProductCostAllocationLine.SetVAM_ProductCostAllocation_ID(ObjMVAMProductCostAllocation.Get_ID());
        //                                ObjMVAMProductCostAllocationLine.SetVAB_InvoiceLine_ID(Convert.ToInt32(DrCostAllocationLine[j]["InvoiceLineID"]));
        //                                ObjMVAMProductCostAllocationLine.SetVAM_Product_ID(Convert.ToInt32(DrCostAllocationLine[j]["ProductID"]));
        //                                ObjMVAMProductCostAllocationLine.SetAmount(Convert.ToDecimal(DrCostAllocationLine[j]["TotalAllocationAmount"]));
        //                                ObjMVAMProductCostAllocationLine.SetAllocationType(Convert.ToString(DrCostAllocationLine[j]["partialType"]));
        //                                ObjMVAMProductCostAllocationLine.SetVAF_Org_ID(VAF_Org_ID);
        //                                if (!ObjMVAMProductCostAllocationLine.Save(trx))
        //                                {
        //                                    CheckExecution = false;
        //                                    Msg = "Error Occured when try to Save record on Cost Allocation Line";
        //                                    break;
        //                                }

        //                            }
        //                            if (!CheckExecution)
        //                            {
        //                                break;
        //                            }
        //                            // Update CostAllocationAmount on CostAllocation generated from sum of All Amount in CostAllocationLine................
        //                            ObjMVAMProductCostAllocation.SetAllocationAmt(CostAllocationAmount);
        //                            if (!ObjMVAMProductCostAllocation.Save(trx))
        //                            {
        //                                CheckExecution = false;
        //                                Msg = "Error Occured when try to Save record on Cost Allocation";
        //                                break;
        //                            }

        //                            //  update CostAllocationID on Allocation Line against generated Allocation Line.........
        //                            MAllocationLine objMAllocationLine = new MAllocationLine(GetCtx(), Convert.ToInt32(DrCostAllocationLine[0]["AllocationLineID"]), trx);
        //                            objMAllocationLine.SetVAM_ProductCostAllocation_ID(ObjMVAMProductCostAllocation.Get_ID());
        //                            if (!objMAllocationLine.Save(trx))
        //                            {
        //                                CheckExecution = false;
        //                                Msg = "Error Occured when try to Update Cost Allocation ID on AllocationLine";
        //                                break;
        //                            }
        //                        }


        //                    }
        //                    else
        //                    {
        //                        CheckExecution = false;
        //                        Msg = "Error Occured when try to Save record on Cost Allocation.";
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CheckExecution = false;
        //        Msg = "Error Occured when try to Save record on Cost Allocation." + ex.Message;
        //    }
        //    finally
        //    {
        //        if (!CheckExecution)
        //        {
        //            trx.Rollback();
        //            ValueNamePair pp = VLogger.RetrieveError();
        //            log.Info(Msg + " Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
        //        }
        //    }

        //    return CheckExecution;
        //}
        private bool UpdateRealizedAmount(int VAB_Order_ID, int VAB_Invoice_ID)
        {
            try
            {
                DataSet dsInvoicePayment = null;
                string sql;
                decimal price = 0;
                PO po = null;
                MVABInvoice invoice = null;
                MVABOrder order = null;
                int currencyTo = 0;
                decimal differenceAmount = 0;
                bool isRealized = false;

                if (GetVA026_LCDetail_ID() > 0)
                {
                    sql = @"SELECT vaf_tableview_ID  FROM vaf_tableview WHERE tablename LIKE 'VA026_LCDetail' AND IsActive = 'Y'";
                    int tableId = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    MVAFTableView tbl = new MVAFTableView(GetCtx(), tableId, null);
                    po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                    currencyTo = Util.GetValueOfInt(po.Get_Value("VAB_Currency_ID"));

                    if (Util.GetValueOfString(po.Get_Value("VA026_Realized")) == "Y")
                    {
                        return true;
                    }

                    if (VAB_Invoice_ID != 0)
                    {
                        #region update Amount on LC Detail
                        invoice = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_Trx());
                        sql = @"SELECT aline.Amount, aline.DiscountAmt , aline.WriteOffAmt , aline.OverUnderAmt , aloc.VAB_Currency_ID , 
                               (aline.Amount - (aline.DiscountAmt + aline.WriteOffAmt) ) AS paidinvamount1 , d.DOCBASETYPE , 
                               CASE WHEN d.DOCBASETYPE = 'API' THEN (-1* aline.Amount - ((-1 * aline.DiscountAmt) + (-1 * aline.WriteOffAmt)))
                                    WHEN d.DOCBASETYPE = 'ARC' THEN (-1* aline.Amount - ((-1 * aline.DiscountAmt) + (-1 * aline.WriteOffAmt)))
                                    ELSE (aline.Amount - (aline.DiscountAmt + aline.WriteOffAmt)) END AS paidinvamount 
                               FROM VAB_DocAllocation aloc 
                               INNER JOIN VAB_DocAllocationLine aline ON aloc.VAB_DocAllocation_ID = aline.VAB_DocAllocation_ID
                               INNER JOIN VAB_Invoice i ON i.VAB_Invoice_ID = aline.VAB_Invoice_ID  
                               INNER JOIN VAB_DocTypes d ON d.VAB_DocTypes_ID = i.VAB_DocTypes_ID 
                               WHERE aline.IsActive = 'Y' 
                               AND aloc.DocStatus = 'CO' AND aline.VAB_Invoice_ID = " + VAB_Invoice_ID + " AND aline.VAB_Payment_ID = " + GetVAB_Payment_ID();
                        dsInvoicePayment = DB.ExecuteDataset(sql, null, Get_Trx());
                        if (dsInvoicePayment != null && dsInvoicePayment.Tables.Count > 0 && dsInvoicePayment.Tables[0].Rows.Count > 0)
                        {
                            for (int i = 0; i < dsInvoicePayment.Tables[0].Rows.Count; i++)
                            {
                                if (currencyTo != Util.GetValueOfInt(dsInvoicePayment.Tables[0].Rows[i]["VAB_Currency_ID"]))
                                {
                                    price += MVABExchangeRate.Convert(GetCtx(), Util.GetValueOfDecimal(dsInvoicePayment.Tables[0].Rows[i]["paidinvamount"]),
                                          Util.GetValueOfInt(dsInvoicePayment.Tables[0].Rows[i]["VAB_Currency_ID"]), currencyTo, GetDateAcct(),
                                          invoice.GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                                }
                                else
                                {
                                    price += Util.GetValueOfDecimal(dsInvoicePayment.Tables[0].Rows[i]["paidinvamount"]);
                                }
                            }
                            if (price == 0)
                            {
                                return false;
                            }
                            if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) < 0)
                            {
                                //if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LcAmtAfterTolerance")))
                                //{
                                //    isRealized = true;
                                //}
                                //else
                                //{
                                //    isRealized = false;
                                //}
                            }
                            else if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) > 0)
                            {
                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                {
                                    isRealized = true;
                                }
                                else
                                {
                                    isRealized = false;
                                }
                            }
                            else
                            {
                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) == Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                {
                                    isRealized = true;
                                }
                                else
                                {
                                    isRealized = false;
                                }
                            }
                            //differenceAmount = Decimal.Subtract(Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")),
                            //                   Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                            //if (differenceAmount < 0)
                            //{
                            //    differenceAmount = Decimal.Negate(differenceAmount);
                            //    if (differenceAmount > 1)
                            //    {
                            //        return false;
                            //    }
                            //}
                            if (isRealized)
                            {
                                //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                po.Set_Value("VA026_Realized", true);
                                po.Set_Value("VA026_RealizedDate", GetDateAcct());
                                if (!po.Save(Get_Trx()))
                                {
                                    Get_Trx().Rollback();
                                    ValueNamePair pp = VLogger.RetrieveError();
                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                    return false;
                                }
                            }
                            else
                            {
                                //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                po.Set_Value("VA026_Realized", false);
                                po.Set_Value("VA026_RealizedDate", null);
                                if (!po.Save(Get_Trx()))
                                {
                                    Get_Trx().Rollback();
                                    ValueNamePair pp = VLogger.RetrieveError();
                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                            po.Set_Value("VA026_RealizedAmt", 0);
                            po.Set_Value("VA026_Realized", false);
                            po.Set_Value("VA026_RealizedDate", null);
                            if (!po.Save(Get_Trx()))
                            {
                                Get_Trx().Rollback();
                                ValueNamePair pp = VLogger.RetrieveError();
                                log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                return false;
                            }
                        }
                        #endregion
                    }
                    else if (VAB_Order_ID != 0)
                    {
                        #region update Amount on LC Detail
                        order = new MVABOrder(GetCtx(), GetVAB_Order_ID(), Get_Trx());
                        //                        sql = @"SELECT aline.Amount, aline.DiscountAmt , aline.WriteOffAmt , aline.OverUnderAmt , aloc.VAB_Currency_ID , 
                        //                               (aline.Amount - (aline.DiscountAmt + aline.WriteOffAmt) + aline.OverUnderAmt) AS paidinvamount FROM VAB_DocAllocation aloc 
                        //                               INNER JOIN VAB_DocAllocationLine aline ON aloc.VAB_DocAllocation_ID = aline.VAB_DocAllocation_ID WHERE aline.IsActive = 'Y' 
                        //                               AND aloc.DocStatus = 'CO' AND aline.VAB_Order_ID = " + VAB_Order_ID + " AND aline.VAB_Payment_ID = " + GetVAB_Payment_ID();

                        sql = @"SELECT payAmt AS Amount,  DiscountAmt ,   WriteOffAmt , OverUnderAmt ,  VAB_Currency_ID ,
                                 (payAmt - (DiscountAmt + WriteOffAmt) + OverUnderAmt) AS paidinvamount
                                FROM VAB_Payment WHERE IsActive   = 'Y' AND VAB_Payment_ID =" + GetVAB_Payment_ID();
                        dsInvoicePayment = DB.ExecuteDataset(sql, null, Get_Trx());
                        if (dsInvoicePayment != null && dsInvoicePayment.Tables.Count > 0 && dsInvoicePayment.Tables[0].Rows.Count > 0)
                        {
                            for (int i = 0; i < dsInvoicePayment.Tables[0].Rows.Count; i++)
                            {
                                if (currencyTo != Util.GetValueOfInt(dsInvoicePayment.Tables[0].Rows[i]["VAB_Currency_ID"]))
                                {
                                    price += MVABExchangeRate.Convert(GetCtx(), Util.GetValueOfDecimal(dsInvoicePayment.Tables[0].Rows[i]["paidinvamount"]),
                                          Util.GetValueOfInt(dsInvoicePayment.Tables[0].Rows[i]["VAB_Currency_ID"]), currencyTo, GetDateAcct(),
                                          order.GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                                }
                                else
                                {
                                    price += Util.GetValueOfDecimal(dsInvoicePayment.Tables[0].Rows[i]["paidinvamount"]);
                                }
                            }

                            if (price == 0)
                            {
                                return false;
                            }
                            if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) < 0)
                            {
                                //if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LcAmtAfterTolerance")))
                                //{
                                //    isRealized = true;
                                //}
                                //else
                                //{
                                //    isRealized = false;
                                //}
                            }
                            else if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) > 0)
                            {
                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                {
                                    isRealized = true;
                                }
                                else
                                {
                                    isRealized = false;
                                }
                            }
                            else
                            {
                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) == Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                {
                                    isRealized = true;
                                }
                                else
                                {
                                    isRealized = false;
                                }
                            }
                            //differenceAmount = Decimal.Subtract(Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")),
                            //                   Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                            //if (differenceAmount < 0)
                            //{
                            //    differenceAmount = Decimal.Negate(differenceAmount);
                            //    if (differenceAmount > 1)
                            //    {
                            //        return false;
                            //    }
                            //}
                            if (isRealized)
                            {
                                //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                po.Set_Value("VA026_Realized", true);
                                po.Set_Value("VA026_RealizedDate", GetDateAcct());
                                if (!po.Save(Get_Trx()))
                                {
                                    Get_Trx().Rollback();
                                    ValueNamePair pp = VLogger.RetrieveError();
                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                    return false;
                                }
                            }
                            else
                            {
                                //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                po.Set_Value("VA026_Realized", false);
                                po.Set_Value("VA026_RealizedDate", null);
                                if (!po.Save(Get_Trx()))
                                {
                                    Get_Trx().Rollback();
                                    ValueNamePair pp = VLogger.RetrieveError();
                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                            po.Set_Value("VA026_RealizedAmt", 0);
                            po.Set_Value("VA026_Realized", false);
                            po.Set_Value("VA026_RealizedDate", null);
                            if (!po.Save(Get_Trx()))
                            {
                                Get_Trx().Rollback();
                                ValueNamePair pp = VLogger.RetrieveError();
                                log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                return false;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        MVABPaymentAllocate[] pAllocs = MVABPaymentAllocate.Get(this);
                        if (pAllocs.Length > 0)
                        {
                            List<int> invoiceList = new List<int>();
                            for (int j = 0; j < pAllocs.Length; j++)
                            {
                                price = 0;
                                MVABPaymentAllocate pa = pAllocs[j];
                                if (!invoiceList.Contains(pa.GetVAB_Invoice_ID()))
                                {
                                    invoiceList.Add(pa.GetVAB_Invoice_ID());

                                    #region update Amount on LC Detail
                                    if (pa.GetVAB_Invoice_ID() != 0)
                                    {
                                        invoice = new MVABInvoice(GetCtx(), pa.GetVAB_Invoice_ID(), Get_Trx());
                                        sql = @"SELECT aline.Amount, aline.DiscountAmt , aline.WriteOffAmt , aline.OverUnderAmt , aloc.VAB_Currency_ID , 
                               (aline.Amount - (aline.DiscountAmt + aline.WriteOffAmt) + aline.OverUnderAmt) AS paidinvamount1 , d.DOCBASETYPE, 
                               CASE WHEN d.DOCBASETYPE = 'API' THEN (-1* aline.Amount - ((-1 * aline.DiscountAmt) + (-1 * aline.WriteOffAmt)))
                                    WHEN d.DOCBASETYPE = 'ARC' THEN (-1* aline.Amount - ((-1 * aline.DiscountAmt) + (-1 * aline.WriteOffAmt)))
                                    ELSE (aline.Amount - (aline.DiscountAmt + aline.WriteOffAmt)) END AS paidinvamount 
                               FROM VAB_DocAllocation aloc 
                               INNER JOIN VAB_DocAllocationLine aline ON aloc.VAB_DocAllocation_ID = aline.VAB_DocAllocation_ID 
                               INNER JOIN VAB_Invoice i ON i.VAB_Invoice_ID = aline.VAB_Invoice_ID  
                               INNER JOIN VAB_DocTypes d ON d.VAB_DocTypes_ID = i.VAB_DocTypes_ID 
                               WHERE aline.IsActive = 'Y' 
                               AND aloc.DocStatus = 'CO' AND aline.VAB_Invoice_ID = " + pa.GetVAB_Invoice_ID() + " AND aline.VAB_Payment_ID = " + GetVAB_Payment_ID();
                                        dsInvoicePayment = DB.ExecuteDataset(sql, null, Get_Trx());
                                        if (dsInvoicePayment != null && dsInvoicePayment.Tables.Count > 0 && dsInvoicePayment.Tables[0].Rows.Count > 0)
                                        {
                                            for (int i = 0; i < dsInvoicePayment.Tables[0].Rows.Count; i++)
                                            {
                                                if (currencyTo != Util.GetValueOfInt(dsInvoicePayment.Tables[0].Rows[i]["VAB_Currency_ID"]))
                                                {
                                                    price += MVABExchangeRate.Convert(GetCtx(), Util.GetValueOfDecimal(dsInvoicePayment.Tables[0].Rows[i]["paidinvamount"]),
                                                          Util.GetValueOfInt(dsInvoicePayment.Tables[0].Rows[i]["VAB_Currency_ID"]), currencyTo, GetDateAcct(),
                                                          invoice.GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                                                }
                                                else
                                                {
                                                    price += Util.GetValueOfDecimal(dsInvoicePayment.Tables[0].Rows[i]["paidinvamount"]);
                                                }
                                            }
                                            if (price == 0)
                                            {
                                                return false;
                                            }
                                            if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) < 0)
                                            {
                                                //if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LcAmtAfterTolerance")))
                                                //{
                                                //    isRealized = true;
                                                //}
                                                //else
                                                //{
                                                //    isRealized = false;
                                                //}
                                            }
                                            else if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) > 0)
                                            {
                                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                                {
                                                    isRealized = true;
                                                }
                                                else
                                                {
                                                    isRealized = false;
                                                }
                                            }
                                            else
                                            {
                                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) == Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                                {
                                                    isRealized = true;
                                                }
                                                else
                                                {
                                                    isRealized = false;
                                                }
                                            }
                                            //differenceAmount = Decimal.Subtract(Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")),
                                            //   Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                            //if (differenceAmount < 0)
                                            //{
                                            //    differenceAmount = Decimal.Negate(differenceAmount);
                                            //    if (differenceAmount > 1)
                                            //    {
                                            //        return false;
                                            //    }
                                            //}
                                            if (isRealized)
                                            {
                                                //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                                po.Set_Value("VA026_Realized", true);
                                                po.Set_Value("VA026_RealizedDate", GetDateAcct());
                                                if (!po.Save(Get_Trx()))
                                                {
                                                    Get_Trx().Rollback();
                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                                    return false;
                                                }
                                            }
                                            else
                                            {
                                                //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                                po.Set_Value("VA026_Realized", false);
                                                po.Set_Value("VA026_RealizedDate", null);
                                                if (!po.Save(Get_Trx()))
                                                {
                                                    Get_Trx().Rollback();
                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                                    return false;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                                            po.Set_Value("VA026_RealizedAmt", 0);
                                            po.Set_Value("VA026_Realized", false);
                                            po.Set_Value("VA026_RealizedDate", null);
                                            if (!po.Save(Get_Trx()))
                                            {
                                                Get_Trx().Rollback();
                                                ValueNamePair pp = VLogger.RetrieveError();
                                                log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                                return false;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                        }
                        else
                        {
                            // Handle LC when its realize against Business Partner
                            //po = tbl.GetPO(GetCtx(), Util.GetValueOfInt(GetVA026_LCDetail_ID()), Get_Trx());
                            price = Decimal.Subtract(GetPayAmt(), Decimal.Add(GetDiscountAmt(), GetWriteOffAmt()));
                            if (GetDescription() != null && GetDescription().Contains("{->") && price > 0)
                                price = Decimal.Negate(price);
                            if (currencyTo != Util.GetValueOfInt(GetVAB_Currency_ID()))
                            {
                                price = MVABExchangeRate.Convert(GetCtx(), price,
                                      Util.GetValueOfInt(GetVAB_Currency_ID()), currencyTo, GetDateAcct(),
                                      GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                            }
                            if (price == 0)
                            {
                                return false;
                            }
                            if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) < 0)
                            {
                                //if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LcAmtAfterTolerance")))
                                //{
                                //    isRealized = true;
                                //}
                                //else
                                //{
                                //    isRealized = false;
                                //}
                            }
                            else if (Util.GetValueOfDecimal(po.Get_Value("VA026_TolerancePercentage")) > 0)
                            {
                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) >= Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                {
                                    isRealized = true;
                                }
                                else
                                {
                                    isRealized = false;
                                }
                            }
                            else
                            {
                                if (Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price) == Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")))
                                {
                                    isRealized = true;
                                }
                                else
                                {
                                    isRealized = false;
                                }
                            }
                            //differenceAmount = Decimal.Subtract(Util.GetValueOfDecimal(po.Get_Value("VA026_LCAmt")),
                            //                   Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                            //if (differenceAmount < 0)
                            //{
                            //    differenceAmount = Decimal.Negate(differenceAmount);
                            //    if (differenceAmount > 1)
                            //    {
                            //        return false;
                            //    }
                            //}
                            if (isRealized)
                            {
                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                po.Set_Value("VA026_Realized", true);
                                po.Set_Value("VA026_RealizedDate", GetDateAcct());
                                if (!po.Save(Get_Trx()))
                                {
                                    Get_Trx().Rollback();
                                    ValueNamePair pp = VLogger.RetrieveError();
                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                    return false;
                                }
                            }
                            else
                            {
                                po.Set_Value("VA026_RealizedAmt", Decimal.Add(Util.GetValueOfDecimal(po.Get_Value("VA026_RealizedAmt")), price));
                                po.Set_Value("VA026_Realized", false);
                                po.Set_Value("VA026_RealizedDate", null);
                                if (!po.Save(Get_Trx()))
                                {
                                    Get_Trx().Rollback();
                                    ValueNamePair pp = VLogger.RetrieveError();
                                    log.Info("Error Occured when try to update record on LC Detail. Error Type : " + pp.GetValue() + "  , Error Name : " + pp.GetName());
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Info("Error Occured when try to update record on LC Detail. " + ex.ToString());
                return false;
            }
            return true;
        }

        // Added By Amit -> 3-Nov-2016
        public static MVABPayment CopyFrom(MVABPayment fromPayment, DateTime? dateDoc, int VAB_DocTypes_ID, Trx trxName)
        {
            MVABPayment to = new MVABPayment(fromPayment.GetCtx(), 0, trxName);
            int countLineRecord = 0;
            try
            {
                to.Set_TrxName(trxName);
                PO.CopyValues(fromPayment, to, fromPayment.GetVAF_Client_ID(), fromPayment.GetVAF_Org_ID());
                to.Set_ValueNoCheck("VAB_Payment_ID", I_ZERO);
                to.Set_ValueNoCheck("DocumentNo", null);
                //
                to.SetDocStatus(DOCSTATUS_Drafted);		//	Draft
                to.SetDocAction(DOCACTION_Complete);
                //
                to.SetDateTrx(dateDoc);
                to.SetDateAcct(dateDoc);
                //
                to.SetIsAllocated(false);
                to.SetIsApproved(false);
                to.SetIsSelfService(false);
                to.SetPosted(false);
                to.SetProcessed(false);
                to.SetIsReconciled(false);
                to.SetIsPrinted(false);
                //if Payment Allocate Exist then Amounts are updated  when adding lines
                countLineRecord = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAB_PaymentAllotment WHERE IsActive = 'Y' AND VAB_Payment_ID = " + fromPayment.GetVAB_Payment_ID(), null, trxName));
                if (countLineRecord > 0)
                {
                    to.SetPayAmt(Env.ZERO);
                    to.SetOverUnderAmt(Env.ZERO);
                    to.SetWriteOffAmt(Env.ZERO);
                    to.SetDiscountAmt(Env.ZERO);
                }
                if (!to.Save(trxName))
                {
                    throw new Exception("Could not create Payment");
                }

                if (countLineRecord > 0)
                {
                    if (to.CopyLinesFrom(fromPayment, trxName) == 0)
                    {
                        throw new Exception("Could not create Payment Allocate");
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return to;
        }

        public int CopyLinesFrom(MVABPayment otherPayment, Trx trxName)
        {
            int count = 0;
            try
            {
                if (IsProcessed() || IsPosted() || otherPayment == null)
                    return 0;

                MVABPaymentAllocate[] fromLines = otherPayment.GetLines(null, null);

                for (int i = 0; i < fromLines.Length; i++)
                {
                    MVABPaymentAllocate line = new MVABPaymentAllocate(otherPayment.GetCtx(), 0, trxName);
                    PO.CopyValues(fromLines[i], line, GetVAF_Client_ID(), GetVAF_Org_ID());
                    line.SetVAB_Payment_ID(GetVAB_Payment_ID());
                    line.Set_ValueNoCheck("VAB_PaymentAllotment_ID", I_ZERO);	//	new
                    if (line.Save(trxName))
                        count++;
                }
                if (fromLines.Length != count)
                {
                    log.Log(Level.SEVERE, "Line difference - From=" + fromLines.Length + " <> Saved=" + count);
                }
            }
            catch (Exception ex)
            {
                log.Log(Level.SEVERE, null, ex);
            }
            return count;
        }

        public MVABPaymentAllocate[] GetLines(String whereClause, String orderClause)
        {
            List<MVABPaymentAllocate> list = new List<MVABPaymentAllocate>();
            StringBuilder sql = new StringBuilder("SELECT * FROM VAB_PaymentAllotment WHERE VAB_Payment_ID =" + GetVAB_Payment_ID() + "");
            if (whereClause != null)
                sql.Append(whereClause);
            if (orderClause != null)
                sql.Append(" ").Append(orderClause);
            try
            {
                DataSet ds = DB.ExecuteDataset(sql.ToString(), null, Get_TrxName());
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        MVABPaymentAllocate ol = new MVABPaymentAllocate(GetCtx(), dr, Get_TrxName());
                        list.Add(ol);
                    }
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql.ToString(), e);
            }
            //
            MVABPaymentAllocate[] lines = new MVABPaymentAllocate[list.Count];
            lines = list.ToArray();
            return lines;
        }
        //end

        /**
         * 	Create Counter Document
         * 	@return payment
         */
        private MVABPayment CreateCounterDoc()
        {
            //	Is this a counter doc ?
            if (GetRef_Payment_ID() != 0)
                return null;

            //	Org Must be linked to BPartner
            MVAFOrg org = MVAFOrg.Get(GetCtx(), GetVAF_Org_ID());
            //jz int counterVAB_BusinessPartner_ID = org.GetLinkedVAB_BusinessPartner_ID(); 
            int counterVAB_BusinessPartner_ID = org.GetLinkedVAB_BusinessPartner_ID(Get_Trx());
            if (counterVAB_BusinessPartner_ID == 0)
                return null;
            //	Business Partner needs to be linked to Org
            //jz MBPartner bp = new MBPartner (GetCtx(), GetVAB_BusinessPartner_ID(), null);
            MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());
            int counterVAF_Org_ID = bp.GetVAF_OrgBP_ID_Int();
            if (counterVAF_Org_ID == 0)
                return null;

            //jz MBPartner counterBP = new MBPartner (GetCtx(), counterVAB_BusinessPartner_ID, null);
            MVABBusinessPartner counterBP = new MVABBusinessPartner(GetCtx(), counterVAB_BusinessPartner_ID, Get_Trx());
            //	MOrgInfo counterOrgInfo = MOrgInfo.Get(GetCtx(), counterVAF_Org_ID);
            log.Info("Counter BP=" + counterBP.GetName());

            //	Document Type
            int VAB_DocTypesTarGet_ID = 0;
            MVABInterCompanyDoc counterDT = MVABInterCompanyDoc.GetCounterDocType(GetCtx(), GetVAB_DocTypes_ID());
            if (counterDT != null)
            {
                log.Fine(counterDT.ToString());
                if (!counterDT.IsCreateCounter() || !counterDT.IsValid())
                    return null;
                VAB_DocTypesTarGet_ID = counterDT.GetCounter_VAB_DocTypes_ID();
            }
            if (VAB_DocTypesTarGet_ID <= 0)
                return null;

            //	Deep Copy
            MVABPayment counter = new MVABPayment(GetCtx(), 0, Get_Trx());
            counter.SetVAF_Org_ID(counterVAF_Org_ID);
            counter.SetVAB_BusinessPartner_ID(counterBP.GetVAB_BusinessPartner_ID());
            counter.SetIsReceipt(!IsReceipt());
            counter.SetVAB_DocTypes_ID(VAB_DocTypesTarGet_ID);
            counter.SetTrxType(GetTrxType());
            counter.SetTenderType(GetTenderType());
            //
            counter.SetPayAmt(GetPayAmt());
            counter.SetDiscountAmt(GetDiscountAmt());
            counter.SetTaxAmt(GetTaxAmt());
            counter.SetWriteOffAmt(GetWriteOffAmt());
            counter.SetIsOverUnderPayment(IsOverUnderPayment());
            counter.SetOverUnderAmt(GetOverUnderAmt());
            counter.SetVAB_Currency_ID(GetVAB_Currency_ID());
            counter.SetVAB_CurrencyType_ID(GetVAB_CurrencyType_ID());
            //
            counter.SetDateTrx(GetDateTrx());
            counter.SetDateAcct(GetDateAcct());
            counter.SetRef_Payment_ID(GetVAB_Payment_ID());
            //
            String sql = "SELECT VAB_Bank_Acct_ID FROM VAB_Bank_Acct "
                + "WHERE VAB_Currency_ID=" + GetVAB_Currency_ID() + " AND VAF_Org_ID IN (0," + counterVAF_Org_ID + ") AND IsActive='Y' "
                + "ORDER BY IsDefault DESC";
            int VAB_Bank_Acct_ID = DataBase.DB.GetSQLValue(Get_Trx(), sql);
            counter.SetVAB_Bank_Acct_ID(VAB_Bank_Acct_ID);

            //	Refernces
            counter.SetVAB_BillingCode_ID(GetVAB_BillingCode_ID());
            counter.SetVAB_Promotion_ID(GetVAB_Promotion_ID());
            counter.SetVAB_Project_ID(GetVAB_Project_ID());
            counter.SetUser1_ID(GetUser1_ID());
            counter.SetUser2_ID(GetUser2_ID());
            counter.Save(Get_Trx());
            log.Fine(counter.ToString());
            SetRef_Payment_ID(counter.GetVAB_Payment_ID());

            //	Document Action
            if (counterDT != null)
            {
                if (counterDT.GetDocAction() != null)
                {
                    counter.SetDocAction(counterDT.GetDocAction());
                    counter.ProcessIt(counterDT.GetDocAction());
                    counter.Save(Get_Trx());
                }
            }
            return counter;
        }

        /// <summary>
        ///   	Allocate It.
        /// 	Only call when there is NO allocation as it will create duplicates.
        /// 	If an invoice exists, it allocates that 
        /// 	otherwise it allocates Payment Selection.
        /// </summary>
        /// <returns>true if allocated</returns>
        public bool AllocateIt()
        {
            //	Create invoice Allocation -	See also MCash.completeIt
            if (GetVAB_Invoice_ID() != 0)
            {
                if (Env.IsModuleInstalled("VA009_"))
                {
                    MVABInvoicePaySchedule paySch = new MVABInvoicePaySchedule(GetCtx(), GetVAB_sched_InvoicePayment_ID(), Get_Trx());
                    // Added by Bharat on 27 June 2017 to restrict multiple payment against same Invoice Pay Schedule.
                    //23 Ap 2018 By pass for POS Terminal Order
                    if (paySch.IsVA009_IsPaid() && DB.GetSQLValue(Get_Trx(), @"SELECT VAPOS_POSTerminal_ID FROM VAB_ORDER WHERE VAB_Order_ID = (
                                                                              SELECT VAB_Order_ID FROM VAB_Invoice WHERE VAB_Invoice_ID = " + GetVAB_Invoice_ID() + ")") < 1)
                    {
                        _AllocMsg = "Payment is already done for selected invoice Schedule";
                        return false;
                    }
                }
                return AllocateInvoice();
            }

            //	Create invoice Allocation for Payment incase of prepay Order
            if (GetVAB_Order_ID() != 0)
            {
                string orderType = Util.GetValueOfString(DB.ExecuteScalar("SELECT DocSubTypeSO FROM VAB_Order o INNER JOIN VAB_DocTypes dt ON o.VAB_DocTypesTarget_ID = dt.VAB_DocTypes_ID WHERE o.IsActive='Y' AND  VAB_Order_ID = " + GetVAB_Order_ID(), null, Get_Trx()));
                if (orderType.Equals(X_VAB_DocTypes.DOCSUBTYPESO_PrepayOrder))
                {
                    return AllocateOrder();
                }
            }

            //	Invoices of a AP Payment Selection
            if (AllocatePaySelection())
                return true;

            if (GetVAB_Order_ID() != 0)
                return false;

            //	Allocate to multiple Payments based on entry
            MVABPaymentAllocate[] pAllocs = MVABPaymentAllocate.Get(this);
            if (pAllocs.Length == 0)
                return false;

            MVABDocAllocation alloc = new MVABDocAllocation(GetCtx(), false,
                GetDateTrx(), GetVAB_Currency_ID(),
                    Msg.Translate(GetCtx(), "VAB_Payment_ID") + ": " + GetDocumentNo(),
                    Get_TrxName());
            alloc.SetVAF_Org_ID(GetVAF_Org_ID());
            // Update ConversionDate from payment to view allocation 
            if (alloc.Get_ColumnIndex("DateAcct") > 0)
            {
                alloc.SetConversionDate(GetDateAcct());
            }
            // Update conversion type from payment to view allocation (required for posting)
            if (alloc.Get_ColumnIndex("VAB_CurrencyType_ID") > 0)
            {
                alloc.SetVAB_CurrencyType_ID(GetVAB_CurrencyType_ID());
            }
            /** when trx date not matched with account date, then we have to set dateacct from payment record*/
            alloc.SetDateAcct(GetDateAcct());
            if (!alloc.Save())
            {
                log.Severe("P.Allocations not created");
                return false;
            }
            DataSet ds = null;
            DataRow[] dr = null;
            if (Get_ColumnIndex("BackupWithholding_ID") > 0 && (GetVAB_Withholding_ID() > 0 || GetBackupWithholding_ID() > 0))
            {
                int precision = MVABCurrency.GetStdPrecision(GetCtx(), GetVAB_Currency_ID());
                ds = DB.ExecuteDataset(@"SELECT VAB_PaymentAllotment.VAB_PaymentAllotment_ID,  
                            (SELECT ROUND((VAB_PaymentAllotment.amount * paypercentage)/100 , " + precision + @") as withholdingAmt 
                            FROM VAB_Withholding where VAB_Withholding_id = VAB_Payment.VAB_Withholding_ID) as withholdingAmt,
                            (SELECT ROUND((VAB_PaymentAllotment.amount * paypercentage)/100 , " + precision + @") as withholdingAmt 
                            FROM VAB_Withholding where VAB_Withholding_id = VAB_Payment.BackupWithholding_ID) as BackupwithholdingAmt
                            FROM VAB_PaymentAllotment INNER JOIN VAB_Payment ON VAB_Payment.VAB_Payment_ID =VAB_PaymentAllotment.VAB_Payment_ID 
                            WHERE VAB_PaymentAllotment.IsActive = 'Y' AND  VAB_PaymentAllotment.VAB_Payment_ID =" + GetVAB_Payment_ID(), null, Get_Trx());
            }

            //	Lines
            for (int i = 0; i < pAllocs.Length; i++)
            {
                MVABPaymentAllocate pa = pAllocs[i];

                if (Get_ColumnIndex("BackupWithholding_ID") > 0 && (GetVAB_Withholding_ID() > 0 || GetBackupWithholding_ID() > 0) && ds != null)
                {
                    dr = ds.Tables[0].Select("VAB_PAYMENTALLOTMENT_ID = " + Util.GetValueOfString(pa.GetVAB_PaymentAllotment_ID()));
                }

                MVABDocAllocationLine aLine = null;
                if (IsReceipt())
                {
                    aLine = new MVABDocAllocationLine(alloc, pa.GetAmount(),
                        pa.GetDiscountAmt(), pa.GetWriteOffAmt(), pa.GetOverUnderAmt());
                    if (dr != null)
                    {
                        aLine.SetWithholdingAmt(Util.GetValueOfDecimal(dr[0]["withholdingAmt"]));
                        aLine.SetBackupWithholdingAmount(Util.GetValueOfDecimal(dr[0]["BackupwithholdingAmt"]));
                    }
                }
                else
                {
                    aLine = new MVABDocAllocationLine(alloc, Decimal.Negate(pa.GetAmount()),
                        Decimal.Negate(pa.GetDiscountAmt()), Decimal.Negate(pa.GetWriteOffAmt()), Decimal.Negate(pa.GetOverUnderAmt()));
                    if (dr != null)
                    {
                        aLine.SetWithholdingAmt(Decimal.Negate(Util.GetValueOfDecimal(dr[0]["withholdingAmt"])));
                        aLine.SetBackupWithholdingAmount(Decimal.Negate(Util.GetValueOfDecimal(dr[0]["BackupwithholdingAmt"])));
                    }
                }
                aLine.SetDocInfo(pa.GetVAB_BusinessPartner_ID(), 0, pa.GetVAB_Invoice_ID());
                aLine.SetPaymentInfo(GetVAB_Payment_ID(), 0);
                if (Env.IsModuleInstalled("VA009_"))
                {
                    aLine.SetVAB_sched_InvoicePayment_ID(pa.GetVAB_sched_InvoicePayment_ID());
                }
                if (!aLine.Save(Get_TrxName()))
                {
                    log.Warning("P.Allocations - line not saved");
                }
                else
                {
                    pa.SetVAB_DocAllocationLine_ID(aLine.GetVAB_DocAllocationLine_ID());
                    pa.Save();
                }
            }
            //	Should start WF
            alloc.ProcessIt(DocActionVariables.ACTION_COMPLETE);
            _processMsg = "@VAB_DocAllocation_ID@: " + alloc.GetDocumentNo();
            return alloc.Save(Get_TrxName());
        }

        /// <summary>
        /// Allocate single AP/AR Invoice
        /// </summary>
        /// <returns>true if allocated</returns>
        private bool AllocateInvoice()
        {
            try
            {
                //	calculate actual allocation
                Decimal allocationAmt = GetPaymentAmount();			//	underpayment
                if (Env.Signum(GetOverUnderAmt()) < 0 && Env.Signum(GetPaymentAmount()) > 0)
                    allocationAmt = Decimal.Add(allocationAmt, GetOverUnderAmt());	//	overpayment (negative)

                MVABDocAllocation alloc = new MVABDocAllocation(GetCtx(), false,
                    GetDateTrx(), GetVAB_Currency_ID(),
                    Msg.Translate(GetCtx(), "VAB_Payment_ID") + ": " + GetDocumentNo() + " [1]", Get_TrxName());

                alloc.SetVAF_Org_ID(GetVAF_Org_ID());
                // Update conversion type from payment to view allocation (required for posting)
                if (alloc.Get_ColumnIndex("VAB_CurrencyType_ID") > 0)
                {
                    alloc.SetVAB_CurrencyType_ID(GetVAB_CurrencyType_ID());
                }
                /** when trx date not matched with account date, then we have to set dateacct from payment record*/
                alloc.SetDateAcct(GetDateAcct());
                if (!alloc.Save())
                {
                    log.Log(Level.SEVERE, "Could not create Allocation Hdr");
                    return false;
                }
                MVABDocAllocationLine aLine = null;
                if (IsReceipt())
                {
                    aLine = new MVABDocAllocationLine(alloc, allocationAmt,
                        GetDiscountAmt(), GetWriteOffAmt(), GetOverUnderAmt());
                    if (aLine.Get_ColumnIndex("WithholdingAmt") > 0)
                    {
                        aLine.SetBackupWithholdingAmount(GetBackupWithholdingAmount());
                        aLine.SetWithholdingAmt(GetWithholdingAmt());
                    }
                }
                else
                {
                    aLine = new MVABDocAllocationLine(alloc, Decimal.Negate(allocationAmt),
                        Decimal.Negate(GetDiscountAmt()), Decimal.Negate(GetWriteOffAmt()), Decimal.Negate(GetOverUnderAmt()));
                    if (aLine.Get_ColumnIndex("WithholdingAmt") > 0)
                    {
                        aLine.SetBackupWithholdingAmount(Decimal.Negate(GetBackupWithholdingAmount()));
                        aLine.SetWithholdingAmt(Decimal.Negate(GetWithholdingAmt()));
                    }
                }
                aLine.SetDocInfo(GetVAB_BusinessPartner_ID(), 0, GetVAB_Invoice_ID());
                aLine.SetVAB_Payment_ID(GetVAB_Payment_ID());
                if (Env.IsModuleInstalled("VA009_"))
                {
                    aLine.SetVAB_sched_InvoicePayment_ID(GetVAB_sched_InvoicePayment_ID());
                }
                if (!aLine.Save(Get_TrxName()))
                {
                    log.Log(Level.SEVERE, "Could not create Allocation Line");
                    return false;
                }
                //	Should start WF
                alloc.ProcessIt(DocActionVariables.ACTION_COMPLETE);
                // SI_0745 : if view allocation not created or completed the not to complete current Trasaction
                if (alloc.GetProcessMsg() != null)
                {
                    _AllocMsg = alloc.GetProcessMsg();
                    return false;
                }
                alloc.Save(Get_Trx());
                //_processMsg = "@VAB_DocAllocation_ID@: " + alloc.GetDocumentNo();
                _processMsg = " View @VAB_DocAllocation_ID@ created successfully with doc no: " + alloc.GetDocumentNo();
                //	Get Project from Invoice
                int VAB_Project_ID = DataBase.DB.GetSQLValue(Get_Trx(),
                    "SELECT MAX(VAB_Project_ID) FROM VAB_Invoice WHERE VAB_Invoice_ID=@param1", GetVAB_Invoice_ID());
                if (VAB_Project_ID > 0 && GetVAB_Project_ID() == 0)
                {
                    SetVAB_Project_ID(VAB_Project_ID);
                }
                else if (VAB_Project_ID > 0 && GetVAB_Project_ID() > 0 && VAB_Project_ID != GetVAB_Project_ID())
                {
                    log.Warning("Invoice VAB_Project_ID=" + VAB_Project_ID
                        + " <> Payment VAB_Project_ID=" + GetVAB_Project_ID());
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("MPayment-Error in AllocateInvoice");
                log.Severe(ex.ToString());
            }
            return true;
        }

        //Allocation for Payment incase of PrePay Order
        private bool AllocateOrder()
        {
            try
            {
                //	calculate actual allocation
                Decimal allocationAmt = GetPaymentAmount();			//	underpayment
                if (Env.Signum(GetOverUnderAmt()) < 0 && Env.Signum(GetPaymentAmount()) > 0)
                    allocationAmt = Decimal.Add(allocationAmt, GetOverUnderAmt());	//	overpayment (negative)

                MVABDocAllocation alloc = new MVABDocAllocation(GetCtx(), false,
                    GetDateTrx(), GetVAB_Currency_ID(),
                    Msg.Translate(GetCtx(), "VAB_Payment_ID") + ": " + GetDocumentNo() + " [1]", Get_TrxName());

                alloc.SetVAF_Org_ID(GetVAF_Org_ID());
                // Update conversion type from payment to view allocation (required for posting)
                if (alloc.Get_ColumnIndex("VAB_CurrencyType_ID") > 0)
                {
                    alloc.SetVAB_CurrencyType_ID(GetVAB_CurrencyType_ID());
                }
                /** when trx date not matched with account date, then we have to set dateacct from payment record*/
                alloc.SetDateAcct(GetDateAcct());
                if (!alloc.Save())
                {
                    //Could not create Allocation Hdr
                    log.Log(Level.SEVERE, Msg.GetMsg(GetCtx(), "CouldnotCrtAllocHdr"));
                    return false;
                }
                MVABDocAllocationLine aLine = null;
                if (IsReceipt())
                {
                    aLine = new MVABDocAllocationLine(alloc, allocationAmt,
                        GetDiscountAmt(), GetWriteOffAmt(), GetOverUnderAmt());
                    if (aLine.Get_ColumnIndex("WithholdingAmt") > 0)
                    {
                        aLine.SetBackupWithholdingAmount(GetBackupWithholdingAmount());
                        aLine.SetWithholdingAmt(GetWithholdingAmt());
                    }
                }
                aLine.SetDocInfo(GetVAB_BusinessPartner_ID(), GetVAB_Order_ID(), 0);
                aLine.SetVAB_Payment_ID(GetVAB_Payment_ID());
                //aLine.SetVAB_Order_ID(GetVAB_Order_ID());
                if (Env.IsModuleInstalled("VA009_"))
                {
                    DataSet _ds = DB.ExecuteDataset(@"SELECT pay.VAB_sched_InvoicePayment_ID, pay.VAB_Invoice_ID FROM VAB_sched_InvoicePayment pay INNER JOIN VAB_Invoice inv 
                                        ON pay.VAB_Invoice_ID=inv.VAB_Invoice_ID WHERE inv.VAB_Order_ID=" + GetVAB_Order_ID(), null, Get_Trx());
                    if (_ds != null && _ds.Tables.Count > 0 && _ds.Tables[0].Rows.Count > 0)
                    {
                        aLine.SetVAB_sched_InvoicePayment_ID(Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_sched_InvoicePayment_ID"]));
                        aLine.SetVAB_Invoice_ID(Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_Invoice_ID"]));
                    }
                    else
                    {
                        //not found invoice for this Order
                        _AllocMsg = Msg.GetMsg(GetCtx(), "NotfoundInv");
                        return false;
                    }
                }
                if (!aLine.Save(Get_TrxName()))
                {
                    log.Log(Level.SEVERE, Msg.GetMsg(GetCtx(), "CouldnotCrtAllocLine"));//Could not create Allocation Line
                    return false;
                }
                //	Should start WF
                alloc.ProcessIt(DocActionVariables.ACTION_COMPLETE);
                // if view allocation not created or completed the not to complete current Trasaction
                if (alloc.GetProcessMsg() != null)
                {
                    _AllocMsg = alloc.GetProcessMsg();
                    return false;
                }
                alloc.Save(Get_Trx());
                //_processMsg = "@VAB_DocAllocation_ID@: " + alloc.GetDocumentNo();
                //" View @VAB_DocAllocation_ID@ created successfully with doc no: "
                _processMsg = Msg.GetMsg(GetCtx(), "SucessflyCrtAlloc") + alloc.GetDocumentNo();
                //	Get Project from Invoice
                int VAB_Project_ID = DataBase.DB.GetSQLValue(Get_Trx(),
                    "SELECT MAX(VAB_Project_ID) FROM VAB_Order WHERE VAB_Order_ID=@param1", GetVAB_Order_ID());
                if (VAB_Project_ID > 0 && GetVAB_Project_ID() == 0)
                {
                    SetVAB_Project_ID(VAB_Project_ID);
                }
                else if (VAB_Project_ID > 0 && GetVAB_Project_ID() > 0 && VAB_Project_ID != GetVAB_Project_ID())
                {
                    log.Warning("Order VAB_Project_ID=" + VAB_Project_ID
                        + " <> Payment VAB_Project_ID=" + GetVAB_Project_ID());
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("MPayment-Error in AllocateInvoice");
                log.Severe(ex.ToString());
            }
            return true;
        }

        /// <summary>
        /// Allocate Payment Selection
        /// </summary>
        /// <returns>return true if allocated</returns>
        private Boolean AllocatePaySelection()
        {
            MVABDocAllocation alloc = new MVABDocAllocation(GetCtx(), false,
                GetDateTrx(), GetVAB_Currency_ID(),
                Msg.Translate(GetCtx(), "VAB_Payment_ID") + ": " + GetDocumentNo() + " [n]", Get_Trx());
            alloc.SetVAF_Org_ID(GetVAF_Org_ID());
            // Update conversion type from payment to view allocation (required for posting)
            if (alloc.Get_ColumnIndex("VAB_CurrencyType_ID") > 0)
            {
                alloc.SetVAB_CurrencyType_ID(GetVAB_CurrencyType_ID());
            }
            /** when trx date not matched with account date, then we have to set dateacct from payment record*/
            alloc.SetDateAcct(GetDateAcct());
            String sql = "SELECT psc.VAB_BusinessPartner_ID, psl.VAB_Invoice_ID, psl.IsSOTrx, "	//	1..3
                + " psl.PayAmt, psl.DiscountAmt, psl.DifferenceAmt, psl.OpenAmt ";
            if (Env.IsModuleInstalled("VA009_"))
            {
                sql += " , psl.VAB_sched_InvoicePayment_ID ";
            }
            sql += " FROM VAB_PaymentOptionLine psl"
              + " INNER JOIN VAB_PaymentOptionCheck psc ON (psl.VAB_PaymentOptionCheck_ID=psc.VAB_PaymentOptionCheck_ID) "
              + "WHERE psc.VAB_Payment_ID=" + GetVAB_Payment_ID();
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_Trx());
                while (idr.Read())
                {
                    int VAB_BusinessPartner_ID = Utility.Util.GetValueOfInt(idr[0].ToString());
                    int VAB_Invoice_ID = Utility.Util.GetValueOfInt(idr[1].ToString());
                    if (VAB_BusinessPartner_ID == 0 && VAB_Invoice_ID == 0)
                        continue;
                    Boolean isSOTrx = "Y".Equals(idr[2].ToString());
                    Decimal payAmt = Utility.Util.GetValueOfDecimal(idr[3]);
                    Decimal discountAmt = Utility.Util.GetValueOfDecimal(idr[4]);
                    Decimal writeOffAmt = Utility.Util.GetValueOfDecimal(idr[5]);
                    Decimal openAmt = Utility.Util.GetValueOfDecimal(idr[6]);
                    Decimal overUnderAmt = 0;
                    if (Env.IsModuleInstalled("VA009_"))
                    {
                        if (Utility.Util.GetValueOfDecimal(idr[5]) < 0)
                        {
                            overUnderAmt = Utility.Util.GetValueOfDecimal(idr[5]);
                            writeOffAmt = decimal.Zero;
                        }
                        else
                        {
                            writeOffAmt = Utility.Util.GetValueOfDecimal(idr[5]);
                            overUnderAmt = decimal.Zero;
                        }

                    }
                    else
                    {
                        overUnderAmt = Decimal.Subtract(Decimal.Subtract(Decimal.Add(openAmt, payAmt),
                           discountAmt), writeOffAmt);
                    }
                    //
                    if (alloc.Get_ID() == 0 && !alloc.Save(Get_Trx()))
                    {
                        log.Log(Level.SEVERE, "Could not create Allocation Hdr");
                        idr.Close();
                        return false;
                    }
                    MVABDocAllocationLine aLine = null;
                    if (isSOTrx)
                        aLine = new MVABDocAllocationLine(alloc, payAmt,
                            discountAmt, writeOffAmt, overUnderAmt);
                    else
                        aLine = new MVABDocAllocationLine(alloc, Decimal.Negate(payAmt),
                            Decimal.Negate(discountAmt), Decimal.Negate(writeOffAmt), Decimal.Negate(overUnderAmt));
                    aLine.SetDocInfo(VAB_BusinessPartner_ID, 0, VAB_Invoice_ID);
                    if (Env.IsModuleInstalled("VA009_"))
                    {
                        aLine.SetVAB_sched_InvoicePayment_ID(Utility.Util.GetValueOfInt(idr[7]));
                    }
                    aLine.SetVAB_Payment_ID(GetVAB_Payment_ID());
                    if (!aLine.Save(Get_Trx()))
                    {
                        log.Log(Level.SEVERE, "Could not create Allocation Line");
                    }
                }
                idr.Close();
                idr = null;
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, "allocatePaySelection", e);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                }
                idr = null;
            }


            //	Should start WF
            Boolean ok = true;
            if (alloc.Get_ID() == 0)
            {
                log.Fine("No Allocation created - VAB_Payment_ID="
                   + GetVAB_Payment_ID());
                ok = false;
            }
            else
            {
                alloc.ProcessIt(DocActionVariables.ACTION_COMPLETE);
                ok = alloc.Save(Get_Trx());
                _processMsg = "@VAB_DocAllocation_ID@: " + alloc.GetDocumentNo();
            }
            return ok;
        }

        /**
         * 	De-allocate Payment.
         * 	Unkink Invoices and Orders and delete Allocations
         */
        private void DeAllocate()
        {
            if (GetVAB_Order_ID() != 0)
                SetVAB_Order_ID(0);
            //	if (GetVAB_Invoice_ID() == 0)
            //		return;
            //	De-Allocate all 
            MVABDocAllocation[] allocations = MVABDocAllocation.GetOfPayment(GetCtx(),
                GetVAB_Payment_ID(), Get_Trx());
            log.Fine("#" + allocations.Length);
            for (int i = 0; i < allocations.Length; i++)
            {
                allocations[i].Set_TrxName(Get_Trx());
                allocations[i].SetDocAction(DocActionVariables.ACTION_REVERSE_CORRECT);
                allocations[i].ProcessIt(DocActionVariables.ACTION_REVERSE_CORRECT);
                allocations[i].Save();
            }

            // 	Unlink (in case allocation did not Get it)
            if (GetVAB_Invoice_ID() != 0)
            {
                //	Invoice					
                String sql = "UPDATE VAB_Invoice "
                    + "SET VAB_Payment_ID = NULL, IsPaid='N' "
                    + "WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID()
                    + " AND VAB_Payment_ID=" + GetVAB_Payment_ID();
                int no = DataBase.DB.ExecuteQuery(sql, null, Get_Trx());
                if (no != 0)
                {
                    log.Fine("Unlink Invoice #" + no);
                }
                //	Order
                sql = "UPDATE VAB_Order o "
                    + "SET VAB_Payment_ID = NULL "
                    + "WHERE EXISTS (SELECT * FROM VAB_Invoice i "
                        + "WHERE o.VAB_Order_ID=i.VAB_Order_ID AND i.VAB_Invoice_ID=" + GetVAB_Invoice_ID() + ")"
                    + " AND VAB_Payment_ID=" + GetVAB_Payment_ID();
                no = DataBase.DB.ExecuteQuery(sql, null, Get_Trx());
                if (no != 0)
                {
                    log.Fine("Unlink Order #" + no);
                }
            }
            //
            SetVAB_Invoice_ID(0);
            SetIsAllocated(false);
        }

        /**
         * 	Void Document.
         * 	@return true if success 
         */
        bool ChekVoidIt = false;
        public Boolean VoidIt()
        {

            // if (GetCostAllocationID() == 0 || GetCostAlloactionDocStatus().Equals("CO"))
            //  {
            ChekVoidIt = true;
            log.Info(ToString());

            if (DOCSTATUS_Closed.Equals(GetDocStatus())
                || DOCSTATUS_Reversed.Equals(GetDocStatus())
                || DOCSTATUS_Voided.Equals(GetDocStatus()))
            {
                _processMsg = "Document Closed: " + GetDocStatus();
                SetDocAction(DOCACTION_None);
                return false;
            }
            //	If on Bank Statement, don't void it - reverse it
            if (GetVAB_BankingJRNLLine_ID() > 0)
                return ReverseCorrectIt();

            //	Not Processed
            if (DOCSTATUS_Drafted.Equals(GetDocStatus())
                || DOCSTATUS_Invalid.Equals(GetDocStatus())
                || DOCSTATUS_InProgress.Equals(GetDocStatus())
                || DOCSTATUS_Approved.Equals(GetDocStatus())
                || DOCSTATUS_NotApproved.Equals(GetDocStatus()))
            {
                AddDescription(Msg.GetMsg(GetCtx(), "Voided") + " (" + GetPayAmt() + ")");
                SetPayAmt(Env.ZERO);
                SetDiscountAmt(Env.ZERO);
                SetWriteOffAmt(Env.ZERO);
                SetOverUnderAmt(Env.ZERO);
                SetIsAllocated(false);

                //If Payment have VA026_TRLoanApplication_ID is present at that time Update VAB_Payment_ID as null in 
                //VA026_TRLoanApplication window.
                if (Env.IsModuleInstalled("VA026_"))
                {
                    // de allocate link from TR Loan Application when TR Loan Application ID > 0
                    if (Get_ValueAsInt("VA026_TRLoanApplication_ID") > 0)
                    {
                        DB.ExecuteQuery("UPDATE VA026_TRLoanApplication SET  VAB_Payment_ID = null WHERE VAB_Payment_ID = " + GetVAB_Payment_ID() +
                                        @" AND VA026_TRLoanApplication_ID = " + Get_ValueAsInt("VA026_TRLoanApplication_ID"), null, null);
                    }
                }

                //	Unlink & De-Allocate
                DeAllocate();
            }
            else
                return ReverseCorrectIt();

            //{
            //    if (ReverseCorrectIt())
            //    {
            //        //----------------Neha---------

            //        if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='VA027_'  AND IsActive = 'Y'")) > 0)
            //        {
            //            if (GetVA027_PostDatedCheck_ID() > 0)
            //            {
            //                if (GetDocStatus() == "RE")
            //                {
            //                    if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VAB_Payment_ID=" + GetVAB_Payment_ID())) > 0)
            //                    {
            //                        DB.ExecuteQuery("UPDATE VA027_ChequeDetails Set VA027_PaymentStatus='3' Where VAB_Payment_ID=" + GetVAB_Payment_ID());
            //                        int count = Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID()));
            //                        if (count > 0)
            //                        {
            //                            if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID() + " And VA027_PAYMENTSTATUS = '3'")) == count)
            //                            {
            //                                DB.ExecuteQuery("UPDATE VA027_PostDatedCheck Set VA027_PaymentStatus='3' where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID());
            //                            }
            //                        }
            //                    }
            //                    else
            //                        DB.ExecuteQuery("UPDATE VA027_PostDatedCheck Set VA027_PaymentStatus='3' where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID());

            //                }
            //            }

            //        }
            //        return true;
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}

            //
            SetProcessed(true);
            SetDocAction(DOCACTION_None);

            return true;
            //  }
            //  else
            //  {
            //      _processMsg = "Cost Allocation not Completed.Could not reverse Payment";
            //       return false;
            //   }
        }

        /**
         * 	Close Document.
         * 	@return true if success 
         */
        public Boolean CloseIt()
        {
            log.Info(ToString());
            SetDocAction(DOCACTION_None);
            return true;
        }

        /// <summary>
        /// Reverse Correction
        /// @return true if success
        /// </summary>
        /// <returns></returns>

        public Boolean ReverseCorrectIt()
        {
            log.Info(ToString());

            // (JID_1472) To check payment is reconciled or not
            if (IsReconciled())
            {
                _processMsg = Msg.GetMsg(GetCtx(), "PaymentAlreadyReconciled");
                return false;
            }

            // JID_1276
            if (GetVAB_Order_ID() > 0 && GetVA009_OrderPaySchedule_ID() > 0)
            {
                if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(VAB_OrderLine_ID) FROM VAB_OrderLine 
                                    WHERE (QtyInvoiced != 0 OR QtyDelivered != 0) AND VAB_Order_ID = " + GetVAB_Order_ID(), null, Get_Trx())) > 0)
                {
                    _processMsg = Msg.GetMsg(GetCtx(), "InvoiceOrGrnAlreadyCreated");
                    return false;
                }
            }

            //	Std Period open?
            DateTime? dateAcct = GetDateAcct();
            if (!MPeriod.IsOpen(GetCtx(), dateAcct,
                IsReceipt() ? MVABMasterDocType.DOCBASETYPE_ARRECEIPT : MVABMasterDocType.DOCBASETYPE_APPAYMENT, GetVAF_Org_ID()))
                dateAcct = DateTime.Now;

            //	Auto Reconcile if not on Bank Statement
            Boolean reconciled = false; //	GetVAB_BankingJRNLLine_ID() == 0;

            // if Payment aginst PDC is voided remove reference of payment from chequedetials and set Payment Generated to false on Header
            if (Env.IsModuleInstalled("VA027_") && GetVA027_PostDatedCheck_ID() > 0)
            {

                int count = Util.GetValueOfInt(DB.ExecuteQuery("UPDATE VA027_PostDatedCheck SET VA027_GeneratePayment ='N', VA027_PaymentGenerated ='N', VAB_Payment_ID = NULL, VA027_PaymentStatus= '0' WHERE VA027_PostDatedCheck_ID= " + GetVA027_PostDatedCheck_ID(), null, Get_Trx()));
                if (count > 0)
                {
                    DB.ExecuteQuery("UPDATE VA027_ChequeDetails SET VAB_Payment_ID = NULL, VA027_PaymentStatus= '0' WHERE VA027_PostDatedCheck_ID= " + GetVA027_PostDatedCheck_ID() + "AND VAB_Payment_ID= " + GetVAB_Payment_ID(), null, Get_Trx());

                }
            }
            //	Create Reversal
            MVABPayment reversal = new MVABPayment(GetCtx(), 0, Get_Trx());
            CopyValues(this, reversal);
            reversal.SetClientOrg(this);
            reversal.SetVAB_Order_ID(0);
            reversal.SetVAB_Invoice_ID(0);
            reversal.SetDateAcct(dateAcct);
            //
            reversal.SetDocumentNo(GetDocumentNo() + REVERSE_INDICATOR);	//	indicate reversals
            reversal.SetDocStatus(DOCSTATUS_Drafted);
            reversal.SetDocAction(DOCACTION_Complete);
            //
            reversal.SetPayAmt(Decimal.Negate(GetPayAmt()));
            reversal.SetDiscountAmt(Decimal.Negate(GetDiscountAmt()));
            reversal.SetWriteOffAmt(Decimal.Negate(GetWriteOffAmt()));
            reversal.SetOverUnderAmt(Decimal.Negate(GetOverUnderAmt()));
            //Remove PDC reference
            if (Env.IsModuleInstalled("VA027_"))
            {
                reversal.SetVA027_PostDatedCheck_ID(0);
            }

            //negate witholding amount - 
            if (Get_ColumnIndex("WithholdingAmt") > 0)
            {
                reversal.SetVAB_Withholding_ID(GetVAB_Withholding_ID());
                reversal.SetWithholdingAmt(Decimal.Negate(GetWithholdingAmt()));
                reversal.SetBackupWithholding_ID(GetBackupWithholding_ID());
                reversal.SetBackupWithholdingAmount(Decimal.Negate(GetBackupWithholdingAmount()));
                reversal.SetPaymentAmount(Decimal.Negate(GetPaymentAmount()));
            }
            //
            // make unique record based on Bank and cheque no - thats why on reversing a record - place reverse indicator on it.
            if (!string.IsNullOrEmpty(reversal.GetCheckNo()))
                reversal.SetCheckNo(reversal.GetCheckNo() + REVERSE_INDICATOR);
            // 
            // during creation of counter document, Payment Execution Status should be "In-Progress"
            if (Env.IsModuleInstalled("VA009_"))
                reversal.SetVA009_ExecutionStatus("I");
            //
            reversal.SetIsAllocated(true);
            // Reconcile true on Original and Reverse Payment if Reconcile is False
            if (!IsReconciled())
            {
                reconciled = true;
            }
            reversal.SetIsReconciled(reconciled);	//	to put on bank statement
            reversal.SetIsOnline(false);
            reversal.SetIsApproved(true);
            reversal.SetR_PnRef(null);
            reversal.SetR_Result(null);
            reversal.SetR_RespMsg(null);
            reversal.SetR_AuthCode(null);
            reversal.SetR_Info(null);
            reversal.SetProcessing(false);
            reversal.SetOProcessing("N");
            reversal.SetProcessed(false);
            reversal.SetPosted(false);
            reversal.SetDescription(GetDescription());
            reversal.AddDescription("{->" + GetDocumentNo() + ")");

            // Set Reversal value to true and add Reference of Original Document.
            if (reversal.Get_ColumnIndex("ReversalDoc_ID") > 0)
            {
                reversal.SetReversalDoc_ID(GetVAB_Payment_ID());
            }
            if (reversal.Get_ColumnIndex("IsReversal") > 0)
            {
                reversal.SetIsReversal(true);
            }

            // for reversal document set Temp Document No to empty
            if (reversal.Get_ColumnIndex("TempDocumentNo") > 0)
            {
                reversal.SetTempDocumentNo("");
            }

            if (!reversal.Save(Get_Trx()))
            {
                ValueNamePair pp = VLogger.RetrieveError();
                if (pp != null)
                {
                    _processMsg = pp.GetName();
                    if (String.IsNullOrEmpty(_processMsg))
                    {
                        _processMsg = pp.GetValue();
                    }
                }
                return false;
            }

            int invoice_ID = 0;
            if (Env.IsModuleInstalled("VA009_"))
            {
                string sql = "SELECT DISTINCT VAB_PaymentAllotment_ID FROM VAB_PaymentAllotment WHERE IsActive = 'Y' AND  NVL(VAB_Invoice_ID , 0) <> 0 AND VAB_Payment_ID =  " + GetVAB_Payment_ID();
                DataSet ds = new DataSet();
                ds = DB.ExecuteDataset(sql, null, Get_Trx());
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        MVABPaymentAllocate originalPaymentAllocate = new MVABPaymentAllocate(GetCtx(), Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAB_PaymentAllotment_ID"]), Get_Trx());
                        MVABPaymentAllocate reversalPaymentAllocate = new MVABPaymentAllocate(GetCtx(), 0, Get_Trx());
                        reversalPaymentAllocate.SetVAF_Client_ID(originalPaymentAllocate.GetVAF_Client_ID());
                        reversalPaymentAllocate.SetVAF_Org_ID(originalPaymentAllocate.GetVAF_Org_ID());
                        reversalPaymentAllocate.SetVAB_Payment_ID(reversal.GetVAB_Payment_ID());
                        reversalPaymentAllocate.SetVAB_Invoice_ID(originalPaymentAllocate.GetVAB_Invoice_ID());
                        reversalPaymentAllocate.SetVAB_sched_InvoicePayment_ID(originalPaymentAllocate.GetVAB_sched_InvoicePayment_ID());
                        reversalPaymentAllocate.SetInvoiceAmt(originalPaymentAllocate.GetInvoiceAmt());
                        reversalPaymentAllocate.SetAmount(Decimal.Negate(originalPaymentAllocate.GetAmount()));
                        reversalPaymentAllocate.SetDiscountAmt(Decimal.Negate(originalPaymentAllocate.GetDiscountAmt()));
                        reversalPaymentAllocate.SetWriteOffAmt(Decimal.Negate(originalPaymentAllocate.GetWriteOffAmt()));
                        reversalPaymentAllocate.SetOverUnderAmt(Decimal.Negate(originalPaymentAllocate.GetOverUnderAmt()));

                        // Set Reference of Original Document.
                        if (reversalPaymentAllocate.Get_ColumnIndex("ReversalDoc_ID") > 0)
                        {
                            reversalPaymentAllocate.SetReversalDoc_ID(Util.GetValueOfInt(ds.Tables[0].Rows[i]["VAB_PaymentAllotment_ID"]));
                        }
                        reversalPaymentAllocate.Save(Get_Trx());
                        invoice_ID = originalPaymentAllocate.GetVAB_Invoice_ID();
                    }
                }
            }

            //	Post Reversal
            if (!reversal.ProcessIt(DocActionVariables.ACTION_COMPLETE))
            {
                _processMsg = "Reversal ERROR: " + reversal.GetProcessMsg();
                return false;
            }
            reversal.CloseIt();
            reversal.SetDocStatus(DOCSTATUS_Reversed);
            reversal.SetDocAction(DOCACTION_None);
            // JID_1363: the allocation is overwritten we need to set Allocated Is TRUE while reversal and Void
            reversal.SetIsAllocated(true);
            reversal.Save(Get_Trx());

            if (Env.IsModuleInstalled("VA009_"))
            {
                //update Payment Batch line set payment = null during reverse of this payment
                string sql = "UPDATE va009_batchlines SET  VAB_Payment_ID = null WHERE VAB_Payment_ID = " + GetVAB_Payment_ID();
                DB.ExecuteQuery(sql, null, null);

                //update Payment Batch line Details set payment = null during reverse of this payment
                sql = "UPDATE va009_batchlinedetails SET  VAB_Payment_ID = null WHERE VAB_Payment_ID = " + GetVAB_Payment_ID();
                DB.ExecuteQuery(sql, null, null);
            }

            if (Env.IsModuleInstalled("VA026_"))
            {
                // de allocate link from TR Loan Application when TR Loan Application ID > 0
                if (Get_ValueAsInt("VA026_TRLoanApplication_ID") > 0)
                {
                    DB.ExecuteQuery("UPDATE VA026_TRLoanApplication SET  VAB_Payment_ID = null WHERE VAB_Payment_ID = " + GetVAB_Payment_ID() +
                                    @" AND VA026_TRLoanApplication_ID = " + Get_ValueAsInt("VA026_TRLoanApplication_ID"), null, null);
                }
            }

            int order_ID = GetVAB_Order_ID();
            if (invoice_ID == 0)
                invoice_ID = GetVAB_Invoice_ID();

            // Get all schedule which are paid against this payment, so that after deAllocate we can set Payment Execution status as Rejected on respective schedules
            string sql1 = @"SELECT VAB_sched_InvoicePayment_ID FROM VAB_DocAllocation ahr 
                           INNER JOIN VAB_DocAllocationLine aline ON ahr.VAB_DocAllocation_ID=aline.VAB_DocAllocation_ID 
                            WHERE ahr.IsActive = 'Y' AND aline.IsActive = 'Y' AND ahr.docstatus in ('CO','CL') AND aline.VAB_Payment_ID = " + GetVAB_Payment_ID();
            DataSet dsSchedule = DB.ExecuteDataset(sql1, null, Get_Trx());

            //	Unlink & De-Allocate
            DeAllocate();
            SetIsReconciled(reconciled);
            SetIsAllocated(true);	//	the allocation below is overwritten
            //	Set Status 
            AddDescription("(" + reversal.GetDocumentNo() + "<-)");
            SetDocStatus(DOCSTATUS_Reversed);
            SetDocAction(DOCACTION_None);
            SetProcessed(true);
            //Remove PostdatedCheck Reference
            if (Env.IsModuleInstalled("VA027_") && GetVA027_PostDatedCheck_ID() > 0)
            {
                SetVA027_PostDatedCheck_ID(0);
            }

            // new change when allocation exist for already made payments then it will create allocation 
            // otherwise it will not create allocation against that payment
            // Changes done by Vivek Kumar on 26/06/2017 assigned by Mandeep sir
            //	Create automatic Allocation

            //JID_0889: show on void full message Reversal Document created
            StringBuilder Info = new StringBuilder(Msg.GetMsg(GetCtx(), "VIS_DocumentReversed") + reversal.GetDocumentNo());
            int Alline_ID = Util.GetValueOfInt(DB.ExecuteScalar("SELECT al.VAB_DocAllocationLine_id FROM VAB_DocAllocationLine al "
                                                                + " INNER JOIN VAB_DocAllocation alhdr ON alhdr.VAB_DocAllocation_ID=al.VAB_DocAllocation_ID WHERE al.VAB_Payment_ID         =" + GetVAB_Payment_ID()
                                                                + " and alhdr.docstatus in ('CO','CL')"));
            MVABDocAllocation alloc = null;
            if (Alline_ID > 0)
            {
                alloc = new MVABDocAllocation(GetCtx(), false,
                GetDateTrx(), GetVAB_Currency_ID(),
                Msg.Translate(GetCtx(), "VAB_Payment_ID") + ": " + reversal.GetDocumentNo(), Get_Trx());
                alloc.SetVAF_Org_ID(GetVAF_Org_ID());
                // Update conversion type from payment to view allocation (required for posting)
                if (alloc.Get_ColumnIndex("VAB_CurrencyType_ID") > 0)
                {
                    alloc.SetVAB_CurrencyType_ID(GetVAB_CurrencyType_ID());
                }
                /** when trx date not matched with account date, then we have to set dateacct from payment record*/
                alloc.SetDateAcct(GetDateAcct());

                if (!alloc.Save())
                {
                    log.Warning("Automatic allocation - hdr not saved");
                }
                else
                {
                    //	Original Allocation
                    MVABDocAllocationLine aLine = new MVABDocAllocationLine(alloc, GetPayAmt(true) -
                        (Get_ColumnIndex("BackupWithholdingAmount") >= 0 ? (GetWithholdingAmt() + GetBackupWithholdingAmount()) : 0),
                        Env.ZERO, Env.ZERO, Env.ZERO);
                    aLine.SetDocInfo(GetVAB_BusinessPartner_ID(), 0, 0);
                    aLine.SetPaymentInfo(GetVAB_Payment_ID(), 0);
                    if (!aLine.Save(Get_Trx()))
                    {
                        log.Warning("Automatic allocation - line not saved");
                    }
                    //	Reversal Allocation
                    aLine = new MVABDocAllocationLine(alloc, reversal.GetPayAmt(true) +
                        (Get_ColumnIndex("BackupWithholdingAmount") >= 0 ? (GetWithholdingAmt() + GetBackupWithholdingAmount()) : 0),
                        Env.ZERO, Env.ZERO, Env.ZERO);
                    aLine.SetDocInfo(reversal.GetVAB_BusinessPartner_ID(), 0, 0);
                    aLine.SetPaymentInfo(reversal.GetVAB_Payment_ID(), 0);
                    if (!aLine.Save(Get_Trx()))
                    {
                        log.Warning("Automatic allocation - reversal line not saved");
                    }
                }
                alloc.ProcessIt(DocActionVariables.ACTION_COMPLETE);
                alloc.Save(Get_Trx());
                Info.Append(" - @VAB_DocAllocation_ID@: ").Append(alloc.GetDocumentNo());
            }


            // change by Amit 2-6-2016 // Letter Of Credit module
            if (Env.IsModuleInstalled("VA026_"))
            {
                if (order_ID != 0 && invoice_ID != 0)
                {
                    if (!UpdateRealizedAmount(order_ID, invoice_ID))
                    {
                        log.Warning("Realized amount not updated");
                    }
                }
            }

            #region Code Commented open balanace is already updating while completion of reversal of same payment document
            // done by Vivek on 24/11/2017
            //	Update BPartner
            //if (GetVAB_BusinessPartner_ID() != 0)
            //{
            //    // nnayak - update BP open balance and credit used
            //    MBPartner bp = new MBPartner(GetCtx(), GetVAB_BusinessPartner_ID(), Get_Trx());
            //    Decimal newCreditAmt = 0;
            //    if (bp.GetCreditStatusSettingOn() == "CH")
            //    {
            //        Decimal? payAmt = MConversionRate.ConvertBase(GetCtx(), Decimal.Add(Decimal.Add(GetPayAmt(false), GetDiscountAmt()), GetWriteOffAmt()),	//	CM adjusted 
            //            GetVAB_Currency_ID(), GetDateAcct(), 0, GetVAF_Client_ID(), GetVAF_Org_ID());
            //        if (payAmt == null)
            //        {
            //            _processMsg = "Could not convert VAB_Currency_ID=" + GetVAB_Currency_ID()
            //                + " to base VAB_Currency_ID=" + MClient.Get(GetCtx()).GetVAB_Currency_ID();
            //            return false;
            //        }
            //        //	Total Balance
            //        Decimal newBalance = bp.GetTotalOpenBalance(false);
            //        //if (newBalance == null)
            //        //    newBalance = Env.ZERO;
            //        if (IsReceipt())
            //        {
            //            newBalance = Decimal.Add(newBalance, (Decimal)payAmt);

            //            newCreditAmt = bp.GetSO_CreditUsed();
            //            //if (newCreditAmt == null)
            //            //    newCreditAmt = (Decimal)payAmt;
            //            //else
            //            newCreditAmt = Decimal.Add(newCreditAmt, (Decimal)payAmt);
            //            //
            //            log.Fine("TotalOpenBalance=" + bp.GetTotalOpenBalance(false) + "(" + payAmt
            //                + ", Credit=" + bp.GetSO_CreditUsed() + "->" + newCreditAmt
            //                + ", Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);
            //            bp.SetSO_CreditUsed(newCreditAmt);
            //        }	//	SO
            //        else
            //        {
            //            newBalance = Decimal.Add(newBalance, (Decimal)payAmt);
            //            log.Fine("Payment Amount =" + GetPayAmt(false) + "(" + payAmt
            //                + ") Balance=" + bp.GetTotalOpenBalance(false) + " -> " + newBalance);
            //        }
            //        bp.SetTotalOpenBalance(newBalance);
            //        //if (bp.GetSO_CreditLimit() > 0)
            //        //{
            //        //    if (((newCreditAmt / bp.GetSO_CreditLimit()) * 100) >= 90)
            //        //    {
            //        //        bp.SetSOCreditStatus("S");
            //        //    }
            //        //}
            //        if (!bp.Save(Get_Trx()))
            //        {
            //            _processMsg = "Could not update Business Partner";
            //            return false;
            //        }

            //        bp.Save(Get_Trx());
            //    }

            //}
            #endregion

            //---------------Anuj----------------------
            if (Env.IsModuleInstalled("VA009_"))
            {
                if (GetVAB_sched_InvoicePayment_ID() > 0)
                {
                    MVABInvoicePaySchedule paySch = new MVABInvoicePaySchedule(GetCtx(), GetVAB_sched_InvoicePayment_ID(), Get_Trx());
                    paySch.SetVA009_ExecutionStatus("C");
                    paySch.SetVA009_IsPaid(false);
                    if (!paySch.Save())
                        log.SaveInfo("Not Saved", "");
                }

                if (GetVAB_Invoice_ID() > 0)
                {
                    MVABInvoice inv = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_Trx());
                    inv.SetIsPaid(false);
                    if (!inv.Save())
                        log.SaveInfo("Not Saved", "");
                }

                // update execution status as Rejecetd when we allocate payment through patyment allocation form or schedule are selected on payment allocate tab
                if (GetVAB_sched_InvoicePayment_ID() <= 0 && GetVAB_Invoice_ID() <= 0 && dsSchedule != null && dsSchedule.Tables.Count > 0 && dsSchedule.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < dsSchedule.Tables[0].Rows.Count; i++)
                    {
                        MVABInvoicePaySchedule paySch = new MVABInvoicePaySchedule(GetCtx(), Util.GetValueOfInt(dsSchedule.Tables[0].Rows[i]["VAB_sched_InvoicePayment_ID"]), Get_Trx());
                        paySch.SetVA009_ExecutionStatus("C");
                        paySch.SetVA009_IsPaid(false);
                        if (!paySch.Save())
                            log.SaveInfo("Not Saved Execution status on Invoice schedule : " + paySch.GetVAB_sched_InvoicePayment_ID(), "");
                    }
                }
            }
            //---------------Anuj----------------------

            if (Env.IsModuleInstalled("VA027_"))
            {
                if (GetVA027_PostDatedCheck_ID() > 0)
                {
                    if (GetDocStatus() == "RE" || GetDocStatus() == "VO")
                    {
                        if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VAB_Payment_ID=" + GetVAB_Payment_ID())) > 0)
                        {
                            DB.ExecuteQuery("UPDATE VA027_ChequeDetails Set VA027_PaymentStatus='3' Where VAB_Payment_ID=" + GetVAB_Payment_ID());
                            int count = Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID()));
                            if (count > 0)
                            {
                                if (Util.GetValueOfInt(DB.ExecuteScalar("Select Count(*) From VA027_ChequeDetails Where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID() + " And VA027_PAYMENTSTATUS = '3'")) == count)
                                {
                                    DB.ExecuteQuery("UPDATE VA027_PostDatedCheck Set VA027_PaymentStatus='3' where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID());
                                }
                            }
                        }
                        else
                            DB.ExecuteQuery("UPDATE VA027_PostDatedCheck Set VA027_PaymentStatus='3' where VA027_PostDatedCheck_ID=" + GetVA027_PostDatedCheck_ID());

                    }
                }

            }

            //if (GetCostAllocationID() != 0)
            //{
            //    MVAMProductCostAllocation objMVAMProductCost = new MVAMProductCostAllocation(GetCtx(), GetCostAllocationID(), Get_Trx());
            //    objMVAMProductCost.ReverseCorrectIt();
            //}

            //Arpit--update UnMatched Balance In Case of Invoice Or Order Or Indepent Payment
            //if (!UpdateUnMatchedBalanceForAccount(true))
            //{
            //    return false;
            //}
            //
            _processMsg = Info.ToString();
            return true;
            //}
            // else
            // {
            //     _processMsg = "Cost Allocation not Completed.Could not reverse Payment";
            //      return false;
            //  }
        }

        /**
         * 	Get Bank Statement Line of payment or 0
         *	@return id or 0
         */
        private int GetVAB_BankingJRNLLine_ID()
        {
            String sql = "SELECT VAB_BankingJRNLLine_ID FROM VAB_BankingJRNLLine WHERE VAB_Payment_ID=" + GetVAB_Payment_ID();
            int id = DataBase.DB.GetSQLValue(Get_Trx(), sql);
            if (id < 0)
                return 0;
            return id;
        }

        /**
         * 	Reverse Accrual - none
         * 	@return true if success 
         */
        public bool ReverseAccrualIt()
        {
            log.Info(ToString());
            return false;
        }

        /** 
         * 	Re-activate
         * 	@return true if success 
         */
        public bool ReActivateIt()
        {
            log.Info(ToString());
            if (ReverseCorrectIt())
                return true;
            return false;
        }

        /**
         * 	String Representation
         *	@return Info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MPayment[");
            sb.Append(Get_ID()).Append("-").Append(GetDocumentNo())
                .Append(",Receipt=").Append(IsReceipt())
                .Append(",PayAmt=").Append(GetPayAmt())
                .Append(",Discount=").Append(GetDiscountAmt())
                .Append(",WriteOff=").Append(GetWriteOffAmt())
                .Append(",OverUnder=").Append(GetOverUnderAmt());
            return sb.ToString();
        }

        /**
         * 	Get Document Info
         *	@return document Info (untranslated)
         */
        public String GetDocumentInfo()
        {
            MVABDocTypes dt = MVABDocTypes.Get(GetCtx(), GetVAB_DocTypes_ID());
            return dt.GetName() + " " + GetDocumentNo();
        }

        /**
         * 	Create PDF
         *	@return File or null
         */
        public FileInfo CreatePDF()
        {
            try
            {
                string fileName = Get_TableName() + Get_ID() + "_" + CommonFunctions.GenerateRandomNo()
                                    + ".txt"; //.pdf
                string filePath = Path.GetTempPath() + fileName;

                FileInfo temp = new FileInfo(filePath);
                if (!temp.Exists)
                {
                    return CreatePDF(temp);
                }
            }
            catch (Exception e)
            {
                log.Severe("Could not create PDF - " + e.Message);
            }
            return null;
        }

        /**
         * 	Create PDF file
         *	@param file output file
         *	@return file if success
         */
        public FileInfo CreatePDF(FileInfo file)
        {
            //	ReportEngine re = ReportEngine.Get (GetCtx(), ReportEngine.PAYMENT, GetVAB_Payment_ID());
            //	if (re == null)
            return null;
            //	return re.GetPDF(file);
        }


        /**
         * 	Get Summary
         *	@return Summary of Document
         */
        public String GetSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetDocumentNo());
            //	: Total Lines = 123.00 (#1)
            sb.Append(": ")
                .Append(Msg.Translate(GetCtx(), "PayAmt")).Append("=").Append(GetPayAmt())
                .Append(",").Append(Msg.Translate(GetCtx(), "WriteOffAmt")).Append("=").Append(GetWriteOffAmt());
            //	 - Description
            if (GetDescription() != null && GetDescription().Length > 0)
                sb.Append(" - ").Append(GetDescription());
            return sb.ToString();
        }

        /**
         * 	Get Process Message
         *	@return clear text error message
         */
        public String GetProcessMsg()
        {
            return _processMsg;
        }

        /**
         * 	Get Document Owner (Responsible)
         *	@return VAF_UserContact_ID
         */
        public int GetDoc_User_ID()
        {
            return GetCreatedBy();
        }

        /**
         * 	Get Document Approval Amount
         *	@return amount payment(AP) or write-off(AR)
         */
        public Decimal GetApprovalAmt()
        {
            if (IsReceipt())
                return GetWriteOffAmt();
            return GetPayAmt();
        }
        #region To Set unmatched Balance in the case of Bank Account *****************
        ////Added method For update UnMatched Balance In Case of Invoice Or Order Or Indepent Payment //Arpit
        ////public bool UpdateUnMatchedBalanceForAccount(bool _Negate)
        public bool UpdateUnMatchedBalanceForAccount()
        {

            MVABBankAcct BankAccount_ = new MVABBankAcct(GetCtx(), GetVAB_Bank_Acct_ID(), Get_TrxName());

            Decimal convertedAmt = GetPayAmt();
            //if (_Negate) //Handled In Case of Void the Payment
            //{
            //    convertedAmt = Decimal.Negate(convertedAmt);
            //}
            //Handled In Case of Invoice Or Order Or Indepent Payment
            if ((GetVAB_Invoice_ID() > 0 || GetVAB_Order_ID() > 0) || (GetVAB_Invoice_ID() == 0 && GetVAB_Order_ID() == 0))
            {
                if (IsReceipt())
                {
                    if (BankAccount_.GetVAB_Currency_ID() == GetVAB_Currency_ID())
                        BankAccount_.SetUnMatchedBalance(Decimal.Add(BankAccount_.GetUnMatchedBalance(), convertedAmt));
                    else
                    {
                        convertedAmt = Util.GetValueOfDecimal(MVABExchangeRate.Convert(GetCtx(), convertedAmt, GetVAB_Currency_ID(), BankAccount_.GetVAB_Currency_ID(), GetDateAcct(),
                            GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID()));
                        if (convertedAmt == 0)
                        {
                            // JID_0084: On payment window if conversin not found system will give message Correct Message: Could not convert currency to base currency - Conversion type: XXXX
                            MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                            _processMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                            + MVABCurrency.GetISO_Code(GetCtx(), BankAccount_.GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();

                            //_processMsg = "Could not convert VAB_Currency_ID=" + GetVAB_Currency_ID()
                            //    + " to Bank Account VAB_Currency_ID=" + BankAccount_.GetVAB_Currency_ID();
                            return false;   //return DocActionVariables.STATUS_INVALID;
                        }
                        BankAccount_.SetUnMatchedBalance(Decimal.Add(BankAccount_.GetUnMatchedBalance(), Util.GetValueOfDecimal(convertedAmt)));
                    }
                    if (!BankAccount_.Save(Get_TrxName()))
                    {
                        Get_TrxName().Rollback();
                        _processMsg = "Could not update Bank Account";
                        return false;   //  return DocActionVariables.STATUS_INVALID;
                    }
                }
                else
                {
                    if (BankAccount_.GetVAB_Currency_ID() == GetVAB_Currency_ID())
                        BankAccount_.SetUnMatchedBalance(Decimal.Add(BankAccount_.GetUnMatchedBalance(), Decimal.Negate(convertedAmt)));
                    else
                    {
                        convertedAmt = Util.GetValueOfDecimal(MVABExchangeRate.Convert(GetCtx(), convertedAmt, GetVAB_Currency_ID(), BankAccount_.GetVAB_Currency_ID(), GetDateAcct(), GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID()));
                        if (convertedAmt == 0)
                        {
                            // JID_0084: On payment window if conversin not found system will give message Correct Message: Could not convert currency to base currency - Conversion type: XXXX
                            MVABCurrencyType conv = new MVABCurrencyType(GetCtx(), GetVAB_CurrencyType_ID(), Get_TrxName());
                            _processMsg = Msg.GetMsg(GetCtx(), "NoConversion") + MVABCurrency.GetISO_Code(GetCtx(), GetVAB_Currency_ID()) + Msg.GetMsg(GetCtx(), "ToBaseCurrency")
                            + MVABCurrency.GetISO_Code(GetCtx(), BankAccount_.GetVAB_Currency_ID()) + " - " + Msg.GetMsg(GetCtx(), "ConversionType") + conv.GetName();

                            //_processMsg = "Could not convert VAB_Currency_ID=" + GetVAB_Currency_ID()
                            //    + " to Bank Account VAB_Currency_ID=" + BankAccount_.GetVAB_Currency_ID();
                            return false; //return DocActionVariables.STATUS_INVALID;
                        }
                        BankAccount_.SetUnMatchedBalance(Decimal.Add(BankAccount_.GetUnMatchedBalance(), Decimal.Negate(Util.GetValueOfDecimal(convertedAmt))));
                    }
                    if (!BankAccount_.Save(Get_TrxName()))
                    {
                        Get_TrxName().Rollback();
                        _processMsg = "Could not update Bank Account";
                        return false;   //  return DocActionVariables.STATUS_INVALID;
                    }
                    //}
                }

            }
            return true;
        }
        #endregion
        #region DocAction Members

        public Env.QueryParams GetLineOrgsQueryInfo()
        {
            return null;
        }

        public DateTime? GetDocumentDate()
        {
            return null;
        }

        public string GetDocBaseType()
        {
            return null;
        }


        public void SetProcessMsg(string processMsg)
        {
            _processMsg = processMsg;
        }



        #endregion
    }
}