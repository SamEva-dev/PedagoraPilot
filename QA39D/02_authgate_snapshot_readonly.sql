-- QA39-D: read-only snapshot. Execute in AuthGate PostgreSQL STAGING.
BEGIN TRANSACTION READ ONLY;
SELECT m."OrganizationId"::text AS organization_id,
       m."UserId"::text AS user_id,
       m."AppId" AS app_id,
       m."Status" AS authgate_status,
       m."InitialRole" AS authgate_role,
       m."GrantedAtUtc" AS granted_at
FROM "UserApplicationOrganizationMemberships" m
WHERE m."AppId" = 'pedagora-pilot-web'
ORDER BY 1, 2;
COMMIT;
