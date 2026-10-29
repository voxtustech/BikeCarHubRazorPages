-- 006: one wishlist row per account and model, and a faster lookup by account.
-- Review before running. Idempotent: safe to run more than once.
-- The site already avoids duplicates when it inserts; this makes the database enforce it too.

-- 1. Report duplicates first. The index below cannot be created while any exist.
SELECT UserID, TwoWheelerID, COUNT(*) AS Copies
FROM Wishlist
WHERE UserID IS NOT NULL
GROUP BY UserID, TwoWheelerID
HAVING COUNT(*) > 1;

-- 2. If step 1 returned rows, keep the oldest copy of each. Uncomment to run.
-- WITH ranked AS (
--     SELECT WishlistID, ROW_NUMBER() OVER (PARTITION BY UserID, TwoWheelerID ORDER BY CreatedAt, WishlistID) AS n
--     FROM Wishlist WHERE UserID IS NOT NULL
-- )
-- DELETE FROM Wishlist WHERE WishlistID IN (SELECT WishlistID FROM ranked WHERE n > 1);

-- 3. Unique index for signed-in rows.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Wishlist_UserID_TwoWheelerID' AND object_id = OBJECT_ID('Wishlist'))
    CREATE UNIQUE INDEX UX_Wishlist_UserID_TwoWheelerID ON Wishlist (UserID, TwoWheelerID) WHERE UserID IS NOT NULL;
