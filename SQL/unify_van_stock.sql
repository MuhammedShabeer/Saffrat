USE [db43829-12-04-2026-aftercleanup];
GO

-- 1. Merge existing FoodItemStocks into 'VanStock'
WITH AggregatedStock AS (
    SELECT FoodItemId, SUM(Quantity) as TotalQuantity, MAX(UpdatedAt) as MaxUpdatedAt
    FROM FoodItemStocks
    WHERE UserId != 'VanStock'
    GROUP BY FoodItemId
)
-- Update existing 'VanStock' records by adding the aggregated quantity
UPDATE s
SET s.Quantity = s.Quantity + a.TotalQuantity,
    s.UpdatedAt = CASE WHEN a.MaxUpdatedAt > s.UpdatedAt THEN a.MaxUpdatedAt ELSE s.UpdatedAt END
FROM FoodItemStocks s
INNER JOIN AggregatedStock a ON s.FoodItemId = a.FoodItemId
WHERE s.UserId = 'VanStock';
GO

-- 2. Insert new 'VanStock' rows for items that didn't have one
INSERT INTO FoodItemStocks (UserId, FoodItemId, Quantity, UpdatedAt)
SELECT 'VanStock', FoodItemId, SUM(Quantity), MAX(UpdatedAt)
FROM FoodItemStocks
WHERE UserId != 'VanStock'
AND FoodItemId NOT IN (SELECT FoodItemId FROM FoodItemStocks WHERE UserId = 'VanStock')
GROUP BY FoodItemId;
GO

-- 3. Delete the old driver-specific records
DELETE FROM FoodItemStocks WHERE UserId != 'VanStock';
GO

-- 4. Update all transactions to 'VanStock' (optional but keeps history clean)
UPDATE InventoryTransactions SET UserId = 'VanStock' WHERE UserId != 'VanStock';
GO
