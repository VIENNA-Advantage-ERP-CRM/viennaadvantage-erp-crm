﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVASExpenseReport
 * Purpose        : Time + Expense Model
 * Class Used     : X_VAS_ExpenseReport,DocAction
 * Chronological    Development
 * Deepak          31-Dec-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
//////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Logging;
using System.IO;

namespace VAdvantage.Model
{
    public class MVASExpenseReport : X_VAS_ExpenseReport, VAdvantage.Process.DocAction
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAS_ExpenseReport_ID">id</param>
        /// <param name="trxName">transaction</param>
        public MVASExpenseReport(Ctx ctx, int VAS_ExpenseReport_ID, Trx trxName)
            : base(ctx, VAS_ExpenseReport_ID, trxName)
        {
            //super(ctx, VAS_ExpenseReport_ID, trxName);
            if (VAS_ExpenseReport_ID == 0)
            {
                //	setVAB_BusinessPartner_ID (0);
                //setDateReport(new Timestamp(System.currentTimeMillis()));
                SetDateReport(DateTime.Now);
                //	setDocumentNo (null);
                SetIsApproved(false);
                //	setVAM_PriceList_ID (0);
                //	setVAM_Warehouse_ID (0);
                //super.setProcessed(false);
                base.SetProcessed(false);
                SetProcessing(false);
            }
        }	//	MVASExpenseReport

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">datarow</param>
        /// <param name="trxName">transaction</param>
        public MVASExpenseReport(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
            //super(ctx, rs, trxName);
        }	//	MVASExpenseReport

