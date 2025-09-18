-- Fix role case in database to ensure consistency
-- Update all roles to uppercase

UPDATE [dbo].[Users] 
SET [Role] = UPPER([Role])
WHERE [Role] IS NOT NULL;

-- Verify the update
SELECT [UserID], [Email], [Role] 
FROM [dbo].[Users] 
ORDER BY [UserID];
