kubectl replace \
    --force \
    --filename ../deployments/messaging/kafkakraft/local/kafkakraft.yaml \
    --namespace messaging
