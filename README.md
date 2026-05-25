# CloudFlash — Kitbox Order & Inventory Manager

CloudFlash is an internal desktop application built for a Kitbox reseller. Kitbox sells modular locker cabinets that customers configure themselves — choosing the number of compartments, dimensions, colors, and whether each locker has doors. Because every cabinet is assembled from individual parts (panels, crossbars, battens, handles, angle irons…), placing an order means figuring out exactly which parts are needed and in what quantities.

CloudFlash handles that entire process: a sales agent configures the cabinet interactively, the app resolves the full parts list automatically by querying the catalogue, the order is confirmed and stock is decremented, and if any part runs low the app triggers a supplier restocking order on its own. A separate stock management screen lets the team monitor inventory levels and manage supplier deliveries.

---

## Features Overview

### 1. Customer
Create or look up a customer before placing an order.  
Fill in an email address and/or phone number and click **Create Account**. The new customer is automatically selected for the current order session.

---

### 2. Configure Cabinet — Step 1

Set the global dimensions of the cabinet:
- **Width** and **Depth** (in cm)
- **Angle iron color**

Add up to **7 lockers** per cabinet. For each locker:
- **Height** (cm)
- **Color**
- **Has Doors** toggle — if enabled, pick a door color (or Glass)

Click **Calculate Parts** to resolve every part required from the database (vertical battens, crossbars, panels, doors, handles, angle iron). The result appears per locker and is added to the global cart.

---

### 3. Cart & Order Confirmation — Step 2

Displays all parts in the cart with quantities and unit prices.  
Shows the **total price** and, if any part is out of stock, a **50% deposit** is required.

Click **Confirm Order** to:
1. Decrement stock for every part used
2. Record the order in the database
3. Automatically generate supplier restocking orders for any part that falls below its minimum stock threshold

---

### 4. Order Tracking — Step 3

Enter an **Order ID** and click **Load** to pull up any existing order.

Displays:
- Customer info, order date, status
- Full list of parts ordered

If the order is still **Pending**, the **Mark as Invoiced** button is available to finalize billing and update the order status.

---

### 5. Supplier / Stock Management

Gives a complete view of the parts inventory and supplier pipeline.

**LOW STOCK** — parts currently below their minimum stock threshold, highlighted for action.

**ALL STOCK** — full scrollable catalogue with current quantities.

**Generate Supplier Orders** — creates one purchase order per supplier covering all low-stock parts, targeting double the minimum stock level.

**Receive All Orders** — marks all pending supplier orders as received and increments stock accordingly.

---

## Running the App

```
dotnet run --project CloudFlash
```

Or open the solution in Visual Studio / Rider and press **F5**.

Requires .NET 9.0 SDK and network access to `pat.infolab.ecam.be`.
