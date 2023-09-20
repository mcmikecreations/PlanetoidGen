kubectl replace \
    --force \
    --filename ../deployments/planetoidgen/planetoidgenapiagentworker/local/planetoidgenapiagentworker.yaml \
    --namespace planetoidgen
