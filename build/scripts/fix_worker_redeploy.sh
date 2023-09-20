./scale_planetoidgenapiagentworker_local.sh 0
kubectl delete namespace messaging
rm -r /c/k8s/kafka/*
./deploy_namespaces.sh
./deploy_kafkakraft_local.sh
sleep 5
./build_planetoidgenapiagentworker_docker.sh
./deploy_planetoidgenapiagentworker_local.sh
# ./scale_planetoidgenapiagentworker_local.sh 2
