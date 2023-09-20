docker build \
    -f "../images/kafkakraft/Dockerfile" \
    --force-rm \
    -t "kafka-kraft:2.13-3.3.2" \
    "../images/kafkakraft"
