using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Saffrat.Hubs;
using Saffrat.Models.AccountingEngine;
using Saffrat.Models;
using Saffrat.Services;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Saffrat.ViewModels;
using Saffrat.Helpers;

namespace Saffrat.Controllers
{
    public class POSController : BaseController
    {
        private readonly ILogger<POSController> _logger;
        private readonly RestaurantDBContext _dbContext;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IConverter _converter;


        public POSController(ILogger<POSController> logger, RestaurantDBContext dbContext,
            IHubContext<NotificationHub> hub, IConverter converter,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _hub = hub;
            _converter = converter;

        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult Index()
        {
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = _dbContext.Users.FirstOrDefault(x => x.UserName == userName);
            ViewBag.IsVanSales = currentUser?.IsVanSales ?? false;

            POSVM pos = new()
            {
                TaxRates = _dbContext.TaxRates.ToList(),
                Discounts = _dbContext.Discounts.ToList(),
                Charges = _dbContext.Charges.ToList(),
                DefaultCustomer = _dbContext.Customers.FirstOrDefault(x => x.Id == 1),
                Waiters = _dbContext.Users.Where(x => x.Role == "waiter").ToList(),
                Drivers = _dbContext.Users.Where(x => x.Role == "deliveryman").ToList(),
                PaymentMethods = _dbContext.PaymentMethods.ToList(),
                DefaultOrderType = GetSetting.DefaultOrderType
            };

            return View(pos);
        }

        //Get Data First Time On POS Screen
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult GetPOSData()
        {
            JsonSerializerOptions options = new()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };

            var foodGroups = _dbContext.FoodGroups.Where(x => x.Status.Equals(true)).ToList();
            var foodItems = _dbContext.FoodItems.ToList();
            var modifiers = _dbContext.Modifiers.ToList();

            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = _dbContext.Users.FirstOrDefault(x => x.UserName == userName);

            // Filter menu items by Sales Type if the user is restricted
            if (currentUser != null && (currentUser.IsVanSales || !string.IsNullOrEmpty(currentUser.PermittedPriceTypes)))
            {
                var permittedTypes = !string.IsNullOrEmpty(currentUser.PermittedPriceTypes) 
                    ? currentUser.PermittedPriceTypes.Split(',').ToList() 
                    : new List<string>();
                
                if (currentUser.IsVanSales && !permittedTypes.Contains("VanSale"))
                    permittedTypes.Add("VanSale");

                foodItems = foodItems.Where(x => 
                    (string.IsNullOrEmpty(x.PermittedSalesTypes)) || 
                    (!currentUser.IsVanSales && x.PermittedSalesTypes.Contains("POS")) ||
                    permittedTypes.Any(t => x.PermittedSalesTypes != null && x.PermittedSalesTypes.Contains(t))
                ).ToList();

                // If strictly VanSales, only show items with "VanSale" in PermittedSalesTypes
                if (currentUser.IsVanSales)
                {
                    foodItems = foodItems.Where(x => x.PermittedSalesTypes != null && x.PermittedSalesTypes.Contains("VanSale")).ToList();
                }

                // Filter groups to only show those that have at least one visible item
                var visibleGroupIds = foodItems.Select(x => x.GroupId).Distinct().ToList();
                foodGroups = foodGroups.Where(x => visibleGroupIds.Contains(Convert.ToInt32(x.Id))).ToList();
            }

            var response = new Dictionary<string, string>
            {
                { "foodGroups", JsonSerializer.Serialize(foodGroups, options) },
                { "foodItems", JsonSerializer.Serialize(foodItems, options) },
                { "modifiers", JsonSerializer.Serialize(modifiers, options) },
                { "permittedPriceTypes", currentUser?.PermittedPriceTypes ?? "" },
                { "permittedOrderTypes", currentUser?.PermittedOrderTypes ?? "" }
            };

            return Json(response);
        }

        // Insert New Order
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> SaveRunningOrder(int[] ItemIds, int[] Quantities, string[] Modifiers, decimal[] ItemPrices, string Note, int CustomerId,
            int OrderType, string TableName, int TaxId, int DiscountId, int ChargeId, int Guests, string WaiterOrDriver, string PriceType, 
            string DiscountReason = "", decimal CustomDiscountValue = 0, bool CustomDiscountIsPercentage = false)
        {
            var results = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var table = _dbContext.RestaurantTables.FirstOrDefault(x => x.TableName == TableName);
            var tax = _dbContext.TaxRates.FirstOrDefault(x => x.Id == TaxId);
            var discount = _dbContext.Discounts.FirstOrDefault(x => x.Id == DiscountId);
            var charge = _dbContext.Charges.FirstOrDefault(x => x.Id == ChargeId);
            var customer = _dbContext.Customers.FirstOrDefault(x => x.Id == CustomerId);

            if (!IsWorkPeriodStarted())
            {
                results.Add("status", "error");
                results.Add("message", "Work Period not started yet.");
            }
            else if (OrderType < 1 || OrderType > 3)
            {
                results.Add("status", "error");
                results.Add("message", "Please select order type.");
            }
            else if (OrderType == 1 && table == null)
            {
                results.Add("status", "error");
                results.Add("message", "Please select table.");
            }
            else if (tax == null)
            {
                results.Add("status", "error");
                results.Add("message", "Please select tax.");
            }
            else if (DiscountId != 0 && discount == null)
            {
                results.Add("status", "error");
                results.Add("message", "Please select discount.");
            }
            else if (charge == null)
            {
                results.Add("status", "error");
                results.Add("message", "Please select charge.");
            }
            else if (customer == null)
            {
                results.Add("status", "error");
                results.Add("message", "Please select customer.");
            }
            else if (PriceType == "VanSale" && CustomerId == GetSetting.DefaultCustomer)
            {
                results.Add("status", "error");
                results.Add("message", "Customer selection is mandatory for Van Sales.");
            }
            else if (ItemIds.Length <= 0)
            {
                results.Add("status", "error");
                results.Add("message", "Please add items in cart.");
            }
            else if (ItemIds.Length == Quantities.Length && Quantities.Length == Modifiers.Length)
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    if (OrderType == 1 || OrderType == 2)
                    {
                        var user = _dbContext.Users.FirstOrDefault(x => x.UserName == WaiterOrDriver && x.Role == "waiter");
                        WaiterOrDriver = user == null ? String.Empty : WaiterOrDriver;
                    }
                    else
                    {
                        var user = _dbContext.Users.FirstOrDefault(x => x.UserName == WaiterOrDriver && x.Role == "deliveryman");
                        WaiterOrDriver = user == null ? String.Empty : WaiterOrDriver;
                    }

                    RunningOrder order;

                    order = new()
                    {
                        TableName = table != null ? table.TableName : "-",
                        CustomerId = CustomerId,
                        WaiterOrDriver = WaiterOrDriver,
                        Guests = Guests,
                        OrderType = OrderType,
                        Status = GetSetting.SkipKitchenOrder ? 2 : 1,
                        Note = !String.IsNullOrEmpty(Note) ? Note : String.Empty,
                        CreatedBy = userName,
                        CreatedAt = CurrentDateTime(),
                        TaxId = TaxId,
                        DiscountId = DiscountId == 0 ? (int?)null : DiscountId,
                        ChargesId = ChargeId,
                        PriceType = PriceType,
                        TaxTotal = 0,
                        DiscountTotal = 0,
                        DiscountReason = DiscountReason,
                        ChargeTotal = 0,
                        SubTotal = 0,
                        PaidAmount = 0,
                        Total = 0,
                        DueAmount = 0
                    };

                    if (OrderType == 1 && table != null)
                    {
                        table.Status = true;
                        _dbContext.RestaurantTables.Update(table);
                    }

                    _dbContext.RunningOrders.Add(order);
                    _dbContext.SaveChanges();
                    if (order.Id == 1)
                    {
                        var corder = _dbContext.Orders.OrderByDescending(x => x.Id).FirstOrDefault();
                        if (corder != null)
                        {
                            _dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT(\"RunningOrders\",RESEED," + corder.Id + ")");
                            _dbContext.RunningOrders.Remove(order);
                            _dbContext.SaveChanges();

                            order = new()
                            {
                                TableName = table != null ? table.TableName : "-",
                                CustomerId = CustomerId,
                                WaiterOrDriver = WaiterOrDriver,
                                Guests = Guests,
                                OrderType = OrderType,
                                Status = GetSetting.SkipKitchenOrder ? 2 : 1,
                                Note = !String.IsNullOrEmpty(Note) ? Note : String.Empty,
                                CreatedBy = userName,
                                CreatedAt = CurrentDateTime(),
                                TaxId = TaxId,
                                DiscountId = DiscountId == 0 ? (int?)null : DiscountId,
                                ChargesId = ChargeId,
                                PriceType = PriceType,
                                TaxTotal = 0,
                                DiscountTotal = 0,
                                DiscountReason = DiscountReason,
                                ChargeTotal = 0,
                                SubTotal = 0,
                                PaidAmount = 0,
                                Total = 0,
                                DueAmount = 0
                            };

                            _dbContext.RunningOrders.Add(order);
                            _dbContext.SaveChanges();
                        }
                    }

                    for (int i = 0; i < ItemIds.Length; i++)
                    {
                        var item = _dbContext.FoodItems.FirstOrDefault(x => x.Id == ItemIds[i]);
                        if (item != null)
                        {
                            var detailPrice = (ItemPrices != null && ItemPrices.Length > i) ? ItemPrices[i] : item.Price;
                            
                            if (ItemPrices == null || ItemPrices.Length <= i) {
                                if (PriceType == "VanSale") detailPrice = item.VanSalePrice;
                                else if (PriceType == "WholeSale") detailPrice = item.WholeSalePrice;
                            }

                            RunningOrderDetail orderDetail = new()
                            {
                                OrderId = order.Id,
                                ItemId = item.Id,
                                Price = detailPrice,
                                Quantity = Quantities[i],
                                ModifierTotal = 0,
                                Total = 0,
                                CreatedAt = CurrentDateTime(),
                            };
                            _dbContext.RunningOrderDetails.Add(orderDetail);
                            _dbContext.SaveChanges();

                            decimal modifierTotal = InsertRunningOrderModifiers(order.Id, orderDetail.Id, Modifiers[i]);
                            orderDetail.ModifierTotal = modifierTotal;
                            orderDetail.Total = (orderDetail.Price + orderDetail.ModifierTotal) * orderDetail.Quantity;
                            _dbContext.RunningOrderDetails.Update(orderDetail);

                            order.SubTotal += Math.Round((decimal)orderDetail.Total, 2);
                        }
                    }
                    order.Total = order.SubTotal;
                    //apply discount
                    if (DiscountId == 0)
                    {
                        if (CustomDiscountIsPercentage)
                        {
                            var perc = CustomDiscountValue / 100;
                            order.DiscountTotal = order.SubTotal * perc;
                        }
                        else
                        {
                            order.DiscountTotal = CustomDiscountValue;
                        }
                        order.Total = order.SubTotal - order.DiscountTotal;
                    }
                    else if (discount.IsPercentage)
                    {
                        var perc = discount.Value / 100;
                        order.DiscountTotal = order.SubTotal * perc;
                        order.Total = order.SubTotal - order.DiscountTotal;
                    }
                    else
                    {
                        order.Total -= discount.Value;
                        order.DiscountTotal = discount.Value;
                    }

                    //apply tax
                    if (tax.IsPercentage)
                    {
                        var perc = tax.Value / 100;
                        order.TaxTotal = order.Total * perc;
                        order.Total += order.TaxTotal;
                    }
                    else
                    {
                        order.Total += tax.Value;
                        order.TaxTotal = tax.Value;
                    }

                    //apply charges
                    if (charge.IsPercentage)
                    {
                        var perc = charge.Value / 100;
                        order.ChargeTotal = order.Total * perc;
                        order.Total += order.ChargeTotal;
                    }
                    else
                    {
                        order.Total += charge.Value;
                        order.ChargeTotal = charge.Value;
                    }

                    order.PaidAmount = 0;

                    order.DueAmount = order.Total - order.PaidAmount;

                    _dbContext.RunningOrders.Update(order);
                    await _dbContext.SaveChangesAsync();

                    transaction.Commit();

                    await _hub.Clients.Group("admin").SendAsync("OrderNotification", "created", order.Id);
                    await _hub.Clients.Group("staff").SendAsync("OrderNotification", "created", order.Id);
                    if (!String.IsNullOrEmpty(order.WaiterOrDriver))
                        await _hub.Clients.User(order.WaiterOrDriver).SendAsync("OrderNotification", "created", order.Id);

                    results.Add("status", "success");
                    results.Add("message", "success");
                    results.Add("id", order.Id.ToString());
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    results.Add("status", "error");
                    results.Add("message", "Something went wrong." + ex.Message);
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Something went wrong.");
            }

