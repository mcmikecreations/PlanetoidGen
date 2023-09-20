docker build \
    -f "../images/planetoidgen/Dockerfile" \
    --force-rm \
    -t "planetoidgenbase:1.0.3" \
    "../images/planetoidgen"
