﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using VAdvantage.DataBase;
using VAdvantage.Model;
using VAdvantage.Utility;

namespace VIS.Models
{
    public class MVAMInventoryLineModel
    {
        /// <summary>
        /// GetMVAMInventoryLine
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetMVAMInventoryLine(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            int VAM_InventoryLine_ID;

            //Assign parameter value
            VAM_InventoryLine_ID = Util.GetValueOfInt(paramValue[0].ToString());
            //End Assign parameter value
            MVAMInventoryLine iLine = new MVAMInventoryLine(ctx, VAM_InventoryLine_ID, null);
            int VAM_Product_ID = iLine.GetVAM_Product_ID();
            int VAM_Locator_ID = iLine.GetVAM_Locator_ID();

            Dictionary<string, string> retDic = new Dictionary<string, string>();
            retDic["VAM_Product_ID"] = VAM_Product_ID.ToString();
            retDic["VAM_Locator_ID"] = VAM_Locator_ID.ToString();

            return retDic;
        }

        // Added by mohit to get product UOM- 12 June 2018
        /// <summary>
        /// Get product UOM
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public string GetproductUOM(Ctx ctx, string field)
        {
            MProduct Product = new MProduct(ctx, Util.GetValueOfInt(field), null);
            return Product.GetVAB_UOM_ID().ToString();
        }

        /// <summary>
        /// Method to get the attribute of a product from a given UPC/EAN number.
        /// </summary>
        /// <param name="ctx">Current Context</param>
        /// <param name="field">Parameters sent from client side includes product id and attribute code(UPC/EAN).</param>
        /// <returns>Returns attributesetinstance id if found.</returns>
        /// Writer : Mohit.
        /// Date : 21 May 2019.
        public int GetProductAttribute(Ctx ctx, string field)
        {
            string[] Values = field.Split(',');
            int attributeSet_ID = 0;
            int Attribute_ID = 0;
            StringBuilder _sql = new StringBuilder();
            _sql.Append("SELECT VAM_PFeature_Set_ID FROM VAM_Product WHERE VAM_Product_ID = " + Util.GetValueOfInt(Values[0]));
            attributeSet_ID = Util.GetValueOfInt(DB.ExecuteScalar(_sql.ToString()));
            _sql.Clear();
            if (attributeSet_ID > 0)
            {
                _sql.Append("SELECT VAM_PFeature_SetInstance_ID FROM VAM_ProductFeatures WHERE UPC = '" + Values[1] + "' AND VAM_Product_ID = " + Util.GetValueOfInt(Values[0]));
                Attribute_ID = Util.GetValueOfInt(DB.ExecuteScalar(_sql.ToString()));
            }
            return Attribute_ID;
        }
    }
}