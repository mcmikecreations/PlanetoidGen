SELECT format('DROP FUNCTION %I.%I(%s);', nspname, proname, oidvectortypes(proargtypes))
FROM pg_proc INNER JOIN pg_namespace ns ON (pg_proc.pronamespace = ns.oid)
WHERE ns.nspname = 'public'  order by proname;