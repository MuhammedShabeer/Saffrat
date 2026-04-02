# Saffrat Tech: Business Management Suite
## Operations Manual - Enterprise Edition

**Version**: 2.2  
**Date**: April 2026  
**Confidentiality**: Professional Use Only

---

## Table of Contents
1. [Executive Summary](#1-executive-summary)
2. [System Configuration & Setup](#2-system-configuration--setup)
3. [Menu & Food Management](#3-menu--food-management)
4. [Daily Operational Lifecycle](#4-daily-operational-lifecycle)
5. [Point of Sale (POS) Workflows](#5-point-of-sale-pos-workflows)
6. [Inventory, Purchasing & Stock Control](#6-inventory-purchasing--stock-control)
7. [Accounting & Financial Data Entry](#7-accounting--financial-data-entry)
8. [Partner Equity Management](#8-partner-equity-management)
9. [HRM & Payroll](#9-hrm--payroll)
10. [User & Role Management](#10-user--role-management)
11. [Reporting & AI Analysis](#11-reporting--ai-analysis)
12. [Best Practices for Data Integrity](#12-best-practices-for-data-integrity)

---

## 1. Executive Summary
The **Business Management Suite** is a unified .NET Core 8 enterprise solution designed for the hospitality and retail sectors. It integrates real-time Point of Sale (POS), Inventory Control, Human Resource Management (HRM), and a professional Double-Entry Accounting Engine. This manual focuses on data integrity, specifically in the accounting, inventory, and core operational modules.

---

## 2. System Configuration & Setup
Before day-to-day operations can begin, administrators must configure core system settings found under **Settings**.
- **General Settings**: Configure application defaults, currency, and operational time zones.
- **Tax Rates**: Define applicable sales taxes (e.g., VAT or GST) which will automatically be calculated at checkout in the POS.
- **Email Templates**: Customize the standard emails sent to customers (e.g., invoices) and staff.
- **Table Management**: Setup the restaurant’s physical layout, sections, and table numbers via **Tables**.

---

## 3. Menu & Food Management
Accurate menu setup is required for POS functionality and inventory tracking.
1. **Food Groups**: Navigate to **Food -> Food Groups** to create categories (e.g., "Main Course", "Beverages").
2. **Food Items**: Create specific dishes under their respective groups. You must define the selling price, and optionally link *Ingredient Items* to track real-time inventory depletion (Recipe Management).
3. **Modifiers**: Add customizable add-ons (e.g., "Extra Cheese", "Spicy") that staff can apply during order taking.

---

## 4. Daily Operational Lifecycle
Data integrity relies on the structured opening and closing of business sessions. Every business day **MUST** begin and end with a Work Period.

### 4.1 Managing Work Periods
The system operates within "Work Periods" to ensure cash reconciliation.
- **Starting the Day**: Navigate to **Sale -> Cash Register**.
  - Action: Enter the **Opening Balance** (Initial Cash-in-Hand).
  - Impact: This initializes the session and begins tracking all cash/bank inflows.
- **Ending the Day**:
  - Prerequisite: All "Running Orders" must be settled.
  - Action: Navigate to **Cash Register** and select **End Session**. Enter the **Closing Balance**.
  - Logic: The system automatically calculates the `Net Balance` based on (`Opening Balance` + `Total Sales` - `Expenses`). Any variance is flagged in the Reconciliation Report.

---

## 5. Point of Sale (POS) Workflows
The POS module is the primary revenue generator and data entry point for sales.

### 5.1 Order Management
- **Active Orders**: Use the **POS** screen to select tables. Items added here update the **Kitchen Display System (KDS)** in real-time.
- **Order Types**: Dine-In (Table-based), Takeaway, and Delivery.
- **Modifiers**: Customize items. Modifiers update the KDS and adjust ingredient costs dynamically.
- **Splitting Orders**: If guests require separate payments, use the **Split Order** feature to move specific items to a new sub-order.

### 5.2 Settle & Payment
- **Finalizing Sales**: Once a customer pays, click **Settle**.
- **Payment Methods**: Support for Cash, Credit Card, and Digital Wallets.
- **Accounting Note**: Closing an order triggers an automated ledger entry:
  - `DEBIT` [Cash/Bank Account]
  - `CREDIT` [Sales Revenue]
  - `CREDIT` [Sales Tax Liability] (if applicable)

---

## 6. Inventory, Purchasing & Stock Control

### 6.1 Adding Stock (Purchases)
1. Navigate to **Purchase -> Add Purchase**.
2. Select the **Supplier** and enter their **Invoice Number**.
3. Add items and specify the **Purchase Price** and **Quantity**.
4. **Saving** does two things:
   - Updates **Inventory Levels** (Ingredient stock).
   - Records an **Accounts Payable (AP) Bill** in the accounting engine, or reduces Cash if paid immediately.

### 6.2 Stock Adjustment & Wastage
To maintain accurate balance sheets, physical inventory must match system records.
1. Navigate to **Ingredients -> Stock Adjustment**.
2. Select **New Adjustment**.
3. Choose the item and the type (**Wastage** or **Manual Correction**).
4. Enter the quantity and a mandatory reason (e.g., "Expired"). 
5. **Accounting Impact**: Wastage creates a `DE  BIT` to "Stock Wastage Expense" and a `CREDIT` to "Inventory Asset," immediately reflecting the loss in your Profit & Loss statement.

---

## 7. Accounting & Financial Data Entry
This module acts as the "General Ledger" of your business. It is powered by the **Accounting Engine**, a robust double-entry financial processor that intercepts system actions (POS, Payroll, Purchasing) and automatically posts balanced journals in the background, ensuring strict compliance with Standard Accounting Practices (SAP).

### 7.1 Chart of Accounts (COA)
Found under **Accounting -> Chart of Accounts**. The COA is categorized for ease of reporting:
- **0 - Assets**: Cash, Inventory, Accounts Receivable.
- **1 - Liabilities**: Sales Tax, Accounts Payable.
- **2 - Equity**: Partner Capital, Retained Earnings.
- **3 - Revenue**: Food Sales, Delivery Charges.
- **4 - Expenses**: Cost of Goods Sold (COGS), Salaries, Utilities.
*Each account tracks its `CurrentBalance` automatically.*

### 7.2 Data Entry: Cash Ledger (Petty Cash)
For day-to-day office expenses (e.g., fuel, cleaning supplies) or small income:
1. Navigate to **Accounting -> Cash Ledger**.
2. Select **Add Transaction**.
3. **Fields**:
   - `Type`: Expense (Outflow) or Income (Inflow).
   - `Offset Account`: Choose the specific category (e.g., "Electricity Expense").
   - `Description`: High-level note for audits.
4. **Validation**: The system ensures debits and credits match before saving. This replaces manual petty cash books and syncs directly with your Balance Sheet.

### 7.3 Managing Invoices & Bills
- **AR Invoices**: For corporate clients who pay later.
- **AP Bills**: For supplier credit.
- Navigate to **Accounting -> AR Invoices** or **AP Bills** to track payment status (Paid/Unpaid/Partial).

---

## 8. Partner Equity Management
Designed for transparent capital management for business owners.
1. **Adding Partners**: Go to **Accounting -> Partner Equity**. Add a partner to generate their unique Equity Account.
2. **Managing Capital**:
   - **Initial Capital / Investments**: Recorded as an `Investment` to the partner's equity account when a partner injects cash.
   - **Drawings**: Partners can record `Withdrawals` for personal use, which reduces their equity balance.
   - **Profit Distribution**: Periodically move "Net Income" to partner accounts based on their ownership percentage.
3. **Visuals**: Use the Dashboard to see real-time ownership percentages and current net equity balances.

---

## 9. HRM & Payroll
- **Attendance**: Staff must clock in/out via the dashboard or specific employee tablets.
- **Leave Requests / Scheduling**: Track vacations and authorized leaves.
- **Payroll Generation**: At the end of the month, use **HRM -> Payroll** to generate slips. The system automatically calculates Net Salary based on base pay, earnings, deductions, and bonuses. 
- **Split Salary Pay (Partial Payments)**: The system supports partial salary payouts (Split Pay). If an employee requires an advance or the business processes salaries in installments, management can issue partial payments against a generated payroll slip. 
  - *Integration with Accounting Engine*: When a split payment is made, the **Accounting Engine** dynamically debits the specific amount paid from the "Salary Payable" liability account and credits "Cash/Bank", accurately maintaining the remaining outstanding balance owed to the employee automatically.

---

## 10. User & Role Management
The system supports multi-layered security and role-specific portals.
- **Authentication**: All personnel log in via credentials managed under **Users**. Staff can be assigned different access levels.
- **Specialized Portals**:
  - **Waiter Portal**: Optimized interface for tableside order taking via mobile/tablets.
  - **Delivery Man Portal**: Dedicated view for dispatching, assigning, and tracking delivery orders.
- **Customer Database**: Maintain records of frequent customers under **People -> Customers** to manage targeted orders and track Accounts Receivable.

---

## 11. Reporting & AI Analysis
Don't wait for monthly reports. Executives can query the system using natural language.

### 11.1 Financial Statements
- **Balance Sheet**: Real-time view of Assets = Liabilities + Equity.
- **Profit & Loss**: Detailed view of Net Income over a custom date range.

### 11.2 AI Assistant Reporting
1. Go to **Reports -> AI Assistant**.
2. Ask questions like:
   - *"What is our current cash balance?"*
   - *"Show me the sales trend for last week."*
   - *"Who is our most active supplier?"*
   - *"Calculate the ROI for Partner X."*
3. The AI queries the SQL Schema directly to provide instantaneous, accurate professional insights.

---

## 12. Best Practices for Data Integrity
- **Shift Handover**: Always end the Work Period before change of staff.
- **Real-time Entry**: Log expenses as they occur to avoid reconciliation errors.
- **Audit Trails**: Most entries (POs, Sales, Journals) record the `CreatedBy` user for accountability.
- **Manual Entries**: Manual Journal Entries should only be performed by authorized accountants. Most business transactions are handled automatically.

---

**Saffrat Tech Support**  
For further training or technical implementation details, please contact your account manager.
