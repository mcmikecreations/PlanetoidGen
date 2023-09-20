CREATE DATABASE "PlanetoidGen.Database"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    CONNECTION LIMIT = -1;
	
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";