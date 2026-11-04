-- Synthetic demonstration data only. Run in a new, empty database.
set date_format 'sweden';
\p\g
create table date_samples (
 id integer not null primary key,
 label varchar(80) not null,
 native_value ingresdate not null,
 nullable_value ingresdate with null,
 lead_time ingresdate not null,
 required_date ingresdate with default '' not null
);
\p\g
insert into date_samples values (1,'Empty and SQL null',ingresdate(''),null,ingresdate('5 days'),ingresdate(''));
\p\g
insert into date_samples values (2,'Calendar date',ingresdate('2026_01_31'),ingresdate(''),ingresdate('1 month'),ingresdate('2026_01_31'));
\p\g
insert into date_samples values (3,'Timestamp',ingresdate('2026_01_31 12:34:56'),ingresdate('2026_01_31 12:34:56'),ingresdate('1 year'),ingresdate('2026_01_31 12:34:56'));
\p\g
insert into date_samples values (4,'Five day interval',ingresdate('5 days'),null,ingresdate('-5 days'),ingresdate(''));
\p\g
insert into date_samples values (5,'Calendar month interval',ingresdate('1 month'),null,ingresdate('2 months'),ingresdate(''));
\p\g
insert into date_samples values (6,'Calendar year interval',ingresdate('1 year'),null,ingresdate('365 days'),ingresdate(''));
\p\g
insert into date_samples values (7,'Mixed negative interval',ingresdate('1 year 2 months -3 days 4 hours'),null,ingresdate('5 days 2 hours'),ingresdate(''));
\p\g
insert into date_samples values (8,'Midnight timestamp',ingresdate('2026_01_31 00:00:00'),null,ingresdate('0 days'),ingresdate('2026_01_31 00:00:00'));
\p\g
commit;
\p\g
select count(*) as rows_created from date_samples;
\p\g
create table multi_database_samples (id integer not null primary key, value ingresdate not null, nullable_value ingresdate with null);
\p\g
insert into multi_database_samples values (1,ingresdate(''),null);
\p\g
insert into multi_database_samples values (2,ingresdate('2026_01_31'),ingresdate(''));
\p\g
insert into multi_database_samples values (3,ingresdate('5 days'),ingresdate('1 month'));
\p\g
commit;
\p\g
