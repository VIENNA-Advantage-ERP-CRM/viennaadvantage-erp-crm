﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVASResType
 * Purpose        : 
 * Class Used     : 
 * Chronological    Development
 * Raghunandan     15-Jun-2009
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


namespace VAdvantage.Model
{
    public class MVASResType : X_VAS_Res_Type
    {
        /* 	Standard Constructor
         *	@param ctx context
         *	@param VAS_Res_Type_ID id
         */
        public MVASResType(Ctx ctx, int VAS_Res_Type_ID, Trx trxName)
            : base(ctx, VAS_Res_Type_ID, trxName)
        {
            
        }	

        /**
         * 	Load Constructor
         *	@param ctx context
         *	@param dr result set
         */
        public MVASResType(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
            
        }

        /**
         * 	After Save
         *	@param newRecord new
         *	@param success success
         *	@return true
         */
        protected override bool AfterSave(bool newRecord, bool success)
        {
            if (!success)
                return success;

            //	Update Products
            if (!newRecord)
            {
                MVAMProduct[] products = MVAMProduct.Get(GetCtx(), "VAS_Resource_ID IN "
                    + "(SELECT VAS_Resource_ID FROM VAS_Resource WHERE VAS_Res_Type_ID="
                    + GetVAS_Res_Type_ID() + ")", Get_TrxName());
                for (int i = 0; i < products.Length; i++)
                {
                    MVAMProduct product = products[i];
                    if (product.SetResource(this))
                        product.Save(Get_TrxName());
                }
            }

            return success;
        }

    }
}