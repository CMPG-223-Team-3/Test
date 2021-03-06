﻿using MySql.Data.MySqlClient;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Website.App_Code;

/*NOTES:
 * The logged in user session name here is "UserName"
 * Make the products cards pretty
 * Need help with sass
 */

namespace Website
{
    public partial class CustomerOrder : System.Web.UI.Page
    {
        /*
         * This page is where the customer selects their order products, and clicks the checkout btn
         * 1. The page.load is pretty messy - might need a bit of work
         * 
         * 
         * 
         * 
         */

        private static Order order;
        private static CartPanel cartPanel;
        private MySqlConnection conn;
        private string pageName = HttpContext.Current.Request.Url.AbsoluteUri;
        private bool isSearched = false; //Has the user searched for something
        private bool isTheThing = false;

        //session var names
        private static string userNameSession = "UserName";
        private static string errorSession = "Error";
        private static string fromPageSession = "FromPage";
        private static string orderIDSession = "OrderID";
        private static string tableIDSession = "TableID";
        private static string selectedWaiterIDSession = "WaiterID";


        private static string menuItemID = "Menu_Item_ID";
        private static string menuItemName = "Item_Name";
        private static string menuItemDesc = "Item_Description";
        private static string menuItemPrice = "Price";
        private string cartSession = "Cart";

        private static string orderCookieName = "OrderCookie";
        private static string orderCookieSubName = "OrderIDCookie";

