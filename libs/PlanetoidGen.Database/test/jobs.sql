DELETE FROM public.generation_jobs;
DELETE FROM public.tile_info;
DELETE FROM public.generation_servers;

INSERT INTO public.generation_servers(id, title) VALUES (0, 0::text);

SELECT * FROM public.tile_info_insert(0::smallint, 0::bigint, 0::bigint);

SELECT * FROM public.generation_jobs_insert('a', 0::smallint, 0::bigint, 0::bigint, 0::smallint, NULL);
SELECT * FROM public.generation_jobs_insert('b', 0::smallint, 0::bigint, 0::bigint, 1::smallint, NULL);
SELECT * FROM public.generation_jobs_insert('c', 0::smallint, 0::bigint, 0::bigint, 2::smallint, NULL);
SELECT * FROM public.generation_jobs_insert('d', 0::smallint, 0::bigint, 0::bigint, 0::smallint, 0::smallint);

SELECT * FROM public.generation_jobs_select(0::smallint); /* (d,z=0,x=0,y=0,is_running,agent_id=0,server_id=0) */
SELECT * FROM public.generation_jobs_delete('d'); /* (z=0,x=0,y=0,last_agent=0) */

/* a is deleted since it is not needed */
SELECT * FROM public.generation_jobs_select(0::smallint); /* (b,z=0,x=0,y=0,is_running,agent_id=1,server_id=NULL) */
SELECT * FROM public.generation_jobs_delete('b', TRUE); /* (z=0,x=0,y=0,last_agent=1) */

SELECT * FROM public.generation_jobs_select(0::smallint); /* (c,z=0,x=0,y=0,is_running,agent_id=2,server_id=NULL) */
SELECT * FROM public.generation_jobs_delete('c', TRUE); /* (z=0,x=0,y=0,last_agent=2) */

SELECT * FROM public.generation_jobs_select(0::smallint); /* NULL */