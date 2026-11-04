CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" nvarchar(150) NOT NULL WITH DEFAULT '',
    "ProductVersion" nvarchar(32) NOT NULL WITH DEFAULT '',
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE "ingres"."date_samples" (
    "id" integer NOT NULL WITH DEFAULT 0,
    "label" varchar(80) NOT NULL WITH DEFAULT '',
    "native_value" ingresdate NOT NULL,
    "nullable_value" ingresdate WITH NULL,
    "lead_time" ingresdate NOT NULL,
    "required_date" ingresdate NOT NULL WITH DEFAULT '',
    CONSTRAINT "PK_date_samples" PRIMARY KEY ("id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES (N'20261104071259_InitialExample', N'10.0.3');

COMMIT;
