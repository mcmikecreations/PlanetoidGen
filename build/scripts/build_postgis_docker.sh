docker build \
    -f "../images/postgis/Dockerfile" \
    --force-rm \
    -t "postgis:14-3.3.2" \
    "../images/postgis"
