kubectl replace \
    --force \
    --filename ../deployments/mongo/mongodb/local/mongodb.yaml \
    --namespace mongo
