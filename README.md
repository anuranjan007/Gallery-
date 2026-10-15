# SecureFileManager

This repository contains a secure single-administrator file manager built with ASP.NET Core, EF Core, and AES-GCM encrypted file storage.

This initial scaffold includes:
- Project solution with API, Application, Domain, Infrastructure, and Tests projects
- Domain entities and DbContext
- Encryption service with DPAPI/fallback key protection
- Filesystem storage service with containment checks
- Basic Auth controller and Files controller (upload/download/preview)

Next steps: implement full audit logging, comprehensive tests, EF migrations, and additional folder/file management operations.
