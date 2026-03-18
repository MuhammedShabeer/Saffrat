# Saffrat Tech: Restaurant Business Management Suite

The **Al Markiya Business Management Suite** is a unified .NET Core 8 enterprise solution purpose-built for the hospitality and retail sectors. It integrates four mission-critical systems into a single, cohesive platform to streamline operations and financial tracking.

---

## 🚀 Key Modules

* **Real-Time POS**: Manages revenue and data entry for all sales transactions including Dine-In, Takeaway, and Delivery.


* **Inventory Control**: Maintains data integrity through manual stock adjustments and wastage logging.


* **Double-Entry Accounting**: A General Ledger system following standard accounting practices with a full Chart of Accounts.


* **HR Management**: Integrated tools to manage human resources within the enterprise.



---

## 🛠 Operational Workflows

### Daily Lifecycle

To ensure data integrity and cash reconciliation, the system operates within structured **Work Periods**:

1. **Starting the Day**: Initialize sessions by entering the Opening Balance in `Sale → Cash Register`.


2. **Ending the Day**: Settle all orders and enter the Closing Balance; the system auto-calculates variances in the Reconciliation Report.



### Financial Data Entry

* **Petty Cash**: Log expenses via `Accounting → Cash Ledger` by specifying the offset account (e.g., Utilities).


* **Supplier Bills**: Record invoices through `Purchase → Add Purchase` to simultaneously update inventory levels and Accounts Payable.



---

## 📊 Advanced Features

### AI Analysis & Reporting

Executives can query the system using natural language. The AI assistant provides instantaneous responses by querying the SQL schema directly for:

* Real-time Balance Sheets ($Assets = Liabilities + Equity$).


* Profit & Loss statements over custom date ranges.


* ROI calculations and expense tracking.



### Partner Equity Management

A transparent portal for owners to track:

* **Initial Capital**: Recorded at onboarding.


* **Drawings**: Personal withdrawals that automatically reduce equity.


* **Profit Distribution**: Moving Net Income based on ownership percentage.



---

## ✅ Best Practices

* **Shift Handovers**: End the Work Period before staff changes to verify independent cash positions.


* **Real-Time Entry**: Log expenses immediately to prevent end-of-day discrepancies.


* **Audit Trails**: All entries (Sales, POs, Journals) record the user for full accountability.
