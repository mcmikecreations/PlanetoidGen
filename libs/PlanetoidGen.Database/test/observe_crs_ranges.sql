SELECT MIN(srid) AS mmin, MAX(srid) AS mmax FROM public.spatial_ref_sys GROUP BY srid/100 ORDER BY mmin;
