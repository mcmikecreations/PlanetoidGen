DROP EXTENSION IF EXISTS "uuid-ossp" CASCADE;
DROP EXTENSION IF EXISTS postgis CASCADE;
DROP SCHEMA IF EXISTS dyn CASCADE;
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA IF NOT EXISTS public;
CREATE EXTENSION IF NOT EXISTS postgis SCHEMA public CASCADE;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp" SCHEMA public CASCADE;