            return Json(results);
        }

        // Update Running Order
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateRunningOrder(int OrderId, int[] ItemIds, int[] Quantities, string[] Modifiers, decimal[] ItemPrices, string Note, int CustomerId,
            int OrderType, string TableName, int TaxId, int DiscountId, int ChargeId, int Guests, string WaiterOrDriver, string PriceType,
            string DiscountReason = "", decimal CustomDiscountValue = 0, bool CustomDiscountIsPercentage = false)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var table = _dbContext.RestaurantTables.FirstOrDefault(x => x.TableName == TableName);
            var tax = _dbContext.TaxRates.FirstOrDefault(x => x.Id == TaxId);
            var discount = _dbContext.Discounts.FirstOrDefault(x => x.Id == DiscountId);
            var charge = _dbContext.Charges.FirstOrDefault(x => x.Id == ChargeId);
            var customer = _dbContext.Customers.FirstOrDefault(x => x.Id == CustomerId);

            if (!IsWorkPeriodStarted())
            {
                response.Add("status", "error");
                response.Add("message", "Work Period not started yet.");
            }
            else if (OrderType < 1 || OrderType > 3)
            {
                response.Add("status", "error");
                response.Add("message", "Please select order type.");
            }
            else if (OrderType == 1 && table == null)
            {
                response.Add("status", "error");
                response.Add("message", "Please select table.");
            }
            else if (tax == null)
            {
                response.Add("status", "error");
                response.Add("message", "Please select tax.");
            }
            else if (DiscountId != 0 && discount == null)
            {
                response.Add("status", "error");
                response.Add("message", "Please select discount.");
            }
            else if (charge == null)
            {
                response.Add("status", "error");
                response.Add("message", "Please select charge.");
            }
            else if (customer == null)
            {
                response.Add("status", "error");
                response.Add("message", "Please select customer.");
            }
            else if (PriceType == "VanSale" && CustomerId == GetSetting.DefaultCustomer)
            {
                response.Add("status", "error");
                response.Add("message", "Customer selection is mandatory for Van Sales.");
            }
            else if (ItemIds.Length <= 0)
            {
                response.Add("status", "error");
                response.Add("message", "Please add items in cart.");
            }
            else if (ItemIds.Length == Quantities.Length && Quantities.Length == Modifiers.Length)
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    if (OrderType == 1 || OrderType == 2)
                    {
                        var user = _dbContext.Users.FirstOrDefault(x => x.UserName == WaiterOrDriver && x.Role == "waiter");
                        WaiterOrDriver = user == null ? String.Empty : WaiterOrDriver;
                    }
                    else
                    {
                        var user = _dbContext.Users.FirstOrDefault(x => x.UserName == WaiterOrDriver && x.Role == "deliveryman");
                        WaiterOrDriver = user == null ? String.Empty : WaiterOrDriver;
                    }
                    var order = _dbContext.RunningOrders.Where(x => x.Id == OrderId)
                        .Include(x => x.RunningOrderDetails)
                        .FirstOrDefault();

