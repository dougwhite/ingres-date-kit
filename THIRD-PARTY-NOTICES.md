# Third-party source and dependencies

## Actian.EFCore source

Included under `vendor/Actian.EFCore` from the official upstream repository:

- URL: https://github.com/ActianCorp/Actian.EFCore
- Branch at retrieval: `net10.0`
- Exact commit: `b9d298a9f4bce47ca7b2be9c0af5c2f072285332`
- Included upstream LICENSE: `vendor/Actian.EFCore/LICENSE` (Apache License 2.0).
- All upstream copyright notices are retained.
- Many upstream source headers additionally identify MIT licensing. Those headers
  are preserved and the referenced MIT text is included in
  `vendor/Actian.EFCore/MIT-LICENSE.txt`. The original example's MIT licence does
  not replace any third-party licence or attribution.

The vendored source has local build changes, recorded in `docs/vendor-changes.patch`: Actian.Client references the locally licensed/tested 8.0.8 version instead of upstream 10.0.0; unused Microsoft.Data.SqlClient and SourceLink build dependencies are removed. Modified build files carry explicit modification notices. The additional vendor lock file records the resolved dependencies. No upstream query/runtime provider code was edited for this example. The custom date integration lives in `src/IngresDates.EntityFrameworkCore`.

The upstream csproj retains its original package repository metadata. This example is a locally modified build, not an official Actian release or vendor support commitment.

## Actian.Client

Proprietary, not included. Recipients must obtain and use it under their own applicable Actian license. Neither the repository nor the ZIP contains the nupkg, DLL, or decompiled client implementation. Validation used an already installed, licensed 8.0.8 package.

## Restored public packages

EF Core, ASP.NET runtime support, Npgsql, Microsoft SQL Server/SQLite providers, xUnit, test SDK, Sprache, and their dependencies retain their own NuGet license metadata. Restore exact direct versions from the project files; do not treat this notice as a replacement for their licenses. The portable project pins patched PKCS and SQLite native bundle dependencies to remove restore-time advisory warnings observed during construction.