        protected void Page_Init(object o, EventArgs e)
        {
            try
            {
                //try to quickly connect to database to see if it works  
                DatabaseConnection connection = new DatabaseConnection();
                conn = connection.getConnection();

                if(Session[userNameSession] == null)
                {
                    throw new Exception("Username not logged in");
                }

                if (cartPanel != null)
                {
                    cartPanel.update();
                }
            }
            catch
            {
                Response.Redirect("CustomerLogin.aspx");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Session[errorSession] = null;
            Session[fromPageSession] = pageName;

            try
            {
                btnSearch_Click(new object(), new EventArgs());
                if (Session[userNameSession] != null)
                {//if the user has logged in, display their name instead of the log in label on the navbar
                    lblLogin.Text = Session[userNameSession].ToString();

                    if (Session[tableIDSession] != null)
                    {//if logged in user has tableid (should be if they logged in)
                        if (Session[orderIDSession] != null)
                        {//if the user had an order pending but closed the thing
                            if (order == null)
                            {
                                order = new Order(int.Parse(Session[orderIDSession].ToString())); //at this point we need a customer ID to make a new order, have to fix
                                order.updateOrderNameAndTable(Session[userNameSession].ToString(), int.Parse(Session[tableIDSession].ToString()));
                                if (Session[selectedWaiterIDSession] != null)
                                {
                                    if (order.updateOrderWaiter(int.Parse(Session[selectedWaiterIDSession].ToString())) != true)
                                    {
                                        throwEx(new Exception("Could not set your waiter, please contact a staff member"));
                                    }
                                }
                                else
                                {
                                    throwEx(new Exception("Could not find selected waiter, please log in again"));
                                }
                            }
                        }
                        else
                        {
                            if(order == null)
                            {
                                
                                if (Session[selectedWaiterIDSession] != null)
                                {
                                    order = new Order(Session[userNameSession].ToString(), int.Parse(Session[tableIDSession].ToString()), 0, 0, int.Parse(Session[selectedWaiterIDSession].ToString()), 0);
                                    Session[orderIDSession] = order.getOrderID();
                                }
                                else
                                {
                                    throwEx(new Exception("Could not find selected waiter, please log in again"));
                                }
                            }
                        }
                    }
                    else
                    {
                        Page_Init(new object(), new EventArgs());
                    }
                }
                else
                {
                    Response.Redirect("CustomerLogin.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                if(cartPanel == null)
                {
                    cartPanel = new CartPanel(order.getConnection(), order.getOrderID());
                }


                if (!IsPostBack || !isSearched)
                {//if the user hasn't searched anything or 1st time page loaded
                    showProducts(conn, "SELECT * FROM `MENU-ITEM`");
                }

                if (isSearched)
                {
                    //if the user did search in the products for what the user wants
                    isSearched = false;
                }

                pnlOrder.Controls.Clear();
                pnlOrder.Controls.Add(cartPanel.getHeadPanel());
            }
            catch(Exception ee)
            {
                throwEx(ee);
            }
        }

        protected void Page_LoadComplete(object sender, EventArgs e)
        {

        }
        
        //Method to load the correct products from the sent query (query in string form instead of a command var is easier to manipulate at this stage)
        public void showProducts(MySqlConnection mysqlConnection, String command)
        {
            int countedProducts = 0;
            try
            {
                //Clear the main products panel to avoid accidental doubles
                pnlMaster.Controls.Clear();

                //Search the database for the searched terms, convert to a reader object(s) to make easier to manipulate
                using(mysqlConnection)
                {
                    mysqlConnection.Open();
                    MySqlCommand mysqlCommand = new MySqlCommand(command, mysqlConnection);

                    using(MySqlDataReader mysqlReader = mysqlCommand.ExecuteReader())
                    {
                        if (mysqlReader != null)
                        {
                            //While database has x many products searched in command parm
                            while (mysqlReader.Read())
                            {
                                //Reading specific product info out of database to use for the cards
                                //According to the data model columns are: 0-Menu-Item-ID; 1-Recipe-ID; 2-Category-ID; 3-Name; 4-Price
                                string productId = mysqlReader[menuItemID].ToString();
                                string productName = mysqlReader[menuItemName].ToString();
                                string productPrice = mysqlReader[menuItemPrice].ToString();
                                string productDesc = mysqlReader[menuItemDesc].ToString();
    
                                countedProducts++;

                                //Create panel to serve as a card, so img, price, name can be added inside it
                                Panel pnl1 = new Panel();
                                pnl1.CssClass = "card row bg-dark mb-xs-2 mb-lg-3 text-light";

                                Panel pnlNameDesc = new Panel();
                                pnlNameDesc.CssClass = "col-sm-8 pnlNameDesc";

                                Panel pnlPriceBtn = new Panel();
                                pnlPriceBtn.CssClass = "col-sm-4 pnlPriceBtn";

                                //Label for the price
                                Label lblPrice = new Label();
                                lblPrice.Text = productPrice;
                                lblPrice.CssClass = "text-light";

                                //Creating image object (for future implementation)
                                /*Image img1 = new Image();
                                img1.ImageUrl = productImageUrl;
                                //im1.CssClass = "";
                                img1.AlternateText = "Product Image";*/

                                //Creating the add to cart button
                                Button btn1 = new Button();
                                btn1.Text = "Add to cart";
                                btn1.CssClass = "btn btn-light";
                                btn1.ID = productId + "_addtocart_" + countedProducts; //Using the product id as the button pressed id for the event that the button is pressed, so we can see which button was pressed
                                btn1.Click += new EventHandler(addToCartBtnClicked); //To correctly link the event to the event handler
                                btn1.CausesValidation = false;

                                //Label object for the name of the item
                                Label lblName = new Label();
                                lblName.Text = productName;

                                //Label object for the desc of the item
                                Label lblDesc = new Label();
                                lblDesc.Text = productDesc;

                                //Add items to their respective panels
                                pnlNameDesc.Controls.Add(lblName);
                                pnlNameDesc.Controls.Add(lblDesc);
                                pnlPriceBtn.Controls.Add(lblPrice);
                                pnlPriceBtn.Controls.Add(btn1);
                                pnl1.Controls.Add(pnlNameDesc);
                                pnl1.Controls.Add(pnlPriceBtn);

                                //Add panel to master panel
                                pnlMaster.Controls.Add(pnl1);
                            }
                        }
                        else
                        {
                            throw new Exception("Could not access database items");
                        }
                    }
                }
            }
            catch(Exception exc)
            {
                throwEx(exc);
            }
        }

        //Eventhandler/method for the add to cart buttons
        void addToCartBtnClicked(object sender, EventArgs evArgs)
        {
            try
            {
                //Code to identify which product clicked by getting the button's id which was programmed as the product's id in showProducts()
                Button btn = sender as Button;
                string[] i = btn.ID.Split('_');
                int Id = int.Parse(i[0]);
                cartPanel.order.getOrderItemsObject().addProduct(Id, 1);
                cartPanel.update();
            }
            catch(Exception x)
            {
                throwEx(x);
            }
            //Server.Transfer("CustomerOrder.aspx");
            btnSearch_Click(new object(), new EventArgs());
        }

        //When user searched for product
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            //If searchbar isn't empty
            if(txtSearch.Text != null)
            {
                try
                {
                    //Create new cmmd to ShowProducts() for filtered items
                    string tmp = "SELECT * FROM `MENU-ITEM` WHERE Item_Name LIKE '" + "%" + txtSearch.Text.ToLower() + "%" + "'";
                    showProducts(conn, tmp);
                }
                catch(Exception x)
                {
                    throwEx(x);
                }
            }
            isSearched = true;
        }

        /*private void checkoutBtnClicked(object sender, EventArgs e)
        {
            try
            {
                cartPanel.update();
                cartPanel.order.getOrderItemsObject().close();
                try
                {
                    HttpCookie userCookie = new HttpCookie(orderCookieName);
                    userCookie[orderCookieSubName] = cartPanel.order.getOrderID().ToString();
                    Response.Cookies.Add(userCookie);
                    userCookie.Expires = DateTime.Now.AddHours(5);
                }
                catch (Exception x)
                {
                    throw new HttpException();
                }

                Session[orderIDSession] = null;

                Response.Write("<script>alert('Thank you! Your waiter will be with you soon to confirm payment')<script>");
                Response.Redirect("OrderStatus.aspx", false);
            }
            catch (HttpException x)
            {
                Response.Write("<script>alert('It seems that we can't create a cookie to store your order... For order details, contact your waiter...')<script>");
                Response.Redirect("Default.aspx", false);
            }
            catch (Exception x)
            {
                throwEx(x);
            }            
        }*/

        private void throwEx(Exception x)
        {
            Session[errorSession] = x.Message;
            HttpContext.Current.Response.Redirect("Error.aspx", false);
        }

        protected void forcePostbackBtn_Click(object sender, EventArgs e)
        {
            // do nothing - for double postback purposes
        }
    }
}