                    if (order != null)
                    {
                        if (OrderType == 1 && table != null)
                        {
                            if (order.OrderType == 1 && order.TableName != table.TableName)
                            {
                                var existingTable = _dbContext.RestaurantTables.FirstOrDefault(x => x.TableName == order.TableName);
                                if (existingTable != null)
                                {
                                    existingTable.Status = false;
                                    _dbContext.RestaurantTables.Update(existingTable);
                                }
                            }

                            table.Status = true;
                            _dbContext.RestaurantTables.Update(table);
                        }
                        else if (order.OrderType == 1)
                        {
                            var existingTable = _dbContext.RestaurantTables.FirstOrDefault(x => x.TableName == order.TableName);
                            if (existingTable != null)
                            {
                                existingTable.Status = false;
                                _dbContext.RestaurantTables.Update(existingTable);
                            }
                        }

                        _dbContext.SaveChanges();
                        order.TableName = table != null ? table.TableName : "-";
                        order.CustomerId = CustomerId;
                        order.WaiterOrDriver = WaiterOrDriver;
                        order.Guests = Guests;
                        order.OrderType = OrderType;
                        order.Note = !String.IsNullOrEmpty(Note) ? Note : String.Empty;
                        order.TaxId = TaxId;
                        order.DiscountId = DiscountId == 0 ? (int?)null : DiscountId;
                        order.DiscountReason = DiscountReason;
                        order.ChargesId = ChargeId;
                        order.PriceType = PriceType;
                        order.WaiterOrDriver = WaiterOrDriver;
                        order.SubTotal = 0;

                        foreach (var item in order.RunningOrderDetails)
                        {
                            _dbContext.RunningOrderDetails.Remove(item);
                        }
                        _dbContext.SaveChanges();
                        for (int i = 0; i < ItemIds.Length; i++)
                        {
                            var item = _dbContext.FoodItems.FirstOrDefault(x => x.Id == ItemIds[i]);
                            if (item != null)
                            {
                                var detailPrice = (ItemPrices != null && ItemPrices.Length > i) ? ItemPrices[i] : item.Price;
                                
                                if (ItemPrices == null || ItemPrices.Length <= i) {
                                    if (PriceType == "VanSale") detailPrice = item.VanSalePrice;
                                    else if (PriceType == "WholeSale") detailPrice = item.WholeSalePrice;
                                }

                                RunningOrderDetail orderDetail = new()
                                {
                                    OrderId = order.Id,
                                    ItemId = item.Id,
                                    Price = detailPrice,
                                    Quantity = Quantities[i],
                                    ModifierTotal = 0,
                                    Total = 0,
                                    CreatedAt = CurrentDateTime(),
                                };
                                _dbContext.RunningOrderDetails.Add(orderDetail);
                                _dbContext.SaveChanges();

                                decimal modifierTotal = InsertRunningOrderModifiers(order.Id, orderDetail.Id, Modifiers[i]);
                                orderDetail.ModifierTotal = modifierTotal;
                                orderDetail.Total = (orderDetail.Price + orderDetail.ModifierTotal) * orderDetail.Quantity;
                                _dbContext.RunningOrderDetails.Update(orderDetail);

                                order.SubTotal += Math.Round((decimal)orderDetail.Total, 2);
                            }
                        }
                        order.Total = order.SubTotal;
                        //apply discount
                        if (DiscountId == 0)
                        {
                            if (CustomDiscountIsPercentage)
                            {
                                var perc = CustomDiscountValue / 100;
                                order.DiscountTotal = order.SubTotal * perc;
                            }
                            else
                            {
                                order.DiscountTotal = CustomDiscountValue;
                            }
                            order.Total = order.SubTotal - order.DiscountTotal;
                        }
                        else if (discount.IsPercentage)
                        {
                            var perc = discount.Value / 100;
                            order.DiscountTotal = order.SubTotal * perc;
                            order.Total = order.SubTotal - order.DiscountTotal;
                        }
                        else
                        {
                            order.Total -= discount.Value;
                            order.DiscountTotal = discount.Value;
                        }

                        //apply tax
                        if (tax.IsPercentage)
                        {
                            var perc = tax.Value / 100;
                            order.TaxTotal = order.Total * perc;
                            order.Total += order.TaxTotal;
                        }
                        else
                        {
                            order.Total += tax.Value;
                            order.TaxTotal = tax.Value;
                        }

                        //apply charges
                        if (charge.IsPercentage)
                        {
                            var perc = charge.Value / 100;
                            order.ChargeTotal = order.Total * perc;
                            order.Total += order.ChargeTotal;
                        }
                        else
                        {
                            order.Total += charge.Value;
                            order.ChargeTotal = charge.Value;
                        }

                        order.PaidAmount = 0;

                        order.DueAmount = order.Total - order.PaidAmount;
                        _dbContext.RunningOrders.Update(order);
                        await _dbContext.SaveChangesAsync();
                        transaction.Commit();

                        await _hub.Clients.Group("admin").SendAsync("OrderNotification", "updated", order.Id);
                        await _hub.Clients.Group("staff").SendAsync("OrderNotification", "updated", order.Id);
                        if (!String.IsNullOrEmpty(order.WaiterOrDriver))
                            await _hub.Clients.User(order.WaiterOrDriver).SendAsync("OrderNotification", "updated", order.Id);

                        response.Add("status", "success");
                        response.Add("message", "success");
                        response.Add("id", order.Id.ToString());
                    }
                    else
                    {
                        response.Add("status", "error");
                        response.Add("message", "Order not exist.");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    response.Add("status", "error");
                    response.Add("message", "Something went wrong.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Split Order
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> SplitOrder(int? OrderId, int[] OrderDetailIds)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var order = _dbContext.RunningOrders.Where(x => x.Id == OrderId && x.Status == 2)
                    .Include(x => x.Customer)
                    .Include(x => x.Tax)
                    .Include(x => x.Discount)
                    .Include(x => x.Charges)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefault();

                if (OrderDetailIds.Length <= 0)
                {
                    response.Add("status", "error");
                    response.Add("message", "Please select items for split.");
                }
                else if (OrderDetailIds.Length == order.RunningOrderDetails.Count)
                {
                    response.Add("status", "error");
                    response.Add("message", "You can't split all items.");
                }
                else if (order == null)
                {
                    response.Add("status", "error");
                    response.Add("message", "Order not exist.");
                }
                else
                {
                    RunningOrder newOrder = new()
                    {
                        Id = 0,
                        CustomerId = order.CustomerId,
                        TableName = order.TableName,
                        WaiterOrDriver = order.WaiterOrDriver,
                        Guests = order.Guests,
                        TaxId = order.TaxId,
                        DiscountId = order.DiscountId,
                        ChargesId = order.ChargesId,
                        SubTotal = 0,
                        TaxTotal = 0,
                        DiscountTotal = 0,
                        ChargeTotal = 0,
                        Total = 0,
                        PaymentMethod = "",
                        PaidAmount = 0,
                        DueAmount = 0,
                        OrderType = order.OrderType,
                        Status = order.Status,
                        Note = order.Note,
                        CreatedBy = userName,
                        CreatedAt = CurrentDateTime(),
                    };

                    _dbContext.RunningOrders.Add(newOrder);
                    _dbContext.SaveChanges();

                    order.SubTotal = 0;
                    foreach (var item in order.RunningOrderDetails)
                    {
                        if (OrderDetailIds.Contains(item.Id))
                        {
                            item.OrderId = newOrder.Id;
                            newOrder.SubTotal += Math.Round((decimal)item.Total, 2);
                            _dbContext.RunningOrderDetails.Update(item);
                            _dbContext.SaveChanges();
                        }
                        else
                        {
                            order.SubTotal += Math.Round((decimal)item.Total, 2);
                        }
                    }

                    order.Total = order.SubTotal;
                    newOrder.Total = newOrder.SubTotal;

                    //apply discount
                    if (order.Discount.IsPercentage)
                    {
                        var perc = order.Discount.Value / 100;
                        order.DiscountTotal = order.SubTotal * perc;
                        order.Total = order.SubTotal - order.DiscountTotal;
                        newOrder.DiscountTotal = newOrder.SubTotal * perc;
                        newOrder.Total = newOrder.SubTotal - newOrder.DiscountTotal;
                    }
                    else
                    {
                        order.Total -= order.Discount.Value;
                        order.DiscountTotal = order.Discount.Value;
                        newOrder.Total -= order.Discount.Value;
                        newOrder.DiscountTotal = order.Discount.Value;
                    }

                    //apply tax
                    if (order.Tax.IsPercentage)
                    {
                        var perc = order.Tax.Value / 100;
                        order.TaxTotal = order.Total * perc;
                        order.Total += order.TaxTotal;
                        newOrder.TaxTotal = newOrder.Total * perc;
                        newOrder.Total += newOrder.TaxTotal;
                    }
                    else
                    {
                        order.Total += order.Tax.Value;
                        order.TaxTotal = order.Tax.Value;
                        newOrder.Total += order.Tax.Value;
                        newOrder.TaxTotal = order.Tax.Value;
                    }

                    //apply charges
                    if (order.Charges.IsPercentage)
                    {
                        var perc = order.Charges.Value / 100;
                        order.ChargeTotal = order.Total * perc;
                        order.Total += order.ChargeTotal;
                        newOrder.ChargeTotal = newOrder.Total * perc;
                        newOrder.Total += newOrder.ChargeTotal;
                    }
                    else
                    {
                        order.Total += order.Charges.Value;
                        order.ChargeTotal = order.Charges.Value;
                        newOrder.Total += order.Charges.Value;
                        newOrder.ChargeTotal = order.Charges.Value;
                    }

                    order.PaidAmount = 0;
                    newOrder.PaidAmount = 0;
                    order.DueAmount = order.Total - order.PaidAmount;
                    newOrder.DueAmount = newOrder.Total - newOrder.PaidAmount;
                    _dbContext.RunningOrders.Update(order);
                    _dbContext.RunningOrders.Update(newOrder);
                    _dbContext.SaveChanges();
                    transaction.Commit();

                    await _hub.Clients.Group("admin").SendAsync("OrderNotification", "updated", order.Id);
                    await _hub.Clients.Group("staff").SendAsync("OrderNotification", "updated", order.Id);
                    if (!String.IsNullOrEmpty(order.WaiterOrDriver))
                        await _hub.Clients.User(order.WaiterOrDriver).SendAsync("OrderNotification", "updated", order.Id);

                    await _hub.Clients.Group("admin").SendAsync("OrderNotification", "created", newOrder.Id);
                    await _hub.Clients.Group("staff").SendAsync("OrderNotification", "created", newOrder.Id);
                    if (!String.IsNullOrEmpty(order.WaiterOrDriver))
                        await _hub.Clients.User(order.WaiterOrDriver).SendAsync("OrderNotification", "created", newOrder.Id);

                    response.Add("status", "success");
                    response.Add("message", "success");
                    response.Add("id", newOrder.Id.ToString());
                }
            }
            catch
            {
                transaction.Rollback();
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Close Running Order
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> CloseOrder([Required] int Id, [Required] string PaymentMethod, decimal PaidAmount = 0)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                if (ModelState.IsValid)
                {
                    var oorder = _dbContext.RunningOrders.Where(x => x.Id == Id)
                                .Include(x => x.Customer)
                                .Include(x => x.RunningOrderDetails)
                                .ThenInclude(x => x.Item)
                                .Include(x => x.RunningOrderDetails)
                                .ThenInclude(x => x.RunningOrderItemModifiers)
                                .ThenInclude(x => x.Modifier)
                                .FirstOrDefault();

                    if (PaidAmount < 0 || PaidAmount > oorder.Total)
                    {
                        response.Add("status", "error");
                        response.Add("message", "Please enter valid amount.");
                    }
                    else if (oorder != null)
                    {
                        if (oorder.PriceType == "VanSale" && oorder.CustomerId == GetSetting.DefaultCustomer)
                        {
                            response.Add("status", "error");
                            response.Add("message", "Customer selection is mandatory for Van Sales.");
                            return Json(response);
                        }

                        if (oorder.OrderType == 1)
                        {
                            var existingTable = _dbContext.RestaurantTables.FirstOrDefault(x => x.TableName == oorder.TableName);
                            if (existingTable != null)
                            {
                                existingTable.Status = false;
                                _dbContext.RestaurantTables.Update(existingTable);
                            }
                        }
                        _dbContext.SaveChanges();

                        Order order = new()
                        {
                            Id = oorder.Id,
                            CustomerId = oorder.CustomerId,
                            TableName = oorder.TableName,
                            WaiterOrDriver = oorder.WaiterOrDriver,
                            Guests = oorder.Guests,
                            SubTotal = oorder.SubTotal,
                            TaxTotal = oorder.TaxTotal,
                            DiscountTotal = oorder.DiscountTotal,
                            ChargeTotal = oorder.ChargeTotal,
                            Total = oorder.Total,
                            DiscountReason = oorder.DiscountReason,
                            PaymentMethod = PaymentMethod,
                            PaidAmount = PaidAmount,
                            DueAmount = oorder.Total - PaidAmount,
                            OrderType = oorder.OrderType,
                            Status = 3,
                            Note = oorder.Note,
                            CreatedBy = oorder.CreatedBy,
                            CreatedAt = oorder.CreatedAt,
                            ClosedBy = userName,
                            ClosedAt = CurrentDateTime(),
                            PriceType = oorder.PriceType,
                        };

                        _dbContext.Orders.Add(order);
                        _dbContext.SaveChanges();

                        foreach (var item in oorder.RunningOrderDetails)
                        {
                            OrderDetail orderDetail = new()
                            {
                                OrderId = order.Id,
                                ItemId = item.ItemId,
                                Price = item.Price,
                                ModifierTotal = item.ModifierTotal,
                                Quantity = item.Quantity,
                                Total = item.Total,
                                CreatedAt = item.CreatedAt
                            };
                            _dbContext.OrderDetails.Add(orderDetail);
                            _dbContext.SaveChanges();

                            foreach (var oitemmodifier in item.RunningOrderItemModifiers)
                            {
                                OrderItemModifier orderItemModifier = new()
                                {
                                    OrderDetailId = orderDetail.Id,
                                    ModifierId = oitemmodifier.ModifierId,
                                    Price = oitemmodifier.Price,
                                    Quantity = oitemmodifier.Quantity,
                                    ModifierTotal = oitemmodifier.ModifierTotal,
                                };
                                _dbContext.Add(orderItemModifier);
                            }
                        }

                        _dbContext.Remove(oorder);
                        await _dbContext.SaveChangesAsync();

                        await transaction.CommitAsync();

                        await _hub.Clients.Group("admin").SendAsync("OrderNotification", "closed", order.Id);
                        await _hub.Clients.Group("staff").SendAsync("OrderNotification", "closed", order.Id);
                        if (!String.IsNullOrEmpty(order.WaiterOrDriver))
                            await _hub.Clients.User(order.WaiterOrDriver).SendAsync("OrderNotification", "closed", order.Id);

                        response.Add("status", "success");
                        response.Add("message", "success");
                        response.Add("id", order.Id.ToString());
                        response.Add("guid", order.OrderGuid.ToString());
                        response.Add("phone", oorder.Customer?.Phone ?? "");
                        response.Add("customerName", oorder.Customer?.CustomerName ?? "Customer");
                    }
                    else
                    {
                        response.Add("status", "error");
                        response.Add("message", "Order not exist.");
                    }
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Enter required fields.");
                }
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DigitalInvoice(Guid guid)
        {
            var order = await _dbContext.Orders.Where(x => x.OrderGuid == guid)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.OrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            var html = String.Empty;
            var lang = _dbContext.Languages.FirstOrDefault(x => x.Culture == GetSetting.DefaultLanguage);
            if (CultureInfo.GetCultureInfo(GetSetting.DefaultLanguage).TextInfo.IsRightToLeft)
            {
                html = RTLInvoice(order, lang.Id);
            }
            else
            {
                html = LTRInvoice(order, lang.Id);
            }

            return Content(html, "text/html");
        }
        
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult ValidateCustomer(int id, string priceType)
        {
            var results = new Dictionary<string, string>();
            var defaultCustomer = GetSetting.DefaultCustomer;

            if (priceType == "VanSale" && id == defaultCustomer)
            {
                results.Add("status", "error");
                results.Add("message", "Customer selection is mandatory for Van Sales.");
            }
            else
            {
                var customer = _dbContext.Customers.FirstOrDefault(x => x.Id == id);
                if (customer == null)
                {
                    results.Add("status", "error");
                    results.Add("message", "Customer not found.");
                }
                else
                {
                    results.Add("status", "success");
                    results.Add("message", "valid");
                }
            }

            return Json(results);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> PrintInvoice(int? Id)
        {
            var order = await _dbContext.Orders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.OrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            var html = String.Empty;
            var lang = _dbContext.Languages.FirstOrDefault(x => x.Culture == GetSetting.DefaultLanguage);
            if (CultureInfo.GetCultureInfo(GetSetting.DefaultLanguage).TextInfo.IsRightToLeft)
            {
                html = RTLInvoice(order, lang.Id);
            }
            else
            {
                html = LTRInvoice(order, lang.Id);
            }

            if (GetSetting.SendInvoiceEmail && GetSetting.DefaultCustomer != order.CustomerId)
            {
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = new PechkinPaperSize("88mm", "250mm"),
                    },
                    Objects = {
                        new ObjectSettings() {
                            HtmlContent = html,
                        }
                    }
                };

                byte[] pdf = _converter.Convert(doc);

                var host = HttpContext.Request.Host;
                var template = _dbContext.EmailTemplates.FirstOrDefault(x => x.Name == "Payment Success");
                if (General.IsValidEmail(order.Customer.Email))
                {
                    var isSend = SendEmail.PaymentSuccess(GetSetting, order.Customer.CustomerName, order.Customer.Email, String.Format("https://{0}", host), order.CreatedAt.ToString(), order.Id.ToString(), order.PaymentMethod, order.Total.ToString(), order.DueAmount.ToString(), order.PaidAmount.ToString(), template.Template, template.Subject, pdf);

                    if (!isSend)
                    {
                        AuditLog log = new()
                        {
                            Username = order.ClosedBy,
                            Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                            Service = "POS",
                            Action = "Close Order",
                            Status = "error",
                            CreatedAt = CurrentDateTime(),
                            Description = "Email failed to send. Order #" + order.Id.ToString()
                        };
                        SaveLog(log, _dbContext);
                    }
                }
                else
                {
                    AuditLog log = new()
                    {
                        Username = order.ClosedBy,
                        Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                        Service = "POS",
                        Action = "Close Order",
                        Status = "error",
                        CreatedAt = CurrentDateTime(),
                        Description = "Email failed to send. Order #" + order.Id.ToString()
                    };
                    SaveLog(log, _dbContext);
                }
            }

            html += "<script>window.onload = function() { window.print(); }</script>";
            return Content(html, "text/html");
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> PrintSticker(int? Id)
        {
            var runningOrder = await _dbContext.RunningOrders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefaultAsync();

            string html = "<html dir='ltr'><head><style>@media print { body { font-family: sans-serif; margin: 0; padding: 10px; font-size: 14px; width: 60mm; height: 40mm; } .page-break { display: block; page-break-after: always; } } </style></head><body>";
            int count = 1;

            if (runningOrder != null)
            {
                foreach (var item in runningOrder.RunningOrderDetails)
                {
                    for (int i = 0; i < (item.Quantity ?? 1); i++)
                    {
                        if (count > 1) html += "<div class='page-break'></div>";
                        html += $"<div style='padding: 5px; text-align: center;'>";
                        html += $"<h3 style='margin: 0;'>Order #{runningOrder.Id}</h3>";
                        html += $"<p style='margin: 2px 0; font-size: 12px'><strong>{runningOrder.Customer.CustomerName}</strong></p>";
                        html += $"<h4 style='margin: 2px 0;'>{item.Item.ItemName}</h4>";
                        var mods = item.RunningOrderItemModifiers.Select(m => m.Modifier.Title).ToList();
                        if (mods.Count > 0)
                        {
                            html += $"<p style='margin: 2px 0; font-size: 10px;'>Modifiers: {string.Join(", ", mods)}</p>";
                        }
                        html += $"</div>";
                        count++;
                    }
                }
            }
            else
            {
                var closedOrder = await _dbContext.Orders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.OrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefaultAsync();

                if (closedOrder == null)
                    return NotFound();

                foreach (var item in closedOrder.OrderDetails)
                {
                    for (int i = 0; i < (item.Quantity ?? 1); i++)
                    {
                        if (count > 1) html += "<div class='page-break'></div>";
                        html += $"<div style='padding: 5px; text-align: center;'>";
                        html += $"<h3 style='margin: 0;'>Order #{closedOrder.Id}</h3>";
                        html += $"<p style='margin: 2px 0; font-size: 12px'><strong>{closedOrder.Customer.CustomerName}</strong></p>";
                        html += $"<h4 style='margin: 2px 0;'>{item.Item.ItemName}</h4>";
                        var mods = item.OrderItemModifiers.Select(m => m.Modifier.Title).ToList();
                        if (mods.Count > 0)
                        {
                            html += $"<p style='margin: 2px 0; font-size: 10px;'>Modifiers: {string.Join(", ", mods)}</p>";
                        }
                        html += $"</div>";
                        count++;
                    }
                }
            }

            html += "<script>window.onload = function() { window.print(); }</script></body></html>";
            return Content(html, "text/html");
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> SendInvoiceEmail(int? Id)
        {
            var order = await _dbContext.Orders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.OrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            var html = String.Empty;
            var lang = _dbContext.Languages.FirstOrDefault(x => x.Culture == GetSetting.DefaultLanguage);
            if (CultureInfo.GetCultureInfo(GetSetting.DefaultLanguage).TextInfo.IsRightToLeft)
            {
                html = RTLInvoice(order, lang.Id);
            }
            else
            {
                html = LTRInvoice(order, lang.Id);
            }

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = new PechkinPaperSize("88mm", "250mm"),
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = html,
                    }
                }
            };

            byte[] pdf = _converter.Convert(doc);

            if (GetSetting.SendInvoiceEmail && GetSetting.DefaultCustomer != order.CustomerId)
            {
                var host = HttpContext.Request.Host;
                var template = _dbContext.EmailTemplates.FirstOrDefault(x => x.Name == "Payment Success");
                if (General.IsValidEmail(order.Customer.Email))
                {
                    var isSend = SendEmail.PaymentSuccess(GetSetting, order.Customer.CustomerName, order.Customer.Email, String.Format("https://{0}", host), order.CreatedAt.ToString(), order.Id.ToString(), order.PaymentMethod, order.Total.ToString(), order.DueAmount.ToString(), order.PaidAmount.ToString(), template.Template, template.Subject, pdf);

                    if (!isSend)
                    {
                        AuditLog log = new()
                        {
                            Username = order.ClosedBy,
                            Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                            Service = "POS",
                            Action = "Close Order",
                            Status = "error",
                            CreatedAt = CurrentDateTime(),
                            Description = "Email failed to send. Order #" + order.Id.ToString()
                        };
                        SaveLog(log, _dbContext);
                    }
                }
                else
                {
                    AuditLog log = new()
                    {
                        Username = order.ClosedBy,
                        Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                        Service = "POS",
                        Action = "Close Order",
                        Status = "error",
                        CreatedAt = CurrentDateTime(),
                        Description = "Email failed to send. Order #" + order.Id.ToString()
                    };
                    SaveLog(log, _dbContext);
                }
            }

            return new FileContentResult(pdf, "application/pdf");
        }