        /** Default Locator				*/
        private int _VAM_Locator_ID = 0;
        /**	Lines						*/
        private MVASExpenseReportLine[] _lines = null;
        /** Cached User					*/
        private int _VAF_UserContact_ID = 0;


        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">If new record</param>
        /// <returns></returns>
        protected override bool BeforeSave(bool newRecord)
        {
            //applied check on changing the pricelist if time epense lines are present.
            if (!newRecord)
            {
                if (Is_ValueChanged("VAM_PriceList_ID"))
                {
                    MVASExpenseReportLine[] lines = GetLines();
                    if (lines.Length > 0)
                    {
                        log.SaveError("LinesExists", "");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get Lines Convenience Wrapper
        /// </summary>
        /// <returns>array of lines</returns>
        public MVASExpenseReportLine[] GetLines()
        {
            return GetLines(true);
        }

        /// <summary>
        /// Get Lines
        /// </summary>
        /// <param name="requery">true requeries</param>
        /// <returns>array of lines</returns>
        public MVASExpenseReportLine[] GetLines(Boolean requery)
        {
            if (_lines != null && !requery)
            {
                return _lines;
            }
            //
            int VAB_Currency_ID = GetVAB_Currency_ID();
            List<MVASExpenseReportLine> list = new List<MVASExpenseReportLine>();
            //
            String sql = "SELECT * FROM VAS_ExpenseReportLine WHERE VAS_ExpenseReport_ID=@param ORDER BY Line";
            //PreparedStatement pstmt = null;
            SqlParameter[] param = new SqlParameter[1];
            IDataReader idr = null;
            DataTable dt = null;
            try
            {
                param[0] = new SqlParameter("@param", GetVAS_ExpenseReport_ID());
                //pstmt = DataBase.prepareStatement(sql, get_TrxName());
                //pstmt.setInt(1, getVAS_ExpenseReport_ID());
                idr = DB.ExecuteReader(sql, param, Get_TrxName());
                //ResultSet rs = pstmt.executeQuery();
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    MVASExpenseReportLine te = new MVASExpenseReportLine(GetCtx(), dr, Get_TrxName());
                    te.SetVAB_Currency_Report_ID(VAB_Currency_ID);
                    list.Add(te);
                }
                dt = null;
            }
            catch (Exception ex)
            {
                if (dt != null)
                {
                    dt = null;
                }
                log.Log(Level.SEVERE, "getLines", ex);
            }
            finally
            {
                if (dt != null)
                {
                    dt = null;
                }
                if (idr != null)
                {
                    idr.Close();
                }
            }
            //
            _lines = new MVASExpenseReportLine[list.Count];
            _lines = list.ToArray();
            return _lines;
        }	//	getLines

        /// <summary>
        /// Add to Description
        /// </summary>
        /// <param name="description">description text</param>
        public void AddDescription(String description)
        {
            String desc = GetDescription();
            if (desc == null)
            {
                SetDescription(description);
            }
            else
            {
                SetDescription(desc + " | " + description);
            }
        }	//	addDescription

        /// <summary>
        ///  Get Default Locator (from Warehouse)
        /// </summary>
        /// <returns>locator</returns>
        public int GetVAM_Locator_ID()
        {
            if (_VAM_Locator_ID != 0)
            {
                return _VAM_Locator_ID;
            }
            //
            String sql = "SELECT VAM_Locator_ID FROM VAM_Locator "
                + "WHERE VAM_Warehouse_ID=@param AND IsActive='Y' ORDER BY IsDefault DESC, Created";
            SqlParameter[] param = new SqlParameter[1];
            IDataReader idr = null;
            try
            {
                //pstmt = DataBase.prepareStatement(sql, null);
                //pstmt.setInt(1, getVAM_Warehouse_ID());
                param[0] = new SqlParameter("@param", GetVAM_Warehouse_ID());
                //ResultSet rs = pstmt.executeQuery();
                idr = DB.ExecuteReader(sql, param, null);
                if (idr.Read())
                {
                    _VAM_Locator_ID = Util.GetValueOfInt(idr[0]);// rs.getInt(1);
                }
                idr.Close();
            }
            catch (Exception ex)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, "getVAM_Locator_ID", ex);
            }
            //
            return _VAM_Locator_ID;
        }	//	getVAM_Locator_ID

        /// <summary>
        /// Set Processed.Propergate to Lines/Taxes
        /// </summary>
        /// <param name="processed">processed</param>
        public new void SetProcessed(Boolean processed)
        {
            base.SetProcessed(processed);
            if (Get_ID() == 0)
            {
                return;
            }
            String sql = "UPDATE VAS_ExpenseReportLine SET Processed='"
                + (processed ? "Y" : "N")
                + "' WHERE VAS_ExpenseReport_ID=" + GetVAS_ExpenseReport_ID();
            int noLine = DB.ExecuteQuery(sql, null, Get_TrxName());
            _lines = null;
            log.Fine(processed + " - Lines=" + noLine);
        }	//	setProcessed

        /// <summary>
        /// Get Document Info
        /// </summary>
        /// <returns>document info</returns>
        public String GetDocumentInfo()
        {
            return Msg.GetElement(GetCtx(), "VAS_ExpenseReport_ID") + " " + GetDocumentNo();
        }	//	getDocumentInfo

        /// <summary>
        //Create PDF
        /// </summary>
        /// <returns>File or null</returns>
        public FileInfo CreatePDF()
        {
            try
            {
                string fileName = Get_TableName() + Get_ID() + "_" + CommonFunctions.GenerateRandomNo()
                                    + ".pdf"; //.pdf
                string filePath = Path.GetTempPath() + fileName;

                FileInfo temp = new FileInfo(filePath);
                if (!temp.Exists)
                {
                    return CreatePDF(temp);
                }
            }//	getPDF

            catch (Exception e)
            {
                log.Severe("Could not create PDF - " + e.Message);
            }
            return null;
        }
        /// <summary>
        /// Create PDF file
        /// </summary>
        /// <param name="file">output file</param>
        /// <returns>file if success</returns>
        public FileInfo CreatePDF(FileInfo file)
        {
            //	ReportEngine re = ReportEngine.get (getCtx(), ReportEngine.INVOICE, getVAB_Invoice_ID());
            //	if (re == null)
            return null;
            //	return re.getPDF(file);
        }	//	createPDF

        /**************************************************************************
         * 	Process document
         *	@param processAction document action
         *	@return true if performed
         */
        public Boolean ProcessIt(String processAction)
        {
            m_processMsg = null;
            DocumentEngine engine = new DocumentEngine(this, GetDocStatus());
            return engine.ProcessIt(processAction, GetDocAction());
        }	//	processIt

        /**	Process Message 			*/
        private String m_processMsg = null;
        /**	Just Prepared Flag			*/
        private Boolean m_justPrepared = false;

        /// <summary>
        /// Unlock Document.
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean UnlockIt()
        {
            log.Info("unlockIt - " + ToString());
            SetProcessing(false);
            return true;
        }	//	unlockIt

        /// <summary>
        /// Invalidate Document
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean InvalidateIt()
        {
            log.Info("invalidateIt - " + ToString());
            SetDocAction(DOCACTION_Prepare);
            return true;
        }	//	invalidateIt

        /// <summary>
        /// Prepare Document
        /// </summary>
        /// <returns>new status (In Progress or Invalid) </returns>
        public String PrepareIt()
        {
            log.Info(ToString());
            m_processMsg = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_BEFORE_PREPARE);
            if (m_processMsg != null)
            {
                return DocActionVariables.STATUS_INVALID;
            }

            //	Std Period open? - AP (Reimbursement) Invoice
            if (!MVABYearPeriod.IsOpen(GetCtx(), GetDateReport(), VAdvantage.Model.MVABMasterDocType.DOCBASETYPE_APINVOICE, GetVAF_Org_ID()))
            {
                m_processMsg = "@PeriodClosed@";
                return DocActionVariables.STATUS_INVALID;
            }

            // is Non Business Day?
            // JID_1205: At the trx, need to check any non business day in that org. if not fund then check * org.
            if (MVABNonBusinessDay.IsNonBusinessDay(GetCtx(), GetDateReport(), GetVAF_Org_ID()))
            {
                m_processMsg = Common.Common.NONBUSINESSDAY;
                return DocActionVariables.STATUS_INVALID;
            }


            MVASExpenseReportLine[] lines = GetLines(false);
            if (lines.Length == 0)
            {
                m_processMsg = "@NoLines@";
                return DocActionVariables.STATUS_INVALID;
            }
            //	Add up Amounts
            Decimal amt = Env.ZERO;
            for (int i = 0; i < lines.Length; i++)
            {
                MVASExpenseReportLine line = lines[i];
                //amt = amt.add(line.GetApprovalAmt());
                amt = Decimal.Add(amt, line.GetApprovalAmt());// amt.add(line.GetApprovalAmt());

            }
            SetApprovalAmt(amt);

            //	Invoiced but no BP
            for (int i = 0; i < lines.Length; i++)
            {
                MVASExpenseReportLine line = lines[i];
                if (line.IsInvoiced() && line.GetVAB_BusinessPartner_ID() == 0)
                {
                    m_processMsg = "@Line@ " + line.GetLine() + ": Invoiced, but no Business Partner";
                    return DocActionVariables.STATUS_INVALID;
                }
            }

            m_justPrepared = true;
            if (!DOCACTION_Complete.Equals(GetDocAction()))
            {
                SetDocAction(DOCACTION_Complete);
            }
            return DocActionVariables.STATUS_INPROGRESS;
        }	//	prepareIt

