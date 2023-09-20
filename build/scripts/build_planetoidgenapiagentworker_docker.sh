docker build \
    -f "..\..\libs\PlanetoidGen.Server\src\PlanetoidGen.API.AgentWorker\Dockerfile" \
    --force-rm \
    -t planetoidgenapiagentworker \
    --label "com.microsoft.created-by=visual-studio" \
    --label "com.microsoft.visual-studio.project-name=PlanetoidGen.API.AgentWorker" \
    "..\..\libs"