        // Check Work Period Is Started Or Not
        private bool IsWorkPeriodStarted()
        {
            var workPeriod = _dbContext.WorkPeriods.Where(x => x.IsEnd.Equals(false)).Count();
            return workPeriod > 0;
        }

        private decimal InsertRunningOrderModifiers(int orderId, int orderDetailId, string modifiers)
        {
            decimal total = 0;
            if (!String.IsNullOrEmpty(modifiers))
            {
                string[] modifiersId = modifiers.Split(',');
                for (int j = 0; j < modifiersId.Length; j++)
                {
                    var modifier = _dbContext.Modifiers.FirstOrDefault(x => x.Id == Convert.ToInt32(modifiersId[j]));
                    if (modifier != null)
                    {
                        RunningOrderItemModifier itemModifier = new()
                        {
                            OrderDetailId = orderDetailId,
                            ModifierId = Convert.ToInt32(modifier.Id),
                            Price = modifier.Price
                        };
                        total += modifier.Price;
                        _dbContext.RunningOrderItemModifiers.Add(itemModifier);
                    }
                }
                _dbContext.SaveChanges();
            }
            return total;
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> Kitchen(string orderType)
        {
            ViewBag.orderType = orderType;
            if (orderType == "dinein")
            {
                var orders = await _dbContext.RunningOrders.Where(x => x.OrderType == 1 && x.Status == 1)
                    .Include(x => x.Customer)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier).ToListAsync();
                return View(orders);
            }
            else if (orderType == "pickup")
            {
                var orders = await _dbContext.RunningOrders.Where(x => x.OrderType == 2 && x.Status == 1)
                    .Include(x => x.Customer)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier).ToListAsync();
                return View(orders);
            }
            else if (orderType == "delivery")
            {
                var orders = await _dbContext.RunningOrders.Where(x => x.OrderType == 3 && x.Status == 1)
                    .Include(x => x.Customer)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier).ToListAsync();
                return View(orders);
            }
            else
            {
                var orders = await _dbContext.RunningOrders.Where(x => x.Status == 1)
                    .Include(x => x.Customer)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier).ToListAsync();
                ViewBag.orderType = "all";
                return View(orders);
            }
        }

        // Change Order Status from kitchen
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> MarkOrderReady(int? Id)
        {
            var results = new Dictionary<string, string>();
            try
            {
                var order = _dbContext.RunningOrders.Where(x => x.Id == Id && x.Status == 1)
                    .Include(x => x.RunningOrderDetails)
                    .FirstOrDefault();
                if (order != null)
                {
                    foreach (var item in order.RunningOrderDetails)
                    {
                        foreach (var item1 in _dbContext.FoodItemIngredients.Where(x => x.FoodItemId == item.ItemId).ToList())
                        {
                            var ingredient = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == item1.IngredientId);
                            ingredient.Quantity -= (decimal)(item.Quantity * item1.Quantity);
                            _dbContext.IngredientItems.Update(ingredient);
                        }
                    }
                    order.Status = 2;

                    _dbContext.RunningOrders.Update(order);
                    _dbContext.SaveChanges();

                    await _hub.Clients.Group("admin").SendAsync("OrderNotification", "ready", order.Id);
                    await _hub.Clients.Group("staff").SendAsync("OrderNotification", "ready", order.Id);
                    if (!String.IsNullOrEmpty(order.WaiterOrDriver))
                        await _hub.Clients.User(order.WaiterOrDriver).SendAsync("OrderNotification", "updated", order.Id);

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                else
                {
                    results.Add("status", "error");
                    results.Add("message", "Something went wrong.");
                }
            }
            catch
            {
                results.Add("status", "error");
                results.Add("message", "Something went wrong.");
            }

            return Json(results);
        }

        // Get Running Order With Minimum Detail
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult GetRunningOrder(int? Id)
        {
            var response = new Dictionary<string, string>();
            JsonSerializerOptions options = new()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };
            try
            {
                var order = _dbContext.RunningOrders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .FirstOrDefault();

                if (order != null)
                {
                    response.Add("order", JsonSerializer.Serialize(order, options));
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Order not exist.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Get All Running Orders With minimum Detail
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult GetAllRunningOrder()
        {
            var response = new Dictionary<string, string>();
            JsonSerializerOptions options = new()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };
            try
            {
                var order = _dbContext.RunningOrders
                    .ToList();

                response.Add("data", JsonSerializer.Serialize(order, options));
                response.Add("status", "success");
                response.Add("message", "success");
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Get Running Order With All Details
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult GetRunningOrderDetail(int? Id)
        {
            var response = new Dictionary<string, string>();
            JsonSerializerOptions options = new()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };
            try
            {
                var order = _dbContext.RunningOrders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.Tax)
                    .Include(x => x.Discount)
                    .Include(x => x.Charges)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefault();

                if (order != null)
                {
                    response.Add("order", JsonSerializer.Serialize(order, options));
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Order not exist.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> OrderHistory(DateTime? start, DateTime? end, string status)
        {
            var from = StartOfDay(start);
            var to = EndOfDay(end);
            ViewBag.start = from;
            ViewBag.end = to;
            ViewBag.status = status;
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.UserName == userName);
            var isAdmin = User.IsInRole("admin");

            var query = _dbContext.Orders.Where(x => x.CreatedAt >= from && x.CreatedAt <= to);

            if (!isAdmin && currentUser != null)
            {
                if (currentUser.IsVanSales)
                {
                    query = query.Where(x => x.ClosedBy == userName);
                }
                else
                {
                    query = query.Where(x => x.PriceType != "VanSale");
                }
            }

            var orders = await query.Include(x => x.Customer).OrderByDescending(x => x.Id).ToListAsync();

            if (status == "paid")
                orders = orders.Where(x => x.DueAmount == 0).ToList();

            if (status == "unpaid")
                orders = orders.Where(x => x.DueAmount > 0).ToList();

            return View(orders);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult GetClosedOrderDetail(int? Id)
        {
            var response = new Dictionary<string, string>();
            JsonSerializerOptions options = new()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };
            try
            {
                var order = _dbContext.Orders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.OrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefault();

                if (order != null)
                {
                    response.Add("order", JsonSerializer.Serialize(order, options));
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Order not exist.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        //Delete Order
        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteOrder(int? Id, string Reason)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var existing = await _dbContext.Orders.Where(x => x.Id == Id)
                                .Include(x => x.OrderDetails)
                                    .ThenInclude(x => x.Item)
                                .Include(x => x.OrderDetails)
                                    .ThenInclude(x => x.OrderItemModifiers)
                                        .ThenInclude(x => x.Modifier)
                                .FirstOrDefaultAsync();

                if (existing != null)
                {
                    if (string.IsNullOrEmpty(Reason))
                    {
                        response.Add("status", "error");
                        response.Add("message", "Deletion reason is required.");
                        return Json(response);
                    }

                    JsonSerializerOptions options = new()
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    };

                    DeletedOrder deletedOrder = new()
                    {
                        OrderId = existing.Id,
                        CustomerId = existing.CustomerId,
                        TableName = existing.TableName,
                        WaiterOrDriver = existing.WaiterOrDriver,
                        Total = existing.Total,
                        OrderType = existing.OrderType,
                        Note = existing.Note,
                        CreatedAt = existing.CreatedAt,
                        DeletedAt = CurrentDateTime(),
                        DeletedBy = userName,
                        DeletionReason = Reason,
                        PriceType = existing.PriceType,
                        DetailsJson = JsonSerializer.Serialize(existing.OrderDetails, options),
                        PaymentMethod = existing.PaymentMethod
                    };

                    _dbContext.DeletedOrders.Add(deletedOrder);
                    _dbContext.Orders.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    var existingJournals = _dbContext.JournalEntries.Where(x => x.SourceDocumentType == "pos" && x.SourceDocumentId == existing.Id).ToList();
                    if (existingJournals.Any())
                    {
                        _dbContext.JournalEntries.RemoveRange(existingJournals);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Something went wrong.");
                }
            }
            catch (Exception ex)
            {
                response.Add("status", "error");
                response.Add("message", ex.Message);
            }
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetDeletedOrderHistory(DateTime? start, DateTime? end)
        {
            var from = StartOfDay(start);
            var to = EndOfDay(end);

            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.UserName == userName);
            var isAdmin = User.IsInRole("admin");

            var query = _dbContext.DeletedOrders.Where(x => x.DeletedAt >= from && x.DeletedAt <= to);

            if (!isAdmin && currentUser != null)
            {
                if (currentUser.IsVanSales)
                {
                    query = query.Where(x => x.DeletedBy == userName);
                }
                else
                {
                    query = query.Where(x => x.PriceType != "VanSale");
                }
            }

            var deletedOrders = await query
                .Include(x => x.Customer)
                .OrderByDescending(x => x.DeletedAt)
                .Select(x => new
                {
                    x.Id,
                    x.OrderId,
                    CustomerName = x.Customer.CustomerName,
                    x.Total,
                    x.OrderType,
                    CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    DeletedAt = x.DeletedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    x.DeletedBy,
                    x.DeletionReason
                })
                .ToListAsync();

            return Json(new { status = "success", data = deletedOrders });
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetDeletedOrderDetail(int Id)
        {
            var order = await _dbContext.DeletedOrders.FindAsync(Id);
            if (order == null)
                return Json(new { status = "error", message = "Order not found" });

            return Json(new { status = "success", order = order.DetailsJson, id = order.OrderId, paymentMethod = order.PaymentMethod });
        }

        //Receive Due Amount From Customer
        [HttpPut]
        [Authorize(Roles = "admin,staff")]
        public async Task<JsonResult> ReceiveDue(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Orders.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    existing.PaidAmount = existing.Total;
                    existing.DueAmount = 0;
                    _dbContext.Orders.Update(existing);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Something went wrong.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Order not exist.");
            }

            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<JsonResult> GetOrderJson(int Id)
        {
            var order = await _dbContext.Orders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.OrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefaultAsync();

            if (order == null)
            {
                return Json(new { status = "error", message = "Order not found" });
            }

            string[] orderTypes = { "", Localize("Dine In"), Localize("Pick Up"), Localize("Delivery") };

            var items = order.OrderDetails.Select(x => new
            {
                ItemName = x.Item.ItemName + (string.IsNullOrEmpty(x.Item.ArabicName) ? "" : " - " + x.Item.ArabicName),
                Quantity = x.Quantity,
                Total = GetCurrency(x.Total ?? 0),
                Modifiers = x.OrderItemModifiers.Select(m => m.Modifier.Title).ToList()
            }).ToList();

            var result = new
            {
                CompanyName = GetSetting.CompanyName,
                CompanyAddress = GetSetting.CompanyAddress,
                CompanyPhone = GetSetting.CompanyPhone,
                CompanyTax = GetSetting.CompanyTaxNum,
                Customer = order.Customer.CustomerName,
                BillNo = order.Id.ToString(),
                Date = order.CreatedAt.ToString(),
                Type = orderTypes[order.OrderType],
                Table = order.OrderType == 1 ? order.TableName : "",
                Items = items,
                SubTotal = GetCurrency(order.SubTotal),
                Discount = GetCurrency(order.DiscountTotal),
                Charge = GetCurrency(order.ChargeTotal),
                Tax = GetCurrency(order.TaxTotal),
                Total = GetCurrency(order.Total),
                CreatedBy = order.CreatedBy,
                ClosedBy = order.ClosedBy,
                Lang = GetSetting.DefaultLanguage,
                IsRTL = CultureInfo.GetCultureInfo(GetSetting.DefaultLanguage).TextInfo.IsRightToLeft
            };

            return Json(result);
        }
        // Print Closed Order Invoice
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        [Produces(MediaTypeNames.Application.Pdf)]
        public IActionResult PrintFinalInvoice(int? Id)
        {
            var order = _dbContext.Orders.Where(x => x.Id == Id)
                    .Include(x => x.Customer)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.OrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefault();

            if (order == null)
                return NotFound();

            // get html template according to default language
            var html = "";
            var lang = _dbContext.Languages.FirstOrDefault(x => x.Culture == GetSetting.DefaultLanguage);
            if (CultureInfo.GetCultureInfo(GetSetting.DefaultLanguage).TextInfo.IsRightToLeft)
            {
                html = RTLInvoice(order, lang.Id);
            }
            else
            {
                html = LTRInvoice(order, lang.Id);
            }

            html += "<script>window.onload = function() { window.print(); }</script>";
            return Content(html, "text/html");
        }

        // Return Left To Right Order Invoice in html format
        private string LTRInvoice(Order order, int lang)
        {
            var logoUrl = !string.IsNullOrEmpty(GetSetting.InvoiceLogo) ? GetSetting.InvoiceLogo : GetSetting.Logo;

            var html = @"<!DOCTYPE html><html><head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Invoice</title>
<style>
@page { size: auto; margin: 0mm; }
body { margin: 0px; padding: 0px; font-family: 'Arial', sans-serif; font-size: 12px; color: #000; }
.ticket { width: 100%; max-width: 80mm; margin: 0 auto; padding: 2mm; background-color: #fff; box-sizing: border-box; }
.centered { text-align: center; }
.right { text-align: right; }
.logo { display: block; margin: 0 auto 5px auto; max-width: 100px; height: auto; }
p { margin: 2px 0; }
table { width: 100%; border-collapse: collapse; }
td, th { padding: 4px 0; text-align: left; vertical-align: top; font-size: 12px; }
.items th { border-bottom: 1px dashed #000; padding: 5px 0; }
.items td { border-bottom: 1px dashed #eee; padding: 5px 0; }
.total td, .total th { border-top: 1px dashed #000; font-weight: bold; padding-top: 5px; font-size: 14px; }
.divider { border-top: 1px dashed #000; margin: 5px 0; }
</style>
</head>
<body>
<div class=""ticket"">
    {LogoTag}
    <div class=""centered"">
        <p style=""font-weight:bold; font-size:14px;"">{CompanyName}</p>
        <p>{CompanyAddress}</p>
        <p>{PhoneTitle}: {CompanyPhone}</p>
        <p>{CompanyTaxNumber}</p>
    </div>
    <div class=""divider""></div>
    <table>
        <tr><td>{CustomerTitle}:</td><td class=""right"">{Customer}</td></tr>
        <tr><td>{BillNoTitle}:</td><td class=""right"">{BillNo}</td></tr>
        <tr><td>{DateTitle}:</td><td class=""right"">{Date}</td></tr>
        <tr><td>{TypeTitle}:</td><td class=""right"">{Type}</td></tr>
        <tr><td>{TableTitle}</td><td class=""right"">{Table}</td></tr>
    </table>
    <div class=""divider""></div>
    <table class=""items"">
        <thead>
            <tr>
                <th>{ItemTitle}</th>
                <th class=""centered"">{QtyTitle}</th>
                <th class=""right"">{AmountTitle}</th>
            </tr>
        </thead>
        <tbody>
            {Items}
        </tbody>
    </table>
    <div class=""divider""></div>
    <table>
        <tr><td>{SubTotalTitle}:</td><td class=""right"">{SubTotal}</td></tr>
        <tr><td>{DiscountTitle}:</td><td class=""right"">{Discount}</td></tr>
        {DiscountReasonRow}
        <tr><td>{ChargeTitle}:</td><td class=""right"">{Charge}</td></tr>
        <tr><td>{TaxTitle}:</td><td class=""right"">{Tax}</td></tr>
        <tr class=""total"">
            <th>{TotalTitle}:</th>
            <td class=""right"">{Total}</td>
        </tr>
    </table>
    <div class=""divider""></div>
    <table>
        <tr><td style=""font-weight:bold;"">{PaymentMethodTitle}:</td><td class=""right"">{PaymentMethod}</td></tr>
        <tr><td style=""font-weight:bold;"">{PaidAmountTitle}:</td><td class=""right"">{PaidAmount}</td></tr>
    </table>
    <div class=""centered"">
        <p>{CreatedByTitle}: {CreatedBy}</p>
        <p>{ClosedByTitle}: {ClosedBy}</p>
        <p style=""margin-top:10px;"">*** Thank You ***</p>
    </div>
</div>
</body></html>";

            var itemHtml = "";
            foreach (var item in order.OrderDetails)
            {
                itemHtml += @"<tr><td>" + item.Item.ItemName + (string.IsNullOrEmpty(item.Item.ArabicName) ? "" : " - " + item.Item.ArabicName);
                if (item.OrderItemModifiers.Count > 0)
                    itemHtml += "<br> - ";
                var i = 1;
                foreach (var modifier in item.OrderItemModifiers)
                {
                    itemHtml += modifier.Modifier.Title;
                    if (i != item.OrderItemModifiers.Count)
                    {
                        itemHtml += ", ";
                    }
                    i++;
                }
                itemHtml += "</td>";
                itemHtml += @"<td class=""centered"">" + item.Quantity + "</td>";
                itemHtml += @"<td class=""right"">" + item.Total + "</td></tr>";
            }
            string[] orderTypes = { "", Localize("Dine In"), Localize("Pick Up"), Localize("Delivery") };
            var host = HttpContext.Request.Host;
            using (MemoryStream memoryStream = new())
            {
                var protocol = HttpContext.Request.Scheme;
                var finalLogoUrl = logoUrl.StartsWith("/") ? logoUrl : "/" + logoUrl;
                var logoTagHtml = GetSetting.PrintLogo ? @"<img src=""{Logo}"" alt=""Logo"" class=""logo"">" : "";
                html = html.Replace("{LogoTag}", logoTagHtml);
                html = html.Replace("{Logo}", $"{protocol}://{host}{finalLogoUrl}");
                html = html.Replace("{CompanyTaxNumber}", GetSetting.CompanyTaxNum);
                html = html.Replace("{CompanyName}", GetSetting.CompanyName);
                html = html.Replace("{CompanyEmail}", GetSetting.CompanyEmail);
                html = html.Replace("{CompanyPhone}", GetSetting.CompanyPhone);
                html = html.Replace("{CompanyAddress}", GetSetting.CompanyAddress);
                html = html.Replace("{BillNo}", order.Id.ToString());
                html = html.Replace("{Date}", order.CreatedAt.ToString());
                html = html.Replace("{Type}", orderTypes[order.OrderType]);
                html = html.Replace("{CustomerTitle}", Localize("Customer", lang));
                html = html.Replace("{Customer}", Localize(order.Customer.CustomerName));
                if (order.OrderType == 1)
                {
                    html = html.Replace("{TableTitle}", Localize("Table", lang) + ":");
                    html = html.Replace("{Table}", order.TableName);
                }
                else
                {
                    html = html.Replace("{Table}", "");
                    html = html.Replace("{TableTitle}", "");
                }

                html = html.Replace("{Items}", itemHtml);
                html = html.Replace("{SubTotal}", GetCurrency(order.SubTotal));
                html = html.Replace("{Charge}", GetCurrency(order.ChargeTotal));
                html = html.Replace("{Discount}", GetCurrency(order.DiscountTotal));

                var discountReasonRow = !string.IsNullOrEmpty(order.DiscountReason) 
                    ? $@"<tr><td colspan=""2"" style=""font-size: 10px; color: #666; font-style: italic;"">({order.DiscountReason})</td></tr>" 
                    : "";
                html = html.Replace("{DiscountReasonRow}", discountReasonRow);

                html = html.Replace("{TaxExcl}", GetCurrency(order.Total - order.TaxTotal));
                html = html.Replace("{Tax}", GetCurrency(order.TaxTotal));
                html = html.Replace("{Total}", GetCurrency(order.Total));

                // Payment Info
                var paymentMethodName = order.PaymentMethod == "1" ? Localize("Cash", lang) : order.PaymentMethod == "2" ? Localize("Card", lang) : Localize("Other", lang);
                html = html.Replace("{PaymentMethodTitle}", Localize("Payment Method", lang));
                html = html.Replace("{PaymentMethod}", paymentMethodName);
                html = html.Replace("{PaidAmountTitle}", Localize("Paid Amount", lang));
                html = html.Replace("{PaidAmount}", GetCurrency(order.PaidAmount));


                html = html.Replace("{CreatedBy}", order.CreatedBy);
                html = html.Replace("{ClosedBy}", order.ClosedBy);

                html = html.Replace("{PhoneTitle}", Localize("Phone", lang));
                html = html.Replace("{BillNoTitle}", Localize("Order No.", lang));
                html = html.Replace("{DateTitle}", Localize("Date", lang));
                html = html.Replace("{TypeTitle}", Localize("Type", lang));
                html = html.Replace("{ItemTitle}", Localize("Item", lang));
                html = html.Replace("{QtyTitle}", Localize("Quantity", lang));
                html = html.Replace("{AmountTitle}", Localize("Amount", lang));
                html = html.Replace("{SubTotalTitle}", Localize("Sub Total", lang));
                html = html.Replace("{ChargeTitle}", Localize("Charge", lang));
                html = html.Replace("{DiscountTitle}", Localize("Discount", lang));
                html = html.Replace("{TaxTitle}", Localize("Tax", lang));
                html = html.Replace("{TotalTitle}", Localize("Total", lang));
                html = html.Replace("{CreatedByTitle}", Localize("Created By", lang));
                html = html.Replace("{ClosedByTitle}", Localize("Closed By", lang));
            }
            return html;
        }
        // Return Right To Left Order Invoice in html format
        private string RTLInvoice(Order order, int lang)
        {
            var logoUrl = !string.IsNullOrEmpty(GetSetting.InvoiceLogo) ? GetSetting.InvoiceLogo : GetSetting.Logo;

            var html = @"<!DOCTYPE html><html><head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Invoice</title>
<style>
@page { size: auto; margin: 0mm; }
body { margin: 0px; padding: 0px; font-family: 'Arial', sans-serif; font-size: 12px; color: #000; direction: rtl; }
.ticket { width: 100%; max-width: 80mm; margin: 0 auto; padding: 2mm; background-color: #fff; box-sizing: border-box; }
.centered { text-align: center; }
.right { text-align: left; } /* in RTL right is left */
.logo { display: block; margin: 0 auto 5px auto; max-width: 100px; height: auto; }
p { margin: 2px 0; }
table { width: 100%; border-collapse: collapse; }
td, th { padding: 4px 0; text-align: right; vertical-align: top; font-size: 12px; }
.items th { border-bottom: 1px dashed #000; padding: 5px 0; }
.items td { border-bottom: 1px dashed #eee; padding: 5px 0; }
.total td, .total th { border-top: 1px dashed #000; font-weight: bold; padding-top: 5px; font-size: 14px; }
.divider { border-top: 1px dashed #000; margin: 5px 0; }
</style>
</head>
<body>
<div class=""ticket"">
    {LogoTag}
    <div class=""centered"">
        <p style=""font-weight:bold; font-size:14px;"">{CompanyName}</p>
        <p>{CompanyAddress}</p>
        <p>{PhoneTitle}: {CompanyPhone}</p>
        <p>{CompanyTaxNumber}</p>
    </div>
    <div class=""divider""></div>
    <table>
        <tr><td>{CustomerTitle}:</td><td class=""right"">{Customer}</td></tr>
        <tr><td>{BillNoTitle}:</td><td class=""right"">{BillNo}</td></tr>
        <tr><td>{DateTitle}:</td><td class=""right"">{Date}</td></tr>
        <tr><td>{TypeTitle}:</td><td class=""right"">{Type}</td></tr>
        <tr><td>{TableTitle}</td><td class=""right"">{Table}</td></tr>
    </table>
    <div class=""divider""></div>
    <table class=""items"">
        <thead>
            <tr>
                <th>{ItemTitle}</th>
                <th class=""centered"">{QtyTitle}</th>
                <th class=""right"">{AmountTitle}</th>
            </tr>
        </thead>
        <tbody>
            {Items}
        </tbody>
    </table>
    <div class=""divider""></div>
    <table>
        <tr><td>{SubTotalTitle}:</td><td class=""right"">{SubTotal}</td></tr>
        <tr><td>{DiscountTitle}:</td><td class=""right"">{Discount}</td></tr>
        {DiscountReasonRow}
        <tr><td>{ChargeTitle}:</td><td class=""right"">{Charge}</td></tr>
        <tr><td>{TaxTitle}:</td><td class=""right"">{Tax}</td></tr>
        <tr class=""total"">
            <th>{TotalTitle}:</th>
            <td class=""right"">{Total}</td>
        </tr>
    </table>
    <div class=""divider""></div>
    <table>
        <tr><td style=""font-weight:bold;"">{PaymentMethodTitle}:</td><td class=""right"">{PaymentMethod}</td></tr>
        <tr><td style=""font-weight:bold;"">{PaidAmountTitle}:</td><td class=""right"">{PaidAmount}</td></tr>
    </table>
    <div class=""centered"">
        <p>{CreatedByTitle}: {CreatedBy}</p>
        <p>{ClosedByTitle}: {ClosedBy}</p>
        <p style=""margin-top:10px;"">*** Thank You ***</p>
    </div>
</div>
</body></html>";

            var itemHtml = "";
            foreach (var item in order.OrderDetails)
            {
                itemHtml += @"<tr><td>" + item.Item.ItemName + (string.IsNullOrEmpty(item.Item.ArabicName) ? "" : " - " + item.Item.ArabicName);
                if (item.OrderItemModifiers.Count > 0)
                    itemHtml += "<br> - ";
                var i = 1;
                foreach (var modifier in item.OrderItemModifiers)
                {
                    itemHtml += modifier.Modifier.Title;
                    if (i != item.OrderItemModifiers.Count)
                    {
                        itemHtml += ", ";
                    }
                    i++;
                }
                itemHtml += "</td>";
                itemHtml += @"<td class=""centered"">" + item.Quantity + "</td>";
                itemHtml += @"<td class=""right"">" + item.Total + "</td></tr>";
            }
            string[] orderTypes = { "", Localize("Dine In"), Localize("Pick Up"), Localize("Delivery") };
            var host = HttpContext.Request.Host;
            using (MemoryStream memoryStream = new())
            {
                var protocol = HttpContext.Request.Scheme;
                var finalLogoUrl = logoUrl.StartsWith("/") ? logoUrl : "/" + logoUrl;
                var logoTagHtml = GetSetting.PrintLogo ? @"<img src=""{Logo}"" alt=""Logo"" class=""logo"">" : "";
                html = html.Replace("{LogoTag}", logoTagHtml);
                html = html.Replace("{Logo}", $"{protocol}://{host}{finalLogoUrl}");
                html = html.Replace("{CompanyTaxNumber}", GetSetting.CompanyTaxNum);
                html = html.Replace("{CompanyName}", GetSetting.CompanyName);
                html = html.Replace("{CompanyEmail}", GetSetting.CompanyEmail);
                html = html.Replace("{CompanyPhone}", GetSetting.CompanyPhone);
                html = html.Replace("{CompanyAddress}", GetSetting.CompanyAddress);
                html = html.Replace("{BillNo}", order.Id.ToString());
                html = html.Replace("{Date}", order.CreatedAt.ToString());
                html = html.Replace("{Type}", orderTypes[order.OrderType]);
                html = html.Replace("{CustomerTitle}", Localize("Customer", lang));
                html = html.Replace("{Customer}", Localize(order.Customer.CustomerName));
                if (order.OrderType == 1)
                {
                    html = html.Replace("{TableTitle}", Localize("Table", lang) + ":");
                    html = html.Replace("{Table}", order.TableName);
                }
                else
                {
                    html = html.Replace("{Table}", "");
                    html = html.Replace("{TableTitle}", "");
                }

                html = html.Replace("{Items}", itemHtml);
                html = html.Replace("{SubTotal}", GetCurrency(order.SubTotal));
                html = html.Replace("{Charge}", GetCurrency(order.ChargeTotal));
                html = html.Replace("{Discount}", GetCurrency(order.DiscountTotal));

                var discountReasonRow = !string.IsNullOrEmpty(order.DiscountReason) 
                    ? $@"<tr><td colspan=""2"" style=""font-size: 10px; color: #666; font-style: italic;"">({order.DiscountReason})</td></tr>" 
                    : "";
                html = html.Replace("{DiscountReasonRow}", discountReasonRow);

                html = html.Replace("{TaxExcl}", GetCurrency(order.Total - order.TaxTotal));
                html = html.Replace("{Tax}", GetCurrency(order.TaxTotal));
                html = html.Replace("{Total}", GetCurrency(order.Total));

                // Payment Info
                var paymentMethodName = order.PaymentMethod == "1" ? Localize("Cash", lang) : order.PaymentMethod == "2" ? Localize("Card", lang) : Localize("Other", lang);
                html = html.Replace("{PaymentMethodTitle}", Localize("Payment Method", lang));
                html = html.Replace("{PaymentMethod}", paymentMethodName);
                html = html.Replace("{PaidAmountTitle}", Localize("Paid Amount", lang));
                html = html.Replace("{PaidAmount}", GetCurrency(order.PaidAmount));

                html = html.Replace("{CreatedBy}", order.CreatedBy);
                html = html.Replace("{ClosedBy}", order.ClosedBy);

                html = html.Replace("{PhoneTitle}", Localize("Phone", lang));
                html = html.Replace("{BillNoTitle}", Localize("Order No.", lang));
                html = html.Replace("{DateTitle}", Localize("Date", lang));
                html = html.Replace("{TypeTitle}", Localize("Type", lang));
                html = html.Replace("{ItemTitle}", Localize("Item", lang));
                html = html.Replace("{QtyTitle}", Localize("Quantity", lang));
                html = html.Replace("{AmountTitle}", Localize("Amount", lang));
                html = html.Replace("{SubTotalTitle}", Localize("Sub Total", lang));
                html = html.Replace("{ChargeTitle}", Localize("Charge", lang));
                html = html.Replace("{DiscountTitle}", Localize("Discount", lang));
                html = html.Replace("{TaxTitle}", Localize("Tax", lang));
                html = html.Replace("{TotalTitle}", Localize("Total", lang));
                html = html.Replace("{CreatedByTitle}", Localize("Created By", lang));
                html = html.Replace("{ClosedByTitle}", Localize("Closed By", lang));
            }
            return html;
        }

    }
}