        /// <summary>
        ///  Approve Document
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean ApproveIt()
        {
            log.Info("approveIt - " + ToString());
            SetIsApproved(true);
            return true;
        }	//	approveIt

        /// <summary>
        /// Reject Approval
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean RejectIt()
        {
            log.Info("rejectIt - " + ToString());
            SetIsApproved(false);
            return true;
        }	//	rejectIt

        /// <summary>
        /// Complete Document
        /// </summary>
        /// <returns>new status (Complete, In Progress, Invalid, Waiting ..)</returns>
        public String CompleteIt()
        {
            //	Re-Check
            if (!m_justPrepared)
            {
                String status = PrepareIt();
                if (!DocActionVariables.STATUS_INPROGRESS.Equals(status))
                {
                    return status;
                }
            }
            //	Implicit Approval
            if (!IsApproved())
            {
                ApproveIt();
            }
            log.Info("completeIt - " + ToString());

            //	User Validation
            String valid = ModelValidationEngine.Get().FireDocValidate(this, ModalValidatorVariables.DOCTIMING_AFTER_COMPLETE);
            if (valid != null)
            {
                m_processMsg = valid;
                return DocActionVariables.STATUS_INVALID;
            }

            //
            SetProcessed(true);
            SetDocAction(DOCACTION_Close);
            return DocActionVariables.STATUS_COMPLETED;
        }	//	completeIt

