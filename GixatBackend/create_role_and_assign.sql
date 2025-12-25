-- First, create the OrgAdmin role if it doesn't exist
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES (
  gen_random_uuid()::text,
  'OrgAdmin',
  'ORGADMIN',
  gen_random_uuid()::text
)
ON CONFLICT ("NormalizedName") DO NOTHING;

-- Also create OrgManager role for future use
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES (
  gen_random_uuid()::text,
  'OrgManager',
  'ORGMANAGER',
  gen_random_uuid()::text
)
ON CONFLICT ("NormalizedName") DO NOTHING;

-- Create OrgUser role
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES (
  gen_random_uuid()::text,
  'OrgUser',
  'ORGUSER',
  gen_random_uuid()::text
)
ON CONFLICT ("NormalizedName") DO NOTHING;

-- Now assign OrgAdmin role to your user
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES (
  'aa70d64b-669a-404f-baa6-8d1d7f4f5723',
  (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'OrgAdmin')
)
ON CONFLICT DO NOTHING;

-- Verify the assignment
SELECT u."Email", r."Name" as "Role"
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'al-hussein@papayatrading.com';
