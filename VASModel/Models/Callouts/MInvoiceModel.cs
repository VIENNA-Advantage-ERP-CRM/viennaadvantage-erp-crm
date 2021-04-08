﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using VAdvantage.Logging;
using VAdvantage.Model;
using VAdvantage.Utility;
using VIS.DBase;

namespace VIS.Models
{
    public class MVABInvoiceModel
    {
        /// <summary>
        /// GetInvoice
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetInvoice(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            int VAB_Invoice_ID;
            //Assign parameter value
            VAB_Invoice_ID = Util.GetValueOfInt(paramValue[0].ToString());
            //End Assign parameter value
            MVABInvoice inv = new MVABInvoice(ctx, VAB_Invoice_ID, null);
            Dictionary<string, string> result = new Dictionary<string, string>();
            result["IsSOTrx"] = inv.IsSOTrx().ToString();
            //Added By amit
            result["IsReturnTrx"] = inv.IsReturnTrx().ToString();
            result["VAB_BusinessPartner_ID"] = inv.GetVAB_BusinessPartner_ID().ToString();
            result["VAM_PriceList_ID"] = inv.GetVAM_PriceList_ID().ToString();
            result["VAF_Org_ID"] = inv.GetVAF_Org_ID().ToString();
            result["VAB_BPart_Location_ID"] = inv.GetVAB_BPart_Location_ID().ToString();
            result["VAB_Currency_ID"] = inv.GetVAB_Currency_ID().ToString();
            //result["DateAcct"] = Util.GetValueOfString(inv.GetDateAcct());
            //End
            return result;

        }
        /// <summary>
        /// Get Tax ID
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Int32 GetTaxNew(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            //Assign parameter value
            int VAB_TaxRate_ID = 0;
            int VAB_Invoice_ID = Util.GetValueOfInt(paramValue[0].ToString());
            int VAM_Product_ID = Util.GetValueOfInt(paramValue[1].ToString());
            int VAB_Charge_ID = Util.GetValueOfInt(paramValue[2].ToString());
            int taxCategory = 0;
            string sql = "";
            if ((VAM_Product_ID == 0 && VAB_Charge_ID == 0) || VAB_Invoice_ID == 0)
            {
                return VAB_TaxRate_ID;
            }
            DataSet dsLoc = null;
            MVABInvoice inv = new MVABInvoice(ctx, VAB_Invoice_ID, null);
            MVABBusinessPartner bp = new MVABBusinessPartner(ctx, inv.GetVAB_BusinessPartner_ID(), null);
            if (bp.IsTaxExempt())
            {
                VAB_TaxRate_ID = GetExemptTax(ctx, inv.GetVAF_Org_ID());
                return VAB_TaxRate_ID;
            }
            if (VAM_Product_ID > 0)
            {
                MVAMProduct prod = new MVAMProduct(ctx, VAM_Product_ID, null);
                taxCategory = Util.GetValueOfInt(prod.GetVAB_TaxCategory_ID());
            }
            if (VAB_Charge_ID > 0)
            {
                MVABCharge chrg = new MVABCharge(ctx, VAB_Charge_ID, null);
                taxCategory = Util.GetValueOfInt(chrg.GetVAB_TaxCategory_ID());
            }
            if (taxCategory > 0)
            {
                MVABTaxCategory taxCat = new MVABTaxCategory(ctx, taxCategory, null);
                int Country_ID = 0, Region_ID = 0, orgCountry = 0, orgRegion = 0, taxRegion = 0;
                string Postal = "", orgPostal = "";
                sql = @"SELECT loc.VAB_Country_ID,loc.VAB_RegionState_ID,loc.Postal FROM VAB_Address loc INNER JOIN VAB_BPart_Location bpl ON loc.VAB_Address_ID = bpl.VAB_Address_ID WHERE bpl.VAB_BPart_Location_ID ="
                    + inv.GetVAB_BPart_Location_ID() + " AND bpl.IsActive = 'Y'";
                dsLoc = DB.ExecuteDataset(sql, null, null);
                if (dsLoc != null)
                {
                    if (dsLoc.Tables[0].Rows.Count > 0)
                    {
                        for (int j = 0; j < dsLoc.Tables[0].Rows.Count; j++)
                        {
                            Country_ID = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][0]);
                            Region_ID = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][1]);
                            Postal = Util.GetValueOfString(dsLoc.Tables[0].Rows[j][2]);
                        }
                    }
                }
                dsLoc = null;
                sql = @"SELECT loc.VAB_Country_ID,loc.VAB_RegionState_ID,loc.Postal FROM VAB_Address loc LEFT JOIN VAF_OrgDetail org ON loc.VAB_Address_ID = org.VAB_Address_ID WHERE org.VAF_Org_ID ="
                        + inv.GetVAF_Org_ID() + " AND org.IsActive = 'Y'";
                dsLoc = DB.ExecuteDataset(sql, null, null);
                if (dsLoc != null)
                {
                    if (dsLoc.Tables[0].Rows.Count > 0)
                    {
                        for (int j = 0; j < dsLoc.Tables[0].Rows.Count; j++)
                        {
                            orgCountry = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][0]);
                            orgRegion = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][1]);
                            orgPostal = Util.GetValueOfString(dsLoc.Tables[0].Rows[j][2]);
                        }
                    }
                }
                // if Tax Preference 1 is Tax Class
                if (taxCat.GetVATAX_Preference1() == "T")
                {
                    sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                   " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                    int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    if (taxType == 0)
                    {
                        sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                        taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                        if (taxType == 0)
                        {
                            if (taxCat.GetVATAX_Preference2() == "L")
                            {
                                VAB_TaxRate_ID = GetTaxFromLocation(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                if (VAB_TaxRate_ID > 0)
                                {
                                    return VAB_TaxRate_ID;
                                }
                                else
                                {
                                    if (taxCat.GetVATAX_Preference3() == "R")
                                    {
                                        if (Country_ID > 0)
                                        {
                                            dsLoc = null;
                                            sql = @"SELECT VATAX_TaxRegion_ID FROM VATAX_TaxCatRate  WHERE VAB_TaxCategory_ID = " + taxCategory +
                                                " AND VATAX_TaxBase = 'R' AND VATAX_DiffCountry = 'Y' AND IsActive = 'Y' AND VAB_Country_ID = " + Country_ID;
                                            dsLoc = DB.ExecuteDataset(sql, null, null);
                                            if (dsLoc != null)
                                            {
                                                if (dsLoc.Tables[0].Rows.Count > 0)
                                                {

                                                }
                                                else
                                                {
                                                    VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                                    if (VAB_TaxRate_ID > 0)
                                                    {
                                                        return VAB_TaxRate_ID;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                                if (VAB_TaxRate_ID > 0)
                                                {
                                                    return VAB_TaxRate_ID;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                            if (VAB_TaxRate_ID > 0)
                                            {
                                                return VAB_TaxRate_ID;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (taxCat.GetVATAX_Preference2() == "R")
                            {
                                if (Country_ID > 0)
                                {
                                    dsLoc = null;
                                    sql = @"SELECT VATAX_TaxRegion_ID FROM VATAX_TaxCatRate  WHERE VAB_TaxCategory_ID = " + taxCategory +
                                        " AND VATAX_TaxBase = 'R' AND VATAX_DiffCountry = 'Y' AND IsActive = 'Y' AND VAB_Country_ID = " + Country_ID;
                                    dsLoc = DB.ExecuteDataset(sql, null, null);
                                    if (dsLoc != null)
                                    {
                                        if (dsLoc.Tables[0].Rows.Count > 0)
                                        {

                                        }
                                        else
                                        {
                                            VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                            if (VAB_TaxRate_ID > 0)
                                            {
                                                return VAB_TaxRate_ID;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                        if (VAB_TaxRate_ID > 0)
                                        {
                                            return VAB_TaxRate_ID;
                                        }
                                    }
                                }
                                else
                                {
                                    VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                    if (VAB_TaxRate_ID > 0)
                                    {
                                        return VAB_TaxRate_ID;
                                    }
                                }
                                if (taxCat.GetVATAX_Preference3() == "L")
                                {
                                    VAB_TaxRate_ID = GetTaxFromLocation(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                    if (VAB_TaxRate_ID > 0)
                                    {
                                        return VAB_TaxRate_ID;
                                    }
                                }
                            }
                        }
                    }
                    if (taxType > 0)
                    {
                        sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID  WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                            " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxType_ID =" + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                    }
                }
                // if Tax Preference 1 is Location
                else if (taxCat.GetVATAX_Preference1() == "L")
                {
                    VAB_TaxRate_ID = GetTaxFromLocation(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                    if (VAB_TaxRate_ID > 0)
                    {
                        return VAB_TaxRate_ID;
                    }
                    else
                    {
                        if (taxCat.GetVATAX_Preference2() == "T")
                        {
                            sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                           " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                            int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            if (taxType == 0)
                            {
                                sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                                taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                                if (taxType == 0)
                                {
                                    if (taxCat.GetVATAX_Preference3() == "R")
                                    {
                                        if (Country_ID > 0)
                                        {
                                            dsLoc = null;
                                            sql = @"SELECT VATAX_TaxRegion_ID FROM VATAX_TaxCatRate  WHERE VAB_TaxCategory_ID = " + taxCategory +
                                                " AND VATAX_TaxBase = 'R' AND VATAX_DiffCountry = 'Y' AND IsActive = 'Y' AND VAB_Country_ID = " + Country_ID;
                                            dsLoc = DB.ExecuteDataset(sql, null, null);
                                            if (dsLoc != null)
                                            {
                                                if (dsLoc.Tables[0].Rows.Count > 0)
                                                {

                                                }
                                                else
                                                {
                                                    VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                                    if (VAB_TaxRate_ID > 0)
                                                    {
                                                        return VAB_TaxRate_ID;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                                if (VAB_TaxRate_ID > 0)
                                                {
                                                    return VAB_TaxRate_ID;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                            if (VAB_TaxRate_ID > 0)
                                            {
                                                return VAB_TaxRate_ID;
                                            }
                                        }
                                    }
                                }
                            }
                            if (taxType > 0)
                            {
                                sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                    " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxType_ID =" + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                                if (VAB_TaxRate_ID > 0)
                                {
                                    return VAB_TaxRate_ID;
                                }
                            }
                        }
                        else if (taxCat.GetVATAX_Preference2() == "R")
                        {
                            if (Country_ID > 0)
                            {
                                dsLoc = null;
                                sql = @"SELECT VATAX_TaxRegion_ID FROM VATAX_TaxCatRate  WHERE VAB_TaxCategory_ID = " + taxCategory +
                                    " AND VATAX_TaxBase = 'R' AND VATAX_DiffCountry = 'Y' AND IsActive = 'Y' AND VAB_Country_ID = " + Country_ID;
                                dsLoc = DB.ExecuteDataset(sql, null, null);
                                if (dsLoc != null)
                                {
                                    if (dsLoc.Tables[0].Rows.Count > 0)
                                    {

                                    }
                                    else
                                    {
                                        VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                        if (VAB_TaxRate_ID > 0)
                                        {
                                            return VAB_TaxRate_ID;
                                        }
                                    }
                                }
                                else
                                {
                                    VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                    if (VAB_TaxRate_ID > 0)
                                    {
                                        return VAB_TaxRate_ID;
                                    }
                                }
                            }
                            else
                            {
                                VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                if (VAB_TaxRate_ID > 0)
                                {
                                    return VAB_TaxRate_ID;
                                }
                            }
                            if (taxCat.GetVATAX_Preference3() == "T")
                            {
                                sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                               " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                                int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                                if (taxType == 0)
                                {
                                    sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                                    taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                                }
                                if (taxType > 0)
                                {
                                    sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                        " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxType_ID =" + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                                    VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                                    if (VAB_TaxRate_ID > 0)
                                    {
                                        return VAB_TaxRate_ID;
                                    }
                                }
                            }
                        }
                    }
                }
                // if Tax Preference 1 is Tax Region
                else if (taxCat.GetVATAX_Preference1() == "R")
                {
                    if (Country_ID > 0)
                    {
                        dsLoc = null;
                        sql = @"SELECT VATAX_TaxRegion_ID FROM VATAX_TaxCatRate  WHERE VAB_TaxCategory_ID = " + taxCategory +
                            " AND VATAX_TaxBase = 'R' AND VATAX_DiffCountry = 'Y' AND IsActive = 'Y' AND VAB_Country_ID = " + Country_ID;
                        dsLoc = DB.ExecuteDataset(sql, null, null);
                        if (dsLoc != null)
                        {
                            if (dsLoc.Tables[0].Rows.Count > 0)
                            {

                            }
                            else
                            {
                                VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                if (VAB_TaxRate_ID > 0)
                                {
                                    return VAB_TaxRate_ID;
                                }
                            }
                        }
                        else
                        {
                            VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                            if (VAB_TaxRate_ID > 0)
                            {
                                return VAB_TaxRate_ID;
                            }
                        }
                    }
                    else
                    {
                        VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                    }
                    if (taxCat.GetVATAX_Preference2() == "T")
                    {
                        sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                       " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                        int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                        if (taxType == 0)
                        {
                            sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                            taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            if (taxType == 0)
                            {
                                if (taxCat.GetVATAX_Preference3() == "L")
                                {
                                    VAB_TaxRate_ID = GetTaxFromLocation(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                    if (VAB_TaxRate_ID > 0)
                                    {
                                        return VAB_TaxRate_ID;
                                    }
                                }
                            }
                        }
                        if (taxType > 0)
                        {
                            sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxType_ID =" + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            if (VAB_TaxRate_ID > 0)
                            {
                                return VAB_TaxRate_ID;
                            }
                        }
                    }
                    else if (taxCat.GetVATAX_Preference2() == "L")
                    {
                        VAB_TaxRate_ID = GetTaxFromLocation(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                        if (taxCat.GetVATAX_Preference3() == "T")
                        {
                            sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                           " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                            int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            if (taxType == 0)
                            {
                                sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                                taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            }
                            if (taxType > 0)
                            {
                                sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                    " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxType_ID =" + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                                if (VAB_TaxRate_ID > 0)
                                {
                                    return VAB_TaxRate_ID;
                                }
                            }
                        }
                    }
                }
                if (taxCat.GetVATAX_Preference1() == "R" || taxCat.GetVATAX_Preference2() == "R" || taxCat.GetVATAX_Preference3() == "R")
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxRegion tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.IsDefault = 'Y' AND tcr.IsActive = 'Y' 
                    AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "') ORDER BY tcr.Updated";
                    VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    if (VAB_TaxRate_ID > 0)
                    {
                        return VAB_TaxRate_ID;
                    }
                }
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VAB_TaxCategory tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory + " AND tcr.IsActive = 'Y'";
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                return VAB_TaxRate_ID;
            }
            return VAB_TaxRate_ID;
        }

        // on change of Tax
        // Added by Bharat on 27 Feb 2018 to check Exempt Tax on Business Partner
        public Dictionary<String, object> GetTaxId(Ctx ctx, string param)
        {
            string[] paramValue = param.Split(',');

            //Assign parameter value            
            int VAB_Invoice_ID = Util.GetValueOfInt(paramValue[0].ToString());
            int VAM_Product_ID = Util.GetValueOfInt(paramValue[1].ToString());
            int VAB_Charge_ID = Util.GetValueOfInt(paramValue[2].ToString());
            //End Assign parameter value

            Dictionary<String, object> retDic = new Dictionary<string, object>();
            string sql = null;
            int taxId = 0;
            int _VAB_BusinessPartner_Id = 0;
            int _c_Bill_Location_Id = 0;
            int _CountVATAX = 0;

            _CountVATAX = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX IN ('VATAX_' )", null, null));
            retDic["_CountVATAX"] = _CountVATAX.ToString();

            Dictionary<string, string> order = GetInvoice(ctx, VAB_Invoice_ID.ToString());
            _VAB_BusinessPartner_Id = Util.GetValueOfInt(order["VAB_BusinessPartner_ID"]);
            _c_Bill_Location_Id = Util.GetValueOfInt(order["VAB_BPart_Location_ID"]);

            MVABBusinessPartner bp = new MVABBusinessPartner(ctx, _VAB_BusinessPartner_Id, null);
            retDic["TaxExempt"] = bp.IsTaxExempt() ? "Y" : "N";

            if (_CountVATAX > 0)
            {
                sql = "SELECT VATAX_TaxRule FROM VAF_OrgDetail WHERE VAF_Org_ID=" + Util.GetValueOfInt(order["VAF_Org_ID"]) + " AND IsActive ='Y' AND VAF_Client_ID =" + ctx.GetVAF_Client_ID();
                string taxRule = Util.GetValueOfString(DB.ExecuteScalar(sql, null, null));
                retDic["taxRule"] = taxRule.ToString();

                sql = "SELECT Count(*) FROM VAF_Column WHERE ColumnName = 'VAB_TaxRate_ID' AND VAF_TableView_ID = (SELECT VAF_TableView_ID FROM VAF_TableView WHERE TableName = 'VAB_TaxCategory')";
                if (Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null)) > 0)
                {
                    var paramString = (VAB_Invoice_ID).ToString() + "," + (VAM_Product_ID).ToString() + "," + (VAB_Charge_ID).ToString();

                    taxId = GetTax(ctx, paramString);
                }
                else
                {
                    sql = "SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + Util.GetValueOfInt(_VAB_BusinessPartner_Id) +
                                      " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + Util.GetValueOfInt(_c_Bill_Location_Id);
                    var taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    if (taxType == 0)
                    {
                        sql = "SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + Util.GetValueOfInt(_VAB_BusinessPartner_Id) + " AND IsActive = 'Y'";
                        taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    }

                    MVAMProductModel objProduct = new MVAMProductModel();
                    var prodtaxCategory = objProduct.GetTaxCategory(ctx, VAM_Product_ID.ToString());
                    sql = "SELECT VAB_TaxRate_ID FROM VATAX_TaxCatRate WHERE VAB_TaxCategory_ID = " + prodtaxCategory + " AND IsActive ='Y' AND VATAX_TaxType_ID =" + taxType;
                    taxId = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                }
            }
            retDic["taxId"] = taxId.ToString();

            return retDic;
        }


        /// <summary>
        /// Get Tax ID
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Int32 GetTax(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            //Assign parameter value
            int VAB_TaxRate_ID = 0;
            int VAB_Invoice_ID = Util.GetValueOfInt(paramValue[0].ToString());
            int VAM_Product_ID = Util.GetValueOfInt(paramValue[1].ToString());
            int VAB_Charge_ID = Util.GetValueOfInt(paramValue[2].ToString());
            int taxCategory = 0;
            string sql = "";
            if ((VAM_Product_ID == 0 && VAB_Charge_ID == 0) || VAB_Invoice_ID == 0)
            {
                return VAB_TaxRate_ID;
            }
            DataSet dsLoc = null;
            MVABInvoice inv = new MVABInvoice(ctx, VAB_Invoice_ID, null);
            MVABBusinessPartner bp = new MVABBusinessPartner(ctx, inv.GetVAB_BusinessPartner_ID(), null);
            if (bp.IsTaxExempt())
            {
                VAB_TaxRate_ID = GetExemptTax(ctx, inv.GetVAF_Org_ID());
                return VAB_TaxRate_ID;
            }
            if (VAM_Product_ID > 0)
            {
                MVAMProduct prod = new MVAMProduct(ctx, VAM_Product_ID, null);
                taxCategory = Util.GetValueOfInt(prod.GetVAB_TaxCategory_ID());
            }
            if (VAB_Charge_ID > 0)
            {
                MVABCharge chrg = new MVABCharge(ctx, VAB_Charge_ID, null);
                taxCategory = Util.GetValueOfInt(chrg.GetVAB_TaxCategory_ID());
            }
            if (taxCategory > 0)
            {
                MVABTaxCategory taxCat = new MVABTaxCategory(ctx, taxCategory, null);
                int Country_ID = 0, Region_ID = 0, orgCountry = 0, orgRegion = 0, taxRegion = 0;
                string Postal = "", orgPostal = "";
                sql = @"SELECT loc.VAB_Country_ID,loc.VAB_RegionState_ID,loc.Postal FROM VAB_Address loc INNER JOIN VAB_BPart_Location bpl ON loc.VAB_Address_ID = bpl.VAB_Address_ID WHERE bpl.VAB_BPart_Location_ID ="
                    + inv.GetVAB_BPart_Location_ID() + " AND bpl.IsActive = 'Y'";
                dsLoc = DB.ExecuteDataset(sql, null, null);
                if (dsLoc != null)
                {
                    if (dsLoc.Tables[0].Rows.Count > 0)
                    {
                        for (int j = 0; j < dsLoc.Tables[0].Rows.Count; j++)
                        {
                            Country_ID = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][0]);
                            Region_ID = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][1]);
                            Postal = Util.GetValueOfString(dsLoc.Tables[0].Rows[j][2]);
                        }
                    }
                }
                dsLoc = null;
                sql = @"SELECT loc.VAB_Country_ID,loc.VAB_RegionState_ID,loc.Postal FROM VAB_Address loc LEFT JOIN VAF_OrgDetail org ON loc.VAB_Address_ID = org.VAB_Address_ID WHERE org.VAF_Org_ID ="
                        + inv.GetVAF_Org_ID() + " AND org.IsActive = 'Y'";
                dsLoc = DB.ExecuteDataset(sql, null, null);
                if (dsLoc != null)
                {
                    if (dsLoc.Tables[0].Rows.Count > 0)
                    {
                        for (int j = 0; j < dsLoc.Tables[0].Rows.Count; j++)
                        {
                            orgCountry = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][0]);
                            orgRegion = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][1]);
                            orgPostal = Util.GetValueOfString(dsLoc.Tables[0].Rows[j][2]);
                        }
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    string pref = taxCat.GetVATAX_Preference1();
                    if (i == 1)
                    {
                        pref = taxCat.GetVATAX_Preference2();
                    }
                    else if (i == 2)
                    {
                        pref = taxCat.GetVATAX_Preference3();
                    }

                    // if Tax Preference  is Tax Class
                    if (pref == "T")
                    {
                        sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                       " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                        int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                        if (taxType == 0)
                        {
                            sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                            taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                        }
                        if (taxType > 0)
                        {
                            sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID  WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxBase = 'T' AND tcr.VATAX_TaxType_ID =" + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            if (VAB_TaxRate_ID > 0)
                            {
                                return VAB_TaxRate_ID;
                            }
                        }
                    }
                    // if Tax Preference is Location
                    else if (pref == "L")
                    {
                        VAB_TaxRate_ID = GetTaxFromLocation(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                    }
                    // if Tax Preference is Tax Region
                    else if (pref == "R")
                    {
                        if (Country_ID > 0)
                        {
                            dsLoc = null;
                            sql = @"SELECT VATAX_TaxRegion_ID FROM VATAX_TaxCatRate  WHERE VAB_TaxCategory_ID = " + taxCategory +
                                " AND VATAX_TaxBase = 'R' AND VATAX_DiffCountry = 'Y' AND IsActive = 'Y' AND VAB_Country_ID = " + Country_ID;
                            dsLoc = DB.ExecuteDataset(sql, null, null);
                            if (dsLoc != null)
                            {
                                if (dsLoc.Tables[0].Rows.Count > 0)
                                {

                                }
                                else
                                {
                                    VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                    if (VAB_TaxRate_ID > 0)
                                    {
                                        return VAB_TaxRate_ID;
                                    }
                                }
                            }
                            else
                            {
                                VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                if (VAB_TaxRate_ID > 0)
                                {
                                    return VAB_TaxRate_ID;
                                }
                            }
                        }
                        else
                        {
                            VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                            if (VAB_TaxRate_ID > 0)
                            {
                                return VAB_TaxRate_ID;
                            }
                        }
                    }

                    // if Tax Preference is Document Type
                    else if (pref == "D")
                    {
                        sql = @"SELECT VATAX_TaxType_ID FROM VAB_DocTypes WHERE VAB_DocTypes_ID = " + inv.GetVAB_DocTypesTarget_ID();
                        int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                        if (taxType > 0)
                        {
                            sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID  WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxBase = 'T' AND tcr.VATAX_TaxType_ID = " + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                            if (VAB_TaxRate_ID > 0)
                            {
                                return VAB_TaxRate_ID;
                            }
                        }
                    }
                }
                if (taxCat.GetVATAX_Preference1() == "R" || taxCat.GetVATAX_Preference2() == "R" || taxCat.GetVATAX_Preference3() == "R")
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxRegion tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.IsDefault = 'Y' AND tcr.IsActive = 'Y' 
                    AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "') ORDER BY tcr.Updated";
                    VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    if (VAB_TaxRate_ID > 0)
                    {
                        return VAB_TaxRate_ID;
                    }
                }
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VAB_TaxCategory tcr WHERE tcr.VAB_TaxCategory_ID = " + taxCategory + " AND tcr.IsActive = 'Y'";
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                return VAB_TaxRate_ID;
            }
            return VAB_TaxRate_ID;
        }

        // Return Exempted Tax Fro the Organization
        VLogger log = VLogger.GetVLogger("Tax");
        private int GetExemptTax(Ctx ctx, int VAF_Org_ID)
        {
            int VAB_TaxRate_ID = 0;
            String sql = "SELECT t.VAB_TaxRate_ID "
                + "FROM VAB_TaxRate t"
                + " INNER JOIN VAF_Org o ON (t.VAF_Client_ID=o.VAF_Client_ID) "
                + "WHERE t.IsActive='Y' AND t.IsTaxExempt='Y' AND o.VAF_Org_ID= " + VAF_Org_ID
                + "ORDER BY t.Rate DESC";
            bool found = false;
            try
            {
                DataSet pstmt = DB.ExecuteDataset(sql, null);
                for (int i = 0; i < pstmt.Tables[0].Rows.Count; i++)
                {
                    DataRow dr = pstmt.Tables[0].Rows[i];
                    VAB_TaxRate_ID = Util.GetValueOfInt(dr[0]);
                    found = true;
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            log.Fine("TaxExempt=Y - VAB_TaxRate_ID=" + VAB_TaxRate_ID);
            if (VAB_TaxRate_ID == 0)
            {
                log.SaveError("TaxCriteriaNotFound", Msg.GetMsg(ctx, "TaxNoExemptFound")
                    + (found ? "" : " (Tax/Org=" + VAF_Org_ID + " not found)"));
            }
            return VAB_TaxRate_ID;
        }

        private int GetTaxFromLocation(bool isSoTrx, int taxCategory, int Country_ID, int Region_ID, string Postal)
        {
            string sql = "";
            int VAB_TaxRate_ID = 0;
            if (String.IsNullOrEmpty(Postal))
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                    " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND NVL(tcr.VAB_RegionState_ID,0) = " + Region_ID +
                    " AND tcr.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            else
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                    " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND NVL(tcr.VAB_RegionState_ID,0) = " + Region_ID +
                    " AND (CASE WHEN (tcr.vatax_ispostal = 'Y') THEN CASE WHEN tcr.postal <= '" + Postal + "' AND tcr.postal_to >= '" + Postal + "' THEN 1 ELSE 2" +
                    " END ELSE  CASE WHEN tcr.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
            if (VAB_TaxRate_ID > 0)
            {
                return VAB_TaxRate_ID;
            }
            else
            {
                if (String.IsNullOrEmpty(Postal))
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                        " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND tcr.VAB_RegionState_ID IS NULL AND tcr.Postal IS NULL AND tx.SOPOType IN ('B','"
                        + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                else
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                        " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND tcr.VAB_RegionState_ID IS NULL AND (CASE WHEN (tcr.vatax_ispostal = 'Y') THEN CASE WHEN tcr.postal <= '" + Postal +
                        "' AND tcr.postal_to >= '" + Postal + "' THEN 1 ELSE 2" + " END ELSE  CASE WHEN tcr.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','"
                        + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                if (VAB_TaxRate_ID > 0)
                {
                    return VAB_TaxRate_ID;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Postal))
                    {
                        sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                            " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID IS NULL " + " AND tcr.VAB_RegionState_ID IS NULL AND (CASE WHEN (tcr.vatax_ispostal = 'Y') THEN CASE WHEN tcr.postal <= '"
                            + Postal + "' AND tcr.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN tcr.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','"
                            + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    }
                    if (VAB_TaxRate_ID > 0)
                    {
                        return VAB_TaxRate_ID;
                    }
                }
            }
            return VAB_TaxRate_ID;
        }

        private int GetTaxFromRegion(bool isSoTrx, int taxCategory, int Country_ID, int Region_ID, string Postal)
        {
            string sql = "";
            int VAB_TaxRate_ID = 0;
            if (String.IsNullOrEmpty(Postal))
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID +
                " AND trl.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            else
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID +
                " AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '" + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '"
                + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
            if (VAB_TaxRate_ID > 0)
            {
                return VAB_TaxRate_ID;
            }
            else
            {
                if (String.IsNullOrEmpty(Postal))
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND trl.VAB_RegionState_ID IS NULL AND trl.Postal IS NULL AND tx.SOPOType IN ('B','"
                    + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                else
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                    + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                if (VAB_TaxRate_ID > 0)
                {
                    return VAB_TaxRate_ID;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Postal))
                    {
                        sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                        + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID IS NULL AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                        + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));

                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                    }
                }
            }
            return VAB_TaxRate_ID;
        }

        private int GetTaxFromRegion(bool isSoTrx, int taxCategory, int Country_ID, int Region_ID, string Postal, int taxRegion, int toCountry)
        {
            string sql = "";
            int VAB_TaxRate_ID = 0;
            if (String.IsNullOrEmpty(Postal))
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID +
                " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID + " AND trl.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            else
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID +
                " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID + " AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '" + Postal + "' AND trl.postal_to >= '" + Postal +
                "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
            if (VAB_TaxRate_ID > 0)
            {
                return VAB_TaxRate_ID;
            }
            else
            {
                if (String.IsNullOrEmpty(Postal))
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID +
                    " AND trl.VAB_RegionState_ID IS NULL AND trl.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                else
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                    + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                if (VAB_TaxRate_ID > 0)
                {
                    return VAB_TaxRate_ID;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Postal))
                    {
                        sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                        + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID IS NULL AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                        + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                    }
                }
            }
            return VAB_TaxRate_ID;
        }
        // Added by mohit to remove client side queries- 12 May 2017
        /// <summary>
        /// Get Invoice payment Schedule Details
        /// </summary>        
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetInvPaySchedDetail(string fields)
        {
            Dictionary<string, object> retValue = null;
            int Invoice_ID = Util.GetValueOfInt(fields);
            DataSet _ds = null;
            string _Sql;
            //if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='VA009_'  AND IsActive = 'Y'", null, null)) > 0)
            if (Env.IsModuleInstalled("VA009_"))
            {
                //Added 2 new fields to get VA009_PaymentMethod_ID and VA009_PaymentBaseType To Set the corrosponding value on Payment Window..
                _Sql = "SELECT * FROM (SELECT ips.VAB_sched_InvoicePayment_ID,"
                             + " NVL(ips.DueAmt , 0) - NVL(ips.va009_paidamntinvce , 0) AS DueAmt, i.IsReturnTrx, IPS.VA009_PaymentMethod_ID, PM.VA009_PaymentBaseType FROM VAB_Invoice i"
                             + " INNER JOIN VAB_sched_InvoicePayment ips ON (i.VAB_Invoice_ID = ips.VAB_Invoice_ID) "
                             + " INNER JOIN VA009_PAYMENTMETHOD PM  ON (ips.VA009_PaymentMethod_ID = PM.VA009_paymentMethod_ID) WHERE i.IsPayScheduleValid='Y' "
                             + " AND ips.IsValid ='Y' AND ips.isactive ='Y' "
                             + " AND i.VAB_Invoice_ID = " + Invoice_ID
                             + " AND ips.VAB_sched_InvoicePayment_ID NOT IN"
                             + "(SELECT NVL(VAB_sched_InvoicePayment_ID,0) FROM VAB_sched_InvoicePayment WHERE VAB_Payment_id IN"
                             + "(SELECT NVL(VAB_Payment_id,0) FROM VAB_sched_InvoicePayment)  union "
                             + " SELECT NVL(VAB_sched_InvoicePayment_id,0) FROM VAB_sched_InvoicePayment WHERE VAB_CashJRNLLine_id IN"
                             + "(SELECT NVL(VAB_CashJRNLLine_id,0) FROM VAB_sched_InvoicePayment )) "
                             + " ORDER BY ips.duedate ASC) t WHERE rownum=1";
            }
            else
            {
                _Sql = "SELECT * FROM (SELECT ips.VAB_sched_InvoicePayment_ID,"
                                + " ips.DueAmt, i.IsReturnTrx FROM VAB_Invoice i  INNER JOIN VAB_sched_InvoicePayment ips "
                                + " ON (i.VAB_Invoice_ID = ips.VAB_Invoice_ID)  WHERE i.IsPayScheduleValid='Y' "
                                + " AND ips.IsValid = 'Y' AND ips.isactive = 'Y' "
                            + " AND i.VAB_Invoice_ID = " + Invoice_ID
                            + "  AND ips.VAB_sched_InvoicePayment_ID NOT IN"
                            + "(SELECT NVL(VAB_sched_InvoicePayment_ID,0) FROM VAB_sched_InvoicePayment WHERE VAB_Payment_id IN"
                            + "(SELECT NVL(VAB_Payment_id,0) FROM VAB_sched_InvoicePayment)  union "
                            + " SELECT NVL(VAB_sched_InvoicePayment_id,0) FROM VAB_sched_InvoicePayment WHERE VAB_CashJRNLLine_id IN"
                            + "(SELECT NVL(VAB_CashJRNLLine_id,0) FROM VAB_sched_InvoicePayment )) "
                            + " ORDER BY ips.duedate ASC) t WHERE rownum=1";
            }
            try
            {
                _ds = DB.ExecuteDataset(_Sql, null, null);
                if (_ds != null && _ds.Tables[0].Rows.Count > 0)
                {
                    retValue = new Dictionary<string, object>();
                    retValue["VAB_sched_InvoicePayment_ID"] = Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_sched_InvoicePayment_id"]);
                    retValue["dueAmount"] = Util.GetValueOfDecimal(_ds.Tables[0].Rows[0]["dueamt"]);
                    retValue["IsReturnTrx"] = Util.GetValueOfString(_ds.Tables[0].Rows[0]["IsReturnTrx"]);

                    //Added 2 new fields to get VA009_PaymentMethod_ID and VA009_PaymentBaseType To Set the corrosponding value on Payment Window..
                    if (Env.IsModuleInstalled("VA009_"))
                    {
                        retValue["VA009_PaymentMethod_ID"] = Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VA009_PaymentMethod_ID"]);
                        retValue["VA009_PaymentBaseType"] = Util.GetValueOfString(_ds.Tables[0].Rows[0]["VA009_PaymentBaseType"]);
                    }
                }
            }
            catch (Exception e)
            {
                if (_ds != null)
                {
                    _ds.Dispose();
                }
            }
            return retValue;
        }
        /// <summary>
        /// Get Invoice Details
        /// </summary>        
        /// <param name="fields">Parameters</param>
        /// <returns>Dictionaty, Invoice Data</returns>
        public Dictionary<string, object> GetInvoiceDetails(string fields)
        {
            DataSet _ds = null;
            string[] paramValue = fields.Split(',');
            Dictionary<string, object> retValue = null;
            string sql = "SELECT VAB_BusinessPartner_ID, VAB_Currency_ID, VAB_CurrencyType_ID, invoiceOpen(VAB_Invoice_ID, " + Util.GetValueOfInt(paramValue[3]) + @") as invoiceOpen, IsSOTrx, 
            paymentTermDiscount(invoiceOpen(VAB_Invoice_ID, 0),VAB_Currency_ID,VAB_PaymentTerm_ID,DateInvoiced, " + paramValue[1] + "," + paramValue[2]
            + " ) as paymentTermDiscount, VAB_DocTypesTarget_ID,VAB_BPart_Location_ID FROM VAB_Invoice WHERE VAB_Invoice_ID=" + Util.GetValueOfInt(paramValue[0]);
            try
            {
                _ds = DB.ExecuteDataset(sql, null, null);
                if (_ds != null && _ds.Tables[0].Rows.Count > 0)
                {
                    retValue = new Dictionary<string, object>();
                    retValue["VAB_BusinessPartner_ID"] = Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_BusinessPartner_ID"]);
                    retValue["VAB_Currency_ID"] = Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_Currency_ID"]);

                    // JID_1208: System should set Currency Type that is defined on Invoice.
                    retValue["VAB_CurrencyType_ID"] = Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_CurrencyType_ID"]);
                    retValue["invoiceOpen"] = Util.GetValueOfDecimal(_ds.Tables[0].Rows[0]["invoiceOpen"]);
                    retValue["IsSOTrx"] = Util.GetValueOfString(_ds.Tables[0].Rows[0]["IsSOTrx"]);
                    retValue["paymentTermDiscount"] = Util.GetValueOfDecimal(_ds.Tables[0].Rows[0]["paymentTermDiscount"]);
                    retValue["docbaseType"] = Util.GetValueOfString(DB.ExecuteScalar("SELECT DocBaseType FROM VAB_DocTypes WHERE VAB_DocTypes_ID = "
                        + Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_DocTypesTarget_ID"]), null, null));
                    retValue["VAB_BPart_Location_ID"] = Util.GetValueOfInt(_ds.Tables[0].Rows[0]["VAB_BPart_Location_ID"]); //Arpit
                }
            }
            catch (Exception e)
            {
                if (_ds != null)
                {
                    _ds.Dispose();
                }
            }

            return retValue;

        }

        // Added by Bharat on 24 May 2017
        /// <summary>
        /// Get Invoice Open Amount
        /// </summary>        
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetInvoiceAmount(string fields)
        {
            DataSet _ds = null;
            string[] paramValue = fields.Split(',');
            int VAB_Invoice_ID = Util.GetValueOfInt(paramValue[0]);
            int VAB_Bank_Acct_ID = Util.GetValueOfInt(paramValue[1]);
            DateTime? payDate = Util.GetValueOfDateTime(paramValue[2]);
            Dictionary<string, object> retValue = null;
            string sql = "SELECT currencyConvert(invoiceOpen(i.VAB_Invoice_ID, 0), i.VAB_Currency_ID,"
                + "ba.VAB_Currency_ID, i.DateInvoiced, i.VAB_CurrencyType_ID, i.VAF_Client_ID, i.VAF_Org_ID) as OpenAmt,"
            + " paymentTermDiscount(i.GrandTotal,i.VAB_Currency_ID,i.VAB_PaymentTerm_ID,i.DateInvoiced,'" + payDate + "') As DiscountAmt, i.IsSOTrx "
            + "FROM VAB_Invoice_v i, VAB_Bank_Acct ba "
            + "WHERE i.VAB_Invoice_ID = " + VAB_Invoice_ID + " AND ba.VAB_Bank_Acct_ID = " + VAB_Bank_Acct_ID;
            try
            {
                _ds = DB.ExecuteDataset(sql, null, null);
                if (_ds != null && _ds.Tables[0].Rows.Count > 0)
                {
                    retValue = new Dictionary<string, object>();
                    retValue["OpenAmt"] = Util.GetValueOfDecimal(_ds.Tables[0].Rows[0]["OpenAmt"]);
                    retValue["DiscountAmt"] = Util.GetValueOfDecimal(_ds.Tables[0].Rows[0]["DiscountAmt"]);
                    retValue["IsSOTrx"] = Util.GetValueOfString(_ds.Tables[0].Rows[0]["IsSOTrx"]);
                }
            }
            catch (Exception e)
            {
                if (_ds != null)
                {
                    _ds.Dispose();
                }
            }
            return retValue;
        }

        /// <summary>
        /// Get Price of Product
        /// </summary>
        /// <param name="ctx">Context</param>
        /// <param name="param">List of Parameters</param>
        /// <returns>List of Price Data</returns>
        public Dictionary<String, Object> GetPrices(Ctx ctx, string param)
        {
            string[] paramValue = param.Split(',');

            Dictionary<String, Object> retDic = new Dictionary<String, Object>();

            //Assign parameter value
            int _VAM_Product_Id = Util.GetValueOfInt(paramValue[0].ToString());
            //int _priceListVersion_Id = Util.GetValueOfInt(paramValue[1].ToString());
            int _VAB_Invoice_Id = Util.GetValueOfInt(paramValue[1].ToString());
            int _VAM_PFeature_SetInstance_Id = Util.GetValueOfInt(paramValue[2].ToString());
            int _VAB_UOM_Id = Util.GetValueOfInt(paramValue[3].ToString());
            int _vaf_client_Id = Util.GetValueOfInt(paramValue[4].ToString());
            int _VAB_BusinessPartner_Id = Util.GetValueOfInt(paramValue[5].ToString());
            //int _VAM_DiscountCalculation_ID = Util.GetValueOfInt(paramValue[5].ToString());
            //decimal _flatDiscount = Util.GetValueOfInt(paramValue[6].ToString());
            decimal _qtyEntered = Util.GetValueOfInt(paramValue[6].ToString());
            //End Assign parameter value

            StringBuilder sql = new StringBuilder();
            decimal PriceEntered = 0;
            decimal PriceList = 0;
            decimal PriceLimit = 0;
            int _VAM_PriceList_ID = 0;
            int _priceListVersion_Id = 0;
            int _VAM_DiscountCalculation_ID = 0;
            decimal _flatDiscount = 0;
            int countEd011 = 0;
            int countVAPRC = 0;

            countEd011 = Env.IsModuleInstalled("ED011_") ? 1 : 0;
            retDic["countEd011"] = countEd011.ToString();

            countVAPRC = Env.IsModuleInstalled("VAPRC_") ? 1 : 0;
            retDic["countVAPRC"] = countVAPRC.ToString();

            if (countEd011 > 0)
            {
                MVABOrderLineModel objOrd = new MVABOrderLineModel();
                _VAM_PriceList_ID = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAM_PriceList_ID FROM VAB_Invoice WHERE VAB_Invoice_ID = " + _VAB_Invoice_Id, null, null));

                MVAMPriceListVersionModel objPLV = new MVAMPriceListVersionModel();
                _priceListVersion_Id = objPLV.GetVAM_PriceListVersion_ID(ctx, _VAM_PriceList_ID.ToString());


                MBPartnerModel objBPartner = new MBPartnerModel();
                Dictionary<String, String> bpartner1 = objBPartner.GetBPartner(ctx, _VAB_BusinessPartner_Id.ToString());
                _VAM_DiscountCalculation_ID = Util.GetValueOfInt(bpartner1["VAM_DiscountCalculation_ID"]);
                _flatDiscount = Util.GetValueOfInt(bpartner1["FlatDiscount"]);

                if (_VAM_PFeature_SetInstance_Id > 0)
                {
                    sql.Append("SELECT COUNT(*) FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                     + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                     + " AND  VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_Id
                                     + "  AND VAB_UOM_ID=" + _VAB_UOM_Id);
                }
                else
                {
                    sql.Append("SELECT COUNT(*) FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                   + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                   + " AND  ( VAM_PFeature_SetInstance_ID = 0 OR VAM_PFeature_SetInstance_ID IS NULL ) "
                                   + "  AND VAB_UOM_ID=" + _VAB_UOM_Id);
                }
                int countrecord = Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, null));
                if (countrecord > 0)
                {
                    // Selected UOM Price Exist
                    sql.Clear();
                    if (_VAM_PFeature_SetInstance_Id > 0)
                    {
                        sql.Append("SELECT PriceStd , PriceList, PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                    + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                    + " AND  VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_Id
                                    + "  AND VAB_UOM_ID=" + _VAB_UOM_Id);
                    }
                    else
                    {
                        sql.Append("SELECT PriceStd , PriceList, PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                  + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                  + " AND  ( VAM_PFeature_SetInstance_ID = 0 OR VAM_PFeature_SetInstance_ID IS NULL ) "
                                  + "  AND VAB_UOM_ID=" + _VAB_UOM_Id);
                    }
                    DataSet ds = DB.ExecuteDataset(sql.ToString());
                    if (ds != null && ds.Tables.Count > 0)
                    {
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            //Flat Discount
                            PriceEntered = objOrd.FlatDiscount(_VAM_Product_Id, _vaf_client_Id, Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceStd"]),
                            _VAM_DiscountCalculation_ID, _flatDiscount, _qtyEntered);
                            //end
                            PriceList = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceList"]);
                            PriceLimit = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceLimit"]);
                        }
                    }
                }
                else //if (_VAM_PFeature_SetInstance_Id > 0 && countrecord == 0)
                {
                    sql.Clear();
                    sql.Append("SELECT PriceStd , PriceList, PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                + " AND  ( VAM_PFeature_SetInstance_ID = 0 OR VAM_PFeature_SetInstance_ID IS NULL ) "
                                + "  AND VAB_UOM_ID=" + _VAB_UOM_Id);
                    DataSet ds1 = DB.ExecuteDataset(sql.ToString());
                    if (ds1 != null && ds1.Tables.Count > 0 && ds1.Tables[0].Rows.Count > 0)
                    {
                        //Flat Discount
                        PriceEntered = objOrd.FlatDiscount(_VAM_Product_Id, _vaf_client_Id, Util.GetValueOfDecimal(ds1.Tables[0].Rows[0]["PriceStd"]),
                       _VAM_DiscountCalculation_ID, _flatDiscount, _qtyEntered);
                        //End
                        PriceList = Util.GetValueOfDecimal(ds1.Tables[0].Rows[0]["PriceList"]);
                        PriceLimit = Util.GetValueOfDecimal(ds1.Tables[0].Rows[0]["PriceLimit"]);
                    }
                    else
                    {
                        // get uom from product
                        var paramStr = _VAM_Product_Id.ToString();
                        MVAMProductModel objProduct = new MVAMProductModel();
                        var prodVAB_UOM_ID = objProduct.GetVAB_UOM_ID(ctx, paramStr);
                        sql.Clear();
                        if (_VAM_PFeature_SetInstance_Id > 0)
                        {
                            sql.Append("SELECT PriceStd , PriceList, PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                        + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                        + " AND  VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_Id
                                        + "  AND VAB_UOM_ID=" + prodVAB_UOM_ID);
                        }
                        else
                        {
                            sql.Append("SELECT PriceStd , PriceList, PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                      + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                      + " AND  ( VAM_PFeature_SetInstance_ID = 0 OR VAM_PFeature_SetInstance_ID IS NULL ) "
                                      + "  AND VAB_UOM_ID=" + prodVAB_UOM_ID);
                        }
                        DataSet ds = DB.ExecuteDataset(sql.ToString());
                        if (ds != null && ds.Tables.Count > 0)
                        {
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                //Flat Discount
                                PriceEntered = objOrd.FlatDiscount(_VAM_Product_Id, _vaf_client_Id, Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceStd"]),
                                _VAM_DiscountCalculation_ID, _flatDiscount, _qtyEntered);
                                //end
                                PriceList = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceList"]);
                                PriceLimit = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceLimit"]);
                            }
                            else if (_VAM_PFeature_SetInstance_Id > 0)
                            {
                                sql.Clear();
                                sql.Append("SELECT PriceStd , PriceList, PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + _VAM_Product_Id
                                            + " AND VAM_PriceListVersion_ID = " + _priceListVersion_Id
                                            + " AND  ( VAM_PFeature_SetInstance_ID = 0 OR VAM_PFeature_SetInstance_ID IS NULL ) "
                                            + "  AND VAB_UOM_ID=" + prodVAB_UOM_ID);
                                DataSet ds2 = DB.ExecuteDataset(sql.ToString());
                                if (ds2 != null && ds.Tables.Count > 0)
                                {
                                    if (ds2.Tables[0].Rows.Count > 0)
                                    {
                                        //Flat Discount
                                        PriceEntered = objOrd.FlatDiscount(_VAM_Product_Id, _vaf_client_Id, Util.GetValueOfDecimal(ds2.Tables[0].Rows[0]["PriceStd"]),
                                                                    _VAM_DiscountCalculation_ID, _flatDiscount, _qtyEntered);
                                        //End
                                        PriceList = Util.GetValueOfDecimal(ds2.Tables[0].Rows[0]["PriceList"]);
                                        PriceLimit = Util.GetValueOfDecimal(ds2.Tables[0].Rows[0]["PriceLimit"]);
                                    }
                                }
                            }
                        }
                        sql.Clear();
                        sql.Append("SELECT con.DivideRate FROM VAB_UOM_Conversion con INNER JOIN VAB_UOM uom ON con.VAB_UOM_ID = uom.VAB_UOM_ID WHERE con.IsActive = 'Y' " +
                                                   " AND con.VAM_Product_ID = " + _VAM_Product_Id +
                                                   " AND con.VAB_UOM_ID = " + prodVAB_UOM_ID + " AND con.VAB_UOM_To_ID = " + _VAB_UOM_Id);
                        var rate = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        if (rate == 0)
                        {
                            sql.Clear();
                            sql.Append("SELECT con.DivideRate FROM VAB_UOM_Conversion con INNER JOIN VAB_UOM uom ON con.VAB_UOM_ID = uom.VAB_UOM_ID WHERE con.IsActive = 'Y'" +
                                  " AND con.VAB_UOM_ID = " + prodVAB_UOM_ID + " AND con.VAB_UOM_To_ID = " + _VAB_UOM_Id);
                            rate = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        }
                        if (rate == 0)
                        {
                            rate = 1;
                        }
                        PriceEntered = PriceEntered * rate;
                        PriceList = PriceList * rate;
                        PriceLimit = PriceLimit * rate;
                    }
                }
            }

            retDic["PriceEntered"] = PriceEntered;
            retDic["PriceList"] = PriceList;
            retDic["PriceLimit"] = PriceLimit;
            return retDic;
        }

        /// <summary>
        /// Getting the percision values
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns>get Percision value</returns>
        public int GetPrecision(Ctx ctx, string fields)
        {
            string sql = "SELECT CC.StdPrecision FROM VAB_Invoice CI INNER JOIN VAB_Currency CC on CC.VAB_Currency_Id = CI.VAB_Currency_Id where CI.VAB_Invoice_ID= " + Util.GetValueOfInt(fields);
            int stdPrecision = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
            return stdPrecision;

        }
    }
}