-- Assign OrgAdmin role to al-hussein@papayatrading.com
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES (
  'aa70d64b-669a-404f-baa6-8d1d7f4f5723',
  (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'OrgAdmin')
)
ON CONFLICT DO NOTHING;
