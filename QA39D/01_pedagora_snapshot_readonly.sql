-- QA39-D: read-only snapshot. Execute in Pedagora PostgreSQL STAGING.
-- Export with \copy (SELECT ...) TO '/tmp/...' CSV HEADER from psql if desired.
BEGIN TRANSACTION READ ONLY;
SELECT m."OrganizationId"::text AS organization_id,
       m."AuthGateUserId"::text AS user_id,
       lower(m."Email") AS email,
       m."IsActive" AS local_active,
       m."CreatedAtUtc" AS local_created_at
FROM organization.organization_members m
ORDER BY 1, 2;
COMMIT;
