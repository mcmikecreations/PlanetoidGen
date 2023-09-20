dotnet dev-certs https -ep C:\\k8s\\cert\\aspnetapp.pfx -p cert_secret
dotnet dev-certs https --trust

docker build \
    -f "..\..\libs\PlanetoidGen.Server\src\PlanetoidGen.API\Dockerfile" \
    --force-rm \
    -t planetoidgenapi \
    --label "com.microsoft.created-by=visual-studio" \
    --label "com.microsoft.visual-studio.project-name=PlanetoidGen.API" \
    "..\..\libs"
