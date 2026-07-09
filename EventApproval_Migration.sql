/* ============================================================
   Event Approval Workflow — database migration
   Run once against Azure SQL database: HandogMobileDB
   ============================================================ */

-- 1. Add the column that stores the admin's denial reason (nullable).
IF COL_LENGTH('dbo.EVENT', 'RejectionReason') IS NULL
BEGIN
    ALTER TABLE EVENT ADD RejectionReason NVARCHAR(500) NULL;
END
GO

-- 2. EventStatus must accept the new values 'Pending' and 'Rejected'
--    (in addition to the existing 'Published' and 'Completed').
--    'Published'/'Completed' are stored as free-form text, so normally
--    no CHECK constraint exists. Run this to confirm:
SELECT name, definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('dbo.EVENT');
--    If a constraint restricts EventStatus, DROP it and recreate it to
--    include 'Pending' and 'Rejected', e.g.:
--    ALTER TABLE EVENT DROP CONSTRAINT CK_EVENT_EventStatus;
--    ALTER TABLE EVENT ADD CONSTRAINT CK_EVENT_EventStatus
--        CHECK (EventStatus IN ('Pending','Published','Rejected','Completed'));
GO

-- 3. (Optional) Existing events created before this change are already
--    'Published' and will keep showing to volunteers — no backfill needed.
