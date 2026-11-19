-- QA39-D: read-only invitation snapshot. Execute in Pedagora PostgreSQL STAGING.
BEGIN TRANSACTION READ ONLY;
SELECT i."OrganizationId"::text AS organization_id,
       i."Id"::text AS invitation_id,
       lower(i."Email") AS email,
       i."RequestedRole" AS requested_role,
       i."Status" AS invitation_status,
       i."AcceptedAuthGateUserId"::text AS accepted_user_id,
       i."FailureReason" AS failure_reason,
       i."CreatedAtUtc" AS invitation_created_at,
       i."AcceptedAtUtc" AS invitation_accepted_at
FROM organization.access_invitations i
ORDER BY i."CreatedAtUtc" DESC;
COMMIT;
