kubectl replace \
    --force \
    --filename ../deployments/planetoidgen/planetoidgenapi/local/planetoidgenapi.yaml \
    --namespace planetoidgen