        /// <summary>
        /// Void Document.Same as Close.
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean VoidIt()
        {
            log.Info("voidIt - " + ToString());
            return CloseIt();
        }	//	voidIt

        /// <summary>
        /// Close Document.Cancel not delivered Qunatities
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean CloseIt()
        {
            log.Info("closeIt - " + ToString());

            //	Close Not delivered Qty
            //	setDocAction(DOCACTION_None);
            return true;
        }	//	closeIt

        /// <summary>
        /// Reverse Correction
        /// </summary>
        /// <returns>false </returns>
        public Boolean ReverseCorrectIt()
        {
            log.Info("reverseCorrectIt - " + ToString());
            return false;
        }	//	reverseCorrectionIt

        /// <summary>
        /// Reverse Accrual - none
        /// </summary>
        /// <returns>false </returns>
        public Boolean ReverseAccrualIt()
        {
            log.Info("reverseAccrualIt - " + ToString());
            return false;
        }	//	reverseAccrualIt

        /// <summary>
        /// Re-activate
        /// </summary>
        /// <returns>true if success </returns>
        public Boolean ReActivateIt()
        {
            log.Info("reActivateIt - " + ToString());
            //	setProcessed(false);
            return false;
        }	//	reActivateIt


        /// <summary>
        /// Get Summary
        /// </summary>
        /// <returns>Summary of Document</returns>
        public String GetSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetDocumentNo());
            //	: Total Lines = 123.00 (#1)
            sb.Append(": ")
                .Append(Msg.Translate(GetCtx(), "ApprovalAmt")).Append("=").Append(GetApprovalAmt())
                .Append(" (#").Append(GetLines(false).Length).Append(")");
            //	 - Description
            if (GetDescription() != null && GetDescription().Length > 0)
            {
                sb.Append(" - ").Append(GetDescription());
            }
            return sb.ToString();
        }	//	getSummary

        /// <summary>
        /// Get Process Message
        /// </summary>
        /// <returns>clear text error message</returns>
        public String GetProcessMsg()
        {
            return m_processMsg;
        }	//	getProcessMsg

        /// <summary>
        /// Get Document Owner (Responsible)
        /// </summary>
        /// <returns>VAF_UserContact_ID</returns>
        public int GetDoc_User_ID()
        {
            if (_VAF_UserContact_ID != 0)
            {
                return _VAF_UserContact_ID;
            }
            if (GetVAB_BusinessPartner_ID() != 0)
            {
                VAdvantage.Model.MVAFUserContact[] users = VAdvantage.Model.MVAFUserContact.GetOfBPartner(GetCtx(), GetVAB_BusinessPartner_ID());
                if (users.Length > 0)
                {
                    _VAF_UserContact_ID = users[0].GetVAF_UserContact_ID();
                    return _VAF_UserContact_ID;
                }
            }
            return GetCreatedBy();
        }	//	getDoc_User_ID


        /// <summary>
        /// Get Document Currency
        /// </summary>
        /// <returns>VAB_Currency_ID</returns>
        public int GetVAB_Currency_ID()
        {
            MVAMPriceList pl = MVAMPriceList.Get(GetCtx(), GetVAM_PriceList_ID(), Get_TrxName());
            return pl.GetVAB_Currency_ID();
        }	//	getVAB_Currency_ID



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

        }
        
    }	//	MVASExpenseReport

}