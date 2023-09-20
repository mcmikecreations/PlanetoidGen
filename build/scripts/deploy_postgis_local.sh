kubectl replace \
    --force \
    --filename ../deployments/postgres/postgis/local/postgis.yaml \
    --namespace postgres
