//------------------------------------------------------------------------------
// The contents of this file are subject to the nopCommerce Public License Version 1.0 ("License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at  http://www.nopCommerce.com/License.aspx. 
// 
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. 
// See the License for the specific language governing rights and limitations under the License.
// 
// The Original Code is nopCommerce.
// The Initial Developer of the Original Code is NopSolutions.
// All Rights Reserved.
// 
// Contributor(s): _______. 
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using NopSolutions.NopCommerce.BusinessLogic;
using NopSolutions.NopCommerce.BusinessLogic.Audit;
using NopSolutions.NopCommerce.BusinessLogic.Configuration.Settings;
using NopSolutions.NopCommerce.BusinessLogic.CustomerManagement;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.Payment;
using NopSolutions.NopCommerce.BusinessLogic.Shipping;
using NopSolutions.NopCommerce.Common.Utils;
using NopSolutions.NopCommerce.Payment.Methods.NganLuong;


namespace NopSolutions.NopCommerce.Web.Modules
{
    public partial class CheckoutConfirmControl : BaseNopUserControl
    {
        ShoppingCart Cart = null;

        protected void btnNextStep_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                try
                {
                    PaymentInfo paymentInfo = this.PaymentInfo;
                    if (paymentInfo == null)
                        Response.Redirect("~/CheckoutPaymentInfo.aspx");
                    paymentInfo.BillingAddress = NopContext.Current.User.BillingAddress;
                    paymentInfo.ShippingAddress = NopContext.Current.User.ShippingAddress;
                    paymentInfo.CustomerLanguage = NopContext.Current.WorkingLanguage;
                    paymentInfo.CustomerCurrency = NopContext.Current.WorkingCurrency;

                    int orderID = 0;
                    string result = OrderManager.PlaceOrder(paymentInfo, NopContext.Current.User, out orderID, txtCustomerNote.Text.Trim());
                    this.PaymentInfo = null;
                    Order order = OrderManager.GetOrderByID(orderID);
                    if (!String.IsNullOrEmpty(result))
                    {
                        lError.Text = Server.HtmlEncode(result);
                        return;
                    }
                    else
                    {
                        PaymentManager.PostProcessPayment(order);
                    }
                    if (paymentInfo.IsNganLuong)
                    {
                        string url =
                            new NL_Checkout().buildCheckoutUrl(SettingManager.GetSettingValue("Common.StoreURL") +
                                                               "/CheckoutCompleted.aspx?isnganluong=1",
                                                               SettingManager.GetSettingValue(
                                                                   "PaymentMethod.NganLuong.TransactEmail"),
                                                               "",
                                                               order.OrderID.ToString(),
                                                               order.OrderTotal.ToString("#"),
                                                               SettingManager.GetSettingValue(
                                                                   "PaymentMethod.NganLuong.MerchantCode"),
                                                               SettingManager.GetSettingValue(
                                                                   "PaymentMethod.NganLuong.SecurePass"),
                                                               SettingManager.GetSettingValue(
                                                                   "PaymentMethod.NganLuong.URL"));
                        Response.Redirect(url);
                    }
                    else
                    {
                        Response.Redirect("~/CheckoutCompleted.aspx");
                    }
                }
                catch (Exception exc)
                {
                    LogManager.InsertLog(LogTypeEnum.OrderError, exc.Message, exc);
                    lError.Text = Server.HtmlEncode(exc.ToString());
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if ((NopContext.Current.User == null) || (NopContext.Current.User.IsGuest && !CustomerManager.AnonymousCheckoutAllowed))
            {
                string loginURL = CommonHelper.GetLoginPageURL(true);
                Response.Redirect(loginURL);
            }

            Cart = ShoppingCartManager.GetCurrentShoppingCart(ShoppingCartTypeEnum.ShoppingCart);
            if (Cart.Count == 0)
                Response.Redirect("~/ShoppingCart.aspx");

            this.btnNextStep.Attributes.Add("onclick", "this.disabled = true;" + Page.ClientScript.GetPostBackEventReference(this.btnNextStep, ""));
        }

        protected PaymentInfo PaymentInfo
        {
            get
            {
                if (this.Session["OrderPaymentInfo"] != null)
                    return (PaymentInfo)(this.Session["OrderPaymentInfo"]);
                return null;
            }
            set
            {
                this.Session["OrderPaymentInfo"] = value;
            }
        }
    }
}