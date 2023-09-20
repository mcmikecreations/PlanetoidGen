./delete_namespaces.sh
rm -r /c/k8s/kafka/*
rm -r /c/k8s/mongo/*
rm -r /c/k8s/postgis/*
./deploy_namespaces.sh
./deploy_overpass_local.sh
./build_postgis_docker.sh
./build_kafkakraft_docker.sh
./deploy_postgis_local.sh
./deploy_mongodb_local.sh
./deploy_kafkakraft_local.sh
sleep 5
./build_planetoidgenbase_docker.sh
./build_planetoidgenapi_docker.sh
./build_planetoidgenapiagentworker_docker.sh
./deploy_planetoidgenapi_local.sh
./deploy_planetoidgenapiagentworker_local.sh
./scale_planetoidgenapiagentworker_local.sh 1
